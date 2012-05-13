Organic Assembler
=================

Organic is an assembler for the DCPU-16 architecture.  It supports version 1.7 of the [DCPU specification](http://pastebin.com/raw.php?i=Q4JvQvnM).  Organic also supports a number of advanced features, such as equates, relative addressing, macros, and expression evalulation.  It also runs on Windows, Linux, and Intel Macs (you may have limited success with PowerPC Macs).

Using Organic
-------------

On Linux, make sure that you have "mono-complete" installed, and prepend any commands to run Organic with "mono", similar to running Java programs.

### Command Line Usage

Usage: Organic.exe [parameters] [input file] [output file]

[output file] is optional, and [input file].bin will be used if it is not specified.  If you specify an input or output file as "-", the standard input/output will be used instead of reading from the disk.

### Parameters

**--little-endian**

Switches output to little-endian mode, the reverse of notchian syntax.

**--equate [key] [value]**

Specifies an equate to use at assembly-time.  Same as .equ in-code.

Shorthand: -e

**--help**

Displays usage information.

Shorthand: -h, -?, /h, /?

**--input-file [filename]**

An alternative way to specify the input file.

**--include [path]**

Adds [path] to the paths to search through for include files.  This only affects files included with <>, such as .include <stdio.asm>.  This is a semicolon-delimited list of directories.

Shorthand: -i

**--listing [filename]**

Specifies a file to output a code listing to.

Shorthand: -l

**--long-literals**

Forces organic to use long-form literals, even when they might be shortened to short-form.

**--output-file [filename]**

An alternative way to specify the output file.

Shorthand: -o, --output

**--pipe "[assembly]"**

Instead of using a file for input, the given assembly will be piped into the assembly core and assembled directly.

Shorthand: -p

**--quiet**

Will not output errors or warnings.

Shorthand: -q

**--verbose**

Will output a listing to the console.

Shorthand: -v

**--working-directory [directory]**

Manually sets the working directory of Organic.

Shorthand: -w

Syntax
------

Organic's assembly syntax is the same as notchan assembly.  Here's an example file:

    :label
        SET A, 10
        SET B, 0x20
        ADD A, B

Organic is completely case-insensitive.  Additionally, you may pre- or post-fix your label names with ":", whichever you prefer.  Organic is also tolerant of any amount of whitespace in any location, though "SE T" is invalid.

### Expressions

Anywhere a number is required, an expression may be used.  Expressions are not evaluated with order-of-operations, but left-to-right, in accordance with assembly standards.  Valid operators are as follows:

    + - / * | & ^ ~ % << >> == != <> < > <= >= ( ) === !== && || ^^

All values are 16-bit.  Boolean operators return a one or zero as appropriate.

=== and !==  are string compares.  For example: ".if abcd === abcd" is true, while ".if 0x5 === 5" is false.

You may also use instructions as literals, placed between "{" and "}".  For example:

    SET A, {RFI 1}

Which will translate to SET A, 0x160.  Only the first word of the instruction is taken.

### Relative Addressing

You may create any number of labels called "$".  These are relative labels.  You can reference the value of a relative label with "$+" or "$-" with any number of + or - characters.  The calculated value will be the value of the relative label that many relative labels away.  For example:

        SET A, $++ ; References the relative label before SET C, B
    $:
        ADD A, B
        SET B, $- ; References the previous relative label, before ADD A, B
    $:
        SET C, B
        SET PC, $ ; $ alone references the current address.  This is an infinite loop.
    
Please note that relative addressing is different than the "$" constant, which refers to the address of the current line.

### Macros

To define a macro, use the .macro and .endmacro directives.  Here is an example macro definition:

    .macro test(param1, param2) ; Also valid: "test()" and "test" for parameter-less macros
        SET param1, param2
        ADD param2, param1
    .endmacro

The parameters defined in the .macro directive may be freely used inside of the macro itself.  To use a macro:

    test(A, B)

You may also use quotes in a parameter if you wish to pass a statement that uses commas, for instance:

    test("SET A, B", 5)

"SET A, B" is passed to the macro as one single parameter.  If you wish to pass an actual quotation mark, you may leave off the closing mark.  Like so:

    test(", 5)

This will pass a quotation mark as the parameter.  If you wish to pass more than one, you should escape it in the string: test("\"\"", 5) will pass two in a row.

This will expand to the following:

    SET A, B
    ADD B, A

You may recursively use macros in a macro definition, for instance:

    .macro test2()
        test(A, C)
    .endmacro

### Local Labels

If you prepend a label with ".", that label will have the name of the previous global label prepended to it.  For example:

    label1:
        SET A, B
    .label: ; becomes label1_label
        SET B, A
        SET A, .label ; becomes label1_label
    label2:
        SET A, C
    .label: ; becomes label2_label

Note that this also works with Notch-style labels, so ":.label" is a local label.  Also permitted is "_label", in accordance with the 0x10c standards committee.

### Pre-processor directives

Organic offers several pre-processor directives to ease use.  These may be used with either "." or "#".

**.ascii "\[text]"**: Inserts the ASCII string [text].

**.asciip "\[text]"**: Inserts the ASCII string [text], prefixed with its length (Pascal-style string).

**.asciiz "\[text]"** and **.asciic [text]**: Inserts the ASCII string [text], postfixed with a zero (C-style string).

**.align \[address]**: Pads the output with zeros until currentAddress is equal to [address].

**.dat \[data]** and **.dw \[data]**: Outputs [data] directly to the listing.

**.echo \[message...]**: Echos [message...] to the console and the listing.  [message...] has the same inputs as .dat/.dw.

**.else**: This will negate the matching .if statement.

**.elseif \[expression]** and **.elif \[expression]**: If the matching .if statement was false, this will execute as a .if statement.

**.endif** and **.end**: Closes a matching .if* statement.

**.equ \[key] (value)** and **.define \[key] (value)**: Equates a value with a key.  The value is optional - if left out, the default is 1.  You may also use "\[key] .equ \[value]" for TASM compatibility.

**.endmacro**: Ends a macro definition.

**.fill \[length], \[value]** and **.pad \[length], \[value]**: Inserts [value] into the output [length] times.

**.if \[expression]**: If the expression is greater than or equal to 1, this will return true.  The assembler will stop assembing until the next .end or .endif directive if false.

**.ifndef \[key]**: If the given key is NOT defined with .equ or .define, this will return true.  The assembler will stop assembing until the next .end or .endif directive if false.

**.ifdef \[key]**: If the given key is defined with .equ or .define, this will return true.  The assembler will stop assembing until the next .end or .endif directive if false.

**.incbin "\[file]"**: Includes an external file as raw data, one byte to a word.  Quotes are optional, and may be " or ' characters.  If you use <> instead of quotes, the path specified with --include will be used.

**.incpack "\[file]"**: Includes an external file as raw data, and packs two bytes to a word.  Quotes are optional, and may be " or ' characters.  If you use <> instead of quotes, the path specified with --include will be used.

**.include "\[file]"**: Includes an external file.  Quotes are optional, and may be " or ' characters.  If you use <> instead of quotes, the path specified with --include will be used.

**.list**: Resumes assembly after .nolist

**.longform**: Forces Organic to use long-form literals from that point forward.

**.macro \[name](\[parameters])**: Begins a macro definition with the given name and parameters.  The parameters are optional, and parenthesis may be omitted for parameter-less macros.

**.nolist**: Stops assembly until .list

**.org \[origin]**: Sets the origin to \[origin]

**.region** and **.endregion**: These are ignored, but do not cause an error.  You may use them to organize a file, and your IDE may support collapsing them.

**.reserve \[amount]**: Inserts [amount] of words into the output.  Each word is zero.

**.shortform**: Forces Organic to use short-form literals when possible from that point forward.

Understanding Listings
----------------------

Here's an example listing file:

    example.asm        (line 1):  [0x0000] 0401        SET A, B
    example.asm        (line 2):  [0x0001] 0012        ADD B, A
    example.asm        (line 3):  [0x0002]             label1:
    example.asm        (line 4):  [0x0002] 7C0A 0102   BOR A, 0x102
    example.asm        (line 5):  [0x0004] 040E        IFG A, B
    example.asm        (line 6):  [0x0005] 7DC1 0002   SET PC, label1
    example.asm        (line 7):  [0x0007]             .nolist
    example.asm        (line 8):  [NOLIST] 0811        SET B, C
    example.asm        (line 9):  [0x0007]             .list
    example.asm        (line 10): [0x3000]             .org 0x3000
    example.asm        (line 11): [0x3000] 0801        SET A, C
    example.asm        (line 12): [0x3001] 1822        ADD C, I
    exampleInclude.asm (line 2):  [0x3002] 7DA1 0035   SET PUSH, 0b101011
    exampleInclude.asm (line 3):  [0x3004]             .dat "Hello, world!"
    exampleInclude.asm (line 3):  [0x3004]                 0048 0065 006C 006C 006F 002C 0020 0077
    exampleInclude.asm (line 3):  [0x300C]                 006F 0072 006C 0064 0021
    example.asm        (line 14): [0x3004] 0011        SET B, A
    example.asm        (line 15): [0x3005] 7C02 0001   ADD A, 1

Let's take this apart piece by piece.

    example.asm        (line 1):  [0x0000] 0401        SET A, B
    
Your basic listing entry has several parts.  First, the name of the file.  After that are line numbers, which are always tab-aligned to the same location.  Within the brackets is the address of the output.  If that particular line of code is unlisted, it will use NOLIST for the address.  Next is the compiled output, in hex, seperated by each word.  After that is the prettified code being parsed.

    exampleInclude.asm (line 3):  [0x3004]             .dat "Hello, world!"
    exampleInclude.asm (line 3):  [0x3004]                 0048 0065 006C 006C 006F 002C 0020 0077
    exampleInclude.asm (line 3):  [0x300C]                 006F 0072 006C 0064 0021

For .dat sections, the data is split up on different lines of a listing.  Each line is 8 words long.

Compiling Organic
-----------------

**Windows**: "msbuild" from the root directory of the project.

**Linux**: Install "mono-complete".  "xbuild" from the root directory of the project.

**Mac**: Install mono (I don't know how to do this on Mac).  "xbuild" from the root directory of the project.

Using Organic as a Library
--------------------------

Organic is coded as a library.  You can directly run the Main method to compile files from your program, or you can use the alternative route of using the Assemble method.  Add a reference to Organic.exe and you can use the Assemble method to get a List<ListEntry> from any given source code.  You can see the information contained in each ListEntry [here](https://github.com/SirCmpwn/Organic/blob/master/Organic/ListEntry.cs).  It contains the prettified code for that line, the file it's contained in, the line number, the ushort[] output, the error code, warning code, address, and whether or not that line is included in the listing.  If an expression was used in that line, you can access the result of that expression's evaluation.

Getting Involved
----------------

Feel free to fork and submit pull requests with your changes.  You'll have good results with small, focused pull requests, rather than broad, sweeping ones.  You can also submit issues for bugs or feature requests.  You may email me as well at sir@cmpwn.com.  If you need general help with DCPU-16 assembly, join #0x10c-dev on Freenode.