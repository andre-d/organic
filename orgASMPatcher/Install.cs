using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Mono.Cecil;

namespace orgASMPatcher
{
    partial class Program
    {
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

            AssemblyDefinition asdDefinition = GetAssembly("orgASM.exe");
            foreach (var resource in asdDefinition.MainModule.Resources)
            {
                if (resource is EmbeddedResource)
                {
                    if ((resource as EmbeddedResource).Name.ToLower() == Path.GetFileName(args[1]).ToLower())
                    {
                        Console.WriteLine("Plugin is already installed.");
                        return;
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
                                    Console.WriteLine("Plugin is already installed.");
                                    return;
                                }
                            }
                            catch { }
                        }
                    }
                }
            }
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
