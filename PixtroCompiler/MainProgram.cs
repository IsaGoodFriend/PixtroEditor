
using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using System.Reflection;

namespace Pixtro.Compiler {
    internal static class Settings {
        public static bool Clean { get; set; }
        public static bool Debug { get; set; }
        public static bool OptimizedCode { get; set; }

        public static string ProjectPath { get; set; }
        public static string EnginePath { get; set; }
        public static string GamePath { get; set; }
        public static string DevkitProPath { get; set; }

        public static int BrickTileSize { get; set; }

        public static void SetInitialArguments(string[] args) {
            Debug = false;
            BrickTileSize = 1;
            Clean = false;
            OptimizedCode = true;
            DevkitProPath = "C:\\devkitPro";

            for (int i = 0; i < args.Length; ++i) {
                string[] arg = args[i].Split('=');


                switch (arg[0]) {
                    case "-d":
                    case "--debug":
                        Debug = true;
                        OptimizedCode = false;
                        break;
                }
            }
        }
        public static void SetArguments(string[] args) {
            for (int i = 0; i < args.Length; ++i) {
                string[] arg = args[i].Split('=');

                string exArg() => arg.Length > 1 ? arg[1] : args[++i];

                switch (arg[0]) {
                    case "-t":
                    case "--brickSize":
                        BrickTileSize = int.Parse(exArg());

                        break;
                    case "-c":
                    case "--clean":
                        Clean = true;

                        break;
                    case "-g":
                    case "--outputPath":
                    case "--gamePath":
                        GamePath = exArg();

                        break;
                    case "-e":
                    case "--enginePath":
                        EnginePath = exArg();

                        break;

                    case "--dkpPath":
                        DevkitProPath = exArg();

                        break;
                }
            }

        }
        public static void SetFolders() {
            if (ProjectPath.EndsWith("\\"))
                ProjectPath = ProjectPath.Substring(0, ProjectPath.Length - 1);
            if (EnginePath.EndsWith("\\"))
                EnginePath = EnginePath.Substring(0, EnginePath.Length - 1);
            if (GamePath.EndsWith("\\"))
                GamePath = GamePath.Substring(0, GamePath.Length - 1);
            if (DevkitProPath.EndsWith("\\"))
                DevkitProPath = ProjectPath.Substring(0, DevkitProPath.Length - 1);
        }
    }
    public static class MainProgram {

        static MainProgram() {
            yamlParse = new DeserializerBuilder().WithNamingConvention(NullNamingConvention.Instance).Build();
        }

        private static bool Error;
        private readonly static IDeserializer yamlParse;


        public static T ParseMeta<T>(string yamlData) {
            T retval = yamlParse.Deserialize<T>(yamlData);
            return retval;
        }

        private static void CopyMakefile() {
            string exeFolder = Settings.EnginePath; //Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            if (File.Exists(Settings.GamePath + ".elf"))
                File.Delete(Settings.GamePath + ".elf");
            if (File.Exists(Settings.GamePath + ".gba"))
                File.Delete(Settings.GamePath + ".gba");

            string[] replacements = new string[]
            {
                Settings.EnginePath,
                Settings.GamePath,
                Settings.Debug ? "-D __DEBUG__ " : "" + (Settings.OptimizedCode ? "-O3 " : ""),
            };

            for (int i = 0; i < replacements.Length; ++i) {
                replacements[i] = replacements[i].Replace('\\', '/');
                if (replacements[i].EndsWith("/"))
                    replacements[i] = replacements[i].Substring(0, replacements[i].Length - 1);
            }

            using (var makeRead = new StreamReader(File.Open(Path.Combine(exeFolder, "Makefile.txt"), FileMode.Open))) {
                using (var makeWrite = new StreamWriter(File.Create(Path.Combine(exeFolder, "Makefile")))) {
                    while (!makeRead.EndOfStream) {
                        string input = makeRead.ReadLine();
                        if (input.Contains('{')) {
                            for (int i = 0; i < replacements.Length; ++i) {
                                input = input.Replace($"{{{i}}}", replacements[i]);
                            }
                        }
                        makeWrite.WriteLine(input);
                    }
                }
            }

        }


        public static void Main(string[] _args) {
            Compile(Directory.GetCurrentDirectory(), _args);
        }

        public static void Compile(string projectPath, string args) {
            string[] argSplit = args.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            Compile(projectPath, argSplit);
        }

