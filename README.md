.orgASM
=======

.orgASM is an assembler for the DCPU-16 architecture that supports the *.org* directive.  It is an incomplete assembler.  Here's a (likely out of date) list of supported features:

* Assembling all instructions
* Labels
* Listing output
* Expression evaluation

*Planned Features*:

* .dw/.dat
* Relative addressing
* Backreferences
* Macros

Using .orgASM
-------------

On Linux, make sure that you have "mono-complete" installed, and prepend any commands to run .orgASM with "mono", similar to running Java programs.

To compile an assembly file with .orgASM, use the following syntax from the command line:

    orgASM.exe inputfile outputfile

.orgASM will assemble your inputfile and produce a listing in outputfile.  At the present time, binary output is not supported; the assember is the current focus.

Syntax
------

.orgASM's assembly syntax is the same as notchan assembly.  Here's an example file:

    :label
        SET A, 10
        SET B, 0x20
        ADD A, B

.orgASM is completely case-insensitive.  Additionally, you may pre- or post-fix your label names with ":", whichever you prefer.  .orgASM is also tolerant of any amount of whitespace in any location, though "SE T" is invalid.

### Expressions

Anywhere a number is required, an expression may be used.  Expressions are not evaluated with order-of-operations, but left-to-right, in accordance with assembly standards.  Valid operators are as follows:

    + - / * | & ^ ~ % << >> == != < > <= >= ( )

All values are 16-bit.  Boolean operators return a one or zero as appropriate.

### Pre-processor directives

.orgASM offers several pre-processor directives to ease use.  These may be used with either "." or "#".

*.org [origin]*: Sets the origin to [origin]

*.nolist*: Stops assembly until .list

*.list*: Resumes assembly after .nolist

*.region* and *.endregion*: These are ignored, but do not cause an error.  You may use them to organize a file, and your IDE may support collapsing them.

*.equ [key] (value)* and *.define [key] (value)*: These are identical. The equate a value with a key.  The value is optional - if left out, the default is 1.  You may also use "[key] .equ [value]" for TASM compatibility.

*.ifdef [key]*: If the given key is defined with .equ or .define, this will return true.  The assembler will stop assembing until the next .end or .endif directive if false.

*.endif* and *.end*: Closes a matching .if* statement.

Compiling .orgASM
-----------------

*Note:* .orgASM's root git directory is ".orgASM" by default, which is hidden on most unix systems.  Use "mv .orgASM orgASM" to fix this.

*Windows*: "msbuild" from the root directory of the project.

*Linux*: Install "mono-complete".  "xbuild" from the root directory of the project.

*Mac*: Install mono (I don't know how to do this on Mac).  "xbuild" from the root directory of the project.