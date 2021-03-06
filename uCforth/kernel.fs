\ Note::: If you are cross compiling on uClinux this file is not needed.
\ load kernel
.S .( stack on entry to kernel.fs) CR

\ tidy up for reload
\ ------------------

\ reduce search order
ONLY FORTH

\ see if empty is defined if so get rid of last cross compile dictionary entries
\ defined empty [IF]
\       empty
\ [THEN]

\ define a marker so we can get rid of this code on next cross compile
MARKER empty

\ HTML tags
\ ---------

\ uCforth is contained in HTML pages to make it simpler to publish
\ on the net. uCforth itself contains extensive html support. This file
\ allows one to add basic html support to gforth so that gforth can be used
\ to create an inital uCforth image.
\ Once uCforth is running, the uClinux system can be used for forth cross
\ compiles.

defined <html> 0= [IF]
	include ./htmltags.fs
[THEN]


\ extend gorth
\ ------------

\ The kernel source uses words that are native to uClinux and not
\ GFORTH, these are now defined.

\ for documentation purposes use 0= when stack it is a number
\ and not when you expect a flag.
: not 0= ;

\ new_buffer and kill_buffer and buffer
\ -------------------------------------
\ A word set designed to keep
\ the memory allocation address off the stack.
\
\ I suspect gforth has takn this further as a garbage collector is offered.

\ new_buffer saves the previous buffer address somewhere, allocates the new space
\ makes the address of the new space the value returned by buffer.
\ buffer is a ansi block word, but as uCforth does not support blocks this is
\ not an issue.
\ As I want this to be portable across forths knowledge of return stack useage
\ across words cannot be assumed. The buffer address is saved on a buffer stack instead.

8 cells CONSTANT #buffer_stack_bytes
VARIABLE %buffer_pointer
0 %buffer_pointer !
CREATE %buffer_stack #buffer_stack_bytes ALLOT

\ The current buffer address is stored here.
VARIABLE %buffer
0 %buffer !

\ return the current buffer address
: buffer %buffer @ ;

\ Push current buffer onto buffer stack, allocate new buffer and
\ make it current.
: get_buffer ( n --)
	%buffer_pointer @
	#buffer_stack_bytes < not ABORT" buffer stack overflow"
	buffer %buffer_stack %buffer_pointer @ + !
	cell %buffer_pointer +!
	allocate throw %buffer !
;

\ destroy current buffer, restore previous.
: kill_buffer ( --)
	buffer free throw
	[ cell NEGATE ] LITERAL %buffer_pointer +!
	%buffer_pointer @ 0< ABORT" buffer stack underflow"
	%buffer_stack %buffer_pointer @ + @ %buffer !
;


\ length required for a string buffer
$100 CONSTANT #$buffer

\ word to get a string buffer
: $get_buffer ( --)
	#$buffer get_buffer
;

\ $ words
\ -------
\
\ uClinux uses $ to indicate a counted string.

\ print a counted string
: $type ( $ --)
	COUNT TYPE
;

\ include a file descibed by a counted string
: $include ( $ --)
	COUNT included
;

\ take two counted strings and create a new counted string in a new buffer.
\ C! is the ANSI character storage word, there is no standard byte storage
\ words. I know the code could be faster, but I long ago decided clarity
\ was the issue
: $+ { W: $1 W: $2 W: addr W: num -- }
	\ get the total count
	$1 COUNT NIP
	$2 COUNT NIP +
	addr C!
	\ move first strings data into place
	$1 COUNT addr 1 CHARS + SWAP CMOVE
	\ move the second strings data into place
	$2 COUNT addr 1 CHARS + $1 COUNT NIP + SWAP CMOVE
;

\ convert bytes>chars
: bytes>chars ;