        public static bool Compile(string projectPath, string[] args) {
            Settings.SetInitialArguments(args);
            Settings.ProjectPath = projectPath.Replace('/', '\\');
            Settings.EnginePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Settings.GamePath =
                Settings.Debug ?
                    Path.Combine(Settings.EnginePath, "output") :
                    Path.Combine(Settings.ProjectPath, Path.GetDirectoryName(projectPath));

            // Check the engine.h header file for information on how to compile level (and other data maybe in the future idk)
            foreach (string s in File.ReadAllLines(Path.Combine(Settings.ProjectPath, @"source\engine.h"))) {
                if (s.StartsWith("#define")) {
                    string removeComments = s;
                    if (removeComments.Contains("/"))
                        removeComments = removeComments.Substring(0, removeComments.IndexOf('/'));

                    string[] split = removeComments.Replace('\t', ' ').Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                    switch (split[1]) {
                        case "LARGE_TILES":
                            Settings.BrickTileSize = 2;
                            break;
                    }
                }
            }

            Settings.SetArguments(args);

            // Make sure directory for build sources exists
            Directory.CreateDirectory(Path.Combine(Settings.ProjectPath, "build/source"));

#if DEBUG
            if (!Settings.Clean)
                FullCompiler.Compile();
#else
			if (Settings.Clean)
			{
				// Todo : Add cleaning functionality
			}
			else
			{
				try
				{
					FullCompiler.Compile();
				}
				catch (Exception e)
				{
					ErrorLog(e);
					Error = true;
				}
			}
#endif

            if (Error)
                return false;

            if (Settings.EnginePath.Contains(" ") || Settings.GamePath.Contains(" ") || Settings.ProjectPath.Contains(" ")) {
                return false;
            }

            CopyMakefile();

            Process cmd = new Process();
            ProcessStartInfo info = new ProcessStartInfo();
            info.FileName = "cmd.exe";

            // TODO: Figure out why the f*** the compiler fails when the window is hidden
			//info.CreateNoWindow = true;
            info.RedirectStandardInput = true;
            info.RedirectStandardError = true;
            info.UseShellExecute = false;

            cmd.StartInfo = info;
            cmd.Start();

            if (WarningOutput == null)
                WarningOutput += (a, b, c) => { };
            if (ErrorOutput == null)
                ErrorOutput += (a, b, c) => { };

            using (StreamWriter sw = cmd.StandardInput) {
                sw.WriteLine($"make -C {Settings.ProjectPath} -f {Settings.EnginePath}/Makefile {(Settings.Clean ? "clean" : "")}");
            }

            if (info.RedirectStandardError) {

                using (StreamWriter sw = new StreamWriter(File.Open("error.log", FileMode.Create))) {

                    using (StreamReader er = cmd.StandardError) {

                        string read() {
                            string line = er.ReadLine();
                            sw.WriteLine(line);
                            sw.Flush();
                            return line;
                        }

                        string projectSource = Path.Combine(Settings.ProjectPath, "source/").Replace('\\', '/'),
                            engineSource = Path.Combine(Settings.EnginePath, "src/").Replace('\\', '/');

                        while (!er.EndOfStream) {
                            string log = read();
                            string file, type, message;
                            int line;

                            string[] split;

                            if (log.Contains("/arm-none-eabi/bin/ld.exe:")) {
                                file = "";
                                type = "";
                                message = "";
                                line = 0;
                                log = log.Split("ld.exe:")[1].Trim();

                                while (!log.StartsWith(projectSource)) {
                                    log = read();
                                }

                                log = log.Replace(projectSource, "");
                                split = log.Split(':');
                                file = split[0].Trim();
                                line = int.Parse(split[1]);

                                type = "error";
                                message = split[2].Trim();
                            }
                            else if (log.StartsWith("In file included from ")) {
                                log = log.Replace("In file included from ", "");
                                if (!log.StartsWith(projectSource)) {
                                    continue;
                                }

                                log = log.Replace(projectSource, "");
                                split = log.Split(':');
                                file = split[0].Trim();
                                line = int.Parse(split[1]);

                                log = read();
                                log = log.Replace(projectSource, "").Replace(engineSource, "");

                                split = log.Split(':');
                                type = split[3].Trim();
                                message = split[4].Trim();
                            }
                            else if (log.StartsWith(projectSource)) {

                                do {
                                    log = log.Replace(projectSource, "");
                                    split = log.Split(':');

                                    log = read();
                                } while (split.Length < 5);

                                file = split[0];

                                file = split[0].Trim();
                                line = int.Parse(split[1]);

                                type = split[3].Trim();
                                message = split[4].Trim();
                            }
                            else
                                continue;

                            string switchCase = Regex.IsMatch(message, @"\[([\w-])+\]$") ? Regex.Match(message, @"\[([\w-]+)\]$").Groups[1].Value : message;

                            if (type == "warning") {

                                switch (switchCase) {
                                    case "backslash-newline at end of file":
                                    case "-Wdiscarded-qualifiers":
                                    case "incompatible implicit declaration of built-in function 'memset'":
                                    case "incompatible implicit declaration of built-in function 'memcpy'":
                                        break;
                                    default:
                                        WarningOutput(file, line, message);
                                        break;
                                }
                            }
                            else if (type == "error") {
                                switch (switchCase) {
                                    default:
                                        ErrorOutput(file, line, message);
                                        break;
                                }
                            }
                            else {

                            }
                        }
                    }
                }
            }

            cmd.WaitForExit();

            WarningOutput = null;
            ErrorOutput = null;
            StandardOutput = null;

            return File.Exists(Settings.GamePath + ".gba");
        }

        public static void ErrorLog(object log) {
            if (StandardOutput != null) {
                StandardOutput("ERROR -- " + log.ToString());
            }
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("ERROR -- ");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine(log.ToString());

            Error = true;
        }
        public static void WarningLog(object log) {
            if (StandardOutput != null) {
                StandardOutput("WARNING -- " + log.ToString());
            }
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("WARNING -- ");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine(log.ToString());
        }
        public static void Log(object log) {
            if (StandardOutput != null) {
                StandardOutput(log.ToString());
            }
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine(log.ToString());
        }
        public static void DebugLog(object log) {
#if DEBUG
            if (StandardOutput != null) {
                StandardOutput(log.ToString());
            }
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(log.ToString());
#endif
        }

        public static event Action<string> StandardOutput;
        public static event Action<string /*file*/, int /*line number*/, string /*warning info*/> WarningOutput, ErrorOutput;
    }
}