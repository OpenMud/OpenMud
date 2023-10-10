using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenMud.Mudpiler.Compiler.Project.Cli
{
    public static class StringUtil
    {
        public static string AsLogical(string name)
        {
            var newName = new StringBuilder();
            bool isCompStart = true;
            foreach (var c in name)
            {
                switch (c)
                {
                    case '0':
                    case '1':
                    case '2':
                    case '3':
                    case '4':
                    case '5':
                    case '6':
                    case '7':
                    case '8':
                    case '9':
                        if(isCompStart)
                            break;
                        newName.Append(c);
                        break;
                    case 'A':
                    case 'B':
                    case 'C':
                    case 'D':
                    case 'E':
                    case 'F':
                    case 'G':
                    case 'H':
                    case 'I':
                    case 'J':
                    case 'K':
                    case 'L':
                    case 'M':
                    case 'N':
                    case 'O':
                    case 'Q':
                    case 'P':
                    case 'R':
                    case 'S':
                    case 'T':
                    case 'U':
                    case 'V':
                    case 'W':
                    case 'X':
                    case 'Y':
                    case 'Z':
                    case 'a':
                    case 'b':
                    case 'c':
                    case 'd':
                    case 'e':
                    case 'f':
                    case 'g':
                    case 'h':
                    case 'i':
                    case 'j':
                    case 'k':
                    case 'l':
                    case 'm':
                    case 'n':
                    case 'o':
                    case 'q':
                    case 'p':
                    case 'r':
                    case 's':
                    case 't':
                    case 'u':
                    case 'v':
                    case 'w':
                    case 'x':
                    case 'y':
                    case 'z':
                        newName.Append(isCompStart ? Char.ToUpper(c) : c);
                        isCompStart = false;
                        break;
                    case '/':
                    case '_':
                    case '-':
                    case '.':
                        isCompStart = true;
                        newName.Append(".");
                        break;
                    case '+':
                    case ',':
                    case '*':
                    case ':':
                    case '=':
                    case ' ':
                    case '^':
                    case '$':
                        break;
                }
            }

            if (newName.Length == 0)
                throw new Exception("Invalid name.");

            return newName.ToString();
        }

        internal static string? AsJsLogical(string projectName)
        {
            return AsLogical(projectName).ToLower().Replace(".", "-");
        }
    }
}
