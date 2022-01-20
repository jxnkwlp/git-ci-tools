using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using NUglify;
using Pastel;

namespace Git_CI_Tools.Commands
{
    public class MinifyCommand : CommandBase
    {
        public override Command GetCommand()
        {
            var command = new Command("minify");

            command.AddCommand(CreateCommand("css"));
            command.AddCommand(CreateCommand("js"));
            command.AddCommand(CreateCommand("html"));

            return command;
        }

        private Command CreateCommand(string ext)
        {
            var command = new Command(ext);

            command.AddOption(new Option<string[]>("--include-files"));
            command.AddOption(new Option<string>("--output-file"));
            command.AddOption(new Option<string>("--dist"));

            command.Handler = CommandHandler.Create<MinifyCommandOptions>(options =>
            {
                if (options.IncludeFiles?.Any() == true)
                {
                    StringBuilder sb = new StringBuilder();

                    foreach (var item in options.IncludeFiles)
                    {
                        Console.WriteLine($"Add file '{item}' ... ");
                        sb.AppendLine(File.ReadAllText(item));
                    }

                    Console.WriteLine($"Compression to '{options.OutputFile}' ... ");

                    UglifyResult result = default;

                    try
                    {
                        if (ext == "css")
                            result = Uglify.Css(sb.ToString());
                        else if (ext == "js")
                            result = Uglify.Js(sb.ToString());
                        else if (ext == "html")
                            result = Uglify.Html(sb.ToString());
                        else
                            throw new Exception("Unknow ext.");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message.Pastel(Color.Red));
                        return;
                    }

                    if (result.HasErrors)
                    {
                        Console.WriteLine(result.Errors[0].ToString().Pastel(Color.Red));
                        return;
                    }

                    File.WriteAllText(options.OutputFile, result.Code);

                    Console.WriteLine($"🎉 Write file '{options.OutputFile}' Done".Pastel(Color.Green));
                }
                else
                {
                    int allFileCount = 0;
                    int successCount = 0;
                    int failedCount = 0;
                    Dictionary<string, string> errorFiles = new Dictionary<string, string>();

                    var files = Directory.GetFiles(options.Project, $"*.{ext}", SearchOption.AllDirectories);

                    foreach (var item in files)
                    {
                        if (item.EndsWith($".min.{ext}"))
                            continue;

                        Console.WriteLine($"Find file '{item}' ... ");
                        Console.WriteLine($"Compression '{item}' ... ");

                        allFileCount++;

                        string content = File.ReadAllText(item);

                        UglifyResult result = default;

                        try
                        {
                            if (ext == "css")
                                result = Uglify.Css(content);
                            else if (ext == "js")
                                result = Uglify.Js(content);
                            else if (ext == "html")
                                result = Uglify.Html(content);
                            else
                                throw new Exception("Unknow ext.");

                            successCount++;
                        }
                        catch (Exception ex)
                        {
                            failedCount++;
                            errorFiles.Add(item, ex.Message);
                            Console.WriteLine(ex.Message.Pastel(Color.Red));
                            continue;
                        }

                        if (result.HasErrors)
                        {
                            failedCount++;
                            errorFiles.Add(item, result.Errors[0].ToString());
                            Console.WriteLine(result.Errors[0].ToString().Pastel(Color.Red));
                            continue;
                        }

                        var outputFile = Path.ChangeExtension(item, $"min.{ext}");
                        File.WriteAllText(outputFile, result.Code);

                        if (!string.IsNullOrEmpty(options.Dist))
                        {
                            CopyTo(item, options.Project, options.Dist);
                            CopyTo(outputFile, options.Project, options.Dist);
                        }

                        Console.WriteLine($"Write file '{outputFile}' Done".Pastel(Color.Green));

                        Console.WriteLine();
                    }

                    Console.WriteLine($"🎉 Done. All:{allFileCount}, success:{successCount}, failed:{failedCount}");

                    Console.WriteLine();

                    if (failedCount > 0)
                    {
                        Console.WriteLine($"Error files: ");
                        foreach (var item in errorFiles)
                        {
                            Console.WriteLine($"file: {item.Key}\n\t{item.Value.Pastel(Color.Red)}");
                        }
                    }
                }

            });

            return command;
        }

        private static void CopyTo(string file, string basePath, string dist)
        {
            var path = Path.GetRelativePath(basePath, file);

            var target = Path.Combine(dist, path);

            if (!Directory.Exists(Path.GetDirectoryName(target)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(target));
            }

            File.Copy(file, target);
        }
    }

    public class MinifyCommandOptions
    {
        public string Project { get; set; }

        public string[] IncludeFiles { get; set; }

        public string OutputFile { get; set; }

        public string Dist { get; set; }
    }
}
