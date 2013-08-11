using System.Diagnostics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Core.DLL
{
    /// <summary>
    /// Class to load DLLs
    /// </summary>
    /// <typeparam name="T">Class type to load</typeparam>
    [DebuggerStepThrough]
    public sealed class DLL_Loader<T>
    {
        /// <summary>
        /// Generic constructor
        /// </summary>
        private DLL_Loader() { }

        /// <summary>
        /// Load Classes from Directory
        /// </summary>
        /// <param name="DirectoryPath">Directory of DLLs</param>
        /// <param name="Filter">File filter</param>
        /// <param name="DirectorySearchOption">How to search the Directory</param>
        /// <returns>List of classes</returns>
        public static T[] LoadDirectory(String DirectoryPath, String Filter = "*.dll", SearchOption DirectorySearchOption = SearchOption.TopDirectoryOnly)
        {
            List<T> DLLs = new List<T>();
            String[] PossibleFiles = Directory.GetFiles(DirectoryPath, Filter, DirectorySearchOption);

            foreach (String PossibleFile in PossibleFiles)
                DLLs.AddRange(LoadDLL(PossibleFile));

            DLLs.TrimExcess();
            return DLLs.ToArray();
        }

        /// <summary>
        /// Load Classes from file
        /// </summary>
        /// <param name="DLLPath">Path of DLL file</param>
        /// <returns>List of classes</returns>
        public static T[] LoadDLL(String DLLPath)
        {
            List<T> Classes = new List<T>();
            try
            {
                Assembly Class = Assembly.LoadFile(DLLPath);
                foreach (Type ClassType in Class.GetTypes())
                {
                    if (ClassType.IsAbstract) continue;
                    if (typeof(T).IsAssignableFrom(ClassType))
                        Classes.Add((T)Activator.CreateInstance(ClassType));
                }
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("Unable to load '{0}'", Path.GetFileName(DLLPath)), ex);
            }
            Classes.TrimExcess();
            return Classes.ToArray();
        }
    }
}
