using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.IO;
using Organic.Plugins;

namespace Organic
{
    // Plugin support for the assembler
    public partial class Assembler
    {
        internal Dictionary<string, Assembly> LoadedAssemblies;
        internal Dictionary<string, IPlugin> LoadedPlugins;

        public void LoadPlugins()
        {
            if (LoadedAssemblies != null)
                return;
            LoadedAssemblies = new Dictionary<string, Assembly>();
            LoadedPlugins = new Dictionary<string, IPlugin>();

            string[] files = Directory.GetFiles(Directory.GetCurrentDirectory(), "*.dll");
            foreach (string file in files)
            {
                try
                {
                    string assemblyFile = Path.GetTempFileName();
                    if (File.Exists(assemblyFile))
                        File.Delete(assemblyFile);
                    File.Copy(file, assemblyFile);
                    Stream assemblyStream = File.Open(assemblyFile, FileMode.Open);
                    byte[] assemblyData = new byte[assemblyStream.Length];
                    assemblyStream.Read(assemblyData, 0, assemblyData.Length);
                    Assembly assembly = Assembly.Load(assemblyData);
                    LoadedAssemblies.Add(file, assembly);
                    foreach (var type in assembly.GetTypes().Where(t => typeof(IPlugin).IsAssignableFrom(t)).Take(1))
                    {
                        IPlugin plugin = (IPlugin)Activator.CreateInstance(type);
                        plugin.Loaded(this);
                        LoadedPlugins.Add(file, plugin);
                    }
                }
                catch
                {
                    Console.WriteLine("Error loading plugin: " + file);
                }
            }
        }

        #region Plugin Resources

        #region Events

        public event EventHandler<HandleParameterEventArgs> TryHandleParameter;
        public event EventHandler<AssemblyCompleteEventArgs> AssemblyComplete;
        public event EventHandler<HandleCodeEventArgs> HandleCodeLine;

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
