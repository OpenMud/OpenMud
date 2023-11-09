lexer grammar DmeLexer;

channels { COMMENTS_CHANNEL }

SHARP:                    '#'     -> mode(DIRECTIVE_MODE);
STRING_BEGIN:             '"'     -> pushMode(STRING_MODE);
MULTILINE_STRING_BEGIN:   '{"'     -> pushMode(STRING_MODE);
RESOURCE_BEGIN:           '\''     -> pushMode(RESOURCE_MODE);
LINE_COMMENT:             '//' ~[\r\n\f]*;
BEGIN_MULTILINE_COMMENT:  '/*' -> pushMode(MULTILINE_COMMENT_MODE);        
CODE_END: [\r\n\f]+ -> type(CODE);


CURLY_BRACE_OPEN: '{'  -> type(CODE);
CURLY_BRACE_CLOSE: '}' -> type(CODE);

START_CODE_EXPR: '[' -> pushMode(DEFAULT_MODE);
END_CODE_EXPR: ']' -> popMode;
CODE_DIV: '/' -> type(CODE);
CODE: ~[\r\n\f[\]"'#/{}]+;

mode MULTILINE_COMMENT_MODE;
END_MULTILINE_COMMENT: '*/' -> popMode;
BEGIN_NESTED_MULTILINE_COMMENT: '/*' -> pushMode(MULTILINE_COMMENT_MODE);
COMMENT_STAR: '*' -> type(COMMENT);
COMMENT_SLASH: '/' -> type(COMMENT);
COMMENT: ~[/*]+;


mode STRING_MODE;
STRING_END_ESCAPE: '\\' '"' -> type(STRING_CONTENTS);
STRING_EXPRESSION_ESCAPE: '\\' '['  -> type(STRING_CONTENTS);
STRING_EXPRESSION_ESCAPE_ESCAPE: '\\' '\\'  -> type(STRING_CONTENTS);
STRING_END: '"' -> popMode;
MULTILINE_STRING_END: '"}' -> popMode;
STRING_MULTILINE_END_ESCAPE: '}' -> type(STRING_CONTENTS);
BEGIN_STRING_EXPRESSION: '[' -> pushMode(DEFAULT_MODE);
STRING_CONTENTS: ~["[\\}]+;


mode RESOURCE_MODE;
RESOURCE_END_ESCAPE: '\\' '\'' -> type(RESOURCE_CONTENTS);
RESOURCE_ESCAPE_ESCAPE: '\\' '\\' -> type(RESOURCE_CONTENTS);
RESOURCE_END: '\'' -> popMode;
RESOURCE_CONTENTS: ~['[\\]+;

mode DIRECTIVE_MODE;

IMPORT:  'import' [ \t]+ -> mode(DIRECTIVE_TEXT);
INCLUDE: 'include' [ \t]+ -> mode(DIRECTIVE_TEXT);
PRAGMA:  'pragma' -> mode(DIRECTIVE_TEXT);

DEFINE:  'define' [ \t]+ -> mode(DIRECTIVE_DEFINE);
DEFINED: 'defined';
IF:      'if';
ELIF:    'elif';
ELSE:    'else';
UNDEF:   'undef';
IFDEF:   'ifdef';
IFNDEF:  'ifndef';
ENDIF:   'endif';
TRUE:     T R U E;
FALSE:    F A L S E;
ERROR:   'error' -> mode(DIRECTIVE_TEXT);

BANG:             '!' ;
LPAREN:           '(' ;
RPAREN:           ')' ;
EQUAL:            '==';
NOTEQUAL:         '!=';
AND:              '&&';
OR:               '||';
LT:               '<' ;
GT:               '>' ;
LE:               '<=';
GE:               '>=';

DIRECTIVE_WHITESPACES:      [ \t]+                           -> channel(HIDDEN);
DIRECTIVE_STRING:           StringFragment;
CONDITIONAL_SYMBOL:         LETTER (LETTER | [0-9])*;
DECIMAL_LITERAL:            [0-9]+;
FLOAT:                      ([0-9]+ '.' [0-9]* | '.' [0-9]+);
NEW_LINE:                   '\r'? '\n'                       -> mode(DEFAULT_MODE);
DIRECITVE_COMMENT:          '/*' .*? '*/'                    -> channel(COMMENTS_CHANNEL);
DIRECITVE_LINE_COMMENT:     '//' ~[\r\n]*                    -> channel(COMMENTS_CHANNEL);
DIRECITVE_NEW_LINE:         '\\' '\r'? '\n'                  -> channel(HIDDEN);

mode DIRECTIVE_DEFINE;

DIRECTIVE_DEFINE_CONDITIONAL_SYMBOL: LETTER (LETTER | [0-9])* ('(' (LETTER | [0-9,. \t])* ')')? -> type(CONDITIONAL_SYMBOL), mode(DIRECTIVE_TEXT);

mode DIRECTIVE_TEXT;

DIRECITVE_TEXT_NEW_LINE:         '\\' '\r'? '\n'  -> channel(HIDDEN);
BACK_SLASH_ESCAPE:               '\\' .           -> type(TEXT);
TEXT_NEW_LINE:                   '\r'? '\n'       -> type(NEW_LINE), mode(DEFAULT_MODE);
DIRECTIVE_COMMENT:               '/*' .*? '*/'    -> channel(COMMENTS_CHANNEL), type(DIRECITVE_COMMENT);
DIRECTIVE_LINE_COMMENT:          '//' ~[\r\n]*    -> channel(COMMENTS_CHANNEL), type(DIRECITVE_LINE_COMMENT);
DIRECTIVE_SLASH:                 '/'              -> type(TEXT);
TEXT:                            ~[\r\n\\/]+;

fragment
EscapeSequence
    : '\\' ('b'|'t'|'n'|'f'|'r'|'"'|'\''|'\\')
    | OctalEscape
    | UnicodeEscape
    ;

fragment
OctalEscape
    :   '\\' [0-3] [0-7] [0-7]
    |   '\\' [0-7] [0-7]
    |   '\\' [0-7]
    ;

fragment
UnicodeEscape
    :   '\\' 'u' HexDigit HexDigit HexDigit HexDigit
    ;

fragment HexDigit:          [0-9a-fA-F];

fragment
StringFragment: '"' (~('\\' | '"') | '\\' .)* '"';

fragment LETTER
    : [$A-Za-z_]
    | ~[\u0000-\u00FF\uD800-\uDBFF]
    | [\uD800-\uDBFF] [\uDC00-\uDFFF]
    | [\u00E9]
    ;

fragment A: [aA];
fragment B: [bB];
fragment C: [cC];
fragment D: [dD];
fragment E: [eE];
fragment F: [fF];
fragment G: [gG];
fragment H: [hH];
fragment I: [iI];
fragment J: [jJ];
fragment K: [kK];
fragment L: [lL];
fragment M: [mM];
fragment N: [nN];
fragment O: [oO];
fragment P: [pP];
fragment Q: [qQ];
fragment R: [rR];
fragment S: [sS];
fragment T: [tT];
fragment U: [uU];
fragment V: [vV];
fragment W: [wW];
fragment X: [xX];
fragment Y: [yY];
fragment Z: [zZ];