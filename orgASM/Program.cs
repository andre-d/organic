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
            DateTime startTime = DateTime.Now;

            DisplaySplash();
            if (args.Length == 0)
            {
                DisplayHelp();
                return;
            }
            string inputFile = null;
            string outputFile = null;
            string listingFile = null;
            bool bigEndian = false, quiet = false, verbose = false;
            Assembler assembler = new Assembler();
            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                if (arg.StartsWith("-"))
                {
                    try
                    {
                        switch (arg)
                        {
                            case "-h":
                            case "-?":
                            case "/h":
                            case "/?":
                            case "--help":
                                DisplayHelp();
                                return;
                            case "-o":
                            case "--output-file":
                                outputFile = args[i + 1];
                                i++;
                                break;
                            case "-i":
                            case "--input-file":
                                inputFile = args[i + 1];
                                i++;
                                break;
                            case "-e":
                            case "--equate":
                                ushort? result = assembler.ParseExpression(args[i + 2]);
                                if (result == null)
                                {
                                    Console.WriteLine("Error: " + ListEntry.GetFriendlyErrorMessage(ErrorCode.IllegalExpression));
                                    return;
                                }
                                assembler.Values.Add(args[i + 1], result.Value);
                                i += 2;
                                break;
                            case "-l":
                            case "--listing":
                                listingFile = args[i + 1];
                                i++;
                                break;
                            case "--big-endian":
                            case "-b":
                                bigEndian = true;
                                break;
                            case "--quiet":
                            case "-q":
                                quiet = true;
                                break;
                            case "--verbose":
                            case "-v":
                                verbose = true;
                                break;
                            default:
                                Console.WriteLine("Error: Invalid parameter: " + arg + "\nUse orgASM.exe --help for usage information.");
                                return;
                        }
                    }
                    catch (ArgumentOutOfRangeException e)
                    {
                        Console.WriteLine("Error: Missing argument: " + arg + "\nUse orgASM.exe --help for usage information.");
                        return;
                    }
                }
                else
                {
                    if (inputFile == null)
                        inputFile = arg;
                    else if (outputFile == null)
                        outputFile = arg;
                    else
                    {
                        Console.WriteLine("Error: Invalid parameter: " + arg + "\nUse orgASM.exe --help for usage information.");
                        return;
                    }
                }
            }
            if (inputFile == null)
            {
                Console.WriteLine("Error: No input file specified.\nUse orgASM.exe --help for usage information.");
                return;
            }
            if (outputFile == null)
                outputFile = Path.GetFileNameWithoutExtension(inputFile) + ".bin";
            if (!File.Exists(inputFile))
            {
                Console.WriteLine("Error: File not found (" + inputFile + ")");
                return;
            }

            StreamReader reader = new StreamReader(inputFile);
            string contents = reader.ReadToEnd();
            reader.Close();

            List<ListEntry> output = assembler.Assemble(contents, inputFile);

            // Output errors
            if (!quiet)
            {
                foreach (var entry in output)
                {
                    if (entry.ErrorCode != ErrorCode.Success)
                        Console.WriteLine("Error " + entry.FileName + " (line " + entry.LineNumber + "): " + ListEntry.GetFriendlyErrorMessage(entry.ErrorCode));
                    if (entry.WarningCode != WarningCode.None)
                        Console.WriteLine("Warning " + entry.FileName + " (line " + entry.LineNumber + "): " + ListEntry.GetFriendlyWarningMessage(entry.WarningCode));
                }
            }

            Stream binStream = File.Open(outputFile, FileMode.Create);
            foreach (var entry in output)
            {
                if (entry.Output != null)
                {
                    foreach (ushort value in entry.Output)
                    {
                        byte[] buffer = BitConverter.GetBytes(value);
                        if (bigEndian)
                            Array.Reverse(buffer);
                        binStream.Write(buffer, 0, buffer.Length);
                    }
                }
            }

            string listing = "";

            if (listingFile != null || verbose)
                listing = CreateListing(output);

            if (verbose)
                Console.Write(listing);
            if (listingFile != null)
            {
                StreamWriter writer = new StreamWriter(listingFile);
                writer.Write(listing);
                writer.Close();
            }

            TimeSpan duration = DateTime.Now - startTime;
            Console.WriteLine(".orgASM build complete " + duration.TotalMilliseconds + "ms");
        }

        private static string CreateListing(List<ListEntry> output)
        {
            string listing = "";
            int maxLength = 0;
            foreach (var entry in output)
            {
                int length = entry.FileName.Length + entry.LineNumber.ToString().Length + 10;
                if (length > maxLength)
                    maxLength = length;
            }
            TabifiedStringBuilder tsb;
            foreach (var listentry in output)
            {
                tsb = new TabifiedStringBuilder();
                if (listentry.Code.StartsWith(".dat") || listentry.Code.StartsWith(".dw"))
                {
                    // Write code line
                    tsb = new TabifiedStringBuilder();
                    tsb.WriteAt(0, listentry.FileName + " (line " + listentry.LineNumber + "): ");
                    if (listentry.Listed)
                        tsb.WriteAt(maxLength, "[0x" + LongHex(listentry.Address) + "] ");
                    else
                        tsb.WriteAt(maxLength, "[NOLIST] ");
                    tsb.WriteAt(maxLength + 21, listentry.Code);
                    listing += tsb.Value + "\n";
                    // Write data
                    for (int i = 0; i < listentry.Output.Length; i += 8)
                    {
                        tsb = new TabifiedStringBuilder();
                        tsb.WriteAt(0, listentry.FileName + " (line " + listentry.LineNumber + "): ");
                        if (listentry.Listed)
                            tsb.WriteAt(maxLength, "[0x" + LongHex((ushort)(listentry.Address + i)) + "] ");
                        else
                            tsb.WriteAt(maxLength, "[NOLIST] ");
                        string data = "";
                        for (int j = 0; j < 8 && i + j < listentry.Output.Length; j++)
                        {
                            data += LongHex(listentry.Output[i + j]) + " ";
                        }
                        tsb.WriteAt(maxLength + 25, data.Remove(data.Length - 1));
                        listing += tsb.Value + "\n";
                    }
                }
                else
                {
                    if (listentry.ErrorCode != ErrorCode.Success)
                    {
                        tsb = new TabifiedStringBuilder();
                        tsb.WriteAt(0, listentry.FileName + " (line " + listentry.LineNumber + "): ");
                        if (listentry.Listed)
                            tsb.WriteAt(maxLength, "[0x" + LongHex(listentry.Address) + "] ");
                        else
                            tsb.WriteAt(maxLength, "[NOLIST] ");
                        tsb.WriteAt(maxLength + 8, "Error: " + ListEntry.GetFriendlyErrorMessage(listentry.ErrorCode));
                        listing += tsb.Value + "\n";
                    }
                    if (listentry.WarningCode != WarningCode.None)
                    {
                        tsb = new TabifiedStringBuilder();
                        tsb.WriteAt(0, listentry.FileName + " (line " + listentry.LineNumber + "): ");
                        if (listentry.Listed)
                            tsb.WriteAt(maxLength, "[0x" + LongHex(listentry.Address) + "] ");
                        else
                            tsb.WriteAt(maxLength, "[NOLIST] ");
                        tsb.WriteAt(maxLength + 8, "Warning: " + ListEntry.GetFriendlyWarningMessage(listentry.WarningCode));
                        listing += tsb.Value + "\n";
                    }
                    tsb = new TabifiedStringBuilder();
                    tsb.WriteAt(0, listentry.FileName + " (line " + listentry.LineNumber + "): ");
                    if (listentry.Listed)
                        tsb.WriteAt(maxLength, "[0x" + LongHex(listentry.Address) + "] ");
                    else
                        tsb.WriteAt(maxLength, "[NOLIST] ");
                    if (listentry.Output != null)
                        tsb.WriteAt(maxLength + 8, DumpArray(listentry.Output));
                    tsb.WriteAt(maxLength + 21, listentry.Code);
                    listing += tsb.Value + "\n";
                }
            }
            return listing;
        }

        private static string LongHex(ushort p)
        {
            string value = p.ToString("x");
            while (value.Length < 4)
                value = "0" + value;
            return value.ToUpper();
        }

        private static string DumpArray(ushort[] array)
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

        private static void DisplaySplash()
        {
            Console.WriteLine(".orgASM DCPU-16 Assembler    Copyright Drew DeVault 2012");
        }

        private static void DisplayHelp()
        {
            Console.WriteLine("Usage: orgASM.exe [parameters] [input file] [output file]\n" +
                "Output file is optional; if left out, .orgASM will use [input file].bin.\n\n" +
                "===Flags:\n" +
                "--help\n" +
                    "\tDisplay this message, then exit without assembling anything.\n" +
                    "\tShorthand: -h, -?, /h, /?\n" +
                "--output-file [filename]\n" +
                    "\tAn alternative way to specify the output file.\n" +
                    "\tShorthand: -o\n" +
                "--input-file [filename]\n" +
                    "\tAn alternative way to specify the input file.\n" +
                    "\tShorthand: -i\n" +
                "--equate [key] [expression]\n" +
                    "\tSpecifies an equate to use at assembly-time.  Same as .equ in-code.\n" +
                    "\tShorthand: -e\n" +
                "--listing [filename]\n" +
                    "\tSpecifies a file to output a code listing to.\n" +
                    "\tShorthand: -l\n" +
                "--big-endian\n" +
                    "\tSwitches output to big-endian mode, the reverse of notchian syntax.\n" +
                    "\tShorthand: -b\n" +
                "--quiet\n" +
                    "\tWill not output errors or warnings.\n" +
                    "\tShorthand: -q\n" +
                "--verbose\n" +
                    "\tWill output a listing to the console.\n" +
                    "\tShorthand: -v");
        }
    }
}
