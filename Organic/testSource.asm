; Dump from BlueDAS DCPU-16 Assembler, version 0.13
; Source file: tests/short-literals.s
               SET A, -0x12c                           ; 7c01 fed4
               SET A, -3                               ; 7c01 fffd
               SET A, -2                               ; 7c01 fffe
               SET A, -1                               ; 8001
               SET A, 0xffff                           ; 8001
               SET A, 0                                ; 8401
               SET A, 1                                ; 8801
               SET A, -0xffff                          ; 8801
               SET A, 2                                ; 8c01
               SET A, 3                                ; 9001
               SET A, 4                                ; 9401
               SET A, 5                                ; 9801
               SET A, 0x1d                             ; f801
               SET A, 0x1c                             ; f401
               SET A, 0x1e                             ; fc01
               dat 0xfc01                              ; fc01
               SET A, 0x1f                             ; 7c01 001f
               SET A, 0x20                             ; 7c01 0020
               SET A, 0x200                            ; 7c01 0200
               SET A, -0x8000                          ; 7c01 8000
               SET A, -0x8001                          ; 7c01 7fff
               SET A, 0xffff                           ; 8001
               dat 0x8401                              ; 8401
               SET A, from + 0x20 - to                 ; 7c01 001e
:from          SET A, from + 0x20 - to                 ; 7c01 001e
:to            SET A, from + 0x20 - to                 ; fc01
:end
:a1            SET A, a2 + 0x20 - b2                   ; 7c01 001f
:b1
:a2            SET A, a1 + 0x20 - b1                   ; fc01
:b2
:a3            SET A, b4 - a4 - 3                      ; 7c01 fffe
:b3
:a4            SET A, b3 - a3 - 3                      ; 8001
:b4