: $move ( $ addr n --)
	2 PICK COUNT 1 + CHARS
	NIP MIN
	\ $ addr n (--
	2DUP 2>R
	MOVE
	2R> bytes>chars 1- SWAP C!
;

\ base
\ -----
: binary
	02 BASE !
;
: octal 
	08 BASE !
;
\ vector
\ -------
\ given a xt table arranged count xt xt etc use the entry number
: @execute ( addr --??)
	@ DUP not ABORT" Zero vector address"
	EXECUTE
;
: vector ( number addr --??)
	SWAP OVER @ MIN \ num addr num
	CELLS + CELL+ @execute
;

\ In old forth systems complete dictionary search was slow.
\ zero gave a faster compile than 0
\ some of the code in this sytem is very old.
: zero 0 ;

\ joined up words
\ ---------------
: 4* 4 * ;
: 4+ 4 + ;
: 4/ 4 / ;
: 2+ 2 + ;


\ more stack manipulators
\ -----------------------
\ does something in gforth but not what I want
: jump ( a b c -- a b c a )
	2 pick
;

\ : -rot ( a b c -- c a b)
\	2 ROLL
\	2 ROLL
\ ;

: 4drop 2DROP 2DROP ;

\ other stuff
\ -----------

: 2**
	1 SWAP LSHIFT
;

\ checksum
\ ---------

\ the word heads are checksummed and the lists searched using the
\ checksum.

: make_table  ( --)
	$100 0 DO
		I  8 0 DO
			DUP 1 AND IF
				1 RSHIFT $0EDB88320 XOR
			ELSE
				1 RSHIFT
			THEN
		LOOP
		,
	LOOP
;


 CREATE checksum_table
 make_table

: checksum ( addr n -- value )
	-1 -rot
	OVER + SWAP ?DO
		DUP 8 RSHIFT   \ old new (--
		SWAP           \ new old (--
		I C@ XOR       \ new old+(--
		$0FF AND        \ new +(--
		4*             \ new offset(--
		checksum_table + @  \ new new_value(--
		XOR            \ new_value
	LOOP
	-1 XOR
;


: $checksum ( addr --n )
	COUNT
	checksum
;

\ unlink and link double
\ ----------------------
	: unlink_double ( link_addr --)
			DUP CELL+ SWAP  \ back_addr link_addr (--
			@ DUP IF        \ back_addr (link_addr) (--
				SWAP @              \ (link) (back) (--
				2DUP                \ (link) (back) (link) (back) (--
				!                   \ (link) (back) (--
				SWAP                \ (back) (link) (--
				CELL+
				!                   \ (--
			ELSE					\ set the link pointing to us to zero
				SWAP @ !            \ (--
			THEN
	;

	: link_double ( link_addr head --)
		2DUP                                   \ link_addr head link_addr head (--
		SWAP                                   \ link_addr head head link_addr (--
		\ back pointer for new link is the head
		CELL+ !                                \ link_addr head
		TUCK                                   \ head link_addr head (--
			@ DUP IF ( buffer linked into head already)
											   \  head link_addr (head) (--
				2DUP                           \  head link_addr (head) link_addr (head) (--
				\ fix up the back pointer of the following buffer
				CELL+                          \ ... link_addr back_point (--
				!                              \  head link_addr (head) (--
				OVER                           \  head link_addr (head) link_addr (--
				!                              \  head link_addr (--
				SWAP                           \  link_addr head (--
				!                              \  (--
			ELSE ( this is first)
											   \ head link_addr (head) (--
				OVER                           \ head link_addr  zero link_addr (--
				!                              \ head link_addr (--
				SWAP                           \ link_addr head (--
				!
			THEN                               \ (--

	;



\ shift operators
\ ------------------------------------------------------

\ is length equal to a power of 2
: ??asl ( asl length --)
	SWAP 2** <> ABORT" ?? asl error"
;

\ The highest number that is less than the input value and is a power of 2
	: >asl ( value -- asl_value)
		zero SWAP                 \ count value (--
		BEGIN
			1 RSHIFT
			DUP not IF
				DROP
				EXIT
			THEN
			SWAP 1+ SWAP
		AGAIN
	;




\ ram data areas
\ --------------

\ uCforth used differnt words to refer to differnt data areas.
\ each data area has a CREAT, ALLOT and HERE word set.
\ The cross compiler uses the ram word set, these have to be mapped into
\ the ansi standard words of CREATE, ALLOT, and HERE
\ The use of the ram set in application code is important as they add
\ to the bss segment. The bss length does not add to the bFLT file length.

: ram_create CREATE ;
: ram_allot ALLOT ;
: ram_variable VARIABLE ;
: ram_here HERE ;

\ word
\ ----------
\ Has to be redone for two reasons. 
\ 1) The Gforth version uses just above here
\    as the storeage region. This makes compiler 
\    extension ( which is all a xcompiler
\    is) a non starter.
\ 2) We support load from html &gt; and &lt; have to be
\    converted to > and <
CREATE %word_buffer #$buffer ALLOT
CREATE #&gt; ," &gt;"
CREATE #&lt; ," &lt;"
\ the codes are 4 characters long we we have to remove 3 as one
\ position is required for the code.
3 CONSTANT _#remove_chars
: remove_char_code ( addr n addr1 n1 char --)
	>R
	SWAP
	\ n1 addr1 (--
	DUP >R
	DUP _#remove_chars + \ n1 to from(--
	SWAP                 \ n1 from to(--
	ROT _#remove_chars - \ from to n1(--
	CMOVE
	\ addr n
	_#remove_chars -
	R> R> SWAP C!
;


\ it may look bad but remember most words have no codes
\ so first scan returns false and we never go around.
\ In other words the common case is fast, the not so common
\ a little slower.

        : alphi? ( char -- flag)
            DUP [CHAR] A < IF
                DROP FALSE EXIT
            THEN
            DUP [CHAR] z > IF
                DROP FALSE EXIT
            THEN
            DUP [CHAR] Z > OVER [CHAR] a < AND IF
                DROP FALSE EXIT
            THEN
            DROP TRUE
        ;

        : Slowercase ( addr n --)
                zero ?DO
                        DUP c@ DUP alphi? IF
                                $20 OR OVER c!
                        ELSE
                                DROP
                        THEN
                        1 CHARS +
                LOOP
                DROP
        ;


: WORD ( char --$)
	WORD \ addr
	%word_buffer #$buffer $move
	%word_buffer COUNT
	BEGIN
		2DUP #&gt; COUNT SEARCH IF
			\ addr n addr3 u3
			[CHAR] > remove_char_code
			FALSE
		ELSE
			2DROP
			TRUE
		THEN
	UNTIL
	BEGIN
		2DUP #&lt; COUNT SEARCH IF
			\ addr n addr3 u3
			[CHAR] < remove_char_code
			FALSE
		ELSE
			2DROP
			TRUE
		THEN
	UNTIL
	\ addr n(--
	%word_buffer C!
	DROP
	%word_buffer
;


\ some words to help HTML layout
\ ------------------------------
: &lt; < ;
: &gt; > ;

\ $number
\ ----------------------------------------------------------------------------
\ The ansi standard number system has to be extended to be of any use.
\ Unfortunatly there is no standard way of fixing up the host interpretor
\ but the cross compiler can use something reasonable
include ./number.fs


\ If the xcompiler code uses these everywhere it should.
\ Redefinition to a character operation is the solution.
: byte@ C@ ;
: byte! C! ;

\ .h
\ ---------------------------------------------------------------------------
\ tried to live without it, gave up.
DECIMAL
: .h ( n --)
	BASE @ >R HEX
	S>D <# # # # # 44 HOLD # # # # #> TYPE
	R> BASE ! SPACE ;


        \ conversion table; first 8 bytes contain the counted string
        \ the next 4 the character

HEX

\ remove html character seqence out of buffer.
\  --------------------------------------------
\

CREATE _ct
HERE
04 C, 26 C, 47 C, 54 C, 3B C, 00 C, 00 C, 00 C, 3E ,   \ GT
04 C, 26 C, 4C C, 54 C, 3B C, 00 C, 00 C, 00 C, 3C ,   \ LT
05 C, 26 C, 41 C, 4D C, 50 C, 3B C, 00 C, 00 C, 26 ,   \ AMP
06 C, 26 C, 51 C, 55 C, 4F C, 54 C, 3B C, 00 C, 22 ,   \ QUOT

HERE SWAP - CONSTANT _ct_length
0C CONSTANT _ct_entry_length

\ look for equality between two strings
: &= ( addr n addr n -- ( flag )
	jump <> IF
		DROP
		2DROP
		FALSE
		EXIT
	THEN
	\ addr n addr
	SWAP 0 DO
		\ addr1 addr2
		OVER I + C@ [ $20 -1 XOR ] LITERAL AND
		OVER I + C@ [ $20 -1 XOR ] LITERAL AND
		<> IF
			UNLOOP
			2DROP
			FALSE
			EXIT
		THEN
	[ 1 CHARS ] LITERAL +LOOP
	2DROP
	TRUE
;



: _convert_data ( addr n -- char true | false)
	CR
       _ct _ct_length + _ct  DO
                 2DUP I COUNT &= IF
                        2DROP
                        I [ 2 CELLS ] LITERAL + @
                        UNLOOP
                        TRUE
                        EXIT
                 THEN
        _ct_entry_length +LOOP
        \ no luck
        2DROP
        FALSE
;



\ this is called when a & is found. It forward scans to the ;
\ then sees if the enclosed characeters are a sequence. If
\ a sequence is found it is replaced with the single character
\ representing the sequence.

variable _%input_count
variable _%input_addr
variable _%test_count
variable _%test_addr

: _amp_token ( input_addr count -- count )
	DUP _%input_count ! _%test_count !
	DUP _%input_addr ! _%test_addr !
	BEGIN
		_%test_count @
	WHILE
		_%test_addr @ C@ [CHAR] ; = IF
			_%input_addr @
			_%input_count @ _%test_count @ -
			\ Allow for the ;
			1 +
			_convert_data

			.S ." out" CR
			IF
				\ char
				_%input_addr @ C!
				\ have to move rest of buffer data down
				_%test_addr @ 1 CHARS +   \ skip the ;
				_%input_addr @ 1 CHARS +  \ skipped the character result
				_%test_count @ 1 - CHARS  \ allow for the ; not counted
				MOVE
				\ %test_count has not been decremented for the ;
				\ that allows for the
				\ single character the sequence represents.
				_%test_count @
				EXIT
			THEN
			\ &unknown;
			_%input_count @
			EXIT
		THEN
		1 CHARS _%test_addr +!
		-1 _%test_count +!
	REPEAT
	\ ran out of characters
	\ no mod required
	_%input_count @
;


: html_string ( addr n --)
	OVER >R
	BEGIN
		DUP
	WHILE
		OVER C@ [CHAR] & = IF
			2DUP _amp_token NIP
		THEN
		SWAP 1 CHARS +
		SWAP 1 -
	REPEAT
	DROP R> -
;



\ send
\ ---------------------------------------------------------------------------
\ uCforth buffers output, send enpties the buffer.
: send ;

\ uClinux and Gforth need differnt mode values for create file
: new_file ( $ -- handle --)
	DUP COUNT DELETE-FILE DROP
	COUNT R/W CREATE-FILE THROW
;

\ we are now ready to start from where uClinux starts when it is used as the
\ host.
\ ----------------------------------------------------------------------------
include ./kernel.html


\ The last is a file that contains the lib and app.
\ It is there for ease of inital use.
bFLT ./uCforth/ucf_lib ./uCforth/ucf_app ./uCforth/ucf


