using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace orgASM
{
    public partial class Assembler
    {
        public static void Main(string[] args)
        {
            Assembler assembler = new Assembler();
            StreamReader sr = new StreamReader(args[0]); // TODO: properly parse args
            var output = assembler.Assemble(sr.ReadToEnd(), args[0]);
            sr.Close();
            StreamWriter outputFile = new StreamWriter(args[1]);
            int maxLength = 0;
            foreach (var listentry in output)
            {
                int length = listentry.FileName.Length + listentry.LineNumber.ToString().Length + 10;
                if (length > maxLength)
                    maxLength = length;
            }
            foreach (var listentry in output)
            {
                TabifiedStringBuilder tsb;
                if (listentry.Code.StartsWith(".dat") || listentry.Code.StartsWith(".dw"))
                {
                    // Write code line
                    tsb = new TabifiedStringBuilder();
                    tsb.WriteAt(listentry.FileName + " (line " + listentry.LineNumber + "): ", 0);
                    if (listentry.Listed)
                        tsb.WriteAt("[0x" + LongHex(listentry.Address) + "] ", maxLength);
                    else
                        tsb.WriteAt("[NOLIST] ", maxLength);
                    tsb.WriteAt(listentry.Code, maxLength + 21);
                    outputFile.WriteLine(tsb.Value);
                    // Write data
                    for (int i = 0; i < listentry.Output.Length; i += 8)
                    {
                        tsb = new TabifiedStringBuilder();
                        tsb.WriteAt(listentry.FileName + " (line " + listentry.LineNumber + "): ", 0);
                        if (listentry.Listed)
                            tsb.WriteAt("[0x" + LongHex((ushort)(listentry.Address + i)) + "] ", maxLength);
                        else
                            tsb.WriteAt("[NOLIST] ", maxLength);
                        string data = "";
                        for (int j = 0; j < 8 && i + j < listentry.Output.Length; j++)
                        {
                            data += LongHex(listentry.Output[i + j]) + " ";
                        }
                        tsb.WriteAt(data.Remove(data.Length - 1), maxLength + 25);
                        outputFile.WriteLine(tsb.Value);
                    }
                }
                else
                {
                    if (listentry.ErrorCode != ErrorCode.Success)
                    {
                        tsb = new TabifiedStringBuilder();
                        tsb.WriteAt(listentry.FileName + " (line " + listentry.LineNumber + "): ", 0);
                        if (listentry.Listed)
                            tsb.WriteAt("[0x" + LongHex(listentry.Address) + "] ", maxLength);
                        else
                            tsb.WriteAt("[NOLIST] ", maxLength);
                        tsb.WriteAt("Error: " + ListEntry.GetFriendlyErrorMessage(listentry), maxLength + 8);
                        outputFile.WriteLine(tsb.Value);
                    }
                    if (listentry.WarningCode != WarningCode.None)
                    {
                        tsb = new TabifiedStringBuilder();
                        tsb.WriteAt(listentry.FileName + " (line " + listentry.LineNumber + "): ", 0);
                        if (listentry.Listed)
                            tsb.WriteAt("[0x" + LongHex(listentry.Address) + "] ", maxLength);
                        else
                            tsb.WriteAt("[NOLIST] ", maxLength);
                        tsb.WriteAt("Warning: " + ListEntry.GetFriendlyWarningMessage(listentry), maxLength + 8);
                        outputFile.WriteLine(tsb.Value);
                    }
                    tsb = new TabifiedStringBuilder();
                    tsb.WriteAt(listentry.FileName + " (line " + listentry.LineNumber + "): ", 0);
                    if (listentry.Listed)
                        tsb.WriteAt("[0x" + LongHex(listentry.Address) + "] ", maxLength);
                    else
                        tsb.WriteAt("[NOLIST] ", maxLength);
                    if (listentry.Output != null)
                        tsb.WriteAt(DumpArray(listentry.Output), maxLength + 8);
                    tsb.WriteAt(listentry.Code, maxLength + 21);
                    outputFile.WriteLine(tsb.Value);
                }
            }
            outputFile.Close();
            if (System.Diagnostics.Debugger.IsAttached)
            {
                StreamReader reader = new StreamReader(args[1]);
                Console.Write(reader.ReadToEnd());
                reader.Close();
                Console.ReadKey(true);
            }
        }

        private static string LongHex(ushort p)
        {
            string value = p.ToString("x");
            while (value.Length < 4)
                value = "0" + value;
            return value.ToUpper();
        }

        static string DumpArray(ushort[] array)
        {
            string output = "";
            foreach (ushort u in array)
            {
                string val = u.ToString("x").ToUpper();
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
    }
}
