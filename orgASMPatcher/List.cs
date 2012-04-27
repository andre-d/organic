using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Cecil;

namespace orgASMPatcher
{
    partial class Program
    {
        private static void ListPlugins(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine("Error: Too many arguments.");
                return;
            }
            AssemblyDefinition asdDefinition = GetAssembly("orgASM.exe");
            foreach (var resource in asdDefinition.MainModule.Resources)
            {
                if (resource is EmbeddedResource)
                {
                    if ((resource as EmbeddedResource).Name.EndsWith(".dll"))
                    {
                        try
                        {
                            PluginInfo Plugin = GetPlugin(LoadResource(resource as EmbeddedResource));
                            Console.WriteLine(Plugin.Name);
                        }
                        catch { }
                    }
                }
            }
        }
    }
}
