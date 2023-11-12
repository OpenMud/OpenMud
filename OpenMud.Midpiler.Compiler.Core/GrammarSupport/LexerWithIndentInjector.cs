using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using OpenMud.Mudpiler.Compiler.DmlGrammar;

namespace OpenMud.Mudpiler.Compiler.Core.GrammarSupport;

public class LexerWithIndentInjector : DmlLexer
{
    // Patterns for the custom error listener to recognize error messages
    public static readonly string TEXT_LEXER = "lexer --> ";

    public static readonly string TEXT_INSERTED_INDENT = "inserted INDENT";

    private readonly ICharStream _input;

    private bool emittedEof;

    // A String list that stores the lexer error messages
    private readonly List<string> errors = new();

    // ************************************************************************************************
    // **** THE FOLLOWING SECTION ALSO CAN BE USED IN THE @lexer::members{} SECTION OF THE GRAMMAR ****
    // ************************************************************************************************
    // The stack that keeps track of the indentation lengths
    private LinkedList<int> indentLengths = new();

    // An int that stores the last pending token type (including the inserted INDENT/DEDENT/NEWLINE token types also)
    private int lastPendingTokenType;

    // The amount of opened braces, brackets and parenthesis
    private int opened;

    // A linked list where extra tokens are pushed on
    private readonly LinkedList<IToken> pendingTokens = new();

    // A String list that stores the lexer warnings
    private readonly List<string> warnings = new();

    // Was there space char in the indentations?
    private bool wasSpaceIndentation;

    // Was there tab char in the indentations?
    private bool wasTabIndentation;

    public LexerWithIndentInjector(ICharStream input) : base(input)
    {
        _input = input;
    }

    public override IToken NextToken()
    {
        emittedEof = false;
        if (_input.Size == 0)
        {
            //return new CommonToken(DmlLexer.NEWLINE, "NEWLINE");
            return
                new CommonToken(TokenConstants.Eof,
                    "<EOF>"); // processing of the input stream until the first returning EOF
        }

        CheckNextToken();
        var v = pendingTokens.First.Value; // append the token stream with the upcoming pending token
        pendingTokens.RemoveFirst();
        return v;
    }

    public override IToken EmitEOF()
    {
        //if(emittedEof)
        return base.EmitEOF();

        //emittedEof = true;
        //return new CommonToken(DmlLexer.NEWLINE, "NEWLINE");//return new CommonToken(TokenConstants.EOF, "<EOF>"); // processing of the input stream until the first returning EOF
    }


    private void CheckNextToken()
    {
        if (indentLengths != null)
        {
            // after the first incoming EOF token the indentLengths stack will be set to null
            var startSize = pendingTokens.Count;
            IToken curToken;
            do
            {
                curToken = base.NextToken(); // get the next token from the input stream
                CheckStartOfInput(curToken);
                switch (curToken.Type)
                {
                    case OPEN_PARENS:
                    case OPEN_BRACKET:
                    case OPEN_BRACE:
                        opened++;
                        pendingTokens.AddLast(curToken);
                        break;
                    case CLOSE_PARENS:
                    case CLOSE_BRACKET:
                    case CLOSE_BRACE:
                        opened--;
                        pendingTokens.AddLast(curToken);
                        break;
                    case NEWLINE:
                        HandleNewLineToken(curToken);
                        break;
                    case TokenConstants.Eof:
                        HandleEofToken(curToken); // indentLengths stack will be set to null
                        break;
                    default:
                        pendingTokens.AddLast(curToken); // insert the current token
                        break;
                }
            } while (pendingTokens.Count == startSize);

            lastPendingTokenType = curToken.Type;
        }
    }

    private void CheckStartOfInput(IToken curToken)
    {
        if (indentLengths.Count == 0)
        {
            // We're at the first token
            indentLengths
                .AddFirst(0); //indentLengths.push(0);  // initialize the stack with default 0 indentation length

            if (_input.GetText(new Interval(0, 0)).Trim().Length == 0) // the first char of the input is a whitespace
                InsertLeadingTokens(curToken.Type, curToken.StartIndex);
        }
    }

    private void HandleNewLineToken(IToken curToken)
    {
        if (opened == 0)
        {
            //*** https://docs.python.org/3/reference/lexical_analysis.html#implicit-line-joining
            if (_input.La(1) == '/' && _input.La(2) == '/')
                return; //Comment

            if (_input.La(1) == '/' && _input.La(2) == '*')
                return; //Comment

            switch (_input.La(1) /* next symbol */)
            {
                //*** https://www.antlr.org/api/Java/org/antlr/v4/framework/IntStream.html#LA(int)
                case '\r':
                case '\n':
                case '\f':
                case '#':
                case TokenConstants.Eof
                    : // skip the trailing inconsistent dedent or the trailing unexpected indent (or the trailing indent)
                    return; // We're on a blank line or before a comment or before the EOF, skip the NEWLINE token
                default:
                    var curIndentLength = GetIndentationLength(curToken.Text);
                    
                    pendingTokens.AddLast(curToken); // insert the current NEWLINE token
                    InsertIndentDedentTokens(curIndentLength); //*** https://docs.python.org/3/reference/lexical_analysis.html#indentation
                    break;
            }
        }
    }

