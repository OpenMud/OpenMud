lexer grammar DmmLexer;

STRING
 : '"' ('\\"'|.|[\r\n])*? '"'
;

SEMICOLON:  ';';
ASSIGNMENT: '=';
COMMA:      ',';
QUOTE:      '"';
LPAREN:     '(';
RPAREN:     ')';
SLASH:      '/';
LCURLY:     '{';
RCURLY:     '}';
NAME:  [a-zA-Z_][a-zA-Z0-9_]*;
INT: [0-9]+;

WS: [ \t\f]+ -> skip;

NEWLINE: '\r'?'\n';