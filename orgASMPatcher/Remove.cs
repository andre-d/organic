using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Cecil;
using System.IO;

namespace orgASMPatcher
{
    partial class Program
    {
        private static void RemovePlugin(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("Error: No plugin specified.");
                return;
            }
            AssemblyDefinition asdDefinition = GetAssembly("orgASM.exe");
            EmbeddedResource toRemove = null;
            foreach (var resource in asdDefinition.MainModule.Resources)
            {
                if (resource is EmbeddedResource)
                {
                    if ((resource as EmbeddedResource).Name.ToLower() == Path.GetFileName(args[1]).ToLower())
                    {
                        toRemove = (EmbeddedResource)resource;
                        break;
                    }
                    else
                    {
                        if ((resource as EmbeddedResource).Name.EndsWith(".dll"))
                        {
                            try
                            {
                                PluginInfo Plugin = GetPlugin(LoadResource(resource as EmbeddedResource));
                                if (Plugin.Name.ToLower() == Path.GetFileName(args[1]).ToLower())
                                {
                                    toRemove = (EmbeddedResource)resource;
                                    break;
                                }
                            }
                            catch { }
                        }
                    }
                }
            }

            if (toRemove == null)
            {
                Console.WriteLine("Plugin is not installed!");
                return;
            }

            asdDefinition.MainModule.Resources.Remove(toRemove);
            AssemblyFactory.SaveAssembly(asdDefinition, "orgASM.exe");

            Console.WriteLine("Plugin successfully removed!");
        }
    }
}
