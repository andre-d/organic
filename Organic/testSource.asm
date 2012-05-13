; Test file for Organic

.macro test()
	SET A, B
	SET B, A
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
	test()