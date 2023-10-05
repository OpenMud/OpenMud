parser grammar DmmParser; // tiny version

options { tokenVocab=DmmLexer; }


dmm_module: 
    ( NEWLINE
    | stmt
    )* EOF;

stmt: stat;

stat
  : mapdecl
  | map_piece_decl
  ;

identifier_name
  : NAME
  ;

fieldinitexpr
  : INT
  | STRING
  ;
fieldinit: NAME ASSIGNMENT fieldinitexpr;
typeinit: LCURLY (fieldinit (SEMICOLON fieldinit)*)? RCURLY;
typename: SLASH NAME (SLASH NAME)*;
typedecl: name=typename typeinit?;
typelist: LPAREN typedecl (COMMA typedecl)* RPAREN;

map_piece_decl: id=STRING ASSIGNMENT typelist;

mapdecl: LPAREN x=INT COMMA y=INT COMMA z=INT RPAREN
   ASSIGNMENT LCURLY STRING RCURLY;