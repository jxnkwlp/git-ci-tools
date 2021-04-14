using System;
using System.IO;

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
	}
}
