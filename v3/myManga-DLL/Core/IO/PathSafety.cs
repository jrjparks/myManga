using System;
using System.Diagnostics;
using System.IO;

namespace Core.IO
{
    [DebuggerStepThrough]
    public static class PathSafety
    {
        public static String SafeFolder(this String IOPath, Boolean Create = true)
        {
            foreach (Char InvalidChar in Path.GetInvalidPathChars())
                IOPath = IOPath.Replace(InvalidChar.ToString(), String.Empty);
            String[] Directories = IOPath.Substring(3).Split('\\');
            IOPath = Directory.GetDirectoryRoot(IOPath);
            foreach (String _Directory in Directories)
                IOPath = Path.Combine(IOPath, SafeFileName(_Directory));
            if (!File.Exists(IOPath) && !Directory.Exists(IOPath) && Create)
                Directory.CreateDirectory(IOPath);
            return IOPath;
        }

        public static String SafeFileName(this String IOPath)
        {
            foreach (Char InvalidChar in Path.GetInvalidFileNameChars())
                IOPath = IOPath.Replace(InvalidChar.ToString(), String.Empty);
            return IOPath;
        }
    }
}
