using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Git_CI_Tools
{
    public static class FileHelper
    {
        public static void AppendLine(string path, string content)
        {
            if (!File.Exists(path))
                File.WriteAllText(path, content);
            else
            {
                var fileContent = File.ReadAllText(path).Trim();
                fileContent += Environment.NewLine;
                fileContent += content.Trim();
                File.WriteAllText(path, fileContent);
            }
        }

        public static IReadOnlyList<DirectoryInfo> SearchPaths(string root, string key)
        {
            var dir = new DirectoryInfo(root);
            var find = dir.EnumerateDirectories(key, SearchOption.TopDirectoryOnly);
            return find.ToList();
        }

    }
}
