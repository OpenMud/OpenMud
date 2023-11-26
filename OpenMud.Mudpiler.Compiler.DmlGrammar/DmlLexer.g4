/*
 * lexer rules
 */

//Misc Support Rules
lexer grammar DmlLexer;

channels {
    LineDirectiveChannel
}

RESOURCE
  : RESOURCE_LITERAL
  ;

STRING
 : STRING_LITERAL
 ;

NUMBER
 : INTEGER
 ;

DECIMAL
 : ([0-9] ([0-9])*)? '.' [0-9] ([0-9])*
 ;

SRC_SET_IN: 'set'[\t ]+'src'[\t ]*('in'|'=')[\t ]*;


INTEGER
 : DECIMAL_INTEGER
 ;

NEWLINE
 : ( '\r'? '\n' | '\r' | '\f') SPACES?
 ;
 
CALL:         'call';

NULL:         'null';

PICK:         'pick';
PROB:         'prob';

GOTO:         'goto';

USR:          'usr';
GROUP:        'group';
LOC:          'loc';
CONTENTS:     'contents';
WORLD:        'world';
CLIENTS:      'clients';

OVIEW:       'oview';
VIEW:        'view';

SPAWN:       'spawn';

DEL:         'del';
LIST:        'list';
ISTYPE:      'istype';
VAR:         'var';
AS:          'as';
RETURN:      'return';
SET:         'set';
NEW:         'new';
SET_IN:      'in';

FOR:         'for';
IF:          'if';
ELSE:        'else';
SWITCH:      'switch';

BREAK:       'break';
CONTINUE:    'continue';
TO:          'to';
DO:          'do';
WHILE:       'while';

OPERATOR:    'operator';

SYMBOL_POUNDLINE: '#line' ' '+ [0-9]+ ' '+ '"' ('\\"'|.)*? '"' -> channel(LineDirectiveChannel);

FWD_SLASH:                '/';

OPEN_BRACKET:             '[';
CLOSE_BRACKET:            ']';
OPEN_PARENS:              '(';
CLOSE_PARENS:             ')';
OPEN_BRACE:               '{';
CLOSE_BRACE:              '}';

DOT:                      '.';
COMMA:                    ',';
COLON:                    ':';
SEMICOLON:                ';';
PLUS:                     '+';
MINUS:                    '-';
STAR:                     '*';
PERCENT:                  '%';
INT_PERCENT:              '%%';
AMP:                      '&';
BITWISE_OR:               '|';
CARET:                    '^';
BANG:                     '!';
TILDE:                    '~';
ASSIGNMENT:               '=';
COPYINTO_ASSIGNMENT:      ':=';
LT:                       '<';
GT:                       '>';
INTERR:                   '?';
OP_INC:                   '++';
OP_DEC:                   '--';
OP_AND:                   '&&';
OP_OR:                    '||';
OP_PTR:                   '->';
OP_EQ:                    '==';
OP_NE:                    '!=';
OP_EQUIV:                 '~=';
OP_POWER:                 '**';
OP_LE:                    '<=';
OP_GE:                    '>=';
OP_ADD_ASSIGNMENT:        '+=';
OP_SUB_ASSIGNMENT:        '-=';
OP_MULT_ASSIGNMENT:       '*=';
OP_DIV_ASSIGNMENT:        '/=';
OP_MOD_ASSIGNMENT:        '%=';
OP_INT_MOD_ASSIGNMENT:    '%%=';
OP_AND_ASSIGNMENT:        '&=';
OP_OR_ASSIGNMENT:         '|=';
OP_XOR_ASSIGNMENT:        '^=';
OP_LEFT_SHIFT:            '<<';
OP_RIGHT_SHIFT:            '>>';
OP_LEFT_SHIFT_ASSIGNMENT: '<<=';
OP_RIGHT_SHIFT_ASSIGNMENT:'>>=';

OP_SUPER:                 '..';

NAME
 : ID_START ID_CONTINUE*
 ;

RESOURCE_LITERAL
 : '\'' ('\\"'|'\\\''|'\\\\'|.)*? '\''
 ;

STRING_LITERAL
 : '"' ('\\"'|'\\\\'|.)*? '"'
 ;

DECIMAL_INTEGER
 : NON_ZERO_DIGIT DIGIT*
 | '0'+
 ;

SCINOTATION_NUMBER
 : NON_ZERO_DIGIT DIGIT* 'e' DIGIT*
 ;

BlockComment 
    : '/*' (BlockComment | .)*? '*/' -> skip
    ;
SKIP_
 : ( SPACES | COMMENT | LINE_JOINING | BlockComment ) -> skip
 ;

UNKNOWN_CHAR
 : .
 ;


/* 
 * fragments 
 */

fragment NON_ZERO_DIGIT
 : [1-9]
 ;

fragment DIGIT
 : [0-9]
 ;

fragment SPACES
 : [ \t]+
 ;

fragment COMMENT
 : '//' ~[\r\n\f]*
 ;

fragment LINE_JOINING
 : '\\' SPACES? ( '\r'? '\n' | '\r' | '\f' )
 ;

fragment ID_START
 : '_'
 | [A-Z]
 | [a-z]
 ;

fragment ID_CONTINUE
 : ID_START
 | [0-9]
 ;
