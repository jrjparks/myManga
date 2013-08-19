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
    public class EmbeddedDLL
    {
        public EmbeddedDLL(String ResourceLocation = "Resources")
        {
            SetEmbeddedDLLResourceLocation(ResourceLocation);
            SetExecutingAssembly(Assembly.GetEntryAssembly());
        }

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
        public String EmbeddedDLLResourceLocation
        { get; private set; }

        public void SetEmbeddedDLLResourceLocation(String ResourceLocation)
        { EmbeddedDLLResourceLocation = ResourceLocation; }

        public Assembly ExecutingAssembly
        { get; private set; }

        public void SetExecutingAssembly(Assembly Assembly)
        { ExecutingAssembly = Assembly; }

        public Assembly ResolveAssembly(Object Sender, ResolveEventArgs args)
        {
            String resourceName = String.Format("{0}.{1}.{2}.dll",
                new AssemblyName(ExecutingAssembly.FullName).Name,
                EmbeddedDLLResourceLocation,
                new AssemblyName(args.Name).Name);
            if (!DLLs.ContainsKey(resourceName))
                using (Stream stream = ExecutingAssembly.GetManifestResourceStream(resourceName))
                {
                    Byte[] assemblyData = new Byte[stream.Length];
                    stream.Read(assemblyData, 0, assemblyData.Length);
                    DLLs.Add(resourceName, Assembly.Load(assemblyData));
                }
            return DLLs[resourceName];
        }
    }
}
