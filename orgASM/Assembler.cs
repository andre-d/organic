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
        private Dictionary<int, ushort> RelativeLabels; // line, value
        private Stack<bool> IfStack;
        private bool noList;

        /// <summary>
        /// Values (such as labels and equates) found in the code
        /// </summary>
        public Dictionary<string, ushort> Values;

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
            RelativeLabels = new Dictionary<int, ushort>();

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
                int ln = LineNumbers.Pop();
                LineNumbers.Push(++ln);

                string line = lines[i].TrimComments().TrimExcessWhitespace();
                if (string.IsNullOrEmpty(line))
                    continue;
                if (line.Contains(".equ") && !line.StartsWith(".equ")) // TASM compatibility
                {
                    line = ".equ " + line.Replace(".equ", "").TrimExcessWhitespace();
                }
                if (line.StartsWith(".") || line.StartsWith("#"))
                {
                    // #include has to be handled in this method
                    if (line.StartsWith("#include ") || line.StartsWith(".include "))
                    {
                        string includedFileName = line.Substring(line.IndexOf(" ") + 1);
                        includedFileName = includedFileName.Trim('"', '\'');
                        if (!File.Exists(includedFileName))
                            output.Add(new ListEntry(line, FileNames.Peek(), LineNumbers.Peek(), currentAddress));
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
                                Array.Copy(lines, i + 1, newLines, i + newSource.Length + 1, lines.Length - i - 1);
                                lines = newLines;
                            }
                            FileNames.Push(includedFileName);
                            LineNumbers.Push(1);
                        }
                    }
                    else if (line == "#endfile" || line == ".endfile")
                    {
                        FileNames.Pop();
                        LineNumbers.Pop();
                    }
                    else
                    {
                        // Parse preprocessor directives
                        ParseDirectives(output, line);
                    }
                }
                else if (line.StartsWith(":") || line.EndsWith(":"))
                {
                    // Parse labels
                    string label = line;
                    if (line.StartsWith(":"))
                        label = label.Substring(1);
                    else
                        label = label.Remove(line.Length - 1);
                    if (label == "$")
                    {
                        RelativeLabels.Add(GetRootNumber(LineNumbers), currentAddress);
                        continue;
                    }
                    if (label.Contains(' ') || label.Contains('\t') || !char.IsLetter(label[0]))
                    {
                        output.Add(new ListEntry(line, FileNames.Peek(), LineNumbers.Peek(), currentAddress, ErrorCode.InvalidLabel));
                        continue;
                    }
                    foreach (char c in label)
                    {
                        if (!char.IsLetterOrDigit(c))
                        {
                            output.Add(new ListEntry(line, FileNames.Peek(), LineNumbers.Peek(), currentAddress, ErrorCode.InvalidLabel));
                            continue;
                        }
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
                        }

                        bool invalidParameter = false;
                        ExpressionResult expression = null;
                        for (int j = appendedValuesStartIndex; j < opcode.appendedValues.Length; j++)
                        {
                            ExpressionResult parameter = ParseExpression(opcode.appendedValues[j]);
                            if (!parameter.Successful)
                            {
                                output.Add(new ListEntry(line, FileNames.Peek(), LineNumbers.Peek(), currentAddress, ErrorCode.IllegalExpression));
                                invalidParameter = true;
                                break;
                            }
                            else
                            {
                                value = value.Concat(new ushort[] { parameter.Value }).ToArray();
                                expression = parameter;
                            }
                        }

                        if (invalidParameter)
                            continue;

                        output.Add(new ListEntry(line, FileNames.Peek(), LineNumbers.Peek(), value, currentAddress, !noList, warning));
                        output[output.Count - 1].Expression = expression;
                        if (!noList)
                            currentAddress += (ushort)value.Length;
                    }
                }
            }

            currentAddress = 0;

            // Fix references
            ushort optimizedWords = 0;
            for (int i = 0; i < output.Count; i++)
            {
                output[i].Address -= optimizedWords;
                if (output[i].Code != null)
                    if (output[i].Code.StartsWith(".org") || output[i].Code.StartsWith("#org"))
                        optimizedWords = 0;
                if (output[i].Expression == null)
                    continue;
                for (int j = 0; j < output[i].Expression.References.Length; j++)
                {
                    if (output[i].Output.Length > 1)
                    {
                        string reference = output[i].Expression.References[j];
                        if (!Values.ContainsKey(reference.ToLower()))
                            output[i].ErrorCode = ErrorCode.UnknownReference;
                        else
                        {
                            ushort originalValue = output[i].Output[1];
                            ushort value = (ushort)(Values[reference.ToLower()] + originalValue);
                            if (value < 0x1F && j == output[i].Expression.References.Length - 1 && false) // TODO: Make values keep track of labels
                            {
                                // Optimize to one word shorter
                                optimizedWords++;
                                // TODO: Assign to literals
                                output[i].Output = new ushort[] {
                                    (ushort)(output[i].Output[0] & ~0xFC00 | (ushort)(value << 10))
                                };
                            }
                            else
                            {
                                output[i].Output[1] = value;
                            }
                        }
                    }
                }
            }

            return output;
        }

        private int GetRootNumber(Stack<int> LineNumbers)
        {
            int res = 0;
            foreach (int i in LineNumbers)
                res += i;
            return res;
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
                {
                    output.Add(new ListEntry(line, FileNames.Peek(), LineNumbers.Peek(), currentAddress, ErrorCode.UncoupledEnd));
                }
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
                else if ((directive.StartsWith("dat") || directive.StartsWith("dw")))
                {
                    if (parameters.Length == 1)
                    {
                        output.Add(new ListEntry(line, FileNames.Peek(), LineNumbers.Peek(), currentAddress, ErrorCode.InsufficientParamters));
                    }
                    else
                    {
                        string[] dataStrings = directive.Substring(directive.IndexOf(" ")).SafeSplit(',');
                        List<ushort> binOutput = new List<ushort>();
                        foreach (string data in dataStrings)
                        {
                            if (data.Trim().StartsWith("\""))
                            {
                                if (!data.Trim().EndsWith("\""))
                                {
                                    output.Add(new ListEntry(line, FileNames.Peek(), LineNumbers.Peek(), currentAddress, ErrorCode.IllegalExpression));
                                }
                                else
                                {
                                    string str = data.Trim().Substring(1, data.Trim().Length - 2).Unescape();
                                    foreach (byte b in Encoding.ASCII.GetBytes(str))
                                        binOutput.Add(b);
                                }
                            }
                            else
                            {
                                ExpressionResult value = ParseExpression(data.Trim());
                                if (!value.Successful)
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
                    {
                        output.Add(new ListEntry(line, FileNames.Peek(), LineNumbers.Peek(), currentAddress, ErrorCode.InsufficientParamters));
                    }
                    else if (parameters.Length > 2)
                    {
                        output.Add(new ListEntry(line, FileNames.Peek(), LineNumbers.Peek(), currentAddress, ErrorCode.TooManyParamters));
                    }
                    else
                    {
                        ExpressionResult value = ParseExpression(parameters[1]);
                        if (value == null)
                        {
                            output.Add(new ListEntry(line, FileNames.Peek(), LineNumbers.Peek(), currentAddress, ErrorCode.IllegalExpression));
                        }
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
                    {
                        output.Add(new ListEntry(line, FileNames.Peek(), LineNumbers.Peek(), currentAddress, ErrorCode.InsufficientParamters));
                    }
                    else if (parameters.Length > 2)
                    {
                        output.Add(new ListEntry(line, FileNames.Peek(), LineNumbers.Peek(), currentAddress, ErrorCode.TooManyParamters));
                    }
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
                                ExpressionResult value = ParseExpression(parameters[2]);
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

        private class StringMatch
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
