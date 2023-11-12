// Generated from c:/Users/jerem/source/repos/OpenMud/OpenMud.Mudpiler.Compiler.DmeGrammar/DmeParser.g4 by ANTLR 4.13.1
import org.antlr.v4.runtime.atn.*;
import org.antlr.v4.runtime.dfa.DFA;
import org.antlr.v4.runtime.*;
import org.antlr.v4.runtime.misc.*;
import org.antlr.v4.runtime.tree.*;
import java.util.List;
import java.util.Iterator;
import java.util.ArrayList;

@SuppressWarnings({"all", "warnings", "unchecked", "unused", "cast", "CheckReturnValue"})
public class DmeParser extends Parser {
	static { RuntimeMetaData.checkVersion("4.13.1", RuntimeMetaData.VERSION); }

	protected static final DFA[] _decisionToDFA;
	protected static final PredictionContextCache _sharedContextCache =
		new PredictionContextCache();
	public static final int
		SHARP=1, STRING_BEGIN=2, MULTILINE_STRING_BEGIN=3, RESOURCE_BEGIN=4, LINE_COMMENT=5, 
		BEGIN_MULTILINE_COMMENT=6, START_CODE_EXPR=7, END_CODE_EXPR=8, CODE=9, 
		END_MULTILINE_COMMENT=10, BEGIN_NESTED_MULTILINE_COMMENT=11, COMMENT=12, 
		MULTILINE_STRING_END=13, BEGIN_ML_STRING_EXPRESSION=14, STRING_NEXT_LINE=15, 
		STRING_END=16, BEGIN_STRING_EXPRESSION=17, STRING_CONTENTS=18, RESOURCE_END=19, 
		RESOURCE_CONTENTS=20, IMPORT=21, INCLUDE=22, PRAGMA=23, DEFINE=24, DEFINED=25, 
		IF=26, ELIF=27, ELSE=28, UNDEF=29, IFDEF=30, IFNDEF=31, ENDIF=32, TRUE=33, 
		FALSE=34, ERROR=35, BANG=36, LPAREN=37, RPAREN=38, EQUAL=39, NOTEQUAL=40, 
		AND=41, OR=42, LT=43, GT=44, LE=45, GE=46, DIRECTIVE_WHITESPACES=47, DIRECTIVE_STRING=48, 
		CONDITIONAL_SYMBOL=49, DECIMAL_LITERAL=50, FLOAT=51, NEW_LINE=52, DIRECITVE_COMMENT=53, 
		DIRECITVE_LINE_COMMENT=54, DIRECITVE_NEW_LINE=55, DIRECITVE_TEXT_NEW_LINE=56, 
		TEXT=57, CURLY_BRACE_OPEN=58, CURLY_BRACE_CLOSE=59, CODE_DIV=60, COMMENT_STAR=61, 
		MULTILINE_STRING_EXPRESSION_AESCAPE_ESCAPE1=62, MULTILINE_STRING_EXPRESSION_AESCAPE_ESCAPE2=63, 
		MULTILINE_STRING_EXPRESSION_AESCAPE_ESCAPE3=64;
	public static final int
		RULE_dmlDocument = 0, RULE_text = 1, RULE_comment_block = 2, RULE_comment_contents = 3, 
		RULE_string = 4, RULE_resource = 5, RULE_string_contents = 6, RULE_string_expression = 7, 
		RULE_string_expression_begin = 8, RULE_code_expr = 9, RULE_code_block = 10, 
		RULE_code = 11, RULE_code_literal = 12, RULE_directive = 13, RULE_directive_text = 14, 
		RULE_preprocessor_expression = 15;
	private static String[] makeRuleNames() {
		return new String[] {
			"dmlDocument", "text", "comment_block", "comment_contents", "string", 
			"resource", "string_contents", "string_expression", "string_expression_begin", 
			"code_expr", "code_block", "code", "code_literal", "directive", "directive_text", 
			"preprocessor_expression"
		};
	}
	public static final String[] ruleNames = makeRuleNames();

	private static String[] makeLiteralNames() {
		return new String[] {
			null, "'#'", null, "'{\"'", null, null, null, null, "']'", null, "'*/'", 
			null, null, "'\"}'", null, null, null, null, null, null, null, null, 
			null, "'pragma'", null, "'defined'", "'if'", "'elif'", "'else'", "'undef'", 
			"'ifdef'", "'ifndef'", "'endif'", null, null, "'error'", "'!'", "'('", 
			"')'", "'=='", "'!='", "'&&'", "'||'", "'<'", "'>'", "'<='", "'>='", 
			null, null, null, null, null, null, null, null, null, null, null, "'{'", 
			null, null, "'*'"
		};
	}
	private static final String[] _LITERAL_NAMES = makeLiteralNames();
	private static String[] makeSymbolicNames() {
		return new String[] {
			null, "SHARP", "STRING_BEGIN", "MULTILINE_STRING_BEGIN", "RESOURCE_BEGIN", 
			"LINE_COMMENT", "BEGIN_MULTILINE_COMMENT", "START_CODE_EXPR", "END_CODE_EXPR", 
			"CODE", "END_MULTILINE_COMMENT", "BEGIN_NESTED_MULTILINE_COMMENT", "COMMENT", 
			"MULTILINE_STRING_END", "BEGIN_ML_STRING_EXPRESSION", "STRING_NEXT_LINE", 
			"STRING_END", "BEGIN_STRING_EXPRESSION", "STRING_CONTENTS", "RESOURCE_END", 
			"RESOURCE_CONTENTS", "IMPORT", "INCLUDE", "PRAGMA", "DEFINE", "DEFINED", 
			"IF", "ELIF", "ELSE", "UNDEF", "IFDEF", "IFNDEF", "ENDIF", "TRUE", "FALSE", 
			"ERROR", "BANG", "LPAREN", "RPAREN", "EQUAL", "NOTEQUAL", "AND", "OR", 
			"LT", "GT", "LE", "GE", "DIRECTIVE_WHITESPACES", "DIRECTIVE_STRING", 
			"CONDITIONAL_SYMBOL", "DECIMAL_LITERAL", "FLOAT", "NEW_LINE", "DIRECITVE_COMMENT", 
			"DIRECITVE_LINE_COMMENT", "DIRECITVE_NEW_LINE", "DIRECITVE_TEXT_NEW_LINE", 
			"TEXT", "CURLY_BRACE_OPEN", "CURLY_BRACE_CLOSE", "CODE_DIV", "COMMENT_STAR", 
			"MULTILINE_STRING_EXPRESSION_AESCAPE_ESCAPE1", "MULTILINE_STRING_EXPRESSION_AESCAPE_ESCAPE2", 
			"MULTILINE_STRING_EXPRESSION_AESCAPE_ESCAPE3"
		};
	}
	private static final String[] _SYMBOLIC_NAMES = makeSymbolicNames();
	public static final Vocabulary VOCABULARY = new VocabularyImpl(_LITERAL_NAMES, _SYMBOLIC_NAMES);

	/**
	 * @deprecated Use {@link #VOCABULARY} instead.
	 */
	@Deprecated
	public static final String[] tokenNames;
	static {
		tokenNames = new String[_SYMBOLIC_NAMES.length];
		for (int i = 0; i < tokenNames.length; i++) {
			tokenNames[i] = VOCABULARY.getLiteralName(i);
			if (tokenNames[i] == null) {
				tokenNames[i] = VOCABULARY.getSymbolicName(i);
			}

			if (tokenNames[i] == null) {
				tokenNames[i] = "<INVALID>";
			}
		}
	}

	@Override
	@Deprecated
	public String[] getTokenNames() {
		return tokenNames;
	}

	@Override

	public Vocabulary getVocabulary() {
		return VOCABULARY;
	}

	@Override
	public String getGrammarFileName() { return "DmeParser.g4"; }

	@Override
	public String[] getRuleNames() { return ruleNames; }

	@Override
	public String getSerializedATN() { return _serializedATN; }

	@Override
	public ATN getATN() { return _ATN; }

	public DmeParser(TokenStream input) {
		super(input);
		_interp = new ParserATNSimulator(this,_ATN,_decisionToDFA,_sharedContextCache);
	}

