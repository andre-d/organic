; Test for isref(value)
.org 0x8000

SET A, label1

.if isref(label1)
label1:
    SET B, C
    SET A, .test
.test:
.end

.if isref(label2)
label2:
    SET C, B
.end