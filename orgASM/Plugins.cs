using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.IO;
using orgASM.Plugins;

namespace orgASM
{
    // Plugin support for the assembler
    public partial class Assembler
    {
        internal List<Assembly> LoadedAssemblies;
        internal List<IPlugin> LoadedPlugins;

        public void LoadPlugins()
        {
            LoadedAssemblies = new List<Assembly>();
            LoadedPlugins = new List<IPlugin>();

            string[] names = Assembly.GetExecutingAssembly().GetManifestResourceNames();
            foreach (string name in names)
            {
                if (name.EndsWith(".dll"))
                {
                    try
                    {
                        Stream assemblyStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name);
                        byte[] assemblyData = new byte[assemblyStream.Length];
                        assemblyStream.Read(assemblyData, 0, assemblyData.Length);
                        Assembly assembly = Assembly.Load(assemblyData);
                        LoadedAssemblies.Add(assembly);
                        foreach (var type in assembly.GetTypes().Where(t => typeof(IPlugin).IsAssignableFrom(t)))
                        {
                            IPlugin plugin = (IPlugin)Activator.CreateInstance(type);
                            plugin.Loaded(this);
                        }
                    }
                    catch
                    {
                        Console.WriteLine("Error loading plugin: " + name);
                    }
                }
            }
        }

        #region Plugin Resources

        #region Events

        public event EventHandler<HandleParameterEventArgs> TryHandleParameter;
        public event EventHandler<AssemblyCompleteEventArgs> AssemblyComplete;

        #endregion

        #region Methods

        public void AddHelpEntry(string Entry)
        {
            PluginHelp.Add(Entry);
        }

        #endregion

        #endregion
    }
}