    private void HandleEofToken(IToken curToken)
    {
        InsertTrailingTokens(lastPendingTokenType); // indentLengths stack will be null!
        pendingTokens.AddLast(curToken); // insert the current EOF token
    }

    private void InsertLeadingTokens(int type, int startIndex)
    {
        if (type != NEWLINE && type != TokenConstants.Eof)
        {
            // (after a whitespace) The first token is visible, so We insert a NEWLINE and an INDENT token before it to raise an 'unexpected indent' error later by the parser
            InsertToken(0, startIndex - 1, "NEWLINE" /*"<inserted leading NEWLINE>" + new string(' ', startIndex)*/,
                NEWLINE, 1, 0);
            InsertToken(startIndex, startIndex - 1,
                "<" + TEXT_INSERTED_INDENT + ", " + GetIndentationDescription(startIndex) + ">", DmlParser.INDENT, 1,
                startIndex);
            indentLengths.AddFirst(startIndex);
        }
    }

    private void InsertIndentDedentTokens(int curIndentLength)
    {
        var prevIndentLength = indentLengths.First?.Value;
        if (curIndentLength > prevIndentLength)
        {
            // insert an INDENT token
            InsertToken("<" + TEXT_INSERTED_INDENT + ", " + GetIndentationDescription(curIndentLength) + ">",
                DmlParser.INDENT);
            indentLengths.AddFirst(curIndentLength);
        }
        else
        {
            while (curIndentLength < prevIndentLength)
            {
                // More than 1 DEDENT token may be inserted
                indentLengths.RemoveFirst(); //pop
                prevIndentLength = indentLengths.First.Value;
                if (curIndentLength <= prevIndentLength)
                {
                    InsertToken(
                        "DEDENT" /*"<inserted DEDENT, " + this.GetIndentationDescription(prevIndentLength) + ">"*/,
                        DmlParser.DEDENT);
                }
                else
                {
                    InsertToken("DEDENT" /*"<inserted inconsistent DEDENT, " + "length=" + curIndentLength + ">"*/,
                        DmlParser.DEDENT);
                    errors.Add(TEXT_LEXER + "line " + base.Line + ":" + Column +
                               "\t IndentationError: unindent does not match any outer indentation level");
                }
            }
        }
    }

    private void InsertTrailingTokens(int type)
    {
        if (type != NEWLINE &&
            type != DmlParser.DEDENT) // If the last pending token was not a NEWLINE and not a DEDENT then
            InsertToken("NEWLINE" /*"<inserted trailing NEWLINE>"*/,
                NEWLINE); // insert an extra trailing NEWLINE token that serves as the end of the statement

        while (indentLengths.Count > 1)
        {
            // Now insert as much trailing DEDENT tokens as needed
            var l = indentLengths.First.Value;
            indentLengths.RemoveFirst();
            InsertToken("DEDENT" /*"<inserted trailing DEDENT, " + this.GetIndentationDescription(l) + ">"*/,
                DmlParser.DEDENT);
        }

        indentLengths = null; // there will be no more token read from the input stream
    }


    private string GetIndentationDescription(int lengthOfIndent)
    {
        return "length=" + lengthOfIndent + ", level=" + indentLengths.Count;
    }

    private void InsertToken(string text, int type)
    {
        int startIndex =
            base._tokenStartCharIndex +
            base.Text.Length; //*** https://www.antlr.org/api/Java/org/antlr/v4/framework/Lexer.html#_tokenStartCharIndex
        InsertToken(startIndex, startIndex - 1, text, type, base.Line,
            Column); // Last arg was: GetCharPositionInLine());
    }

    private void InsertToken(int startIndex, int stopIndex, string text, int type, int line, int charPositionInLine)
    {
        //CommonToken token = new CommonToken(_tokenFactorySourcePair, type, base.Channel, startIndex, stopIndex); //*** https://www.antlr.org/api/Java/org/antlr/v4/framework/CommonToken.html
        var token = base.TokenFactory.Create(type, text) as CommonToken;
        token.Text = text;
        token.Line = line;
        token.Column = charPositionInLine; // CharPositionInLine
        pendingTokens.AddLast(token);
    }

    private int GetIndentationLength(string textOfMatchedNEWLINE)
    {
        var count = 0;
        foreach (var ch in textOfMatchedNEWLINE)
            switch (ch)
            {
                case ' ': // A normal space char
                    wasSpaceIndentation = true;
                    count++;
                    break;
                case '\t':
                    wasTabIndentation = true;
                    count += 8 - count % 8;
                    break;
            }

        return count;
    }

    public List<string> GetWarnings()
    {
        return warnings;
    }

    public List<string> GetErrorMessages()
    {
        return errors;
    }
}