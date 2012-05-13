; Test file for Organic

.macro test(arg)
	SET A, B
	SET B, A
	arg
.endmacro

start:
	SET A, B
subroutine:
	SET B, A
	.loop:
		IFL 10, B
		SET PC, .loop
otherstuff:
	SET B, A
	_loop:
		IFL 10, B
		SET PC, _loop
end:
	.dat "woo!"
	test("SET I, J")
	.dat "hoo!"