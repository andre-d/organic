.orgASM
=======

.orgASM is an assembler for the DCPU-16 architecture that supports the *.org* directive.  It is an incomplete assembler.  Here's a (likely out of date) list of supported features:

* Assembling all instructions
* Labels
* Listing output
* Expression evaluation
* Relative addressing

*Planned Features*:

* Macros

Using .orgASM
-------------

On Linux, make sure that you have "mono-complete" installed, and prepend any commands to run .orgASM with "mono", similar to running Java programs.

### Command Line Usage

Usage: orgASM.exe [parameters] [input file] [output file]

[output file] is optional, and [input file].bin will be used if it is not specified.

### Parameters

**--help**

Displays usage information.

Shorthand: -h, -?, /h, /?

**--output-file [filename]**

An alternative way to specify the output file.

Shorthand: -o

**--input-file [filename]**

An alternative way to specify the input file.

Shorthand: -i

**--equate [key] [value]**

Specifies an equate to use at assembly-time.  Same as .equ in-code.

Shorthand: -e

**--listing [filename]**

Specifies a file to output a code listing to.

Shorthand: -l

**--big-endian**

Switches output to big-endian mode, the reverse of notchian syntax.

Shorthand: -b

**--quiet**

Will not output errors or warnings.

Shorthand: -q

**--verbose**

Will output a listing to the console.

Shorthand: -v

**--pipe "[assembly]"**

Instead of using a file for input, the given assembly will be piped into the assembly core and assembled directly.

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

### Pre-processor directives

.orgASM offers several pre-processor directives to ease use.  These may be used with either "." or "#".

**.org \[origin]**: Sets the origin to \[origin]

**.dat \[data]** and **.dw \[data]**: Outputs [data] directly to the listing.

**.nolist**: Stops assembly until .list

**.list**: Resumes assembly after .nolist

**.region** and **.endregion**: These are ignored, but do not cause an error.  You may use them to organize a file, and your IDE may support collapsing them.

**.equ \[key] (value)** and **.define \[key] (value)***: These are identical. The equate a value with a key.  The value is optional - if left out, the default is 1.  You may also use "\[key] .equ \[value]" for TASM compatibility.

**.ifdef \[key]**: If the given key is defined with .equ or .define, this will return true.  The assembler will stop assembing until the next .end or .endif directive if false.

**.endif** and **.end**: Closes a matching .if* statement.

**.include "[file]"**: Includes an external file.  For example, #include "file.asm".  Quotes are optional, and may be " or ' characters.

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

Compiling .orgASM
-----------------

*Note:* .orgASM's root git directory is ".orgASM" by default, which is hidden on most unix systems.  Use "mv .orgASM orgASM" to fix this.

*Windows*: "msbuild" from the root directory of the project.

*Linux*: Install "mono-complete".  "xbuild" from the root directory of the project.

*Mac*: Install mono (I don't know how to do this on Mac).  "xbuild" from the root directory of the project.

Using .orgASM as a Library
--------------------------

.orgASM is coded as a library.  You can directly run the Main method to compile files from your program, or you can use the alternative route of using the Assemble method.  Add a reference to orgASM.exe and you can use the Assemble method to get a List<ListEntry> from any given source code.  You can see the information contained in each ListEntry [here](https://github.com/SirCmpwn/.orgASM/blob/master/orgASM/ListEntry.cs).  It contains the prettified code for that line, the file it's contained in, the line number, the ushort[] output, the error code, warning code, address, and whether or not that line is included in the listing.  If an expression was used in that line, you can access the result of that expression's evaluation.

Getting Involved
----------------

Feel free to fork and submit pull requests with your changes.  You'll have good results with small, focused pull requests, rather than broad, sweeping ones.  You can also submit issues for bugs or feature requests.  You may email me as well at sir@cmpwn.com.  If you need general help with DCPU-16 assembly, join #0x10c-dev on Freenode.