using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace myManga_App.IO.DLL
{
    /// <summary>
    /// Class to load DLLs
    /// </summary>
    /// <typeparam name="ItemType">Class type to load</typeparam>
    [DebuggerStepThrough]
    public sealed class Loader<ItemType>
    {
        /// <summary>
        /// Generic constructor
        /// </summary>
        private Loader() { }

        /// <summary>
        /// Load Classes from Directory
        /// </summary>
        /// <param name="DirectoryPath">Directory of DLLs</param>
        /// <param name="Filter">File filter</param>
        /// <param name="DirectorySearchOption">How to search the Directory</param>
        /// <returns>List of classes</returns>
        public static List<ItemType> LoadDirectory(String DirectoryPath, String Filter = "*.dll", SearchOption DirectorySearchOption = SearchOption.TopDirectoryOnly)
        {
            List<ItemType> DLLs = new List<ItemType>();
            String[] PossibleFiles = Directory.GetFiles(DirectoryPath, Filter, DirectorySearchOption);

            foreach (String PossibleFile in PossibleFiles)
                DLLs.AddRange(Load(PossibleFile));

            DLLs.TrimExcess();
            return DLLs;
        }

        /// <summary>
        /// Load Classes from file
        /// </summary>
        /// <param name="DLLPath">Path of DLL file</param>
        /// <returns>List of classes</returns>
        public static List<ItemType> Load(String DLLPath)
        {
            List<ItemType> Classes = new List<ItemType>();
            try
            {
                Assembly LoadedAssembly = Assembly.LoadFile(DLLPath);
                foreach (Type ClassType in LoadedAssembly.GetTypes())
                {
                    if (ClassType.IsAbstract) continue;
                    if (typeof(ItemType).IsAssignableFrom(ClassType))
                        Classes.Add((ItemType)Activator.CreateInstance(ClassType));
                }
            }
            catch (Exception ex)
            { throw new Exception(String.Format("Unable to load '{0}'", Path.GetFileName(DLLPath)), ex); }

            Classes.TrimExcess();
            return Classes;
        }
    }
}
