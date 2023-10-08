using System;
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


        private static Blackout CreateCommentBlackouts(string source, bool blackoutResources)
        {
            //Sometimes just writing a parser is simpler than trying to get Antlr to do what you want it to.
            List<Tuple<int, int>> blackouts = new();

            var multilineCommentDepth = 0;
            var isInString = false;
            var isInResource = false;
            var isInSingleLineComment = false;

            var start = 0;

            void EndOfBlackout(int idx)
            {
                blackouts.Add(Tuple.Create(start, idx));
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
                        if (blackoutResources)
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

                        if (blackoutResources)
                            start = startBuffer;
                    }
                }
            }

            return new Blackout((matchIdx, matchLen) =>
            {
                return !Enumerable.Range(matchIdx, matchLen).Any(
                    idx => blackouts.Any(blk => idx >= blk.Item1 && idx <= blk.Item2)
                );
            });
        }

        public static Blackout CreateBlackouts(string source, bool blackoutResources = true)
        {
            return CreateCommentBlackouts(source, blackoutResources);
        }
    }
}
