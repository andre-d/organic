using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Cecil;
using System.IO;
using System.Reflection;

namespace OrganicPatcher
{
    partial class Program
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
                case "remove":
                    RemovePlugin(args);
                    break;
                case "list":
                    ListPlugins(args);
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
                "install-file [file].dll: Installs the specified plugin file from the disk.\n" +
                "list: Lists installed plugins.\n" +
                "remove [name]: Removes the specified plugin by name, or by filename (.dll).\n");
        }

        static Assembly LoadResource(EmbeddedResource resource)
        {
            byte[] data = new byte[resource.Data.Length];
            Array.Copy(resource.Data, data, data.Length);
            return Assembly.Load(data);
        }

        static PluginInfo GetPlugin(Assembly assembly)
        {
            foreach (var type in assembly.GetTypes().Where(t => t.GetInterfaces().Where(i => i.Name == "IPlugin").Count() != 0))
            {
                object plugin = Activator.CreateInstance(type);
                PluginInfo pInfo = new PluginInfo();
                pInfo.Name = (string)type.GetProperty("Name").GetValue(plugin, null);
                pInfo.Description = (string)type.GetProperty("Description").GetValue(plugin, null);
                pInfo.Version = (Version)type.GetProperty("Version").GetValue(plugin, null);
                return pInfo;
            }
            return null;
        }

        static AssemblyDefinition GetAssembly(string file)
        {
            byte[] assembly;
            using (Stream stream = File.OpenRead(file))
            {
                assembly = new byte[stream.Length];
                stream.Read(assembly, 0, assembly.Length);
                stream.Close();
            }
            return AssemblyFactory.GetAssembly(assembly);
        }
    }
}
