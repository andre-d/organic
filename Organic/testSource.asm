; Test for isref(value)

SET A, label1

.if isref(label1)
label1:
    SET B, C
.end

.if isref(label2)
label2:
    SET C, B
.end