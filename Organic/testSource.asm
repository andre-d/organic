SET A, B
label1:
SET B, C
.relocate
SET A, label2
label2:
SET B, 10
SET C, label1
SET X, label2
JSR label1
ADD B, 10
SET label2, label1
.endrelocate