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
    public class Assembler
    {
        #region Runtime values

        private ushort currentAddress;
        private Stack<string> FileNames;
        private Stack<int> LineNumbers;
        private Dictionary<string, byte> OpcodeTable;
        private Dictionary<string, byte> NonBasicOpcodeTable;
        private Dictionary<string, byte> ValueTable;
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

            List<ListEntry> output = new List<ListEntry>();

            string[] lines = code.Replace("\r", "").Split('\n');

            for (int i = 0; i < lines.Length; i++)
            {
                int ln = LineNumbers.Pop();
                LineNumbers.Push(++ln);

                string line = lines[i].TrimComments();
                if (string.IsNullOrEmpty(line))
                    continue;
                if (line.StartsWith(".") || line.StartsWith("#"))
                {
                    // Parse preprocessor directives
                    string directive = line.Substring(1);
                    if (directive == "nolist")
                    {
                        noList = true;
                    }
                    else
                    {
                        output.Add(new ListEntry(lines[i].TrimComments(), FileNames.Peek(), LineNumbers.Peek(), ErrorCode.InvalidDirective));
                        continue;
                    }
                    output.Add(new ListEntry(lines[i].TrimComments(), FileNames.Peek(), LineNumbers.Peek()));
                }
                else if (line.StartsWith(":") || line.EndsWith(":"))
                {
                    // Parse labels
                    if (line.StartsWith(":"))
                        line = line.Substring(1);
                    else
                        line = line.Remove(line.Length - 1);
                    if (line.Contains(' ') || line.Contains('\t'))
                    {
                        output.Add(new ListEntry(lines[i].TrimComments(), FileNames.Peek(), LineNumbers.Peek(), ErrorCode.WhitespaceInLabel));
                        continue;
                    }
                    if (Values.ContainsKey(line.ToLower()))
                    {
                        output.Add(new ListEntry(lines[i].TrimComments(), FileNames.Peek(), LineNumbers.Peek(), ErrorCode.DuplicateName));
                        continue;
                    }
                    Values.Add(line.ToLower(), currentAddress);
                    output.Add(new ListEntry(lines[i].TrimComments(), FileNames.Peek(), LineNumbers.Peek()));
                }
                else
                {
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
                        output.Add(new ListEntry(lines[i].TrimComments(), FileNames.Peek(), LineNumbers.Peek(), ErrorCode.InvalidOpcode));
                        continue;
                    }
                    else
                    {
                        StringMatch valueA = null, valueB = null;
                        if (!nonBasic)
                        {
                            if (opcode.valueA != null)
                                valueA = MatchString(opcode.valueA, ValueTable);
                            if (opcode.valueB != null)
                                valueB = MatchString(opcode.valueB, ValueTable);
                            if (valueA == null || valueB == null)
                            {
                                output.Add(new ListEntry(lines[i].TrimComments(), FileNames.Peek(), LineNumbers.Peek(), ErrorCode.InvalidParameter));
                                continue;
                            }
                            opcode.appendedValues = opcode.appendedValues.Concat(valueA.appendedValues).ToArray();
                            opcode.appendedValues = opcode.appendedValues.Concat(valueB.appendedValues).ToArray();
                        }
                        ushort[] value = new ushort[1];

                        int appendedValuesStartIndex = 0;

                        if (nonBasic)
                            value[0] = (ushort)((int)(opcode.value) << 4);
                        else
                        {
                            value[0] = (ushort)(opcode.value | ((int)(valueA.value) << 4) | ((int)(valueB.value) << 10));
                            if (opcode.appendedValues.Length > 0 && ParseValue(opcode.appendedValues[0]) != null)
                            {
                                if (ParseValue(opcode.appendedValues[0]).Value <= 0x1F)
                                {
                                    // Compress the appended value into the opcode
                                    // TODO: Support for writing to literals (fails silenty on DCPU)
                                    value[0] &= 0x3FF;
                                    value[0] |= (ushort)(0x20 + ParseValue(opcode.appendedValues[0]) << 10);
                                    appendedValuesStartIndex++;
                                }
                            }
                        }

                        for (int j = appendedValuesStartIndex; j < opcode.appendedValues.Length; j++)
                        {
                            ushort? parameter = ParseValue(opcode.appendedValues[j]);
                            if (parameter == null)
                            {
                                output.Add(new ListEntry(lines[i].TrimComments(), FileNames.Peek(), LineNumbers.Peek(), ErrorCode.IllegalExpression));
                            }
                            else
                                value = value.Concat(new ushort[] { parameter.Value }).ToArray();
                        }

                        output.Add(new ListEntry(lines[i].TrimComments(), FileNames.Peek(), LineNumbers.Peek(), value));
                        currentAddress += (ushort)value.Length;
                    }
                }
            }

            return output;
        }

        #region Helper Code

        private ushort? ParseValue(string value)
        {
            // TODO: Arithmetic
            if (Values.ContainsKey(value.ToLower()))
                return Values[value.ToLower()];
            int val;
            if (int.TryParse(value, out val))
                return (ushort)val;
            return null;
        }

        private StringMatch MatchString(string value, Dictionary<string, byte> keys)
        {
            value = value.Trim().ToUpper();
            StringMatch match = new StringMatch();
            match.appendedValues = new string[0];
            foreach (var opcode in keys)
            {
                int valueIndex = 0;
                bool requiredWhitespaceMet = false;
                bool matchFound = true;
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
                        if (value[valueIndex] != opcode.Key[i])
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

        #region Console Program Code

        public static void Main(string[] args)
        {
            Assembler assembler = new Assembler();
            StreamReader sr = new StreamReader(args[0]); // TODO: properly parse args
            var output = assembler.Assemble(sr.ReadToEnd(), args[0]);
            sr.Close();
            foreach (var listentry in output)
            {
                if (listentry.ErrorCode == ErrorCode.Success)
                {
                    if (listentry.Output != null && listentry.Output.Length > 0)
                        Console.WriteLine(listentry.File + " (line " + listentry.LineNumber + "):\t" + DumpArray(listentry.Output));
                    else
                        Console.WriteLine(listentry.File + " (line " + listentry.LineNumber + ")");
                }
                else
                    Console.WriteLine(ListEntry.GetFriendlyErrorMessage(listentry));

            }
            Console.WriteLine("Label addresses:");
            foreach (var label in assembler.Values)
            {
                Console.WriteLine(label.Key + ": 0x" + label.Value.ToString("x"));
            }
            Console.ReadKey(true);
        }

        static string DumpArray(ushort[] array)
        {
            string output = "";
            foreach (ushort u in array)
            {
                string val = u.ToString("x");
                while (val.Length < 4)
                    val = "0" + val;
                output += " " + val;
            }
            return output.Substring(1);
        }

        static void DisplayHelp()
        {
            // TODO
        }

        #endregion
    }
}
