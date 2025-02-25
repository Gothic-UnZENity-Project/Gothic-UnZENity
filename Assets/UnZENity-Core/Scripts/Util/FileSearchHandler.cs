using System;
using System.IO;

namespace GUZ.Core.Util
{
    public class FileSearchHandler
    {
        public static string FindFileCaseInsensitive(string fullPath)
        {
            // Extract directory and filename components
            string directory = Path.GetDirectoryName(fullPath);
            string targetFileName = Path.GetFileName(fullPath);

            if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
            {
                throw new DirectoryNotFoundException($"Directory not found: {directory}");
            }

            // Case-insensitive search in the target directory
            foreach (string filePath in Directory.EnumerateFiles(directory))
            {
                string currentFileName = Path.GetFileName(filePath);
                if (currentFileName.Equals(targetFileName, StringComparison.OrdinalIgnoreCase))
                {
                    return filePath; // Returns actual case-sensitive path
                }
            }

            return null; // File not found
        }
    }
}
