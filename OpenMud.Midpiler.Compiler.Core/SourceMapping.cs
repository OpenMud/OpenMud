using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Antlr4.Runtime;
using OpenMud.Mudpiler.Compiler.DmlPreprocessor.Visitors;

namespace OpenMud.Mudpiler.Compiler.Core
{
    internal class SourceMapping
    {
        private Dictionary<int, SourceFileLineAddress> mapping = new();

        public SourceMapping(IEnumerable<IToken> sourceDirectives)
        {
            foreach (var token in sourceDirectives)
            {
                var d = token.Text.Trim();

                var components = d.Split(new char[] { ' ', '\t'}, StringSplitOptions.RemoveEmptyEntries);

                bool isInvalid = components.Length != 3 ||
                               components[0].ToLower() != "#line" ||
                               !int.TryParse(components[1], out var ln) ||
                               ln < 1 ||
                               !Regex.Match(components[2], "\"[^\"]+\"").Success;

                if (isInvalid)
                    throw new Exception("Invalid line directive.");

                var line = int.Parse(components[1]);
                var fileline = components[2];

                mapping[token.Line] = new SourceFileLineAddress(line, fileline.Trim('"'));
            }
        }

        public SourceFileLineAddress? Lookup(int line)
        {
            var r = mapping.Where(
                    x => x.Key <= line
                )
                .OrderByDescending(k => k.Key)
                .Take(1)
                .ToList();

            if (!r.Any())
                return null;

            return r.First().Value;
        }
    }
}
