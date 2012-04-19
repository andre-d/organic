.orgASM
=======

.orgASM is an assembler for the DCPU-16 architecture that supports the *.org* directive.  It is an incomplete assembler.  Here's a (likely out of date) list of supported features:

* Assembling all instructions
* Labels
* Listing output
* Expression evaluation

*Planned Features*:

* Relative addressing
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

**.org \[origin]**: Sets the origin to \[origin]

**.dat \[data]** and **.dw \[data]**: Outputs [data] directly to the listing.

**.nolist**: Stops assembly until .list

**.list**: Resumes assembly after .nolist

**.region** and **.endregion**: These are ignored, but do not cause an error.  You may use them to organize a file, and your IDE may support collapsing them.

**.equ \[key] (value)** and **.define \[key] (value)***: These are identical. The equate a value with a key.  The value is optional - if left out, the default is 1.  You may also use "\[key] .equ \[value]" for TASM compatibility.

**.ifdef \[key]**: If the given key is defined with .equ or .define, this will return true.  The assembler will stop assembing until the next .end or .endif directive if false.

**.endif** and **.end**: Closes a matching .if* statement.

Compiling .orgASM
-----------------

*Note:* .orgASM's root git directory is ".orgASM" by default, which is hidden on most unix systems.  Use "mv .orgASM orgASM" to fix this.

*Windows*: "msbuild" from the root directory of the project.

*Linux*: Install "mono-complete".  "xbuild" from the root directory of the project.

*Mac*: Install mono (I don't know how to do this on Mac).  "xbuild" from the root directory of the project.

Getting Involved
----------------

Feel free to fork and submit pull requests with your changes.  You'll have good results with small, focused pull requests, rather than broad, sweeping ones.  You can also submit issues for bugs or feature requests.  You may email me as well at sir@cmpwn.com.  If you need general help with DCPU-16 assembly, join #0x10c-dev on Freenode.