using System.Collections.Generic;
using System.IO;
using GUZ.Core.Util;
using Logger = GUZ.Core.Util.Logger;

namespace GUZ.Core.Domain.Config
{
    public static class IniLoader
    {
        public static Dictionary<string, string> LoadFile(string filePath)
        {
            var filePathCaseInsensitive = FileSearchHandler.FindFileCaseInsensitive(filePath);
            if (filePathCaseInsensitive == null)
            {
                Logger.LogError($"The Gothic.ini/GothicGame.ini file does not exist at the specified path: {filePath}", LogCat.Loading);
                return null;
            }

            var data = new Dictionary<string, string>();

            foreach (var line in File.ReadLines(filePathCaseInsensitive))
            {
                var trimmedLine = line.Trim();
                if (string.IsNullOrWhiteSpace(trimmedLine) || trimmedLine.StartsWith(";"))
                {
                    continue;
                }

                // We don't need to store [section] information. Every property name is unique.
                if (trimmedLine.StartsWith("[") && trimmedLine.EndsWith("]"))
                {
                    continue;
                }
                else
                {
                    var keyValue = trimmedLine.Split(new[] { '=' }, 2);
                    if (keyValue.Length == 2)
                    {
                        data[keyValue[0].Trim()] = keyValue[1].Trim();
                    }
                }
            }

            return data;
        }
    }
}