	@SuppressWarnings("CheckReturnValue")
	public static class DmlDocumentContext extends ParserRuleContext {
		public List<TextContext> text() {
			return getRuleContexts(TextContext.class);
		}
		public TextContext text(int i) {
			return getRuleContext(TextContext.class,i);
		}
		public DmlDocumentContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_dmlDocument; }
	}

	public final DmlDocumentContext dmlDocument() throws RecognitionException {
		DmlDocumentContext _localctx = new DmlDocumentContext(_ctx, getState());
		enterRule(_localctx, 0, RULE_dmlDocument);
		int _la;
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(35);
			_errHandler.sync(this);
			_la = _input.LA(1);
			while ((((_la) & ~0x3f) == 0 && ((1L << _la) & 1022L) != 0)) {
				{
				{
				setState(32);
				text();
				}
				}
				setState(37);
				_errHandler.sync(this);
				_la = _input.LA(1);
			}
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	@SuppressWarnings("CheckReturnValue")
	public static class TextContext extends ParserRuleContext {
		public TerminalNode SHARP() { return getToken(DmeParser.SHARP, 0); }
		public DirectiveContext directive() {
			return getRuleContext(DirectiveContext.class,0);
		}
		public List<TerminalNode> NEW_LINE() { return getTokens(DmeParser.NEW_LINE); }
		public TerminalNode NEW_LINE(int i) {
			return getToken(DmeParser.NEW_LINE, i);
		}
		public Code_blockContext code_block() {
			return getRuleContext(Code_blockContext.class,0);
		}
		public Comment_blockContext comment_block() {
			return getRuleContext(Comment_blockContext.class,0);
		}
		public TextContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_text; }
	}

	public final TextContext text() throws RecognitionException {
		TextContext _localctx = new TextContext(_ctx, getState());
		enterRule(_localctx, 2, RULE_text);
		int _la;
		try {
			setState(48);
			_errHandler.sync(this);
			switch (_input.LA(1)) {
			case SHARP:
				enterOuterAlt(_localctx, 1);
				{
				setState(38);
				match(SHARP);
				setState(39);
				directive();
				setState(43);
				_errHandler.sync(this);
				_la = _input.LA(1);
				while (_la==NEW_LINE) {
					{
					{
					setState(40);
					match(NEW_LINE);
					}
					}
					setState(45);
					_errHandler.sync(this);
					_la = _input.LA(1);
				}
				}
				break;
			case STRING_BEGIN:
			case MULTILINE_STRING_BEGIN:
			case RESOURCE_BEGIN:
			case START_CODE_EXPR:
			case END_CODE_EXPR:
			case CODE:
				enterOuterAlt(_localctx, 2);
				{
				setState(46);
				code_block();
				}
				break;
			case LINE_COMMENT:
			case BEGIN_MULTILINE_COMMENT:
				enterOuterAlt(_localctx, 3);
				{
				setState(47);
				comment_block();
				}
				break;
			default:
				throw new NoViableAltException(this);
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	@SuppressWarnings("CheckReturnValue")
	public static class Comment_blockContext extends ParserRuleContext {
		public TerminalNode BEGIN_MULTILINE_COMMENT() { return getToken(DmeParser.BEGIN_MULTILINE_COMMENT, 0); }
		public TerminalNode END_MULTILINE_COMMENT() { return getToken(DmeParser.END_MULTILINE_COMMENT, 0); }
		public List<Comment_contentsContext> comment_contents() {
			return getRuleContexts(Comment_contentsContext.class);
		}
		public Comment_contentsContext comment_contents(int i) {
			return getRuleContext(Comment_contentsContext.class,i);
		}
		public TerminalNode LINE_COMMENT() { return getToken(DmeParser.LINE_COMMENT, 0); }
		public Comment_blockContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_comment_block; }
	}

	public final Comment_blockContext comment_block() throws RecognitionException {
		Comment_blockContext _localctx = new Comment_blockContext(_ctx, getState());
		enterRule(_localctx, 4, RULE_comment_block);
		int _la;
		try {
			setState(59);
			_errHandler.sync(this);
			switch (_input.LA(1)) {
			case BEGIN_MULTILINE_COMMENT:
				enterOuterAlt(_localctx, 1);
				{
				setState(50);
				match(BEGIN_MULTILINE_COMMENT);
				setState(54);
				_errHandler.sync(this);
				_la = _input.LA(1);
				while (_la==BEGIN_NESTED_MULTILINE_COMMENT || _la==COMMENT) {
					{
					{
					setState(51);
					comment_contents();
					}
					}
					setState(56);
					_errHandler.sync(this);
					_la = _input.LA(1);
				}
				setState(57);
				match(END_MULTILINE_COMMENT);
				}
				break;
			case LINE_COMMENT:
				enterOuterAlt(_localctx, 2);
				{
				setState(58);
				match(LINE_COMMENT);
				}
				break;
			default:
				throw new NoViableAltException(this);
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	@SuppressWarnings("CheckReturnValue")
	public static class Comment_contentsContext extends ParserRuleContext {
		public TerminalNode COMMENT() { return getToken(DmeParser.COMMENT, 0); }
		public TerminalNode BEGIN_NESTED_MULTILINE_COMMENT() { return getToken(DmeParser.BEGIN_NESTED_MULTILINE_COMMENT, 0); }
		public TerminalNode END_MULTILINE_COMMENT() { return getToken(DmeParser.END_MULTILINE_COMMENT, 0); }
		public List<Comment_contentsContext> comment_contents() {
			return getRuleContexts(Comment_contentsContext.class);
		}
		public Comment_contentsContext comment_contents(int i) {
			return getRuleContext(Comment_contentsContext.class,i);
		}
		public Comment_contentsContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_comment_contents; }
	}

	public final Comment_contentsContext comment_contents() throws RecognitionException {
		Comment_contentsContext _localctx = new Comment_contentsContext(_ctx, getState());
		enterRule(_localctx, 6, RULE_comment_contents);
		int _la;
		try {
			setState(70);
			_errHandler.sync(this);
			switch (_input.LA(1)) {
			case COMMENT:
				enterOuterAlt(_localctx, 1);
				{
				setState(61);
				match(COMMENT);
				}
				break;
			case BEGIN_NESTED_MULTILINE_COMMENT:
				enterOuterAlt(_localctx, 2);
				{
				setState(62);
				match(BEGIN_NESTED_MULTILINE_COMMENT);
				setState(66);
				_errHandler.sync(this);
				_la = _input.LA(1);
				while (_la==BEGIN_NESTED_MULTILINE_COMMENT || _la==COMMENT) {
					{
					{
					setState(63);
					comment_contents();
					}
					}
					setState(68);
					_errHandler.sync(this);
					_la = _input.LA(1);
				}
				setState(69);
				match(END_MULTILINE_COMMENT);
				}
				break;
			default:
				throw new NoViableAltException(this);
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	@SuppressWarnings("CheckReturnValue")
	public static class StringContext extends ParserRuleContext {
		public TerminalNode STRING_BEGIN() { return getToken(DmeParser.STRING_BEGIN, 0); }
		public TerminalNode STRING_END() { return getToken(DmeParser.STRING_END, 0); }
		public List<String_contentsContext> string_contents() {
			return getRuleContexts(String_contentsContext.class);
		}
		public String_contentsContext string_contents(int i) {
			return getRuleContext(String_contentsContext.class,i);
		}
		public List<TerminalNode> STRING_NEXT_LINE() { return getTokens(DmeParser.STRING_NEXT_LINE); }
		public TerminalNode STRING_NEXT_LINE(int i) {
			return getToken(DmeParser.STRING_NEXT_LINE, i);
		}
		public TerminalNode MULTILINE_STRING_BEGIN() { return getToken(DmeParser.MULTILINE_STRING_BEGIN, 0); }
		public TerminalNode MULTILINE_STRING_END() { return getToken(DmeParser.MULTILINE_STRING_END, 0); }
		public StringContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_string; }
	}

	public final StringContext string() throws RecognitionException {
		StringContext _localctx = new StringContext(_ctx, getState());
		enterRule(_localctx, 8, RULE_string);
		int _la;
		try {
			setState(94);
			_errHandler.sync(this);
			switch (_input.LA(1)) {
			case STRING_BEGIN:
				enterOuterAlt(_localctx, 1);
				{
				setState(72);
				match(STRING_BEGIN);
				setState(79);
				_errHandler.sync(this);
				_la = _input.LA(1);
				while ((((_la) & ~0x3f) == 0 && ((1L << _la) & 442368L) != 0)) {
					{
					{
					setState(74);
					_errHandler.sync(this);
					_la = _input.LA(1);
					if (_la==STRING_NEXT_LINE) {
						{
						setState(73);
						match(STRING_NEXT_LINE);
						}
					}

					setState(76);
					string_contents();
					}
					}
					setState(81);
					_errHandler.sync(this);
					_la = _input.LA(1);
				}
				setState(82);
				match(STRING_END);
				}
				break;
			case MULTILINE_STRING_BEGIN:
				enterOuterAlt(_localctx, 2);
				{
				setState(83);
				match(MULTILINE_STRING_BEGIN);
				setState(90);
				_errHandler.sync(this);
				_la = _input.LA(1);
				while ((((_la) & ~0x3f) == 0 && ((1L << _la) & 442368L) != 0)) {
					{
					{
					setState(85);
					_errHandler.sync(this);
					_la = _input.LA(1);
					if (_la==STRING_NEXT_LINE) {
						{
						setState(84);
						match(STRING_NEXT_LINE);
						}
					}

					setState(87);
					string_contents();
					}
					}
					setState(92);
					_errHandler.sync(this);
					_la = _input.LA(1);
				}
				setState(93);
				match(MULTILINE_STRING_END);
				}
				break;
			default:
				throw new NoViableAltException(this);
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	@SuppressWarnings("CheckReturnValue")
	public static class ResourceContext extends ParserRuleContext {
		public TerminalNode RESOURCE_BEGIN() { return getToken(DmeParser.RESOURCE_BEGIN, 0); }
		public TerminalNode RESOURCE_END() { return getToken(DmeParser.RESOURCE_END, 0); }
		public List<TerminalNode> RESOURCE_CONTENTS() { return getTokens(DmeParser.RESOURCE_CONTENTS); }
		public TerminalNode RESOURCE_CONTENTS(int i) {
			return getToken(DmeParser.RESOURCE_CONTENTS, i);
		}
		public ResourceContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_resource; }
	}

	public final ResourceContext resource() throws RecognitionException {
		ResourceContext _localctx = new ResourceContext(_ctx, getState());
		enterRule(_localctx, 10, RULE_resource);
		int _la;
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(96);
			match(RESOURCE_BEGIN);
			setState(100);
			_errHandler.sync(this);
			_la = _input.LA(1);
			while (_la==RESOURCE_CONTENTS) {
				{
				{
				setState(97);
				match(RESOURCE_CONTENTS);
				}
				}
				setState(102);
				_errHandler.sync(this);
				_la = _input.LA(1);
			}
			setState(103);
			match(RESOURCE_END);
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	@SuppressWarnings("CheckReturnValue")
	public static class String_contentsContext extends ParserRuleContext {
		public String_contentsContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_string_contents; }
	 
		public String_contentsContext() { }
		public void copyFrom(String_contentsContext ctx) {
			super.copyFrom(ctx);
		}
	}
	@SuppressWarnings("CheckReturnValue")
	public static class String_contents_placeholderContext extends String_contentsContext {
		public String_expression_beginContext string_expression_begin() {
			return getRuleContext(String_expression_beginContext.class,0);
		}
		public TerminalNode END_CODE_EXPR() { return getToken(DmeParser.END_CODE_EXPR, 0); }
		public String_contents_placeholderContext(String_contentsContext ctx) { copyFrom(ctx); }
	}
	@SuppressWarnings("CheckReturnValue")
	public static class String_contents_expressionContext extends String_contentsContext {
		public String_expressionContext string_expression() {
			return getRuleContext(String_expressionContext.class,0);
		}
		public String_contents_expressionContext(String_contentsContext ctx) { copyFrom(ctx); }
	}
	@SuppressWarnings("CheckReturnValue")
	public static class String_contents_literalContext extends String_contentsContext {
		public TerminalNode STRING_CONTENTS() { return getToken(DmeParser.STRING_CONTENTS, 0); }
		public String_contents_literalContext(String_contentsContext ctx) { copyFrom(ctx); }
	}

	public final String_contentsContext string_contents() throws RecognitionException {
		String_contentsContext _localctx = new String_contentsContext(_ctx, getState());
		enterRule(_localctx, 12, RULE_string_contents);
		try {
			setState(110);
			_errHandler.sync(this);
			switch ( getInterpreter().adaptivePredict(_input,13,_ctx) ) {
			case 1:
				_localctx = new String_contents_literalContext(_localctx);
				enterOuterAlt(_localctx, 1);
				{
				setState(105);
				match(STRING_CONTENTS);
				}
				break;
			case 2:
				_localctx = new String_contents_expressionContext(_localctx);
				enterOuterAlt(_localctx, 2);
				{
				setState(106);
				string_expression();
				}
				break;
			case 3:
				_localctx = new String_contents_placeholderContext(_localctx);
				enterOuterAlt(_localctx, 3);
				{
				setState(107);
				string_expression_begin();
				setState(108);
				match(END_CODE_EXPR);
				}
				break;
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	@SuppressWarnings("CheckReturnValue")
	public static class String_expressionContext extends ParserRuleContext {
		public String_expression_beginContext string_expression_begin() {
			return getRuleContext(String_expression_beginContext.class,0);
		}
		public Code_blockContext code_block() {
			return getRuleContext(Code_blockContext.class,0);
		}
		public TerminalNode END_CODE_EXPR() { return getToken(DmeParser.END_CODE_EXPR, 0); }
		public String_expressionContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_string_expression; }
	}

	public final String_expressionContext string_expression() throws RecognitionException {
		String_expressionContext _localctx = new String_expressionContext(_ctx, getState());
		enterRule(_localctx, 14, RULE_string_expression);
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(112);
			string_expression_begin();
			setState(113);
			code_block();
			setState(114);
			match(END_CODE_EXPR);
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	@SuppressWarnings("CheckReturnValue")
	public static class String_expression_beginContext extends ParserRuleContext {
		public TerminalNode BEGIN_STRING_EXPRESSION() { return getToken(DmeParser.BEGIN_STRING_EXPRESSION, 0); }
		public TerminalNode BEGIN_ML_STRING_EXPRESSION() { return getToken(DmeParser.BEGIN_ML_STRING_EXPRESSION, 0); }
		public String_expression_beginContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_string_expression_begin; }
	}

	public final String_expression_beginContext string_expression_begin() throws RecognitionException {
		String_expression_beginContext _localctx = new String_expression_beginContext(_ctx, getState());
		enterRule(_localctx, 16, RULE_string_expression_begin);
		int _la;
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(116);
			_la = _input.LA(1);
			if ( !(_la==BEGIN_ML_STRING_EXPRESSION || _la==BEGIN_STRING_EXPRESSION) ) {
			_errHandler.recoverInline(this);
			}
			else {
				if ( _input.LA(1)==Token.EOF ) matchedEOF = true;
				_errHandler.reportMatch(this);
				consume();
			}
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	@SuppressWarnings("CheckReturnValue")
	public static class Code_exprContext extends ParserRuleContext {
		public TerminalNode START_CODE_EXPR() { return getToken(DmeParser.START_CODE_EXPR, 0); }
		public TerminalNode END_CODE_EXPR() { return getToken(DmeParser.END_CODE_EXPR, 0); }
		public List<CodeContext> code() {
			return getRuleContexts(CodeContext.class);
		}
		public CodeContext code(int i) {
			return getRuleContext(CodeContext.class,i);
		}
		public Code_exprContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_code_expr; }
	}

	public final Code_exprContext code_expr() throws RecognitionException {
		Code_exprContext _localctx = new Code_exprContext(_ctx, getState());
		enterRule(_localctx, 18, RULE_code_expr);
		try {
			int _alt;
			enterOuterAlt(_localctx, 1);
			{
			setState(118);
			match(START_CODE_EXPR);
			setState(122);
			_errHandler.sync(this);
			_alt = getInterpreter().adaptivePredict(_input,14,_ctx);
			while ( _alt!=2 && _alt!=org.antlr.v4.runtime.atn.ATN.INVALID_ALT_NUMBER ) {
				if ( _alt==1 ) {
					{
					{
					setState(119);
					code();
					}
					} 
				}
				setState(124);
				_errHandler.sync(this);
				_alt = getInterpreter().adaptivePredict(_input,14,_ctx);
			}
			setState(125);
			match(END_CODE_EXPR);
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	@SuppressWarnings("CheckReturnValue")
	public static class Code_blockContext extends ParserRuleContext {
		public List<CodeContext> code() {
			return getRuleContexts(CodeContext.class);
		}
		public CodeContext code(int i) {
			return getRuleContext(CodeContext.class,i);
		}
		public Code_blockContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_code_block; }
	}

	public final Code_blockContext code_block() throws RecognitionException {
		Code_blockContext _localctx = new Code_blockContext(_ctx, getState());
		enterRule(_localctx, 20, RULE_code_block);
		try {
			int _alt;
			enterOuterAlt(_localctx, 1);
			{
			setState(128); 
			_errHandler.sync(this);
			_alt = 1;
			do {
				switch (_alt) {
				case 1:
					{
					{
					setState(127);
					code();
					}
					}
					break;
				default:
					throw new NoViableAltException(this);
				}
				setState(130); 
				_errHandler.sync(this);
				_alt = getInterpreter().adaptivePredict(_input,15,_ctx);
			} while ( _alt!=2 && _alt!=org.antlr.v4.runtime.atn.ATN.INVALID_ALT_NUMBER );
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	@SuppressWarnings("CheckReturnValue")
	public static class CodeContext extends ParserRuleContext {
		public List<Code_literalContext> code_literal() {
			return getRuleContexts(Code_literalContext.class);
		}
		public Code_literalContext code_literal(int i) {
			return getRuleContext(Code_literalContext.class,i);
		}
		public TerminalNode START_CODE_EXPR() { return getToken(DmeParser.START_CODE_EXPR, 0); }
		public TerminalNode END_CODE_EXPR() { return getToken(DmeParser.END_CODE_EXPR, 0); }
		public StringContext string() {
			return getRuleContext(StringContext.class,0);
		}
		public ResourceContext resource() {
			return getRuleContext(ResourceContext.class,0);
		}
		public CodeContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_code; }
	}

	public final CodeContext code() throws RecognitionException {
		CodeContext _localctx = new CodeContext(_ctx, getState());
		enterRule(_localctx, 22, RULE_code);
		try {
			int _alt;
			setState(141);
			_errHandler.sync(this);
			switch ( getInterpreter().adaptivePredict(_input,17,_ctx) ) {
			case 1:
				enterOuterAlt(_localctx, 1);
				{
				setState(133); 
				_errHandler.sync(this);
				_alt = 1;
				do {
					switch (_alt) {
					case 1:
						{
						{
						setState(132);
						code_literal();
						}
						}
						break;
					default:
						throw new NoViableAltException(this);
					}
					setState(135); 
					_errHandler.sync(this);
					_alt = getInterpreter().adaptivePredict(_input,16,_ctx);
				} while ( _alt!=2 && _alt!=org.antlr.v4.runtime.atn.ATN.INVALID_ALT_NUMBER );
				}
				break;
			case 2:
				enterOuterAlt(_localctx, 2);
				{
				setState(137);
				match(START_CODE_EXPR);
				}
				break;
			case 3:
				enterOuterAlt(_localctx, 3);
				{
				setState(138);
				match(END_CODE_EXPR);
				}
				break;
			case 4:
				enterOuterAlt(_localctx, 4);
				{
				setState(139);
				string();
				}
				break;
			case 5:
				enterOuterAlt(_localctx, 5);
				{
				setState(140);
				resource();
				}
				break;
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	@SuppressWarnings("CheckReturnValue")
	public static class Code_literalContext extends ParserRuleContext {
		public TerminalNode CODE() { return getToken(DmeParser.CODE, 0); }
		public TerminalNode START_CODE_EXPR() { return getToken(DmeParser.START_CODE_EXPR, 0); }
		public TerminalNode END_CODE_EXPR() { return getToken(DmeParser.END_CODE_EXPR, 0); }
		public Code_literalContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_code_literal; }
	}

	public final Code_literalContext code_literal() throws RecognitionException {
		Code_literalContext _localctx = new Code_literalContext(_ctx, getState());
		enterRule(_localctx, 24, RULE_code_literal);
		int _la;
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(143);
			_la = _input.LA(1);
			if ( !((((_la) & ~0x3f) == 0 && ((1L << _la) & 896L) != 0)) ) {
			_errHandler.recoverInline(this);
			}
			else {
				if ( _input.LA(1)==Token.EOF ) matchedEOF = true;
				_errHandler.reportMatch(this);
				consume();
			}
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	@SuppressWarnings("CheckReturnValue")
	public static class DirectiveContext extends ParserRuleContext {
		public DirectiveContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_directive; }
	 
		public DirectiveContext() { }
		public void copyFrom(DirectiveContext ctx) {
			super.copyFrom(ctx);
		}
	}
	@SuppressWarnings("CheckReturnValue")
	public static class PreprocessorDefContext extends DirectiveContext {
		public TerminalNode IFDEF() { return getToken(DmeParser.IFDEF, 0); }
		public TerminalNode CONDITIONAL_SYMBOL() { return getToken(DmeParser.CONDITIONAL_SYMBOL, 0); }
		public TerminalNode IFNDEF() { return getToken(DmeParser.IFNDEF, 0); }
		public TerminalNode UNDEF() { return getToken(DmeParser.UNDEF, 0); }
		public PreprocessorDefContext(DirectiveContext ctx) { copyFrom(ctx); }
	}
	@SuppressWarnings("CheckReturnValue")
	public static class PreprocessorErrorContext extends DirectiveContext {
		public TerminalNode ERROR() { return getToken(DmeParser.ERROR, 0); }
		public Directive_textContext directive_text() {
			return getRuleContext(Directive_textContext.class,0);
		}
		public PreprocessorErrorContext(DirectiveContext ctx) { copyFrom(ctx); }
	}
	@SuppressWarnings("CheckReturnValue")
	public static class PreprocessorConditionalContext extends DirectiveContext {
		public TerminalNode IF() { return getToken(DmeParser.IF, 0); }
		public Preprocessor_expressionContext preprocessor_expression() {
			return getRuleContext(Preprocessor_expressionContext.class,0);
		}
		public TerminalNode ELIF() { return getToken(DmeParser.ELIF, 0); }
		public TerminalNode ELSE() { return getToken(DmeParser.ELSE, 0); }
		public TerminalNode ENDIF() { return getToken(DmeParser.ENDIF, 0); }
		public PreprocessorConditionalContext(DirectiveContext ctx) { copyFrom(ctx); }
	}
	@SuppressWarnings("CheckReturnValue")
	public static class PreprocessorImportContext extends DirectiveContext {
		public Directive_textContext directive_text() {
			return getRuleContext(Directive_textContext.class,0);
		}
		public TerminalNode IMPORT() { return getToken(DmeParser.IMPORT, 0); }
		public TerminalNode INCLUDE() { return getToken(DmeParser.INCLUDE, 0); }
		public PreprocessorImportContext(DirectiveContext ctx) { copyFrom(ctx); }
	}
	@SuppressWarnings("CheckReturnValue")
	public static class PreprocessorPragmaContext extends DirectiveContext {
		public TerminalNode PRAGMA() { return getToken(DmeParser.PRAGMA, 0); }
		public Directive_textContext directive_text() {
			return getRuleContext(Directive_textContext.class,0);
		}
		public PreprocessorPragmaContext(DirectiveContext ctx) { copyFrom(ctx); }
	}
	@SuppressWarnings("CheckReturnValue")
	public static class PreprocessorDefineContext extends DirectiveContext {
		public TerminalNode DEFINE() { return getToken(DmeParser.DEFINE, 0); }
		public TerminalNode CONDITIONAL_SYMBOL() { return getToken(DmeParser.CONDITIONAL_SYMBOL, 0); }
		public Directive_textContext directive_text() {
			return getRuleContext(Directive_textContext.class,0);
		}
		public PreprocessorDefineContext(DirectiveContext ctx) { copyFrom(ctx); }
	}

	public final DirectiveContext directive() throws RecognitionException {
		DirectiveContext _localctx = new DirectiveContext(_ctx, getState());
		enterRule(_localctx, 26, RULE_directive);
		int _la;
		try {
			setState(168);
			_errHandler.sync(this);
			switch (_input.LA(1)) {
			case IMPORT:
			case INCLUDE:
				_localctx = new PreprocessorImportContext(_localctx);
				enterOuterAlt(_localctx, 1);
				{
				setState(145);
				_la = _input.LA(1);
				if ( !(_la==IMPORT || _la==INCLUDE) ) {
				_errHandler.recoverInline(this);
				}
				else {
					if ( _input.LA(1)==Token.EOF ) matchedEOF = true;
					_errHandler.reportMatch(this);
					consume();
				}
				setState(146);
				directive_text();
				}
				break;
			case IF:
				_localctx = new PreprocessorConditionalContext(_localctx);
				enterOuterAlt(_localctx, 2);
				{
				setState(147);
				match(IF);
				setState(148);
				preprocessor_expression(0);
				}
				break;
			case ELIF:
				_localctx = new PreprocessorConditionalContext(_localctx);
				enterOuterAlt(_localctx, 3);
				{
				setState(149);
				match(ELIF);
				setState(150);
				preprocessor_expression(0);
				}
				break;
			case ELSE:
				_localctx = new PreprocessorConditionalContext(_localctx);
				enterOuterAlt(_localctx, 4);
				{
				setState(151);
				match(ELSE);
				}
				break;
			case ENDIF:
				_localctx = new PreprocessorConditionalContext(_localctx);
				enterOuterAlt(_localctx, 5);
				{
				setState(152);
				match(ENDIF);
				}
				break;
			case IFDEF:
				_localctx = new PreprocessorDefContext(_localctx);
				enterOuterAlt(_localctx, 6);
				{
				setState(153);
				match(IFDEF);
				setState(154);
				match(CONDITIONAL_SYMBOL);
				}
				break;
			case IFNDEF:
				_localctx = new PreprocessorDefContext(_localctx);
				enterOuterAlt(_localctx, 7);
				{
				setState(155);
				match(IFNDEF);
				setState(156);
				match(CONDITIONAL_SYMBOL);
				}
				break;
			case UNDEF:
				_localctx = new PreprocessorDefContext(_localctx);
				enterOuterAlt(_localctx, 8);
				{
				setState(157);
				match(UNDEF);
				setState(158);
				match(CONDITIONAL_SYMBOL);
				}
				break;
			case PRAGMA:
				_localctx = new PreprocessorPragmaContext(_localctx);
				enterOuterAlt(_localctx, 9);
				{
				setState(159);
				match(PRAGMA);
				setState(160);
				directive_text();
				}
				break;
			case ERROR:
				_localctx = new PreprocessorErrorContext(_localctx);
				enterOuterAlt(_localctx, 10);
				{
				setState(161);
				match(ERROR);
				setState(162);
				directive_text();
				}
				break;
			case DEFINE:
				_localctx = new PreprocessorDefineContext(_localctx);
				enterOuterAlt(_localctx, 11);
				{
				setState(163);
				match(DEFINE);
				setState(164);
				match(CONDITIONAL_SYMBOL);
				setState(166);
				_errHandler.sync(this);
				_la = _input.LA(1);
				if (_la==TEXT) {
					{
					setState(165);
					directive_text();
					}
				}

				}
				break;
			default:
				throw new NoViableAltException(this);
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	@SuppressWarnings("CheckReturnValue")
	public static class Directive_textContext extends ParserRuleContext {
		public List<TerminalNode> TEXT() { return getTokens(DmeParser.TEXT); }
		public TerminalNode TEXT(int i) {
			return getToken(DmeParser.TEXT, i);
		}
		public Directive_textContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_directive_text; }
	}

	public final Directive_textContext directive_text() throws RecognitionException {
		Directive_textContext _localctx = new Directive_textContext(_ctx, getState());
		enterRule(_localctx, 28, RULE_directive_text);
		int _la;
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(171); 
			_errHandler.sync(this);
			_la = _input.LA(1);
			do {
				{
				{
				setState(170);
				match(TEXT);
				}
				}
				setState(173); 
				_errHandler.sync(this);
				_la = _input.LA(1);
			} while ( _la==TEXT );
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	@SuppressWarnings("CheckReturnValue")
	public static class Preprocessor_expressionContext extends ParserRuleContext {
		public Preprocessor_expressionContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_preprocessor_expression; }
	 
		public Preprocessor_expressionContext() { }
		public void copyFrom(Preprocessor_expressionContext ctx) {
			super.copyFrom(ctx);
		}
	}
	@SuppressWarnings("CheckReturnValue")
	public static class PreprocessorParenthesisContext extends Preprocessor_expressionContext {
		public TerminalNode LPAREN() { return getToken(DmeParser.LPAREN, 0); }
		public Preprocessor_expressionContext preprocessor_expression() {
			return getRuleContext(Preprocessor_expressionContext.class,0);
		}
		public TerminalNode RPAREN() { return getToken(DmeParser.RPAREN, 0); }
		public PreprocessorParenthesisContext(Preprocessor_expressionContext ctx) { copyFrom(ctx); }
	}
	@SuppressWarnings("CheckReturnValue")
	public static class PreprocessorNotContext extends Preprocessor_expressionContext {
		public TerminalNode BANG() { return getToken(DmeParser.BANG, 0); }
		public Preprocessor_expressionContext preprocessor_expression() {
			return getRuleContext(Preprocessor_expressionContext.class,0);
		}
		public PreprocessorNotContext(Preprocessor_expressionContext ctx) { copyFrom(ctx); }
	}
	@SuppressWarnings("CheckReturnValue")
	public static class PreprocessorBinaryContext extends Preprocessor_expressionContext {
		public Token op;
		public List<Preprocessor_expressionContext> preprocessor_expression() {
			return getRuleContexts(Preprocessor_expressionContext.class);
		}
		public Preprocessor_expressionContext preprocessor_expression(int i) {
			return getRuleContext(Preprocessor_expressionContext.class,i);
		}
		public TerminalNode EQUAL() { return getToken(DmeParser.EQUAL, 0); }
		public TerminalNode NOTEQUAL() { return getToken(DmeParser.NOTEQUAL, 0); }
		public TerminalNode AND() { return getToken(DmeParser.AND, 0); }
		public TerminalNode OR() { return getToken(DmeParser.OR, 0); }
		public TerminalNode LT() { return getToken(DmeParser.LT, 0); }
		public TerminalNode GT() { return getToken(DmeParser.GT, 0); }
		public TerminalNode LE() { return getToken(DmeParser.LE, 0); }
		public TerminalNode GE() { return getToken(DmeParser.GE, 0); }
		public PreprocessorBinaryContext(Preprocessor_expressionContext ctx) { copyFrom(ctx); }
	}
	@SuppressWarnings("CheckReturnValue")
	public static class PreprocessorConstantContext extends Preprocessor_expressionContext {
		public TerminalNode TRUE() { return getToken(DmeParser.TRUE, 0); }
		public TerminalNode FALSE() { return getToken(DmeParser.FALSE, 0); }
		public TerminalNode DECIMAL_LITERAL() { return getToken(DmeParser.DECIMAL_LITERAL, 0); }
		public TerminalNode DIRECTIVE_STRING() { return getToken(DmeParser.DIRECTIVE_STRING, 0); }
		public PreprocessorConstantContext(Preprocessor_expressionContext ctx) { copyFrom(ctx); }
	}
	@SuppressWarnings("CheckReturnValue")
	public static class PreprocessorConditionalSymbolContext extends Preprocessor_expressionContext {
		public TerminalNode CONDITIONAL_SYMBOL() { return getToken(DmeParser.CONDITIONAL_SYMBOL, 0); }
		public TerminalNode LPAREN() { return getToken(DmeParser.LPAREN, 0); }
		public Preprocessor_expressionContext preprocessor_expression() {
			return getRuleContext(Preprocessor_expressionContext.class,0);
		}
		public TerminalNode RPAREN() { return getToken(DmeParser.RPAREN, 0); }
		public PreprocessorConditionalSymbolContext(Preprocessor_expressionContext ctx) { copyFrom(ctx); }
	}
	@SuppressWarnings("CheckReturnValue")
	public static class PreprocessorDefinedContext extends Preprocessor_expressionContext {
		public TerminalNode DEFINED() { return getToken(DmeParser.DEFINED, 0); }
		public TerminalNode CONDITIONAL_SYMBOL() { return getToken(DmeParser.CONDITIONAL_SYMBOL, 0); }
		public TerminalNode LPAREN() { return getToken(DmeParser.LPAREN, 0); }
		public TerminalNode RPAREN() { return getToken(DmeParser.RPAREN, 0); }
		public PreprocessorDefinedContext(Preprocessor_expressionContext ctx) { copyFrom(ctx); }
	}

	public final Preprocessor_expressionContext preprocessor_expression() throws RecognitionException {
		return preprocessor_expression(0);
	}

	private Preprocessor_expressionContext preprocessor_expression(int _p) throws RecognitionException {
		ParserRuleContext _parentctx = _ctx;
		int _parentState = getState();
		Preprocessor_expressionContext _localctx = new Preprocessor_expressionContext(_ctx, _parentState);
		Preprocessor_expressionContext _prevctx = _localctx;
		int _startState = 30;
		enterRecursionRule(_localctx, 30, RULE_preprocessor_expression, _p);
		int _la;
		try {
			int _alt;
			enterOuterAlt(_localctx, 1);
			{
			setState(200);
			_errHandler.sync(this);
			switch (_input.LA(1)) {
			case TRUE:
				{
				_localctx = new PreprocessorConstantContext(_localctx);
				_ctx = _localctx;
				_prevctx = _localctx;

				setState(176);
				match(TRUE);
				}
				break;
			case FALSE:
				{
				_localctx = new PreprocessorConstantContext(_localctx);
				_ctx = _localctx;
				_prevctx = _localctx;
				setState(177);
				match(FALSE);
				}
				break;
			case DECIMAL_LITERAL:
				{
				_localctx = new PreprocessorConstantContext(_localctx);
				_ctx = _localctx;
				_prevctx = _localctx;
				setState(178);
				match(DECIMAL_LITERAL);
				}
				break;
			case DIRECTIVE_STRING:
				{
				_localctx = new PreprocessorConstantContext(_localctx);
				_ctx = _localctx;
				_prevctx = _localctx;
				setState(179);
				match(DIRECTIVE_STRING);
				}
				break;
			case CONDITIONAL_SYMBOL:
				{
				_localctx = new PreprocessorConditionalSymbolContext(_localctx);
				_ctx = _localctx;
				_prevctx = _localctx;
				setState(180);
				match(CONDITIONAL_SYMBOL);
				setState(185);
				_errHandler.sync(this);
				switch ( getInterpreter().adaptivePredict(_input,21,_ctx) ) {
				case 1:
					{
					setState(181);
					match(LPAREN);
					setState(182);
					preprocessor_expression(0);
					setState(183);
					match(RPAREN);
					}
					break;
				}
				}
				break;
			case LPAREN:
				{
				_localctx = new PreprocessorParenthesisContext(_localctx);
				_ctx = _localctx;
				_prevctx = _localctx;
				setState(187);
				match(LPAREN);
				setState(188);
				preprocessor_expression(0);
				setState(189);
				match(RPAREN);
				}
				break;
			case BANG:
				{
				_localctx = new PreprocessorNotContext(_localctx);
				_ctx = _localctx;
				_prevctx = _localctx;
				setState(191);
				match(BANG);
				setState(192);
				preprocessor_expression(6);
				}
				break;
			case DEFINED:
				{
				_localctx = new PreprocessorDefinedContext(_localctx);
				_ctx = _localctx;
				_prevctx = _localctx;
				setState(193);
				match(DEFINED);
				setState(198);
				_errHandler.sync(this);
				switch (_input.LA(1)) {
				case CONDITIONAL_SYMBOL:
					{
					setState(194);
					match(CONDITIONAL_SYMBOL);
					}
					break;
				case LPAREN:
					{
					setState(195);
					match(LPAREN);
					setState(196);
					match(CONDITIONAL_SYMBOL);
					setState(197);
					match(RPAREN);
					}
					break;
				default:
					throw new NoViableAltException(this);
				}
				}
				break;
			default:
				throw new NoViableAltException(this);
			}
			_ctx.stop = _input.LT(-1);
			setState(216);
			_errHandler.sync(this);
			_alt = getInterpreter().adaptivePredict(_input,25,_ctx);
			while ( _alt!=2 && _alt!=org.antlr.v4.runtime.atn.ATN.INVALID_ALT_NUMBER ) {
				if ( _alt==1 ) {
					if ( _parseListeners!=null ) triggerExitRuleEvent();
					_prevctx = _localctx;
					{
					setState(214);
					_errHandler.sync(this);
					switch ( getInterpreter().adaptivePredict(_input,24,_ctx) ) {
					case 1:
						{
						_localctx = new PreprocessorBinaryContext(new Preprocessor_expressionContext(_parentctx, _parentState));
						pushNewRecursionContext(_localctx, _startState, RULE_preprocessor_expression);
						setState(202);
						if (!(precpred(_ctx, 5))) throw new FailedPredicateException(this, "precpred(_ctx, 5)");
						setState(203);
						((PreprocessorBinaryContext)_localctx).op = _input.LT(1);
						_la = _input.LA(1);
						if ( !(_la==EQUAL || _la==NOTEQUAL) ) {
							((PreprocessorBinaryContext)_localctx).op = (Token)_errHandler.recoverInline(this);
						}
						else {
							if ( _input.LA(1)==Token.EOF ) matchedEOF = true;
							_errHandler.reportMatch(this);
							consume();
						}
						setState(204);
						preprocessor_expression(6);
						}
						break;
					case 2:
						{
						_localctx = new PreprocessorBinaryContext(new Preprocessor_expressionContext(_parentctx, _parentState));
						pushNewRecursionContext(_localctx, _startState, RULE_preprocessor_expression);
						setState(205);
						if (!(precpred(_ctx, 4))) throw new FailedPredicateException(this, "precpred(_ctx, 4)");
						setState(206);
						((PreprocessorBinaryContext)_localctx).op = match(AND);
						setState(207);
						preprocessor_expression(5);
						}
						break;
					case 3:
						{
						_localctx = new PreprocessorBinaryContext(new Preprocessor_expressionContext(_parentctx, _parentState));
						pushNewRecursionContext(_localctx, _startState, RULE_preprocessor_expression);
						setState(208);
						if (!(precpred(_ctx, 3))) throw new FailedPredicateException(this, "precpred(_ctx, 3)");
						setState(209);
						((PreprocessorBinaryContext)_localctx).op = match(OR);
						setState(210);
						preprocessor_expression(4);
						}
						break;
					case 4:
						{
						_localctx = new PreprocessorBinaryContext(new Preprocessor_expressionContext(_parentctx, _parentState));
						pushNewRecursionContext(_localctx, _startState, RULE_preprocessor_expression);
						setState(211);
						if (!(precpred(_ctx, 2))) throw new FailedPredicateException(this, "precpred(_ctx, 2)");
						setState(212);
						((PreprocessorBinaryContext)_localctx).op = _input.LT(1);
						_la = _input.LA(1);
						if ( !((((_la) & ~0x3f) == 0 && ((1L << _la) & 131941395333120L) != 0)) ) {
							((PreprocessorBinaryContext)_localctx).op = (Token)_errHandler.recoverInline(this);
						}
						else {
							if ( _input.LA(1)==Token.EOF ) matchedEOF = true;
							_errHandler.reportMatch(this);
							consume();
						}
						setState(213);
						preprocessor_expression(3);
						}
						break;
					}
					} 
				}
				setState(218);
				_errHandler.sync(this);
				_alt = getInterpreter().adaptivePredict(_input,25,_ctx);
			}
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			unrollRecursionContexts(_parentctx);
		}
		return _localctx;
	}

	public boolean sempred(RuleContext _localctx, int ruleIndex, int predIndex) {
		switch (ruleIndex) {
		case 15:
			return preprocessor_expression_sempred((Preprocessor_expressionContext)_localctx, predIndex);
		}
		return true;
	}
	private boolean preprocessor_expression_sempred(Preprocessor_expressionContext _localctx, int predIndex) {
		switch (predIndex) {
		case 0:
			return precpred(_ctx, 5);
		case 1:
			return precpred(_ctx, 4);
		case 2:
			return precpred(_ctx, 3);
		case 3:
			return precpred(_ctx, 2);
		}
		return true;
	}

	public static final String _serializedATN =
		"\u0004\u0001@\u00dc\u0002\u0000\u0007\u0000\u0002\u0001\u0007\u0001\u0002"+
		"\u0002\u0007\u0002\u0002\u0003\u0007\u0003\u0002\u0004\u0007\u0004\u0002"+
		"\u0005\u0007\u0005\u0002\u0006\u0007\u0006\u0002\u0007\u0007\u0007\u0002"+
		"\b\u0007\b\u0002\t\u0007\t\u0002\n\u0007\n\u0002\u000b\u0007\u000b\u0002"+
		"\f\u0007\f\u0002\r\u0007\r\u0002\u000e\u0007\u000e\u0002\u000f\u0007\u000f"+
		"\u0001\u0000\u0005\u0000\"\b\u0000\n\u0000\f\u0000%\t\u0000\u0001\u0001"+
		"\u0001\u0001\u0001\u0001\u0005\u0001*\b\u0001\n\u0001\f\u0001-\t\u0001"+
		"\u0001\u0001\u0001\u0001\u0003\u00011\b\u0001\u0001\u0002\u0001\u0002"+
		"\u0005\u00025\b\u0002\n\u0002\f\u00028\t\u0002\u0001\u0002\u0001\u0002"+
		"\u0003\u0002<\b\u0002\u0001\u0003\u0001\u0003\u0001\u0003\u0005\u0003"+
		"A\b\u0003\n\u0003\f\u0003D\t\u0003\u0001\u0003\u0003\u0003G\b\u0003\u0001"+
		"\u0004\u0001\u0004\u0003\u0004K\b\u0004\u0001\u0004\u0005\u0004N\b\u0004"+
		"\n\u0004\f\u0004Q\t\u0004\u0001\u0004\u0001\u0004\u0001\u0004\u0003\u0004"+
		"V\b\u0004\u0001\u0004\u0005\u0004Y\b\u0004\n\u0004\f\u0004\\\t\u0004\u0001"+
		"\u0004\u0003\u0004_\b\u0004\u0001\u0005\u0001\u0005\u0005\u0005c\b\u0005"+
		"\n\u0005\f\u0005f\t\u0005\u0001\u0005\u0001\u0005\u0001\u0006\u0001\u0006"+
		"\u0001\u0006\u0001\u0006\u0001\u0006\u0003\u0006o\b\u0006\u0001\u0007"+
		"\u0001\u0007\u0001\u0007\u0001\u0007\u0001\b\u0001\b\u0001\t\u0001\t\u0005"+
		"\ty\b\t\n\t\f\t|\t\t\u0001\t\u0001\t\u0001\n\u0004\n\u0081\b\n\u000b\n"+
		"\f\n\u0082\u0001\u000b\u0004\u000b\u0086\b\u000b\u000b\u000b\f\u000b\u0087"+
		"\u0001\u000b\u0001\u000b\u0001\u000b\u0001\u000b\u0003\u000b\u008e\b\u000b"+
		"\u0001\f\u0001\f\u0001\r\u0001\r\u0001\r\u0001\r\u0001\r\u0001\r\u0001"+
		"\r\u0001\r\u0001\r\u0001\r\u0001\r\u0001\r\u0001\r\u0001\r\u0001\r\u0001"+
		"\r\u0001\r\u0001\r\u0001\r\u0001\r\u0001\r\u0003\r\u00a7\b\r\u0003\r\u00a9"+
		"\b\r\u0001\u000e\u0004\u000e\u00ac\b\u000e\u000b\u000e\f\u000e\u00ad\u0001"+
		"\u000f\u0001\u000f\u0001\u000f\u0001\u000f\u0001\u000f\u0001\u000f\u0001"+
		"\u000f\u0001\u000f\u0001\u000f\u0001\u000f\u0003\u000f\u00ba\b\u000f\u0001"+
		"\u000f\u0001\u000f\u0001\u000f\u0001\u000f\u0001\u000f\u0001\u000f\u0001"+
		"\u000f\u0001\u000f\u0001\u000f\u0001\u000f\u0001\u000f\u0003\u000f\u00c7"+
		"\b\u000f\u0003\u000f\u00c9\b\u000f\u0001\u000f\u0001\u000f\u0001\u000f"+
		"\u0001\u000f\u0001\u000f\u0001\u000f\u0001\u000f\u0001\u000f\u0001\u000f"+
		"\u0001\u000f\u0001\u000f\u0001\u000f\u0005\u000f\u00d7\b\u000f\n\u000f"+
		"\f\u000f\u00da\t\u000f\u0001\u000f\u0000\u0001\u001e\u0010\u0000\u0002"+
		"\u0004\u0006\b\n\f\u000e\u0010\u0012\u0014\u0016\u0018\u001a\u001c\u001e"+
		"\u0000\u0005\u0002\u0000\u000e\u000e\u0011\u0011\u0001\u0000\u0007\t\u0001"+
		"\u0000\u0015\u0016\u0001\u0000\'(\u0001\u0000+.\u00fb\u0000#\u0001\u0000"+
		"\u0000\u0000\u00020\u0001\u0000\u0000\u0000\u0004;\u0001\u0000\u0000\u0000"+
		"\u0006F\u0001\u0000\u0000\u0000\b^\u0001\u0000\u0000\u0000\n`\u0001\u0000"+
		"\u0000\u0000\fn\u0001\u0000\u0000\u0000\u000ep\u0001\u0000\u0000\u0000"+
		"\u0010t\u0001\u0000\u0000\u0000\u0012v\u0001\u0000\u0000\u0000\u0014\u0080"+
		"\u0001\u0000\u0000\u0000\u0016\u008d\u0001\u0000\u0000\u0000\u0018\u008f"+
		"\u0001\u0000\u0000\u0000\u001a\u00a8\u0001\u0000\u0000\u0000\u001c\u00ab"+
		"\u0001\u0000\u0000\u0000\u001e\u00c8\u0001\u0000\u0000\u0000 \"\u0003"+
		"\u0002\u0001\u0000! \u0001\u0000\u0000\u0000\"%\u0001\u0000\u0000\u0000"+
		"#!\u0001\u0000\u0000\u0000#$\u0001\u0000\u0000\u0000$\u0001\u0001\u0000"+
		"\u0000\u0000%#\u0001\u0000\u0000\u0000&\'\u0005\u0001\u0000\u0000\'+\u0003"+
		"\u001a\r\u0000(*\u00054\u0000\u0000)(\u0001\u0000\u0000\u0000*-\u0001"+
		"\u0000\u0000\u0000+)\u0001\u0000\u0000\u0000+,\u0001\u0000\u0000\u0000"+
		",1\u0001\u0000\u0000\u0000-+\u0001\u0000\u0000\u0000.1\u0003\u0014\n\u0000"+
		"/1\u0003\u0004\u0002\u00000&\u0001\u0000\u0000\u00000.\u0001\u0000\u0000"+
		"\u00000/\u0001\u0000\u0000\u00001\u0003\u0001\u0000\u0000\u000026\u0005"+
		"\u0006\u0000\u000035\u0003\u0006\u0003\u000043\u0001\u0000\u0000\u0000"+
		"58\u0001\u0000\u0000\u000064\u0001\u0000\u0000\u000067\u0001\u0000\u0000"+
		"\u000079\u0001\u0000\u0000\u000086\u0001\u0000\u0000\u00009<\u0005\n\u0000"+
		"\u0000:<\u0005\u0005\u0000\u0000;2\u0001\u0000\u0000\u0000;:\u0001\u0000"+
		"\u0000\u0000<\u0005\u0001\u0000\u0000\u0000=G\u0005\f\u0000\u0000>B\u0005"+
		"\u000b\u0000\u0000?A\u0003\u0006\u0003\u0000@?\u0001\u0000\u0000\u0000"+
		"AD\u0001\u0000\u0000\u0000B@\u0001\u0000\u0000\u0000BC\u0001\u0000\u0000"+
		"\u0000CE\u0001\u0000\u0000\u0000DB\u0001\u0000\u0000\u0000EG\u0005\n\u0000"+
		"\u0000F=\u0001\u0000\u0000\u0000F>\u0001\u0000\u0000\u0000G\u0007\u0001"+
		"\u0000\u0000\u0000HO\u0005\u0002\u0000\u0000IK\u0005\u000f\u0000\u0000"+
		"JI\u0001\u0000\u0000\u0000JK\u0001\u0000\u0000\u0000KL\u0001\u0000\u0000"+
		"\u0000LN\u0003\f\u0006\u0000MJ\u0001\u0000\u0000\u0000NQ\u0001\u0000\u0000"+
		"\u0000OM\u0001\u0000\u0000\u0000OP\u0001\u0000\u0000\u0000PR\u0001\u0000"+
		"\u0000\u0000QO\u0001\u0000\u0000\u0000R_\u0005\u0010\u0000\u0000SZ\u0005"+
		"\u0003\u0000\u0000TV\u0005\u000f\u0000\u0000UT\u0001\u0000\u0000\u0000"+
		"UV\u0001\u0000\u0000\u0000VW\u0001\u0000\u0000\u0000WY\u0003\f\u0006\u0000"+
		"XU\u0001\u0000\u0000\u0000Y\\\u0001\u0000\u0000\u0000ZX\u0001\u0000\u0000"+
		"\u0000Z[\u0001\u0000\u0000\u0000[]\u0001\u0000\u0000\u0000\\Z\u0001\u0000"+
		"\u0000\u0000]_\u0005\r\u0000\u0000^H\u0001\u0000\u0000\u0000^S\u0001\u0000"+
		"\u0000\u0000_\t\u0001\u0000\u0000\u0000`d\u0005\u0004\u0000\u0000ac\u0005"+
		"\u0014\u0000\u0000ba\u0001\u0000\u0000\u0000cf\u0001\u0000\u0000\u0000"+
		"db\u0001\u0000\u0000\u0000de\u0001\u0000\u0000\u0000eg\u0001\u0000\u0000"+
		"\u0000fd\u0001\u0000\u0000\u0000gh\u0005\u0013\u0000\u0000h\u000b\u0001"+
		"\u0000\u0000\u0000io\u0005\u0012\u0000\u0000jo\u0003\u000e\u0007\u0000"+
		"kl\u0003\u0010\b\u0000lm\u0005\b\u0000\u0000mo\u0001\u0000\u0000\u0000"+
		"ni\u0001\u0000\u0000\u0000nj\u0001\u0000\u0000\u0000nk\u0001\u0000\u0000"+
		"\u0000o\r\u0001\u0000\u0000\u0000pq\u0003\u0010\b\u0000qr\u0003\u0014"+
		"\n\u0000rs\u0005\b\u0000\u0000s\u000f\u0001\u0000\u0000\u0000tu\u0007"+
		"\u0000\u0000\u0000u\u0011\u0001\u0000\u0000\u0000vz\u0005\u0007\u0000"+
		"\u0000wy\u0003\u0016\u000b\u0000xw\u0001\u0000\u0000\u0000y|\u0001\u0000"+
		"\u0000\u0000zx\u0001\u0000\u0000\u0000z{\u0001\u0000\u0000\u0000{}\u0001"+
		"\u0000\u0000\u0000|z\u0001\u0000\u0000\u0000}~\u0005\b\u0000\u0000~\u0013"+
		"\u0001\u0000\u0000\u0000\u007f\u0081\u0003\u0016\u000b\u0000\u0080\u007f"+
		"\u0001\u0000\u0000\u0000\u0081\u0082\u0001\u0000\u0000\u0000\u0082\u0080"+
		"\u0001\u0000\u0000\u0000\u0082\u0083\u0001\u0000\u0000\u0000\u0083\u0015"+
		"\u0001\u0000\u0000\u0000\u0084\u0086\u0003\u0018\f\u0000\u0085\u0084\u0001"+
		"\u0000\u0000\u0000\u0086\u0087\u0001\u0000\u0000\u0000\u0087\u0085\u0001"+
		"\u0000\u0000\u0000\u0087\u0088\u0001\u0000\u0000\u0000\u0088\u008e\u0001"+
		"\u0000\u0000\u0000\u0089\u008e\u0005\u0007\u0000\u0000\u008a\u008e\u0005"+
		"\b\u0000\u0000\u008b\u008e\u0003\b\u0004\u0000\u008c\u008e\u0003\n\u0005"+
		"\u0000\u008d\u0085\u0001\u0000\u0000\u0000\u008d\u0089\u0001\u0000\u0000"+
		"\u0000\u008d\u008a\u0001\u0000\u0000\u0000\u008d\u008b\u0001\u0000\u0000"+
		"\u0000\u008d\u008c\u0001\u0000\u0000\u0000\u008e\u0017\u0001\u0000\u0000"+
		"\u0000\u008f\u0090\u0007\u0001\u0000\u0000\u0090\u0019\u0001\u0000\u0000"+
		"\u0000\u0091\u0092\u0007\u0002\u0000\u0000\u0092\u00a9\u0003\u001c\u000e"+
		"\u0000\u0093\u0094\u0005\u001a\u0000\u0000\u0094\u00a9\u0003\u001e\u000f"+
		"\u0000\u0095\u0096\u0005\u001b\u0000\u0000\u0096\u00a9\u0003\u001e\u000f"+
		"\u0000\u0097\u00a9\u0005\u001c\u0000\u0000\u0098\u00a9\u0005 \u0000\u0000"+
		"\u0099\u009a\u0005\u001e\u0000\u0000\u009a\u00a9\u00051\u0000\u0000\u009b"+
		"\u009c\u0005\u001f\u0000\u0000\u009c\u00a9\u00051\u0000\u0000\u009d\u009e"+
		"\u0005\u001d\u0000\u0000\u009e\u00a9\u00051\u0000\u0000\u009f\u00a0\u0005"+
		"\u0017\u0000\u0000\u00a0\u00a9\u0003\u001c\u000e\u0000\u00a1\u00a2\u0005"+
		"#\u0000\u0000\u00a2\u00a9\u0003\u001c\u000e\u0000\u00a3\u00a4\u0005\u0018"+
		"\u0000\u0000\u00a4\u00a6\u00051\u0000\u0000\u00a5\u00a7\u0003\u001c\u000e"+
		"\u0000\u00a6\u00a5\u0001\u0000\u0000\u0000\u00a6\u00a7\u0001\u0000\u0000"+
		"\u0000\u00a7\u00a9\u0001\u0000\u0000\u0000\u00a8\u0091\u0001\u0000\u0000"+
		"\u0000\u00a8\u0093\u0001\u0000\u0000\u0000\u00a8\u0095\u0001\u0000\u0000"+
		"\u0000\u00a8\u0097\u0001\u0000\u0000\u0000\u00a8\u0098\u0001\u0000\u0000"+
		"\u0000\u00a8\u0099\u0001\u0000\u0000\u0000\u00a8\u009b\u0001\u0000\u0000"+
		"\u0000\u00a8\u009d\u0001\u0000\u0000\u0000\u00a8\u009f\u0001\u0000\u0000"+
		"\u0000\u00a8\u00a1\u0001\u0000\u0000\u0000\u00a8\u00a3\u0001\u0000\u0000"+
		"\u0000\u00a9\u001b\u0001\u0000\u0000\u0000\u00aa\u00ac\u00059\u0000\u0000"+
		"\u00ab\u00aa\u0001\u0000\u0000\u0000\u00ac\u00ad\u0001\u0000\u0000\u0000"+
		"\u00ad\u00ab\u0001\u0000\u0000\u0000\u00ad\u00ae\u0001\u0000\u0000\u0000"+
		"\u00ae\u001d\u0001\u0000\u0000\u0000\u00af\u00b0\u0006\u000f\uffff\uffff"+
		"\u0000\u00b0\u00c9\u0005!\u0000\u0000\u00b1\u00c9\u0005\"\u0000\u0000"+
		"\u00b2\u00c9\u00052\u0000\u0000\u00b3\u00c9\u00050\u0000\u0000\u00b4\u00b9"+
		"\u00051\u0000\u0000\u00b5\u00b6\u0005%\u0000\u0000\u00b6\u00b7\u0003\u001e"+
		"\u000f\u0000\u00b7\u00b8\u0005&\u0000\u0000\u00b8\u00ba\u0001\u0000\u0000"+
		"\u0000\u00b9\u00b5\u0001\u0000\u0000\u0000\u00b9\u00ba\u0001\u0000\u0000"+
		"\u0000\u00ba\u00c9\u0001\u0000\u0000\u0000\u00bb\u00bc\u0005%\u0000\u0000"+
		"\u00bc\u00bd\u0003\u001e\u000f\u0000\u00bd\u00be\u0005&\u0000\u0000\u00be"+
		"\u00c9\u0001\u0000\u0000\u0000\u00bf\u00c0\u0005$\u0000\u0000\u00c0\u00c9"+
		"\u0003\u001e\u000f\u0006\u00c1\u00c6\u0005\u0019\u0000\u0000\u00c2\u00c7"+
		"\u00051\u0000\u0000\u00c3\u00c4\u0005%\u0000\u0000\u00c4\u00c5\u00051"+
		"\u0000\u0000\u00c5\u00c7\u0005&\u0000\u0000\u00c6\u00c2\u0001\u0000\u0000"+
		"\u0000\u00c6\u00c3\u0001\u0000\u0000\u0000\u00c7\u00c9\u0001\u0000\u0000"+
		"\u0000\u00c8\u00af\u0001\u0000\u0000\u0000\u00c8\u00b1\u0001\u0000\u0000"+
		"\u0000\u00c8\u00b2\u0001\u0000\u0000\u0000\u00c8\u00b3\u0001\u0000\u0000"+
		"\u0000\u00c8\u00b4\u0001\u0000\u0000\u0000\u00c8\u00bb\u0001\u0000\u0000"+
		"\u0000\u00c8\u00bf\u0001\u0000\u0000\u0000\u00c8\u00c1\u0001\u0000\u0000"+
		"\u0000\u00c9\u00d8\u0001\u0000\u0000\u0000\u00ca\u00cb\n\u0005\u0000\u0000"+
		"\u00cb\u00cc\u0007\u0003\u0000\u0000\u00cc\u00d7\u0003\u001e\u000f\u0006"+
		"\u00cd\u00ce\n\u0004\u0000\u0000\u00ce\u00cf\u0005)\u0000\u0000\u00cf"+
		"\u00d7\u0003\u001e\u000f\u0005\u00d0\u00d1\n\u0003\u0000\u0000\u00d1\u00d2"+
		"\u0005*\u0000\u0000\u00d2\u00d7\u0003\u001e\u000f\u0004\u00d3\u00d4\n"+
		"\u0002\u0000\u0000\u00d4\u00d5\u0007\u0004\u0000\u0000\u00d5\u00d7\u0003"+
		"\u001e\u000f\u0003\u00d6\u00ca\u0001\u0000\u0000\u0000\u00d6\u00cd\u0001"+
		"\u0000\u0000\u0000\u00d6\u00d0\u0001\u0000\u0000\u0000\u00d6\u00d3\u0001"+
		"\u0000\u0000\u0000\u00d7\u00da\u0001\u0000\u0000\u0000\u00d8\u00d6\u0001"+
		"\u0000\u0000\u0000\u00d8\u00d9\u0001\u0000\u0000\u0000\u00d9\u001f\u0001"+
		"\u0000\u0000\u0000\u00da\u00d8\u0001\u0000\u0000\u0000\u001a#+06;BFJO"+
		"UZ^dnz\u0082\u0087\u008d\u00a6\u00a8\u00ad\u00b9\u00c6\u00c8\u00d6\u00d8";
	public static final ATN _ATN =
		new ATNDeserializer().deserialize(_serializedATN.toCharArray());
	static {
		_decisionToDFA = new DFA[_ATN.getNumberOfDecisions()];
		for (int i = 0; i < _ATN.getNumberOfDecisions(); i++) {
			_decisionToDFA[i] = new DFA(_ATN.getDecisionState(i), i);
		}
	}
}