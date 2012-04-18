; Test file for .orgASM

#nolist

.list
	SET A, 10 + 7 * 4
	sEt a, 0
label1:
:label2
	SET A, B ; Comment "te'st"
	SET pop, A
	ADD A, B
label3:
invalid label:
	IFB A, Z
	MUL notaregister, b
label1: ; Duplicate
	DIV A,I
	SET PC, pop
	JSR label3