\ float test
: ??= = not ABORT" Problems" ;

HEX

.( difficult adds ) CR send
7FFFFFFF+0 1+0 F+ F>D 80000000L D= TRUE ??=
-7FFFFFFF+0 -1+0 F+ F>D -80000000L D= TRUE ??=

.(  division sign ) CR send
-80000000+0 2+0 F/ F>D -40000000L D= TRUE ??=
-80000000+0 -2+0 F/ F>D 40000000L D= TRUE ??=
 80000000+0 2+0 F/ F>D  40000000L D= TRUE ??=
 80000000+0 -2+0 F/ F>D -40000000L D= TRUE ??=

.( rounding) CR send
7FFFFFFF+ 2+0 F/ F>D 40000000L D= TRUE ??=
7FFFFFFE+ 2+0 F/ F>D 3FFFFFFFL D= TRUE ??=


.( allocate some memory) CR send
ram_variable %a $80 ram_allot
ram_variable %b $80 ram_allot
ram_variable %c $80 ram_allot

.( store some floats) CR send
DECIMAL
101E2 %a            F!
102E2 %a 1 FLOATS + F!
103E2 %a 2 FLOATS + F!
104E2 %a 3 FLOATS + F!
105E2 %a 4 FLOATS + F!
106E2 %a 5 FLOATS + F!
107E2 %a 6 FLOATS + F!
108E2 %a 7 FLOATS + F!
109E2 %a 8 FLOATS + F!

1E2 %b            F!
2E2 %b 1 FLOATS + F!
3E2 %b 2 FLOATS + F!
4E2 %b 3 FLOATS + F!
5E2 %b 4 FLOATS + F!
6E2 %b 5 FLOATS + F!
7E2 %b 6 FLOATS + F!
8E2 %b 7 FLOATS + F!
9E2 %b 8 FLOATS + F!

.( vector mult) CR send
: tv*v
." va * vb = vc " CR send
%a %b %c 8 fv*fv
CR %a 8 fv. send
CR %b 8 fv. send
CR %c 8 fv. send
;

.( vector add ) CR send
: tv+v
." va + vb = vc " CR send
%a %b %c 8 fv+fv
CR %a 8 fv. send
CR %b 8 fv. send
CR %c 8 fv. send
;

.( vector subtract) CR send
: tv-v
." va - vb = vc " CR send
%a %b %c 8 fv-fv
CR %a 8 fv. send
CR %b 8 fv. send
CR %c 8 fv. send
;

.( vector by constant) CR send

: tv*c
." va * b = vc " CR send
%a %b FLOAT+ F@ %c 8
fv*f
CR %a 8 fv. send
CR %b FLOAT+ F@ FS. CR
CR %c 8 fv. send
;


.( vector constant +) CR send
: tv+f
." fa + b = vc" CR send

%a %b FLOAT+ F@ %c 8
fv+f
CR %a 8 fv. send
CR %b FLOAT+ F@ FS. CR
CR %c 8 fv. send
;
