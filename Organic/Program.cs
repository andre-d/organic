using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;
using Organic.Plugins;

namespace Organic
{
    public partial class Assembler
    {
        [STAThread]
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
            string pipe = null;
            string workingDirectory = Directory.GetCurrentDirectory();
            bool bigEndian = true, quiet = false, verbose = false;
            Assembler assembler = new Assembler();
            assembler.IncludePath = Environment.GetEnvironmentVariable("ORGINCLUDE");
            if (string.IsNullOrEmpty(assembler.IncludePath))
                assembler.IncludePath = "";
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
                            case "--output":
                            case "--output-file":
                                outputFile = args[++i];
                                break;
                            case "--input-file":
                                inputFile = args[++i];
                                break;
                            case "-e":
                            case "--equate":
                                ExpressionResult result = assembler.ParseExpression(args[i + 2]);
                                if (!result.Successful)
                                {
                                    Console.WriteLine("Error: " + ListEntry.GetFriendlyErrorMessage(ErrorCode.IllegalExpression));
                                    return;
                                }
                                assembler.Values.Add(args[i + 1].ToLower(), result.Value);
                                i += 2;
                                break;
                            case "-l":
                            case "--listing":
                                listingFile = args[++i];
                                break;
                            case "--little-endian":
                                bigEndian = false;
                                break;
                            case "--long-literals":
                                assembler.ForceLongLiterals = true;
                                break;
                            case "--quiet":
                            case "-q":
                                quiet = true;
                                break;
                            case "--pipe":
                            case "-p":
                                pipe = args[++i];
                                break;
                            case "--include":
                            case "-i":
                                assembler.IncludePath = Environment.GetEnvironmentVariable("ORGINCLUDE") + ";" + args[++i];
                                break;
                            case "--plugins":
                                ListPlugins(assembler);
                                return;
                            case "--working-directory":
                            case "-w":
                                workingDirectory = args[++i];
                                break;
                            case "--verbose":
                            case "-v":
                                verbose = true;
                                break;
                            case "--debug-mode":
                                Console.ReadKey();
                                break;
                            case "--install":
                                assembler.InstallPlugin(args[++i]);
                                return;
                            case "--remove":
                                assembler.RemovePlugin(args[++i]);
                                return;
                            case "--search":
                                assembler.SearchPlugins(args[++i]);
                                return;
                            case "--info":
                                assembler.GetInfo(args[++i]);
                                return;
                            default:
                                HandleParameterEventArgs hpea = new HandleParameterEventArgs(arg);
                                hpea.Arguments = args;
                                hpea.Index = i;
                                if (assembler.TryHandleParameter != null)
                                    assembler.TryHandleParameter(assembler, hpea);
                                if (!hpea.Handled)
                                {
                                    Console.WriteLine("Error: Invalid parameter: " + arg + "\nUse orgASM.exe --help for usage information.");
                                    return;
                                }
                                else
                                    i = hpea.Index;
                                if (hpea.StopProgram)
                                    return;
                                break;
                        }
                    }
                    catch (ArgumentOutOfRangeException)
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
            if (inputFile == null && pipe == null)
            {
                Console.WriteLine("Error: No input file specified.\nUse orgASM.exe --help for usage information.");
                return;
            }
            if (outputFile == null)
                outputFile = Path.GetFileNameWithoutExtension(inputFile) + ".bin";
            if (!File.Exists(inputFile) && pipe == null && inputFile != "-")
            {
                Console.WriteLine("Error: File not found (" + inputFile + ")");
                return;
            }

            string contents;
            if (pipe == null)
            {
                if (inputFile != "-")
                {
                    StreamReader reader = new StreamReader(inputFile);
                    contents = reader.ReadToEnd();
                    reader.Close();
                }
                else
                    contents = Console.In.ReadToEnd();
            }
            else
                contents = pipe;


            List<ListEntry> output;
            string wdOld = Directory.GetCurrentDirectory();
            Directory.SetCurrentDirectory(workingDirectory);
            if (pipe == null)
                output = assembler.Assemble(contents, inputFile);
            else
                output = assembler.Assemble(contents, "[piped input]");
            Directory.SetCurrentDirectory(wdOld);

            if (assembler.AssemblyComplete != null)
                assembler.AssemblyComplete(assembler, new AssemblyCompleteEventArgs(output));

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

            ushort currentAddress = 0;
            Stream binStream = null;
            if (outputFile != "-")
                binStream = File.Open(outputFile, FileMode.Create);
            foreach (var entry in output)
            {
                if (entry.Output != null)
                {
                    foreach (ushort value in entry.Output)
                    {
                        currentAddress++;
                        byte[] buffer = BitConverter.GetBytes(value);
                        if (bigEndian)
                            Array.Reverse(buffer);
                        if (outputFile != "-")
                            binStream.Write(buffer, 0, buffer.Length);
                        else
                            Console.Out.Write(Encoding.ASCII.GetString(buffer));
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
            Console.WriteLine("Organic build complete " + duration.TotalMilliseconds + "ms");
        }

        private static void ListPlugins(Assembler assembler)
        {
            assembler.LoadPlugins();
            Console.WriteLine("Listing plugins:");
            foreach (var plugin in assembler.LoadedPlugins)
                Console.WriteLine(plugin.Value.Name + ": " + plugin.Value.Description);
        }

        public static string CreateListing(List<ListEntry> output)
        {
            string listing = "";
            int maxLength = 0, maxFileLength = 0;
            foreach (var entry in output)
            {
                int length = entry.FileName.Length + 1;
                if (length > maxFileLength)
                    maxFileLength = length;
            }
            foreach (var entry in output)
            {
                int length = maxFileLength + entry.LineNumber.ToString().Length + 9;
                if (length > maxLength)
                    maxLength = length;
            }
            TabifiedStringBuilder tsb;
            foreach (var listentry in output)
            {
                tsb = new TabifiedStringBuilder();
                if ((listentry.Code.ToLower().StartsWith(".dat") || listentry.Code.ToLower().StartsWith(".dw") ||
                    listentry.Code.ToLower().StartsWith(".db") || listentry.Code.ToLower().StartsWith(".ascii") ||
                    listentry.Code.ToLower().StartsWith(".asciiz") || listentry.Code.ToLower().StartsWith(".asciip") ||
                    listentry.Code.ToLower().StartsWith(".asciic") || listentry.Code.ToLower().StartsWith(".align") ||
                    listentry.Code.ToLower().StartsWith(".fill") || listentry.Code.ToLower().StartsWith(".pad") ||
                    listentry.Code.ToLower().StartsWith(".incbin") || listentry.Code.ToLower().StartsWith(".reserve") ||
                    listentry.Code.ToLower().StartsWith(".incpack"))
                    && listentry.ErrorCode == ErrorCode.Success) // TODO: Move these to an array?
                {
                    // Write code line
                    tsb = new TabifiedStringBuilder();
                    tsb.WriteAt(0, listentry.FileName);
                    tsb.WriteAt(maxFileLength, "(line " + listentry.LineNumber + "): ");
                    if (listentry.Listed)
                        tsb.WriteAt(maxLength, "[0x" + LongHex(listentry.Address) + "] ");
                    else
                        tsb.WriteAt(maxLength, "[NOLIST] ");
                    tsb.WriteAt(maxLength + 25, listentry.Code);
                    listing += tsb.Value + "\n";
                    // Write data
                    for (int i = 0; i < listentry.Output.Length; i += 8)
                    {
                        tsb = new TabifiedStringBuilder();
                        tsb.WriteAt(0, listentry.FileName);
                        tsb.WriteAt(maxFileLength, "(line " + listentry.LineNumber + "): ");
                        if (listentry.Listed)
                            tsb.WriteAt(maxLength, "[0x" + LongHex((ushort)(listentry.Address + i)) + "] ");
                        else
                            tsb.WriteAt(maxLength, "[NOLIST] ");
                        string data = "";
                        for (int j = 0; j < 8 && i + j < listentry.Output.Length; j++)
                        {
                            data += LongHex(listentry.Output[i + j]) + " ";
                        }
                        tsb.WriteAt(maxLength + 30, data.Remove(data.Length - 1));
                        listing += tsb.Value + "\n";
                    }
                }
                else
                {
                    if (listentry.ErrorCode != ErrorCode.Success)
                    {
                        tsb = new TabifiedStringBuilder();
                        tsb.WriteAt(0, listentry.FileName);
                        tsb.WriteAt(maxFileLength, "(line " + listentry.LineNumber + "): ");
                        if (listentry.Listed)
                            tsb.WriteAt(maxLength, "[0x" + LongHex(listentry.Address) + "] ");
                        else
                            tsb.WriteAt(maxLength, "[NOLIST] ");
                        tsb.WriteAt(maxLength + 8, "ERROR: " + ListEntry.GetFriendlyErrorMessage(listentry.ErrorCode));
                        listing += tsb.Value + "\n";
                    }
                    if (listentry.WarningCode != WarningCode.None)
                    {
                        tsb = new TabifiedStringBuilder();
                        tsb.WriteAt(0, listentry.FileName);
                        tsb.WriteAt(maxFileLength, "(line " + listentry.LineNumber + "): ");
                        if (listentry.Listed)
                            tsb.WriteAt(maxLength, "[0x" + LongHex(listentry.Address) + "] ");
                        else
                            tsb.WriteAt(maxLength, "[NOLIST] ");
                        tsb.WriteAt(maxLength + 8, "WARNING: " + ListEntry.GetFriendlyWarningMessage(listentry.WarningCode));
                        listing += tsb.Value + "\n";
                    }
                    tsb = new TabifiedStringBuilder();
                    tsb.WriteAt(0, listentry.FileName);
                    tsb.WriteAt(maxFileLength, "(line " + listentry.LineNumber + "): ");
                    if (listentry.Listed)
                        tsb.WriteAt(maxLength, "[0x" + LongHex(listentry.Address) + "] ");
                    else
                        tsb.WriteAt(maxLength, "[NOLIST] ");
                    if (listentry.Output != null)
                    {
                        if (listentry.Output.Length > 0)
                        {
                            tsb.WriteAt(maxLength + 8, DumpArray(listentry.Output));
                            tsb.WriteAt(maxLength + 25, listentry.Code);
                        }
                    }
                    else
                        tsb.WriteAt(maxLength + 23, listentry.Code);
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

        /// <summary>
        /// Creates a string of an array's content
        /// </summary>
        /// <param name="array"></param>
        /// <returns></returns>
        public static string DumpArray(ushort[] array)
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
            Console.WriteLine("Organic DCPU-16 Assembler    Copyright Drew DeVault 2012");
        }

        internal static List<string> PluginHelp = new List<string>();

        private static void DisplayHelp()
        {
            Console.WriteLine("Usage: Organic.exe [parameters] [input file] [output file]\n" +
                "Output file is optional; if left out, Organic will use [input file].bin.\n\n" +
                "===Flags:\n" +
                "--equate [key] [value]: Adds an equate, with the same syntax as .equ.\n" +
                "--help: Displays this message.\n" +
                "--input-file [filename]: An alternative way to specify the input file.\n" +
                "--include [path]: Adds [path] to the search index for #include <> files.\n" +
                "--listing [filename]: Outputs a listing to [filename].\n" +
                "--little-endian: Switches output to little-endian mode.\n" +
                "--long-literals: Forces all literal values to take up an entire word.\n" + 
                "--output-file [filename]: An alternative way to specify the output file.\n" +
                "--pipe [assembly]: Assemble [assembly], instead of the input file.\n" +
                "--quiet: Organic will not output error information.\n" +
                "--verbose: Organic will output a listing to the console.\n" +
                "--working-directory [directory]: Change Organic's working directory.");
            if (PluginHelp.Count != 0)
            {
                Console.WriteLine("\n===Plugins");
                foreach (var help in PluginHelp)
                    Console.WriteLine(help);
            }
        }
    }
}
