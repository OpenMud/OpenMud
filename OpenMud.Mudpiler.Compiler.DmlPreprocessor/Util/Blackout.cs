using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OpenMud.Mudpiler.Compiler.DmlPreprocessor.Util
{
    delegate bool AllowMatch(int index, int length);
    public class Blackout
    {
        private readonly AllowMatch allowMatch;
        private Blackout(AllowMatch allowMatch)
        {
            this.allowMatch = allowMatch;
        }

        public bool Allow(Match m)
        {
            return allowMatch(m.Index, m.Length);
        }

        public bool Allow(int idx)
        {
            return Allow(idx, 1);
        }

        public bool Allow(int index, int length)
        {
            return allowMatch(index, length);
        }

        private static (List<Tuple<int, int>> resourceBlackout, List<Tuple<int, int>> commentBlackout) CreateCommentBlackoutRegions(string source)
        {
            List<Tuple<int, int>> commentBlackout = new();
            List<Tuple<int, int>> resourceBlackout = new();


            var multilineCommentDepth = 0;
            var isInString = false;
            var isInResource = false;
            var isInSingleLineComment = false;

            var start = 0;
            var isResourceBlackout = false;

            void EndOfBlackout(int idx)
            {
                if(isResourceBlackout)
                    resourceBlackout.Add(Tuple.Create(start, idx));
                else
                    commentBlackout.Add(Tuple.Create(start, idx));

                isResourceBlackout = false;
                start = 0;
            }

            for (var i = 0; i < source.Length; i++)
            {
                var remaining = source.Length - i - 1;

                bool acceptAndSkip(string v)
                {
                    if (remaining < v.Length)
                        return false;

                    for (var w = 0; w < v.Length; w++)
                        if (source[w + i] != v[w])
                            return false;

                    i += v.Length - 1;
                    return true;
                }

                if (multilineCommentDepth > 0)
                {
                    if (acceptAndSkip("*/"))
                    {
                        multilineCommentDepth--;

                        if (multilineCommentDepth == 0)
                            EndOfBlackout(i);
                        continue;
                    }

                    if (acceptAndSkip("/*")) multilineCommentDepth++;
                }
                else if (isInString)
                {
                    if (acceptAndSkip("\\\""))
                        continue;

                    if (acceptAndSkip("\""))
                    {
                        isInString = false;
                        EndOfBlackout(i);
                    }
                }
                else if (isInResource)
                {
                    if (acceptAndSkip("\\\'"))
                        continue;

                    if (acceptAndSkip("'"))
                    {
                        EndOfBlackout(i);
                        isInResource = false;
                    }
                }
                else if (isInSingleLineComment)
                {
                    if (acceptAndSkip("\r\n") || acceptAndSkip("\n") || acceptAndSkip("\r"))
                    {
                        isInSingleLineComment = false;
                        EndOfBlackout(i);
                    }
                }
                else
                {
                    var startBuffer = i;
                    if (acceptAndSkip("//"))
                    {
                        isInSingleLineComment = true;
                        start = startBuffer;
                    }
                    else if (acceptAndSkip("/*"))
                    {
                        multilineCommentDepth++;
                        start = startBuffer;
                    }
                    else if (acceptAndSkip("\""))
                    {
                        isInString = true;
                        start = startBuffer;
                    }
                    else if (acceptAndSkip("'"))
                    {
                        isInResource = true;
                        isResourceBlackout = true;
                        start = startBuffer;
                    }
                }
            }

            return (resourceBlackout, commentBlackout);
        }

        public static (List<bool> CommentAndStrings, List<bool> Resources) CreateBitmaps(string source)
        {
            //There aren't any decent C# bitset libraries so we are just going to use a list. It will probably perform fine...

            var commentAndStrings = Enumerable.Range(0, source.Length).Select(_ => false).ToList();
            var resources = Enumerable.Range(0, source.Length).Select(_ => false).ToList();

            var (rsrc, cmt) = CreateCommentBlackoutRegions(source);

            foreach (var r in cmt.SelectMany(r => Enumerable.Range(r.Item1, r.Item2 - r.Item1 + 1)))
            {
                commentAndStrings[r] = true;
            }

            foreach (var r in rsrc.SelectMany(r => Enumerable.Range(r.Item1, r.Item2 - r.Item1 + 1)))
            {
                resources[r] = true;
            }

            return (commentAndStrings, resources);
        }
    }
}
