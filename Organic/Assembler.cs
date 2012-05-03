using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;
using System.Globalization;

namespace Organic
{
    /// <summary>
    ///  Organic Assembler Program
    /// </summary>
    public partial class Assembler
    {
        #region Runtime values

        private ushort currentAddress;
        private Stack<string> FileNames;
        private Stack<int> LineNumbers;
        private Dictionary<string, byte> OpcodeTable;
        private Dictionary<string, byte> NonBasicOpcodeTable;
        private Dictionary<string, byte> ValueTable;
        private Dictionary<int, ushort> RelativeLabels; // line, value
        private Stack<bool> IfStack;
        private bool noList;

        /// <summary>
        /// Values (such as labels and equates) found in the code
        /// </summary>
        public Dictionary<string, ushort> Values;
        public Dictionary<string, ushort> LabelValues;
        public List<Macro> Macros;

        /// <summary>
        /// Path to search for include files in.
        /// </summary>
        public string IncludePath;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes all values for this assembler.  Assembler is designed to handle
        /// one assembly per instance.  If you intend to assemble several times, create
        /// new instances of this class each time.
        /// </summary>
        public Assembler()
        {
            // All default values for the assembler
            currentAddress = 0;

            // Load table
            OpcodeTable = new Dictionary<string, byte>();
            NonBasicOpcodeTable = new Dictionary<string, byte>();
            ValueTable = new Dictionary<string, byte>();
            IfStack = new Stack<bool>();
            noList = false;

            LoadTable();

            Values = new Dictionary<string, ushort>();
            LabelValues = new Dictionary<string, ushort>();
            Macros = new List<Macro>();

            RelativeLabels = new Dictionary<int, ushort>();

            LineNumbers = new Stack<int>();
            FileNames = new Stack<string>();

            LoadPlugins();
        }

