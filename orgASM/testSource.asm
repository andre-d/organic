; Test file for .orgASM
.equ TEST 14
TEST2 .equ 18
TEST .equ 12 ; Duplicate

.macro test(param1, param2)
	SET param1, param2
	ADD param1, param2
.endmacro

.macro test2(param1, param2) {
	.if param1 === A
	SUB param1, param2
	.else
	ADD param1, param2
	.end
}

	test2(A, B)

	test2(B, A)

.org 5

	SET A, {SET B, A}
	SET B, A

#nolist
	ADD B, A
	SET PC, 0x8000
.list
#include "testInclude.asm"
	SET A, 10 + 'b' - 0b101101 * 0x14 / (0o7 << TEST) | (4 >> (10 & 13)) ^ ~10
	SET PC, label1 ; Forward-referencing
label1:
:label2
	SET A,B ; Comment "te'st"
	SET pop, A
	JSR $++
	SET A,    [B]
$:
	SET B	, [0]
	SET C, [A+2]
	ADD A, B
	.org 0x8000
$:
	SET A, $+0x20
label3:
invalid label:
	IFB A, Z
	MUL notaregister, b
	INVALIDOPCODE A, Z
label1: ; Duplicate
	DIV A,I
	SET PC, pop
	JSR label3
	SET A, A
	SET 10, A
.equ testValue 10
.if testValue == 9
	SET A, B
.elseif
	SET B, A
.end
#ifdef TEST
	SET A, 1
#end
#ifdef NOTDEFINED
	SET A, 0
#end
	.dat 0, 1, 0x2, 0x03, 'b', "Hello\nworld!"

; Test for new features
	DVI A, 10
	ASR A, 2
	IFC A, 2
	IFG A, 2
	IFA A, 2
	IFL A, 2
	IFU A, 2
	IFE EX, 10
	INT 10
	ING A
	INS A
	HWN A
	HWQ A
	HWI A

.org 0x1000
.align 0x1005
.fill 0x10, 0x20
.ascii "Hello, world!"
.asciiz "Hello, world!"
.asciip "Hello, world!"