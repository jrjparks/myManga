using System;
using System.Diagnostics;
using System.IO;

namespace BakaBox.IO
{
    [DebuggerStepThrough]
    public static class Naming
    {
        /// <summary>
        /// Creates the file path if it does not exist.
        /// </summary>
        /// <param name="FolderPath">Folder Path.</param>
        /// <returns>Folder Path.</returns>
        public static String SafeFolder(this String FolderPath)
        { return FolderPath.SafeFolder(true); }
        /// <summary>
        /// Creates the file path if it does not exist.
        /// </summary>
        /// <param name="FolderPath">Folder Path.</param>
        /// <returns>Folder Path.</returns>
        public static String SafeFolder(this String FolderPath, Boolean CreateFolder)
        {
            foreach (Char InvalidChar in Path.GetInvalidPathChars())
                FolderPath = FolderPath.Replace(InvalidChar.ToString(), String.Empty);

            String[] Directories = FolderPath.Substring(3).Split('\\');
            FolderPath = Directory.GetDirectoryRoot(FolderPath);
            foreach (String _Directory in Directories)
                FolderPath = Path.Combine(FolderPath, SafeFileName(_Directory));

            if (!File.Exists(FolderPath) &&
                !Directory.Exists(FolderPath))
                Directory.CreateDirectory(FolderPath);
            return FolderPath;
        }

        /// <summary>
        /// Validates and cleans the file name if it does not exist.
        /// </summary>
        /// <param name="FileName">File name.</param>
        /// <returns>FileName.</returns>
        public static String SafeFileName(this String FileName)
        {
            foreach (Char InvalidChar in Path.GetInvalidFileNameChars())
                FileName = FileName.Replace(InvalidChar.ToString(), String.Empty);
            return FileName;
        }
    }
}
