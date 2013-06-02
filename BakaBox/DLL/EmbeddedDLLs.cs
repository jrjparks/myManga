using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace BakaBox.DLL
{
    [DebuggerStepThrough]
    [Serializable]
    public class EmbeddedDLLs
    {
        #region Constructor
        public EmbeddedDLLs()
            : this("Resources")
        { }
        public EmbeddedDLLs(String ResourceLocation)
        {
            SetEmbeddedDLLResourceLocation(ResourceLocation);
            SetExecutingAssembly(Assembly.GetEntryAssembly());
        }
        #endregion

        #region Assembly Storage
        private Dictionary<String, Assembly> _DLLs;
        public Dictionary<String, Assembly> DLLs
        {
            get
            {
                if (_DLLs == null)
                    _DLLs = new Dictionary<String, Assembly>();
                return _DLLs;
            }
        }
        #endregion

        #region Properties
        /// <summary>
        /// Location of Embedded DLLs
        /// </summary>
        public String EmbeddedDLLResourceLocation
        { get; private set; }
        /// <summary>
        /// Set location of Embedded DLLs
        /// </summary>
        /// <param name="ResourceLocation">Path to location.</param>
        public void SetEmbeddedDLLResourceLocation(String ResourceLocation)
        { EmbeddedDLLResourceLocation = ResourceLocation; }

        /// <summary>
        /// Default: EntryAssembly
        /// </summary>
        public Assembly ExecutingAssembly
        { get; private set; }
        /// <summary>
        /// Change the Executing Assembly.
        /// Default: EntryAssembly
        /// </summary>
        /// <param name="Assembly">Assembly link.</param>
        public void SetExecutingAssembly(Assembly Assembly)
        { ExecutingAssembly = Assembly; }
        #endregion

        #region Resolve Assembly
        /// <summary>
        /// <para>Use: </para>
        /// <para>AppDomain.CurrentDomain.AssemblyResolve += EmbeddedDLLs.Instance.ResolveAssembly;</para>
        /// </summary>
        /// <param name="Sender"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public Assembly ResolveAssembly(Object Sender, ResolveEventArgs args)
        {
            String resourceName = String.Format("{0}.{1}.{2}.dll", new AssemblyName(ExecutingAssembly.FullName).Name, EmbeddedDLLResourceLocation, new AssemblyName(args.Name).Name);
            if (!DLLs.ContainsKey(resourceName))
                using (Stream stream = ExecutingAssembly.GetManifestResourceStream(resourceName))
                {
                    Byte[] assemblyData = new Byte[stream.Length];
                    stream.Read(assemblyData, 0, assemblyData.Length);
                    DLLs.Add(resourceName, Assembly.Load(assemblyData));
                }
            return DLLs[resourceName];
        }
        #endregion
    }
}
