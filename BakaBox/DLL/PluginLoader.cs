using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Diagnostics;

namespace BakaBox.DLL
{
    /// <summary>
    /// Class to load plugins.
    /// </summary>
    /// <typeparam name="T">Class type to load.</typeparam>
    [DebuggerStepThrough]
    public class PluginLoader<T>
    {
        /// <summary>
        /// Generic constructor
        /// </summary>
        private PluginLoader() { }

        /// <summary>
        /// Load a default named plugins. *.dll
        /// </summary>
        /// <param name="Path">Directory of plugins.</param>
        /// <returns>List of type T plugins.</returns>
        public static List<T> LoadPluginDirectory(String Path) { return LoadPluginDirectory(Path, "*.dll"); }
        
        /// <summary>
        /// Load plugins based on a filter.
        /// </summary>
        /// <param name="Path">Directory of plugins.</param>
        /// <param name="Filter">Plugin name filter.</param>
        /// <returns>List of type T plugins.</returns>
        public static List<T> LoadPluginDirectory(String Path, String Filter)
        {
            List<T> Plugins = new List<T>();
            String[] PossiblePluginFiles = Directory.GetFiles(Path, Filter, SearchOption.TopDirectoryOnly);

            foreach (String PossiblePluginFile in PossiblePluginFiles)
                Plugins.AddRange(LoadPlugin(PossiblePluginFile));

            Plugins.TrimExcess();
            return Plugins;
        }
        
        /// <summary>
        /// Load plugins from file.
        /// </summary>
        /// <param name="Path">Path of plugin file.</param>
        /// <returns>List of type T plugins.</returns>
        public static List<T> LoadPlugin(String Path)
        {
            List<T> Plugins = new List<T>();
            try
            {
                Assembly Plugin = Assembly.LoadFile(Path);
                foreach (Type PluginType in Plugin.GetTypes())
                {
                    if (PluginType.IsAbstract) continue;

                    if (typeof(T).IsAssignableFrom(PluginType))
                        Plugins.Add((T)Activator.CreateInstance(PluginType));
                }
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("Unable to load '{0}'", System.IO.Path.GetFileName(Path)), ex);
            }
            Plugins.TrimExcess();
            return Plugins;
        }
    }
}
