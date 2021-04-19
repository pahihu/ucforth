\ this is a copy of the ucforth number code.
\ it os compiled into ansi systems so the cross compiler
\ has decent number input code. It only upgraqdes
\ the ]T compiler. Interpreted code is limited to the
\ standard supported by the host system.

\
\ This version only deals with the case when the host system is in interpreting state.
\ Comments below relate the uCforth version.
\
\		(NUMBER) traces down the number conversion list until the output flag is true.
\		The words conversion words have the form:
\		: name \ interpret ( add num -- ?? flag)
\		       \ compile   ( add num -- flag)
\			action
\		;
\
\		In other words the entry is reposible for putting the number onto the stack
\		in interpret state and compiling the literal in compile state. Numbers can now deal with
\		multicell conversions.
\
\		Application programs may add to the list.
\


\ number conversion words list
ram_variable %conversion_list

\ descibe an entry
	zero
		DUP CONSTANT _#cl_link CELL+
		DUP CONSTANT _#cl_xt   CELL+
		DROP

: add_number_conversion ( xt --)
	%conversion_list @  HERE %conversion_list !
	, \ xt(--
	, \ (--
;


: _finish_single_cell_conversion ( ud1 addr count -- single true|false)
	0=
	IF \ valid
		DROP
		d>s
		TRUE    \ num true(--
		EXIT
	ELSE
		2DROP
		DROP
		FALSE
	THEN
;

: _finish_double_cell_conversion ( ud1 addr count --double true|false)
	0=  \ ud1 addr flag (--
	IF \ valid
		DROP
		TRUE    \ num true(--
		EXIT
	ELSE
		2DROP
		DROP
		FALSE
	THEN
;


: _+pointer   ( addr n -- addr+ n-)
	SWAP CHAR+ SWAP 1-
;

\ -nnnnnnn nnnnnnn

: simple_number \ interpret ( add num  -- num true|false)
	zero zero 2SWAP \ ud1 addr count(--
	OVER C@ [CHAR] - = DUP >R IF
		_+pointer
	THEN
	>NUMBER
	R> IF
		 2SWAP DNEGATE 2SWAP
	THEN                      \ ud1 addr count(--
	_finish_single_cell_conversion
;

\ -nnnnnnnl nnnnnnnL
: simple_double_number \ interpret ( addr num -- num true|false)
		zero zero 2SWAP    \ ud1 addr count(--
		OVER C@ [CHAR] - = DUP >R IF
			_+pointer
		THEN
		>NUMBER
	R> IF
		 2SWAP DNEGATE 2SWAP
	THEN                      \ ud1 addr count(--
		DUP IF
			OVER C@ DUP [CHAR] L <>  SWAP [CHAR] l <> AND IF
				4drop
				FALSE
				EXIT
			THEN
		ELSE
				4drop
				FALSE
				EXIT
		THEN
		_+pointer   \ ud1 addr count(--
		_finish_double_cell_conversion
;

\ hex number
\ -0xnnnnnn -0Xnnnnnnn 0Xnnnnnn 0xnnnnnnn
: hex_number \ interpret ( addr count -- num true|false)
		zero zero 2SWAP   \ ud1 addr count(--
		OVER C@ [CHAR] - = DUP >R IF
			_+pointer
		THEN
		OVER C@ [CHAR] 0 <> IF \ ud1 addr count (--
			4drop
			FALSE
			r> drop
			EXIT
		THEN
		_+pointer  \ ud1 addr count(--

		OVER C@ DUP [CHAR] X <>  SWAP [CHAR] x <> AND IF
			4drop
			FALSE
			r> drop
			EXIT
		THEN
		_+pointer  \ ud1 addr count(--
		\ set hex
		BASE @ >R HEX
		>NUMBER
		\ restore old base
		R> BASE !
	R> IF
		 2SWAP DNEGATE 2SWAP
	THEN                      \ ud1 addr count(--
		_finish_single_cell_conversion
;

\ double hex number
\ -0xnnnnnnL -0Xnnnnnnnl 0XnnnnnnL 0xnnnnnnnl etc.
: hex_double_number \ interpret ( addr count -- num true|false)
		zero zero 2SWAP   \ ud1 addr count(--
		OVER C@ [CHAR] - = DUP >R IF
			_+pointer
		THEN
		OVER C@ [CHAR] 0 <> IF \ ud1 addr count (--
			4drop
			FALSE
			r> drop
			EXIT
		THEN
		_+pointer  \ ud1 addr count(--

		OVER C@ DUP [CHAR] X <>  SWAP [CHAR] x <> AND IF
			4drop
			FALSE
			r> drop
			EXIT
		THEN
		_+pointer  \ ud1 addr count(--
		\ set hex
		BASE @ >R HEX
		>NUMBER
		\ restore old base
		R> BASE !
	R> IF
		 2SWAP DNEGATE 2SWAP
	THEN                      \ ud1 addr count(--
		DUP 0= IF
			4drop
			FALSE
			EXIT
		THEN
		OVER C@ DUP [CHAR] L <>  SWAP [CHAR] l <> AND IF
			4drop
			FALSE
			EXIT
		THEN
		_+pointer  \ ud1 addr count(--
		_finish_double_cell_conversion
;

\
\ -$nnnnnn -$nnnnnnn $nnnnnn $nnnnnnn
: hex$_number \ interpret ( addr count -- num true|false)
	zero zero 2SWAP   \ ud1 addr count(--
	OVER C@ [CHAR] - = DUP >R IF
		_+pointer
	THEN
	OVER C@ [CHAR] $ <> IF \ ud1 addr count (--
		4drop
		FALSE
		r> drop
		EXIT
	THEN
	_+pointer  \ ud1 addr count(--

	\ set hex
	BASE @ >R HEX
	>NUMBER
	\ restore old base
	R> BASE !
	R> IF
		 2SWAP DNEGATE 2SWAP
	THEN                      \ ud1 addr count(--
	_finish_single_cell_conversion
;


\ -$nnnnnnL -$nnnnnnnl $nnnnnnL $nnnnnnnl etc.
: hex$_double_number \ interpret ( addr count -- num true|false)
		zero zero 2SWAP   \ ud1 addr count(--
		OVER C@ [CHAR] - = DUP >R IF
			_+pointer
		THEN
		OVER C@ [CHAR] $ <> IF \ ud1 addr count (--
			4drop
			FALSE
			r> drop
			EXIT
		THEN
		_+pointer  \ ud1 addr count(--

		\ set hex
		BASE @ >R HEX
		>NUMBER
		\ restore old base
		R> BASE !
    	R> IF
    		 2SWAP DNEGATE 2SWAP
    	THEN                      \ ud1 addr count(--
		DUP 0= IF
			4drop
			FALSE
			EXIT
		THEN
		OVER C@ DUP [CHAR] L <>  SWAP [CHAR] l <> AND IF
			4drop
			FALSE
			EXIT
		THEN
		_+pointer  \ ud1 addr count(--
		_finish_double_cell_conversion
;

\ -#nnnnnn -#nnnnnnn #nnnnnn #nnnnnnn
: decimal_number \ interpret ( addr count -- num true|false)
		zero zero 2SWAP   \ ud1 addr count(--
		OVER C@ [CHAR] - = DUP >R IF
			_+pointer
		THEN
		OVER C@ [CHAR] # <> IF \ ud1 addr count (--
			4drop
			FALSE
			r> drop
			EXIT
		THEN
		_+pointer  \ ud1 addr count(--

		\ set decimal
		BASE @ >R DECIMAL
		>NUMBER
		\ restore old base
		R> BASE !
    	R> IF
    		 2SWAP DNEGATE 2SWAP
    	THEN                      \ ud1 addr count(--
		_finish_single_cell_conversion
;


\ -#nnnnnnL -#nnnnnnnl #nnnnnnL #nnnnnnnl etc.
: decimal_double_number \ interpret ( addr num --num true|false)
	             \ compile   ( addr num -- flag)
		zero zero 2SWAP   \ ud1 addr count(--
		OVER C@ [CHAR] - = DUP >R IF
			_+pointer
		THEN
		OVER C@ [CHAR] # <> IF \ ud1 addr count (--
			4drop
			FALSE
			r> drop
			EXIT
		THEN
		_+pointer  \ ud1 addr count(--

		\ set decimal
		BASE @ >R DECIMAL
		>NUMBER
		\ restore old base
		R> BASE !
    	R> IF
    		 2SWAP DNEGATE 2SWAP
    	THEN                      \ ud1 addr count(--
		DUP 0= IF
			4drop
			FALSE
			EXIT
		THEN
		OVER C@ DUP [CHAR] L <>  SWAP [CHAR] l <> AND IF
			4drop
			FALSE
			EXIT
		THEN
		_+pointer  \ ud1 addr count(--
		_finish_double_cell_conversion
	;

\ -%nnnnnnn %nnnnnn
: binary_number \ interpret ( addr num -- num true|false)
		zero zero 2SWAP   \ ud1 addr count(--
		OVER C@ [CHAR] - = DUP >R IF
			_+pointer
		THEN
		OVER C@ [CHAR] % <> IF \ ud1 addr count (--
			2DROP
			2DROP
			FALSE
			r> drop
			EXIT
		THEN
		_+pointer  \ ud1 addr count(--

		\ set binary
		BASE @ >R binary
		>NUMBER
		\ restore old base
		R> BASE !
    	R> IF
    		 2SWAP DNEGATE 2SWAP
    	THEN                      \ ud1 addr count(--
		_finish_single_cell_conversion
;

\ -%nnnnnnL -%nnnnnnnl %nnnnnnL %nnnnnnnl .
: binary_double_number \ interpret ( addr num -- num true|false)
		zero zero 2SWAP  \ ud1 addr count(--
		OVER C@ [CHAR] - = DUP >R IF
			_+pointer
		THEN
		OVER C@ [CHAR] % <> IF \ ud1 addr count (--
			2DROP
			2DROP
			FALSE
			r> drop
			EXIT
		THEN
		_+pointer  \ ud1 addr count(--

		\ set binary
		BASE @ >R binary
		>NUMBER
		\ restore old base
		R> BASE !
    	R> IF
    		 2SWAP DNEGATE 2SWAP
    	THEN                      \ ud1 addr count(--
		DUP 0= IF
			4drop
			FALSE
			EXIT
		THEN
		OVER C@ DUP [CHAR] L <>  SWAP [CHAR] l <> AND IF
			4drop
			FALSE
			EXIT
		THEN
		_+pointer  \ ud1 addr count(--
		_finish_double_cell_conversion
	;


\ 'c'
: single_character \ interpret ( addr count -- num true|false)
		\ addr count(--
		OVER C@ [CHAR] ' <> IF \ addr count (--
			2DROP
			FALSE
			EXIT
		THEN

		\ The character
		_+pointer   \ addr count(--

		OVER C@ >R

		_+pointer
		OVER C@ [CHAR] ' <>  IF
			2DROP
			FALSE
			r> drop
			EXIT
		THEN      \ addr count(--
		_+pointer
		R> zero 2SWAP
		_finish_single_cell_conversion
;

\ '^c'
: control_character \ interpret ( addr count -- num true|false)
	                    \ compile   ( addr count -- flag)
		                          \ addr count(--
		OVER C@ [CHAR] ' <> IF \ addr count (--
			2DROP
			FALSE
			EXIT
		THEN
		_+pointer
		OVER C@ [CHAR] ^ <> IF \ addr count (--
			2DROP
			FALSE
			EXIT
		THEN
		_+pointer

		OVER C@ $1F AND >R

		_+pointer      \ addr count(--
		OVER C@ [CHAR] ' <>  IF
			2DROP
			FALSE
			r> drop
			EXIT
		THEN      \ addr count(--
		_+pointer   \ addr count(--
		R> zero 2SWAP
		_finish_single_cell_conversion
;



\ First are tried last. This allows user to override default behavior.

	' control_character       add_number_conversion
	' single_character        add_number_conversion
	' binary_double_number    add_number_conversion
	' decimal_double_number   add_number_conversion
	' hex$_double_number      add_number_conversion
	' hex_double_number       add_number_conversion
	' simple_double_number    add_number_conversion
	' binary_number           add_number_conversion
	' decimal_number          add_number_conversion
	' hex$_number             add_number_conversion
	' hex_number              add_number_conversion
 	' simple_number           add_number_conversion



	: Snumber { W^ %addr W^ %num -- ( ?? ) }
		%conversion_list
		BEGIN
			@ DUP   \ list (--
		WHILE
			>R
			%addr @ %num @
			R@ _#cl_xt + @
			execute  \ ?? flag (--
			IF \ success
				r> drop EXIT
			THEN
			R>
		REPEAT
		\ Get to here all is lost
		TRUE ABORT" Can't convert to number"
	;

	: $number ( $ -- ?? )
		COUNT Snumber
	;

