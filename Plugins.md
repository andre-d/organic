Writing Plugins
===============

A plugin consists of a .NET DLL file that contains a single class that derives from Organic.IPlugin.  For an example, see [TestPlugin](https://github.com/SirCmpwn/organic/blob/master/TestPlugin/Plugin.cs).

An IPlugin has a Name, Description, Version, and a Loaded(Organic.Assembler) method.  The Loaded event will fire upon loading the plugin, which occurs on startup, and it is passed the Organic.Assembler object that will be used for this instance of Organic.

A number of events and methods are exposed to the plugin developer to integrate with Organic.  If you'd like an additional method or event, make an issue on GitHub.

There are a few example plugins [here](https://github.com/SirCmpwn/organic/wiki/Plugin-Examples).

Events
------

The following events are exposed by Organic:

    EventHandler<Organic.Plugins.HandleParameterEventArgs> Organic.Assembler.TryHandlerParameter

This event is fired when Organic encounters a command line parameter that it does not understand.  HandleParameterEventArgs includes the following properties:

* string Parameter: The actual parameter that was misunderstood in this case.
* string[] Arguments: All arguments supplied to Organic.
* int Index: The index in the arguments array that this parameter was found at.  If you change this, Organic will continue with the new number.
* bool Handled: If your plugin can handle this argument, set this to true before exiting the event handler.
* bool StopProgram: If Organic should discontinue execution after executing the event handler, set this to true.

If you handle the parameter yourself, make sure you set Handled to true, or Organic will discontinue execution with an error.

    EventHandler<Organic.Plugins.AssemblyCompleteArgs> Organic.Assembler.AssemblyComplete

This event is fired when Organic has completed assembly.  AssemblyCompleteArgs exposes the List<ListEntry> generated during assembly.  This event fires before Organic creates output files.

    EventHandler<Organic.Plugins.HandleCodeEventArgs> Organic.Assembler.HandleCodeLine

This event is fired on each line of code Organic parses, before Organic parses it.  HandleCodeEventArgs includes the following properties:

* string Code: The code being interpreted.  It is free of comments and excess whitespace.
* bool Handled: If Organic should skip this line of code and move on, set this to true.  Organic will add the Output property to the overall output.
* ListEntry Output: Set this value if you handle this line of code yourself.

Methods
-------

    Assembler.AddHelpEntry(string)

Variables
---------

Organic.Assembler has several variables that you can manipulate in the Loaded event to change how Organic assembles files.  Here's what you can manipulate:

* ushort currentAddress: The current address of output.
* Stack<string> FileNames: The stack of file names, determined by #include directives.
* Stack<int> LineNumbers: The stack of line numbers, for each #include file.
* Stack<int> SuspendedLineNumbers: When a macro is expanded, an integer is pushed to this stack for the number of lines it expanded to.  While the top value != 0, LineNumbers doesn't update.
* Dictionary<string, byte> OpcodeTable: A table of opcode definitions.  See [DCPUTable.txt](https://github.com/SirCmpwn/organic/blob/master/Organic/DCPUtable.txt) for information on the string format.
* Dictionary<string, byte> NonBasicOpcodeTable: A table of non-basic opcode definitions.
* Dictionary<string, byte> ValueTable: A table of values, such as registers.
* Dictionary<int, ushort> RelativeLabels: All relative labels currently known to Organic.  The key is the line number, and the ushort is the address.
* string PriorGlobalLabel: The last known global label, used for local labels.
* Stack<bool> IfStack: The result of a .if statement, or similar directives, is pushed to this stack.  When the top value is false, each line of code is ignored and unlisted.
* bool noList: When true, output is unlisted.
* bool ForceLongLiterals: When true, short literal optimization is disabled.  Note that this only affects the second pass.
* Dictionary<string, ushort> Values: A list of values known to Organic thus far, such as .equ statements.
* List<Label> LabelValues: All labels known to Organic thus far.
* List<Macro> Macros: All macros known to Organic thus far.
* string IncludePath: The semicolon-delimited list of paths to look for include files in when <> is used to include them.  (Example: #include <file>)

This method will add an entry to the output of "organic --help".