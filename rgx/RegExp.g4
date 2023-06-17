grammar RegExp;


PAR_L: '(';
PAR_R: ')';
SQR_L: '[';
SQR_R: ']';
DIA_L: '<';
DIA_R: '>';
ACC_L: '{';
ACC_R: '}';

DASH: '-';
PLUS: '+';
STAR: '*';
POW: '^';
DOLLAR: '$';
EXCLAMATION: '!';
QUESTION: '?';
ESCAPE: '\\';
COLON: ':';
EQUALS: '=';
OR: '|';
DOT: '.';
COMMA: ',';
ALPHA: [a-zA-Z];
NUMERIC: [0-9];
ALPHANUMERIC: ALPHA | NUMERIC;
DWORD: ALPHANUMERIC+;
ANY: ALPHANUMERIC | ESCAPE | .;
WORD: ANY+;

group: PAR_L (QUESTION (COLON | EQUALS | EXCLAMATION | (DIA_L name=WORD DIA_R)))? atom PAR_R;

set_content
    : ANY           #char
    | ANY DASH ANY  #cRange
;
set: SQR_L POW? set_content+ SQR_R;

escapeSeq
    : ESCAPE 't'        #tab
    | ESCAPE 'r'        #cr
    | ESCAPE 'n'        #lf
    | ESCAPE ('w'|'W')  #word
    | ESCAPE ('d'|'D')  #digit
    | ESCAPE ('s'|'S')  #whitespace
    //| ESCAPE ('b'|'B')#wordBoundary
    | ESCAPE NUMERIC    #reference
;

atom
    : POW atom                                               #startOfInput
    | atom DOLLAR                                            #endOfInput
    | group                                                  #capturingGroup
    | set                                                    #characterSet
    | DOT                                                    #anyChar
    | escapeSeq                                              #escapeSequence
    | atom STAR                                              #many
    | atom PLUS                                              #any
    | atom QUESTION                                          #opt
    | atom ACC_L lower=NUMERIC? COMMA upper=NUMERIC? ACC_R   #range
;

WS: [ \r\n\t] -> channel(HIDDEN);
UNMATCHED: . ; // raise errors
