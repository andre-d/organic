using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;
using System.Globalization;

namespace orgASM
{
    /// <summary>
    ///  orgASM Assembler Program
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
        private Stack<bool> IfStack;
        private List<int> References;
        private bool noList;

        /// <summary>
        /// Values (such as labels and equates) found in the code
        /// </summary>
        public Dictionary<string, ushort> Values;

        #endregion

        #region Constructor

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
            References = new List<int>();

            LineNumbers = new Stack<int>();
            FileNames = new Stack<string>();
        }

        private void LoadTable()
        {
            StreamReader sr = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream("orgASM.DCPUtable.txt"));
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
            FileNames.Push(FileName);
            LineNumbers.Push(0);
            IfStack.Push(true);

            List<ListEntry> output = new List<ListEntry>();

            string[] lines = code.Replace("\r", "").Split('\n');

            for (int i = 0; i < lines.Length; i++)
            {
                int ln = LineNumbers.Pop();
                LineNumbers.Push(++ln);

                string line = lines[i].TrimComments().TrimExcessWhitespace();
                if (string.IsNullOrEmpty(line))
                    continue;
                if (line.Contains(".equ") && !line.StartsWith(".equ")) // TASM compatibility
                {
                    line = ".equ " + line.Replace(".equ", "").TrimExcessWhitespace();
                }
                if (line.StartsWith("dat ")) // notchcode compatibility
                {
                    line = ".dat " + line.Remove(3);
                }
                if (line.StartsWith(".") || line.StartsWith("#"))
                {
                    // Parse preprocessor directives
                    ParseDirectives(output, line);
                }
                else if (line.StartsWith(":") || line.EndsWith(":"))
                {
                    // Parse labels
                    string label = line;
                    if (line.StartsWith(":"))
                        label = label.Substring(1);
                    else
                        label = label.Remove(line.Length - 1);
                    if (label.Contains(' ') || label.Contains('\t'))
                    {
                        output.Add(new ListEntry(line, FileNames.Peek(), LineNumbers.Peek(), currentAddress, ErrorCode.WhitespaceInLabel));
                        continue;
                    }
                    if (Values.ContainsKey(label.ToLower()))
                    {
                        output.Add(new ListEntry(line, FileNames.Peek(), LineNumbers.Peek(), currentAddress, ErrorCode.DuplicateName));
                        continue;
                    }
                    Values.Add(label.ToLower(), currentAddress);
                    output.Add(new ListEntry(line, FileNames.Peek(), LineNumbers.Peek(), currentAddress));
                }
                else
                {
                    if (!IfStack.Peek())
                        continue;
                    // Search through macros
                    // TODO

                    // Check for OPCodes
                    var opcode = MatchString(line, OpcodeTable);
                    bool nonBasic = false;
                    if (opcode == null)
                    {
                        opcode = MatchString(line, NonBasicOpcodeTable);
                        nonBasic = true;
                    }
                    if (opcode == null)
                    {
                        output.Add(new ListEntry(line, FileNames.Peek(), LineNumbers.Peek(), currentAddress, ErrorCode.InvalidOpcode));
                        continue;
                    }
                    else
                    {
                        StringMatch valueA = null, valueB = null;
                        WarningCode warning = WarningCode.None;
                        if (!nonBasic)
                        {
                            if (opcode.valueA != null)
                                valueA = MatchString(opcode.valueA, ValueTable);
                            if (opcode.valueB != null)
                                valueB = MatchString(opcode.valueB, ValueTable);
                            if (valueA == null || valueB == null)
                            {
                                output.Add(new ListEntry(line, FileNames.Peek(), LineNumbers.Peek(), currentAddress, ErrorCode.InvalidParameter));
                                continue;
                            }
                            opcode.appendedValues = opcode.appendedValues.Concat(valueA.appendedValues).ToArray();
                            opcode.appendedValues = opcode.appendedValues.Concat(valueB.appendedValues).ToArray();
                            if (valueA.value == valueB.value)
                                warning = WarningCode.RedundantStatement;
                            if (valueA.appendedValues.Length != 0)
                                warning = WarningCode.AssignToLiteral;
                        }
                        ushort[] value = new ushort[1];

                        int appendedValuesStartIndex = 0;

                        if (nonBasic)
                            value[0] = (ushort)((int)(opcode.value) << 4);
                        else
                        {
                            value[0] = (ushort)(opcode.value | ((int)(valueA.value) << 4) | ((int)(valueB.value) << 10));
                            if (opcode.appendedValues.Length > 0 && ParseExpression(opcode.appendedValues[0]) != null)
                            {
                                if (ParseExpression(opcode.appendedValues[0]).Value <= 0x1F && !valueB.match.Contains("["))
                                {
                                    // Compress the appended value into the opcode
                                    // TODO: Support for writing to literals (fails silenty on DCPU)
                                    value[0] &= 0x3FF;
                                    value[0] |= (ushort)(0x20 + ParseExpression(opcode.appendedValues[0]) << 10);
                                    appendedValuesStartIndex++;
                                }
                            }
                        }

                        bool invalidParameter = false;
                        for (int j = appendedValuesStartIndex; j < opcode.appendedValues.Length; j++)
                        {
                            ushort? parameter = ParseExpression(opcode.appendedValues[j]);
                            if (parameter == null)
                            {
                                output.Add(new ListEntry(line, FileNames.Peek(), LineNumbers.Peek(), currentAddress, ErrorCode.IllegalExpression));
                                invalidParameter = true;
                                break;
                            }
                            else
                                value = value.Concat(new ushort[] { parameter.Value }).ToArray();
                        }

                        if (invalidParameter)
                            continue;

                        output.Add(new ListEntry(line, FileNames.Peek(), LineNumbers.Peek(), value, currentAddress, !noList, warning));
                        if (!noList)
                            currentAddress += (ushort)value.Length;
                    }
                }
            }

            return output;
        }

        #endregion

        #region Preprocessor Directives

        private void ParseDirectives(List<ListEntry> output, string line)
        {
            string directive = line.Substring(1);
            string[] parameters = directive.Split(' ');
            if (directive == "endif" || directive == "end")
            {
                if (IfStack.Count == 1)
                    output.Add(new ListEntry(line, FileNames.Peek(), LineNumbers.Peek(), currentAddress, ErrorCode.UncoupledEnd));
                else
                {
                    IfStack.Pop();
                    output.Add(new ListEntry(line, FileNames.Peek(), LineNumbers.Peek(), currentAddress));
                }
            }
            else if (IfStack.Peek())
            {
                if (directive == "region" || directive == "endregion") { } // Allowed but ignored
                else if (directive == "nolist")
                {
                    noList = true;
                    output.Add(new ListEntry(line, FileNames.Peek(), LineNumbers.Peek(), currentAddress));
                }
                else if (directive == "list")
                {
                    noList = false;
                    output.Add(new ListEntry(line, FileNames.Peek(), LineNumbers.Peek(), currentAddress));
                }
                else if (directive.StartsWith("dat") || directive.StartsWith("dw"))
                {
                    if (parameters.Length == 1)
                        output.Add(new ListEntry(line, FileNames.Peek(), LineNumbers.Peek(), currentAddress, ErrorCode.InsufficientParamters));
                    else
                    {
                        string[] dataStrings = directive.Substring(directive.IndexOf(" ")).SafeSplit(',');
                        List<ushort> binOutput = new List<ushort>();
                        foreach (string data in dataStrings)
                        {
                            if (data.Trim().StartsWith("\""))
                            {
                                if (!data.Trim().EndsWith("\""))
                                    output.Add(new ListEntry(line, FileNames.Peek(), LineNumbers.Peek(), currentAddress, ErrorCode.IllegalExpression));
                                else
                                {
                                    string str = data.Trim().Substring(1, data.Trim().Length - 2).Unescape();
                                    foreach (byte b in Encoding.ASCII.GetBytes(str))
                                        binOutput.Add(b);
                                }
                            }
                            else
                            {
                                ushort? value = ParseExpression(data.Trim());
                                if (value == null)
                                    output.Add(new ListEntry(line, FileNames.Peek(), LineNumbers.Peek(), currentAddress, ErrorCode.IllegalExpression));
                                else
                                    binOutput.Add(value.Value);
                            }
                        }
                        output.Add(new ListEntry(line, FileNames.Peek(), LineNumbers.Peek(), binOutput.ToArray(), currentAddress));
                    }
                }
                else if (directive.StartsWith("org")) // .orgASM's namesake :)
                {
                    if (parameters.Length == 1)
                        output.Add(new ListEntry(line, FileNames.Peek(), LineNumbers.Peek(), currentAddress, ErrorCode.InsufficientParamters));
                    else if (parameters.Length > 2)
                        output.Add(new ListEntry(line, FileNames.Peek(), LineNumbers.Peek(), currentAddress, ErrorCode.TooManyParamters));
                    else
                    {
                        ushort? value = ParseExpression(parameters[1]);
                        if (value == null)
                            output.Add(new ListEntry(line, FileNames.Peek(), LineNumbers.Peek(), currentAddress, ErrorCode.IllegalExpression));
                        else
                        {
                            currentAddress = value.Value;
                            output.Add(new ListEntry(line, FileNames.Peek(), LineNumbers.Peek(), currentAddress));
                        }
                    }
                }
                else if (directive.StartsWith("ifdef"))
                {
                    if (parameters.Length == 1)
                        output.Add(new ListEntry(line, FileNames.Peek(), LineNumbers.Peek(), currentAddress, ErrorCode.InsufficientParamters));
                    else if (parameters.Length > 2)
                        output.Add(new ListEntry(line, FileNames.Peek(), LineNumbers.Peek(), currentAddress, ErrorCode.TooManyParamters));
                    else
                    {
                        if (Values.ContainsKey(parameters[1].ToLower()))
                            IfStack.Push(true);
                        else
                            IfStack.Push(false);
                        output.Add(new ListEntry(line, FileNames.Peek(), LineNumbers.Peek(), currentAddress));
                    }
                }
                else if (directive.StartsWith("equ") || directive.StartsWith("define"))
                {
                    if (parameters.Length > 1)
                    {
                        if (Values.ContainsKey(parameters[1].ToLower()))
                            output.Add(new ListEntry(line, FileNames.Peek(), LineNumbers.Peek(), currentAddress, ErrorCode.DuplicateName));
                        else
                        {
                            if (parameters.Length == 2)
                            {
                                Values.Add(parameters[1].ToLower(), 1);
                                output.Add(new ListEntry(line, FileNames.Peek(), LineNumbers.Peek(), currentAddress));
                            }
                            else if (parameters.Length > 2)
                            {
                                ushort? value = ParseExpression(parameters[2]);
                                if (value != null)
                                {
                                    Values.Add(parameters[1].ToLower(), value.Value);
                                    output.Add(new ListEntry(line, FileNames.Peek(), LineNumbers.Peek(), currentAddress));
                                }
                                else
                                    output.Add(new ListEntry(line, FileNames.Peek(), LineNumbers.Peek(), currentAddress, ErrorCode.IllegalExpression));
                            }
                            else
                                output.Add(new ListEntry(line, FileNames.Peek(), LineNumbers.Peek(), currentAddress, ErrorCode.InsufficientParamters));
                        }
                    }
                }
                else
                {
                    output.Add(new ListEntry(line, FileNames.Peek(), LineNumbers.Peek(), currentAddress, ErrorCode.InvalidDirective));
                }
            }
        }

        #endregion

        #region Helper Code

        private StringMatch MatchString(string value, Dictionary<string, byte> keys)
        {
            value = value.Trim();
            StringMatch match = new StringMatch();
            match.appendedValues = new string[0];
            foreach (var opcode in keys)
            {
                int valueIndex = 0;
                bool requiredWhitespaceMet = false;
                bool matchFound = true;
                match.appendedValues = new string[0];
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
                        {
                            valueIndex = value.Length;
                        }
                        else
                        {
                            int delimiter = value.IndexOf(',', valueIndex);
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
                        match.appendedValues = match.appendedValues.Concat(new string[] {
                            value.Substring(valueStart, valueIndex - valueStart) }).ToArray();
                    }
                    else
                    {
                        if (value.ToUpper()[valueIndex] != opcode.Key[i])
                        {
                            matchFound = false;
                            break;
                        }
                        valueIndex++;
                    }
                }
                if (matchFound)                    
                    return match;
            }
            return null;
        }

        class StringMatch
        {
            public string valueA;
            public string valueB;
            public string match;
            public byte value;
            public string[] appendedValues;
        }

        #endregion
    }
}
