using System;
using System.Diagnostics;
using System.IO;

namespace Core.IO
{
    [DebuggerStepThrough]
    public static class PathSafety
    {
        /// <summary>
        /// Verify the path exists and create it if need be
        /// </summary>
        /// <param name="IOPath">Path to check</param>
        /// <param name="Create">Create it if the path does not exist</param>
        /// <returns></returns>
        public static String SafeFolder(this String IOPath, Boolean Create = true)
        {
            if (IOPath.Contains(":\\"))
            {
                foreach (Char InvalidChar in Path.GetInvalidPathChars())
                    IOPath = IOPath.Replace(InvalidChar.ToString(), String.Empty);
                String[] Directories = IOPath.Substring(3).Split('\\');
                IOPath = Directory.GetDirectoryRoot(IOPath);
                foreach (String _Directory in Directories)
                    IOPath = Path.Combine(IOPath, _Directory.SafeFileName());
                if (!File.Exists(IOPath) && !Directory.Exists(IOPath) && Create)
                    Directory.CreateDirectory(IOPath);
            }
            return IOPath.SafeFileName();
        }

        /// <summary>
        /// Path to check for invalid characters
        /// </summary>
        /// <param name="IOPath"></param>
        /// <returns></returns>
        public static String SafeFileName(this String IOPath)
        {
            foreach (Char InvalidChar in Path.GetInvalidFileNameChars())
                IOPath = IOPath.Replace(InvalidChar.ToString(), String.Empty);
            return IOPath;
        }
    }
}
