using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Cecil;
using System.IO;

namespace PatchOrgASM
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine(".orgASM Plugin Patcher   Copyright Drew DeVault 2012");

            // Parse arguments
            if (args.Length < 1)
            {
                Console.WriteLine("No method specified.  Use \"PatchOrgAsm help\" for usage information.");
                return;
            }
            string function = args[0].ToLower();
            if (function == "help")
            {
                DisplayHelp();
                return;
            }

            if (!File.Exists("orgASM.exe"))
            {
                Console.WriteLine("Error!  orgASM.exe not found!");
                return;
            }
            switch (function)
            {
                case "install-file":
                    InstallPlugin(args);
                    break;
                default:
                    Console.WriteLine("Unknown function specified: " + function);
                    break;
            }
        }

        static void DisplayHelp()
        {
            Console.WriteLine("Usage: PatchOrgASM [function] [parameters]\n" +
                "PatchOrgASM will install DLL files into orgASM.exe, if present in the directory.\n" +
                "===Functions:\n" +
                "install-file [file].dll: Installs the specified plugin file from the disk.");
        }

        static void InstallPlugin(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("Error: No plugin specified.");
                return;
            }

            if (!File.Exists(args[1]))
            {
                Console.WriteLine("Plugin file not found!");
                return;
            }

            AssemblyDefinition asdDefinition = AssemblyFactory.GetAssembly("orgASM.exe");
            EmbeddedResource erTemp = new EmbeddedResource(Path.GetFileName(args[1]), ManifestResourceAttributes.Public);

            using (Stream stream = File.OpenRead(args[1]))
            {
                erTemp.Data = new byte[stream.Length];
                stream.Read(erTemp.Data, 0, (int)stream.Length);
            }

            asdDefinition.MainModule.Resources.Add(erTemp);
            AssemblyFactory.SaveAssembly(asdDefinition, "orgASM.exe");

            Console.WriteLine("Plugin successfully installed!");
        }
    }
}
