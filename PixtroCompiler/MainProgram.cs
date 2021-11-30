
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Drawing;
using Newtonsoft.Json;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using System.Reflection;

namespace Pixtro.Compiler
{
	public struct FloatColor
	{
		public float R, G, B, A;

		public FloatColor(byte r, byte g, byte b, byte a)
		{
			R = r / 255f;
			G = g / 255f;
			B = b / 255f;
			A = a / 255f;

			if (A == 0)
			{
				R = 0;
				G = 0;
				B = 0;
			}
		}

		public static FloatColor FlattenColor(FloatColor colorA, FloatColor colorB, BlendType blend)
		{
			FloatColor color = colorA;

			if (colorB.A <= 0)
				return colorA;

			switch (blend)
			{
				case BlendType.Normal:
					color.R = colorB.R;
					color.G = colorB.G;
					color.B = colorB.B;
					color.A = Math.Max(colorA.A, colorB.A);
					break;
			}

			return color;
		}

		public ushort ToGBA(ushort _transparent = 0x8000)
		{
			if (A <= 0)
				return _transparent;

			int r = (int)(R * 255);
			int g = (int)(G * 255);
			int b = (int)(B * 255);

			r = (r & 0xF8) >> 3;
			g = (g & 0xF8) >> 3;
			b = (b & 0xF8) >> 3;

			return (ushort)(r | (g << 5) | (b << 10));
		}
		public Color ToGBAColor()
		{
			if (A <= 0.5f)
			{
				return Color.FromArgb(0, 0, 0, 0);
			}

			byte r = (byte)(Math.Floor(R * 31) * 4);
			byte g = (byte)(Math.Floor(G * 31) * 4);
			byte b = (byte)(Math.Floor(B * 31) * 4);

			return Color.FromArgb(255, r, g, b);
		}

		public override string ToString()
		{
			return $"{{{R:0.00} - {G:0.00} - {B:0.00} :: {A:0.00}}}";
		}
	}
	internal static class Settings
	{
		public static bool Clean { get; set; }
		public static bool Debug { get; set; }
		public static bool OptimizedCode { get; set; }

		public static string ProjectPath { get; set; }
		public static string EnginePath { get; set; }
		public static string GamePath { get; set; }
		public static string DevkitProPath { get; set; }

		public static int BrickTileSize { get; set; }

		public static void SetInitialArguments(string[] args)
		{
			Debug = false;
			BrickTileSize = 1;
			Clean = false;
			OptimizedCode = true;
			DevkitProPath = "C:\\devkitPro";

			for (int i = 0; i < args.Length; ++i)
			{
				string[] arg = args[i].Split('=');


				switch (arg[0])
				{
					case "-d":
					case "--debug":
						Debug = true;
						OptimizedCode = false;
						break;
				}
			}
		}
		public static void SetArguments(string[] args)
		{
			for (int i = 0; i < args.Length; ++i)
			{
				string[] arg = args[i].Split('=');

				string exArg() => arg.Length > 1 ? arg[1] : args[++i];

				switch (arg[0])
				{
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
		public static void SetFolders()
		{
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
	public class PointConverter : JsonConverter<Point>
	{
		public override Point ReadJson(JsonReader reader, Type objectType, Point existingValue, bool hasExistingValue, JsonSerializer serializer)
		{

			var points = (reader.Value as string).Split(',');

			return new Point(int.Parse(points[0].Trim()), int.Parse(points[1].Trim()));
		}

		public override void WriteJson(JsonWriter writer, Point value, JsonSerializer serializer)
		{
		}
	}
	public static class MainProgram
	{

		static MainProgram()
		{
			yamlParse = new DeserializerBuilder().WithNamingConvention(NullNamingConvention.Instance).Build();
		}

		private static bool Error;
		private readonly static IDeserializer yamlParse;


		public static T ParseMeta<T>(string yamlData)
		{
			T retval = yamlParse.Deserialize<T>(yamlData);
			return retval;
		}

		private static void CopyMakefile()
		{
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
				"",
			};

			for (int i = 0; i < replacements.Length; ++i)
			{
				replacements[i] = replacements[i].Replace('\\', '/');
				if (replacements[i].EndsWith("/"))
					replacements[i] = replacements[i].Substring(0, replacements[i].Length - 1);
			}

			using (var makeRead = new StreamReader(File.Open(Path.Combine(exeFolder, "Makefile.txt"), FileMode.Open)))
			{
				using (var makeWrite = new StreamWriter(File.Create(Path.Combine(exeFolder, "Makefile"))))
				{
					while (!makeRead.EndOfStream)
					{
						string input = makeRead.ReadLine();
						if (input.Contains('{'))
						{
							for (int i = 0; i < replacements.Length; ++i)
							{
								input = input.Replace($"{{{i}}}", replacements[i]);
							}
						}
						makeWrite.WriteLine(input);
					}
				}
			}

		}


		public static void Main(string[] _args)
		{
			Compile(Directory.GetCurrentDirectory(), _args);
		}

		public static void Compile(string projectPath, string args)
		{
			string[] argSplit = args.Split(new char[]{ ' ' }, StringSplitOptions.RemoveEmptyEntries);

			Compile(projectPath, argSplit);
		}

		public static bool Compile(string projectPath, string[] args) {
			Settings.SetInitialArguments(args);
			Settings.ProjectPath = projectPath.Replace('/', '\\');
			Settings.EnginePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			Settings.GamePath =
				Settings.Debug ?
					Path.Combine(Settings.EnginePath, "dll", "output") :
					Path.Combine(Settings.ProjectPath, Path.GetDirectoryName(projectPath));

			//Settings.Clean = true;

			// Check the engine.h header file for information on how to compile level (and other data maybe in the future idk)
			foreach (string s in File.ReadAllLines(Path.Combine(Settings.ProjectPath, @"source\engine.h"))) {
				if (s.StartsWith("#define")) {
					string removeComments = s;
					if (removeComments.Contains("/"))
						removeComments = removeComments.Substring(0, removeComments.IndexOf('/'));

					string[] split = removeComments.Replace('\t', ' ').Split(new char[] {' ' }, StringSplitOptions.RemoveEmptyEntries);

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
			info.FileName = Path.Combine(Settings.DevkitProPath, "msys2\\usr\\bin\\make.exe");
			info.Arguments = $"-C {Settings.ProjectPath} -f {Settings.EnginePath}/Makefile {(Settings.Clean ? "clean" : "")}";

			if (StandardOutput != null) {

				info.RedirectStandardError = true;
			}

			info.CreateNoWindow = true;

			cmd.StartInfo = info;
			cmd.Start();

			if (StandardOutput != null) {
				using (var error = cmd.StandardError) {
					while (!error.EndOfStream) {
						string line = error.ReadLine();


					}
				}
			}

			cmd.WaitForExit();

			StandardOutput = null;

			return File.Exists(Settings.GamePath + ".gba");
		}

		public static void ErrorLog(object log)
		{
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
		public static void DebugLog(object log)
		{
#if DEBUG
			if (StandardOutput != null) {
				StandardOutput(log.ToString());
			}
			Console.ForegroundColor = ConsoleColor.White;
			Console.WriteLine(log.ToString());
#endif
		}

		public static event Action<string> StandardOutput;
		public static event Action<string, int, string> Warning;
	}
}