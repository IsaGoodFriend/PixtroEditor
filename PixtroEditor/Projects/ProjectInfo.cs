using Monocle;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Pixtro.Editor;

namespace Pixtro.Projects {
	public sealed class ProjectInfo : IDisposable {

		public static ProjectInfo CurrentProject { get; private set; }
		public static bool Building => buildThread != null && buildThread.IsAlive;
		public static bool BuildSuccess { get; private set; }

		private static event Action OnSuccessfulBuild;

		public static void OpenProject(string filePath) {
			if (CurrentProject != null) {
				CurrentProject.Dispose();
			}
			CurrentProject = new ProjectInfo(filePath);

			filePath = Path.GetDirectoryName(filePath);
		}

		public static void BuildProject(bool release) {

			BuildSuccess = false;
			ReleaseBuild = release;

			Compiler.MainProgram.StandardOutput += Scenes.ConsoleScene.Log;
			Compiler.MainProgram.WarningOutput += Scenes.ConsoleScene.CompilerWarning;
			Compiler.MainProgram.ErrorOutput += Scenes.ConsoleScene.CompilerError;

			if (CurrentProject.BuiltRelease != release) {
				CurrentProject.CleanProject();
				CurrentProject.BuiltRelease = release;
			}

			buildThread = new Thread(BuildThread);
			buildThread.Start();
		}

		public static void BuildAndRun() {
			BuildProject(false);
			OnSuccessfulBuild += () => {
				Engine.OverloadGameLoop = () => {
					Emulation.EmulationHandler.LoadGame(Path.Combine(Directory.GetCurrentDirectory(), "output.gba"));
					Engine.OverloadGameLoop = null;
				};
			};
		}

		private static Version CurrentFormatVersion = new Version(1, 0, 0);


		static Thread buildThread;
		private static bool ReleaseBuild;
		private static void BuildThread() {

#if DEBUG
			string sourceDir = Directory.GetCurrentDirectory();
			sourceDir = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(sourceDir))));
			sourceDir = Path.Combine(sourceDir, "PixtroEngine\\source");
			string output = Path.Combine(Directory.GetCurrentDirectory(), "src");
			foreach (var file in Directory.GetFiles(sourceDir)) {
				string path = file.Replace(sourceDir, output);
				File.Copy(file, path, true);
			}
#endif
			List<string> args = new List<string>();

			if (!ReleaseBuild) {
				args.Add("--debug");
			}

#if DEBUG
			bool success = Compiler.MainProgram.Compile(CurrentProject.ProjectDirectory, args.ToArray());
#else
			try
			{
				success = Compiler.Compiler.Compile(CurrentProject.ProjectDirectory, args.ToArray());
			}
			catch (Exception e)
			{
				success = false;
				Console.WriteLine(e);
			}
#endif

			BuildSuccess = success;
			if (success && OnSuccessfulBuild != null) {
				OnSuccessfulBuild();
			}
		}


		public readonly string ProjectPath;
		public string ProjectDirectory => Path.GetDirectoryName(ProjectPath);
		private Version formatVersion;
		private BinaryFileWriter nodes;

		public bool BuiltRelease = false;

		private bool dirty;
		public bool Dirty {
			get { return dirty; }
			set {
				if (value)
					dirty = true;
			}
		}

		public string Name => Path.GetFileNameWithoutExtension(ProjectPath);

		public string CurrentLevelPack { get; set; }

		private ProjectInfo(string path) {
			ProjectPath = path;

			if (File.Exists(path)) {
				InitFromFile(path);
			}
			else {
				Init();
			}

		}
		private void InitFromFile(string path) {
			try {
				var parsed = new BinaryFileParser(path, "pixtro");

				nodes = new BinaryFileWriter();
				nodes.Nodes = parsed.Nodes;
			}
			catch {
				// todo: do stuff if fails
			}

			var node = nodes["Version"];

			formatVersion = new Version(node.GetInteger("Major"), node.GetInteger("Minor"), node.GetInteger("Build"));

			if (formatVersion > CurrentFormatVersion) {
				// Oops, you opened up something from the future D:

				//var result = MessageBox.Show("Are you a time traveller?  This version of pixtro is a newer version than expected and may not load correctly.  Do you want to proceed?", "Pixtro", MessageBoxButtons.YesNo);

				//if (result == DialogResult.No) {
				//	throw new FormatException();
				//}
			}

			if (formatVersion.Major != CurrentFormatVersion.Major) {
				// This shouldn't happen, but in case it does, figure out how to parse and reformat the data.  Version 2 shouldn't exist though
				throw new FormatException();

			}
			else if (formatVersion < CurrentFormatVersion) {
				// For any time something is added, removed, or changed.
				// Not used yet, but just in case
			}

		}
		private void Init() {
			nodes = new BinaryFileWriter();
			formatVersion = new Version(CurrentFormatVersion.ToString());

			BinaryFileNode node = nodes.AddNode("Version");

			node.AddAttribute("Major", CurrentFormatVersion.Major);
			node.AddAttribute("Minor", CurrentFormatVersion.Minor);
			node.AddAttribute("Build", CurrentFormatVersion.Build);

			Save();
		}

		public void CleanProject() {
			if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "dll", "output.elf")))
				File.Delete(Path.Combine(Directory.GetCurrentDirectory(), "dll", "output.elf"));
			if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "dll", "output.gba")))
				File.Delete(Path.Combine(Directory.GetCurrentDirectory(), "dll", "output.gba"));

			if (Directory.Exists(ProjectDirectory + "\\build"))
				Directory.Delete(ProjectDirectory + "\\build", true);
		}
		public void Save() {
			nodes.Save(ProjectPath, "pixtro");

			dirty = false;
		}

		public void Dispose() {
			nodes.Dispose();
		}
	}
}