using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Reflection;
using System.IO;

namespace Core.DLL
{
    [Serializable, DebuggerStepThrough]
    public sealed class EmbeddedDLL
    {
        private readonly Assembly ExecutingAssembly;
        private readonly List<String> _ManifestResourceNames = new List<String>();

        private readonly Dictionary<String, Assembly> _DLLs = new Dictionary<String, Assembly>();
        public Dictionary<String, Assembly> DLLs { get { return _DLLs; } }

        public EmbeddedDLL(Assembly ExecutingAssembly = null, Boolean PreLoadDLLs = false)
        {
            this.ExecutingAssembly = ExecutingAssembly ?? Assembly.GetEntryAssembly();
            _ManifestResourceNames.AddRange(this.ExecutingAssembly.GetManifestResourceNames().Where((rn) => rn.EndsWith(".dll")));
        }

        private void PreLoadDLLs()
        {
            foreach (String ResourceName in this._ManifestResourceNames)
            {
                if (!this.DLLs.ContainsKey(ResourceName))
                {
                    using (Stream stream = ExecutingAssembly.GetManifestResourceStream(ResourceName))
                    {
                        if (stream != null)
                        {
                            Byte[] assemblyData = new Byte[stream.Length];
                            stream.Read(assemblyData, 0, assemblyData.Length);
                            DLLs.Add(ResourceName, Assembly.Load(assemblyData));
                        }
                    }
                }
            }
        }

        public Assembly ResolveAssembly(Object Sender, ResolveEventArgs args)
        {
            AssemblyName ResourceAssemblyName = new AssemblyName(args.Name);
            String ResourceName = _ManifestResourceNames.FirstOrDefault((s) => s.Contains(ResourceAssemblyName.Name));
            if (ResourceName != null)
            {
                if (!DLLs.ContainsKey(ResourceName))
                    using (Stream stream = ExecutingAssembly.GetManifestResourceStream(ResourceName))
                    {
                        if (stream != null)
                        {
                            Byte[] assemblyData = new Byte[stream.Length];
                            stream.Read(assemblyData, 0, assemblyData.Length);
                            DLLs.Add(ResourceName, Assembly.Load(assemblyData));
                        }
                    }
                if (DLLs.ContainsKey(ResourceName))
                    return DLLs[ResourceName];
            }
            return null;
        }
    }
}
