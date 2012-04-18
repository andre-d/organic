.orgASM
=======

.orgASM is an assembler for the DCPU-16 architecture.  It is a work-in-progress.

Planned Features
----------------

* Support for common directives (include, define, **org** etc)
* Relative addressing
* Macro support
* Listing output

Basically, everything you'd expect in a professional-grade assembler.

The goal of .orgASM is to be able to successfully assemble [this](http://pastebin.com/raw.php?i=W3WTDC09) file.

Compiling .orgASM
-----------------

*Windows*: "msbuild" from the root directory of the project.

*Linux*: Install "mono-complete".  "xbuild" from the root directory of the project.

*Mac*: Install mono (I don't know how to do this on Mac).  "xbuild" from the root directory of the project.