        private void LoadTable()
        {
            StreamReader sr = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream("Organic.DCPUtable.txt"));
            string[] lines = sr.ReadToEnd().Replace("\r", "").Split('\n');
            sr.Close();
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (string.IsNullOrEmpty(line) || line.StartsWith("#"))
                    continue;
                string[] parts = line.Split(' ');
                if (parts[0] == "o")
                    OpcodeTable.Add(parts[2], byte.Parse(parts[1], NumberStyles.HexNumber));
                else if (parts[0] == "n")
                    NonBasicOpcodeTable.Add(parts[2], byte.Parse(parts[1], NumberStyles.HexNumber));
                else if (parts[0] == "a,b")
                    ValueTable.Add(parts[2], byte.Parse(parts[1], NumberStyles.HexNumber));
            }
        }

        #endregion

        #region Assembler

        /// <summary>
        /// Assembles the provided code.
        /// This will use the current directory to fetch include files and such.
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        public List<ListEntry> Assemble(string code)
        {
            return Assemble(code, "sourceFile");
        }

        /// <summary>
        /// Assembles the provided code.
        /// This will use the current directory to fetch include files and such.
        /// </summary>
        /// <returns>A listing for the code</returns>
        public List<ListEntry> Assemble(string code, string FileName)
        {
            FileNames = new Stack<string>();
            LineNumbers = new Stack<int>();
            FileNames.Push(FileName);
            LineNumbers.Push(0);
            IfStack.Push(true);

            // Pass one
            string[] lines = code.Replace("\r", "").Split('\n');
            List<ListEntry> output = new List<ListEntry>();
            for (int i = 0; i < lines.Length; i++)
            {
                LineNumbers.Push(LineNumbers.Pop() + 1);

                string line = lines[i].TrimComments().TrimExcessWhitespace();
                if (string.IsNullOrEmpty(line))
                    continue;
                if (line.SafeContains(':') && !noList)
                {
                    if (!IfStack.Peek())
                        continue;
                    ListEntry entry = new ListEntry(line, FileNames.Peek(), LineNumbers.Peek(), currentAddress);
                    entry.CodeType = CodeType.Directive;
                    // Parse labels
                    string label = line;
                    if (line.StartsWith(":"))
                    {
                        label = label.Substring(1);
                        if (line.Contains(' '))
                            line = line.Substring(line.IndexOf(' ') + 1).Trim();
                        else
                            line = "";
                    }
                    else
                    {
                        label = label.Remove(label.IndexOf(':'));
                        line = line.Substring(line.IndexOf(':') + 1);
                    }
                    line = line.Trim();
                    if (label.Contains(" "))
                        label = label.Remove(label.IndexOf(' '));
                    if (label == "$")
                    {
                        RelativeLabels.Add(GetRootNumber(LineNumbers), currentAddress);
                        continue;
                    }
                    if (label.Contains(' ') || label.Contains('\t') || !char.IsLetter(label[0]))
                    {
                        entry.ErrorCode = ErrorCode.InvalidLabel;
                        output.Add(entry);
                        continue;
                    }
                    foreach (char c in label)
                    {
                        if (!char.IsLetterOrDigit(c) && c != '_')
                        {
                            entry.ErrorCode = ErrorCode.InvalidLabel;
                            output.Add(entry);
                            continue;
                        }
                    }
                    if (Values.ContainsKey(label.ToLower()) || LabelValues.ContainsKey(label.ToLower()))
                    {
                        entry.ErrorCode = ErrorCode.DuplicateName;
                        output.Add(entry);
                        continue;
                    }
                    LabelValues.Add(label.ToLower(), currentAddress);
                    output.Add(entry);
                }
                if (string.IsNullOrEmpty(line))
                    continue;
                if (line.Contains(".equ") && !line.StartsWith(".equ")) // TASM compatibility
                {
                    line = ".equ " + line.Replace(".equ", "").TrimExcessWhitespace();
                }
                if (line.ToLower().StartsWith("dat"))
                {
                    line = "." + line;
                }
                if (line.StartsWith(".") || line.StartsWith("#"))
                {
                    // #include has to be handled in this method
                    if (line.StartsWith("#include") || line.StartsWith(".include"))
                    {
                        if (!IfStack.Peek())
                            continue;
                        string includedFileName = line.Substring(line.IndexOf(" ") + 1);
                        includedFileName = includedFileName.Trim('"', '\'');
                        if (includedFileName.StartsWith("<") && includedFileName.EndsWith(">"))
                        {
                            // Find included file
                            includedFileName = includedFileName.Trim('<', '>');
                            string[] paths = IncludePath.Split(';');
                            foreach (var path in paths)
                            {
                                if (File.Exists(Path.Combine(path, includedFileName)))
                                {
                                    includedFileName = Path.Combine(path, includedFileName);
                                    break;
                                }
                            }
                        }
                        if (!File.Exists(includedFileName))
                            output.Add(new ListEntry(line, FileNames.Peek(), LineNumbers.Peek(), currentAddress, ErrorCode.FileNotFound, !noList));
                        else
                        {
                            using (Stream includedFile = File.Open(includedFileName, FileMode.Open))
                            {
                                StreamReader sr = new StreamReader(includedFile);
                                string contents = sr.ReadToEnd();
                                sr.Close();

                                string[] newSource = contents.Replace("\r", "").Split('\n');
                                string[] newLines = new string[newSource.Length + lines.Length];
                                Array.Copy(lines, newLines, i);
                                Array.Copy(newSource, 0, newLines, i, newSource.Length);
                                newLines[i + newSource.Length] = "#endfile";
                                if (lines.Length > i + 1)
                                    Array.Copy(lines, i + 1, newLines, i + newSource.Length + 1, lines.Length - i - 1);
                                lines = newLines;
                            }
                            FileNames.Push(includedFileName);
                            LineNumbers.Push(1);
                            i--;
                            continue;
                        }
                    }
                    else if ((line.StartsWith("#incbin") || line.StartsWith(".incbin")) && !noList)
                    {
                        if (!IfStack.Peek())
                            continue;
                        string includedFileName = line.Substring(line.IndexOf(" ") + 1);
                        includedFileName = includedFileName.Trim('"', '\'');
                        if (includedFileName.StartsWith("<") && includedFileName.EndsWith(">"))
                        {
                            // Find included file
                            includedFileName = includedFileName.Trim('<', '>');
                            string[] paths = IncludePath.Split(';');
                            foreach (var path in paths)
                            {
                                if (File.Exists(Path.Combine(path, includedFileName)))
                                {
                                    includedFileName = Path.Combine(path, includedFileName);
                                    break;
                                }
                            }
                        }
                        if (!File.Exists(includedFileName))
                            output.Add(new ListEntry(line, FileNames.Peek(), LineNumbers.Peek(), currentAddress, ErrorCode.FileNotFound, !noList));
                        else
                        {
                            using (Stream includedFile = File.Open(includedFileName, FileMode.Open))
                            {
                                byte[] rawData = new byte[includedFile.Length];
                                includedFile.Read(rawData, 0, (int)includedFile.Length);

                                List<ushort> binOutput = new List<ushort>();
                                foreach (byte b in rawData)
                                    binOutput.Add(b);
                                output.Add(new ListEntry(line, includedFileName, LineNumbers.Peek(), binOutput.ToArray(), currentAddress, !noList));
                                if (!noList)
                                    currentAddress += (ushort)binOutput.Count;
                            }
                        }
                    }
                    else if ((line.StartsWith("#incpack") || line.StartsWith(".incpack")) && !noList)
                    {
                        if (!IfStack.Peek())
                            continue;
                        string includedFileName = line.Substring(line.IndexOf(" ") + 1);
                        includedFileName = includedFileName.Trim('"', '\'');
                        if (includedFileName.StartsWith("<") && includedFileName.EndsWith(">"))
                        {
                            // Find included file
                            includedFileName = includedFileName.Trim('<', '>');
                            string[] paths = IncludePath.Split(';');
                            foreach (var path in paths)
                            {
                                if (File.Exists(Path.Combine(path, includedFileName)))
                                {
                                    includedFileName = Path.Combine(path, includedFileName);
                                    break;
                                }
                            }
                        }
                        if (!File.Exists(includedFileName))
                            output.Add(new ListEntry(line, FileNames.Peek(), LineNumbers.Peek(), currentAddress, ErrorCode.FileNotFound, !noList));
                        else
                        {
                            using (Stream includedFile = File.Open(includedFileName, FileMode.Open))
                            {
                                byte[] rawData = new byte[includedFile.Length];
                                includedFile.Read(rawData, 0, (int)includedFile.Length);

                                List<ushort> binOutput = new List<ushort>();
                                ushort working = 0;
                                for (int j = 0; i < rawData.Length; i++)
                                {
                                    working |= (ushort)(rawData[j] << ((j % 2) * 8));
                                    if (j % 2 == 1)
                                    {
                                        binOutput.Add(working);
                                        working = 0;
                                    }
                                }
                                output.Add(new ListEntry(line, includedFileName, LineNumbers.Peek(), binOutput.ToArray(), currentAddress, !noList));
                                if (!noList)
                                    currentAddress += (ushort)binOutput.Count;
                            }
                        }
                    }
                    else if (line == "#endfile" || line == ".endfile")
                    {
                        if (!IfStack.Peek())
                            continue;
                        FileNames.Pop();
                        LineNumbers.Pop();
                    }
                    else if (line.StartsWith(".macro") && !noList)
                    {
                        if (!IfStack.Peek())
                            continue;
                        string macroDefinition = line.Substring(7).Trim();
                        Macro macro = new Macro();
                        macro.Args = new string[0];
                        if (macroDefinition.EndsWith("{"))
                            macroDefinition = macroDefinition.Remove(macroDefinition.Length - 1).Trim();
                        if (macroDefinition.Contains("("))
                        {
                            string paramDefinition = macroDefinition.Substring(macroDefinition.IndexOf("(") + 1);
                            macro.Name = macroDefinition.Remove(macroDefinition.IndexOf("("));
                            if (!paramDefinition.EndsWith(")"))
                                output.Add(new ListEntry(line, FileNames.Peek(), LineNumbers.Peek(), currentAddress, ErrorCode.InvalidMacroDefintion));
                            else
                            {
                                paramDefinition = paramDefinition.Remove(paramDefinition.Length - 1);
                                if (paramDefinition.Length > 0)
                                {
                                    string[] parameters = paramDefinition.Split(',');
                                    bool continueEvaluation = true;
                                    for (int j = 0; j < parameters.Length; j++)
                                    {
                                        string parameter = parameters[j].Trim();
                                        if (!char.IsLetter(parameter[0]))
                                        {
                                            continueEvaluation = false;
                                            break;
                                        }
                                        foreach (char c in parameter)
                                        {
                                            if (!char.IsLetterOrDigit(c) && c != '_')
                                            {
                                                continueEvaluation = false;
                                                break;
                                            }
                                        }
                                        if (!continueEvaluation)
                                            break;
                                        macro.Args = macro.Args.Concat(new string[] { parameter }).ToArray();
                                    }
                                    if (!continueEvaluation)
                                        continue;
                                }
                            }
                        }
                        else
                            macro.Name = macroDefinition;
                        // Isolate macro code
                        macro.Code = "";
                        bool foundEndmacro = false;
                        string macroLine = line;
                        i++;
                        for (; i < lines.Length; i++)
                        {
                            line = lines[i].TrimComments().TrimExcessWhitespace();
                            LineNumbers.Push(LineNumbers.Pop() + 1);
                            if (line == ".endmacro" || line == "#endmacro" || line == "}")
                            {
                                foundEndmacro = true;
                                break;
                            }
                            if (line != "{")
                                macro.Code += "\n" + line;
                        }
                        if (!foundEndmacro)
                        {
                            output.Add(new ListEntry(macroLine, FileNames.Peek(), LineNumbers.Peek(),
                                currentAddress, ErrorCode.UncoupledStatement));
                            continue;
                        }
                        macro.Code = macro.Code.Trim('\n');
                        Macros.Add(macro);
                        output.Add(new ListEntry(".macro " + macroDefinition, FileNames.Peek(), LineNumbers.Peek(), currentAddress));
                        foreach (var codeLine in macro.Code.Split('\n'))
                            output.Add(new ListEntry(codeLine, FileNames.Peek(), LineNumbers.Peek(), currentAddress));
                        output.Add(new ListEntry(".endmacro", FileNames.Peek(), LineNumbers.Peek(), currentAddress));
                    }
                    else
                    {
                        // Parse preprocessor directives
                        ParseDirectives(output, line);
                    }
                }
                else
                {
                    if (!IfStack.Peek())
                        continue;
                    // Search through macros
                    bool mayHaveMacro = false;
                    foreach (Macro macro in Macros)
                    {
                        if (line.ToLower().StartsWith(macro.Name.ToLower()))
                        {
                            mayHaveMacro = true;
                            break;
                        }
                    }
                    if (line.SafeContains('(') && line.SafeContains(')') && mayHaveMacro)
                    {
                        Macro userMacro = new Macro();
                        userMacro.Args = new string[0];
                        string macroDefinition = line;
                        string paramDefinition = macroDefinition.Substring(macroDefinition.IndexOf("(") + 1);
                        userMacro.Name = macroDefinition.Remove(macroDefinition.IndexOf("("));
                        if (!paramDefinition.EndsWith(")"))
                            output.Add(new ListEntry(line, FileNames.Peek(), LineNumbers.Peek(), currentAddress, ErrorCode.InvalidMacroDefintion));
                        else
                        {
                            paramDefinition = paramDefinition.Remove(paramDefinition.Length - 1);
                            if (paramDefinition.Length > 0)
                            {
                                string[] parameters = paramDefinition.Split(',');
                                for (int j = 0; j < parameters.Length; j++)
                                {
                                    string parameter = parameters[j].Trim();
                                    userMacro.Args = userMacro.Args.Concat(new string[] { parameter }).ToArray();
                                }
                            }
                        }
                        bool macroMatched = false;
                        foreach (Macro macro in Macros)
                        {
                            if (macro.Name.ToLower() == userMacro.Name.ToLower() &&
                                macro.Args.Length == userMacro.Args.Length)
                            {
                                // Expand the macro
                                userMacro.Code = macro.Code;
                                for (int j = 0; j < macro.Args.Length; j++)
                                    userMacro.Code = userMacro.Code.Replace(macro.Args[j], userMacro.Args[j]);
                                string[] macroCode = userMacro.Code.Replace("\r", "\n").Split('\n');
                                string[] newLines = new string[lines.Length + macroCode.Length - 1];
                                Array.Copy(lines, 0, newLines, 0, i);
                                Array.Copy(macroCode, 0, newLines, i, macroCode.Length);
                                if (lines.Length > i + 1)
                                    Array.Copy(lines, i + 1, newLines, i + macroCode.Length, lines.Length - i - 1);
                                lines = newLines;
                                output.Add(new ListEntry(line, FileNames.Peek(), LineNumbers.Peek(), currentAddress));
                                line = lines[i].TrimComments().TrimExcessWhitespace();
                                macroMatched = true;
                            }
                        }
                        if (macroMatched)
                        {
                            i--;
                            continue;
                        }
                        // We'll just let the opcode matcher yell at them if it isn't found
                    }

                    // Check for OPCodes
                    ListEntry entry = new ListEntry(line, FileNames.Peek(), LineNumbers.Peek(), currentAddress);
                    entry.Listed = !noList;
                    var opcode = MatchString(line, OpcodeTable);
                    bool nonBasic = false;
                    if (opcode == null)
                    {
                        opcode = MatchString(line, NonBasicOpcodeTable);
                        nonBasic = true;
                    }
                    if (opcode == null)
                    {
                        entry.ErrorCode = ErrorCode.InvalidOpcode;
                        output.Add(entry);
                        continue;
                    }
                    else
                    {
                        entry.Opcode = opcode;
                        StringMatch valueA = null, valueB = null;
                        if (!nonBasic)
                        {
                            entry.CodeType = CodeType.BasicInstruction;
                            if (opcode.valueA != null)
                                valueA = MatchString(opcode.valueA, ValueTable);
                            if (opcode.valueB != null)
                                valueB = MatchString(opcode.valueB, ValueTable);
                            if (valueA.value == valueB.value && valueA.value != 0x1E && valueB.value != 0x1E)
                                entry.WarningCode = WarningCode.RedundantStatement;
                            if (valueB.value == 0x1F && !opcode.match.Contains("IF"))
                                entry.WarningCode = WarningCode.AssignToLiteral;
                            entry.ValueA = valueA;
                            entry.ValueB = valueB;
                        }
                        else
                        {
                            entry.CodeType = CodeType.NonBasicInstruction;
                            if (opcode.valueA != null)
                                valueA = MatchString(opcode.valueA, ValueTable);
                            entry.ValueA = valueA;
                        }
                        output.Add(entry);
                        currentAddress++;
                        if (valueA != null)
                            if (valueA.isLiteral)
                                currentAddress++;
                        if (valueB != null)
                            if (valueB.isLiteral)
                                currentAddress++;
                    }
                }
            }

            return EvaluateAssembly(output);
        }

        private List<ListEntry> EvaluateAssembly(List<ListEntry> output)
        {
            for (int i = 0; i < output.Count; i++)
            {
                foreach (var kvp in output[i].PostponedExpressions)
                {
                    ExpressionResult result = ParseExpression(kvp.Value);
                    if (!result.Successful)
                    {
                        output[i].ErrorCode = ErrorCode.IllegalExpression;
                        continue;
                    }
                    output[i].Output[kvp.Key] = result.Value;
                }
                if (output[i].Opcode != null)
                {
                    // Assemble output
                    if (output[i].CodeType == CodeType.BasicInstruction)
                    {
                        byte value = output[i].Opcode.value;
                        byte valueA = output[i].ValueA.value;
                        byte valueB = output[i].ValueB.value;
                        output[i].Output = new ushort[1];
                        if (output[i].ValueA.isLiteral) // next-word
                        {
                            ExpressionResult result = ParseExpression(output[i].ValueA.literal);
                            if (!result.Successful)
                            {
                                output[i].ErrorCode = ErrorCode.IllegalExpression;
                                continue;
                            }
                            output[i].Output = output[i].Output.Concat(new ushort[] { result.Value }).ToArray();
                        }
                        if (output[i].ValueB.isLiteral)
                        {
                            ExpressionResult result = ParseExpression(output[i].ValueB.literal);
                            if (!result.Successful)
                            {
                                output[i].ErrorCode = ErrorCode.IllegalExpression;
                                continue;
                            }
                            output[i].Output = output[i].Output.Concat(new ushort[] { result.Value }).ToArray();
                        }
                        output[i].Output[0] = (ushort)(value | (valueB << 5) | (valueA << 10));
                    }
                    else if (output[i].CodeType == CodeType.NonBasicInstruction)
                    {
                        byte value = output[i].Opcode.value;
                        byte valueA = 0;
                        if (output[i].ValueA != null)
                            valueA = output[i].ValueA.value;
                        output[i].Output = new ushort[1];
                        if (output[i].ValueA != null)
                        {
                            if (output[i].ValueA.isLiteral) // next-word
                            {
                                ExpressionResult result = ParseExpression(output[i].ValueA.literal);
                                if (!result.Successful)
                                {
                                    output[i].ErrorCode = ErrorCode.IllegalExpression;
                                    continue;
                                }
                                output[i].Output = output[i].Output.Concat(new ushort[] { result.Value }).ToArray();
                            }
                        }
                        output[i].Output[0] = (ushort)(value << 5 | (valueA << 10));
                    }
                }
            }

            return output;
        }

        #endregion

        #region Helper Code

        internal StringMatch MatchString(string value, Dictionary<string, byte> keys)
        {
            value = value.Trim();
            StringMatch match = new StringMatch();
            match.original = value;
            foreach (var opcode in keys)
            {
                int valueIndex = 0;
                bool requiredWhitespaceMet = false;
                bool matchFound = true;
                match.isLiteral = false;
                for (int i = 0; i < opcode.Key.Length && valueIndex < value.Length; i++)
                {
                    match.match = opcode.Key;
                    match.value = opcode.Value;
                    if (opcode.Key[i] == '_') // Required whitespace
                    {
                        if (value[valueIndex] == ' ' || value[valueIndex] == '\t')
                        {
                            requiredWhitespaceMet = true;
                            i--;
                            valueIndex++;
                        }
                        else
                        {
                            if (!requiredWhitespaceMet)
                            {
                                matchFound = false;
                                break;
                            }
                            requiredWhitespaceMet = false;
                        }
                    }
                    else if (opcode.Key[i] == '-') // Optional whitespace
                    {
                        if (value[valueIndex] == ' ' || value[valueIndex] == '\t')
                        {
                            i--;
                            valueIndex++;
                        }
                    }
                    else if (opcode.Key[i] == '%') // value, like A or POP
                    {
                        i++;
                        char valID = opcode.Key[i];
                        int valueStart = valueIndex;
                        if (i == opcode.Key.Length - 1)
                        {
                            valueIndex = value.Length;
                        }
                        else
                        {
                            int delimiter = value.IndexOf(',', valueIndex);
                            if (delimiter == -1)
                            {
                                matchFound = false;
                                break;
                            }
                            else
                                valueIndex = delimiter;
                        }
                        if (valID == 'a')
                            match.valueA = value.Substring(valueStart, valueIndex - valueStart);
                        else
                            match.valueB = value.Substring(valueStart, valueIndex - valueStart);
                    }
                    else if (opcode.Key[i] == '$') // Literal
                    {
                        i++;
                        char valID = opcode.Key[i];
                        int valueStart = valueIndex;
                        if (i == opcode.Key.Length - 1)
                            valueIndex = value.Length;
                        else
                        {
                            int delimiter = value.IndexOf(',', valueIndex);
                            if (delimiter == -1 && opcode.Value != 0x1E)
                                delimiter = value.IndexOf('+', valueIndex);
                            if (delimiter == -1)
                                delimiter = value.IndexOf(']', valueIndex);
                            if (delimiter == -1)
                            {
                                matchFound = false;
                                break;
                            }
                            else
                                valueIndex = delimiter;
                        }
                        match.isLiteral = true;
                        match.literal = value.Substring(valueStart, valueIndex - valueStart);
                    }
                    else
                    {
                        if (value.ToUpper()[valueIndex] != opcode.Key.ToUpper()[i])
                        {
                            matchFound = false;
                            break;
                        }
                        valueIndex++;
                    }
                }
                if (matchFound && valueIndex == value.Length)
                    return match;
            }
            return null;
        }

        private int GetRootNumber(Stack<int> LineNumbers)
        {
            int res = 0;
            foreach (int i in LineNumbers)
                res += i;
            return res;
        }

        public class StringMatch
        {
            public string valueA;
            public string valueB;
            public string match;
            public string original;
            public byte value;
            public bool isLiteral;
            public string literal;
        }

        #endregion
    }
}
