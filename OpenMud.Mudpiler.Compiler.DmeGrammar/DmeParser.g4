parser grammar DmeParser; // tiny version

options { tokenVocab=DmeLexer; }

dmlDocument
    : text*
    ;

text
    : SHARP directive NEW_LINE*
    | code_block
    | comment_block
    ;

comment_block
    : BEGIN_MULTILINE_COMMENT comment_contents* END_MULTILINE_COMMENT
    | LINE_COMMENT
    ;

comment_contents
    : COMMENT
    | BEGIN_NESTED_MULTILINE_COMMENT comment_contents* END_MULTILINE_COMMENT
    ;

string
    : STRING_BEGIN string_contents* STRING_END
    | MULTILINE_STRING_BEGIN string_contents* MULTILINE_STRING_END;

resource:
    RESOURCE_BEGIN RESOURCE_CONTENTS* RESOURCE_END;

string_contents
    : STRING_CONTENTS #string_contents_literal
    | string_expression #string_contents_expression
    | string_expression_begin END_CODE_EXPR #string_contents_placeholder
    ;

string_expression
    : string_expression_begin code_block END_CODE_EXPR
    ;

string_expression_begin
    : BEGIN_STRING_EXPRESSION
    | BEGIN_ML_STRING_EXPRESSION
    ;

code_expr
    : START_CODE_EXPR code* END_CODE_EXPR
    ;

code_block: code+;

code
    : code_literal+
    | START_CODE_EXPR
    | END_CODE_EXPR
    | string
    | resource
    ;

code_literal
    : CODE
    | START_CODE_EXPR
    | END_CODE_EXPR
    ;

directive
    : (IMPORT | INCLUDE) directive_text     #preprocessorImport
    | IF preprocessor_expression            #preprocessorConditional
    | ELIF preprocessor_expression          #preprocessorConditional
    | ELSE                                  #preprocessorConditional
    | ENDIF                                 #preprocessorConditional
    | IFDEF CONDITIONAL_SYMBOL              #preprocessorDef
    | IFNDEF CONDITIONAL_SYMBOL             #preprocessorDef
    | UNDEF CONDITIONAL_SYMBOL              #preprocessorDef
    | PRAGMA directive_text                           #preprocessorPragma
    | ERROR directive_text                            #preprocessorError
    | DEFINE CONDITIONAL_SYMBOL directive_text?       #preprocessorDefine
    ;

directive_text
    : TEXT+
    ;

preprocessor_expression
    : TRUE                                                                   #preprocessorConstant
    | FALSE                                                                  #preprocessorConstant
    | DECIMAL_LITERAL                                                        #preprocessorConstant
    | DIRECTIVE_STRING                                                       #preprocessorConstant
    | CONDITIONAL_SYMBOL (LPAREN preprocessor_expression RPAREN)?            #preprocessorConditionalSymbol
    | LPAREN preprocessor_expression RPAREN                                  #preprocessorParenthesis
    | BANG preprocessor_expression                                           #preprocessorNot
    | preprocessor_expression op=(EQUAL | NOTEQUAL) preprocessor_expression  #preprocessorBinary
    | preprocessor_expression op=AND preprocessor_expression                 #preprocessorBinary
    | preprocessor_expression op=OR preprocessor_expression                  #preprocessorBinary
    | preprocessor_expression op=(LT | GT | LE | GE) preprocessor_expression #preprocessorBinary
    | DEFINED (CONDITIONAL_SYMBOL | LPAREN CONDITIONAL_SYMBOL RPAREN)         #preprocessorDefined
    ;