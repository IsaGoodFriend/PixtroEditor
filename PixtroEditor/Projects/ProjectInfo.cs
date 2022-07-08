using Monocle;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Pixtro.Editor;
using Newtonsoft.Json;

namespace Pixtro.Projects {
	public sealed class ProjectInfo : IDisposable {

		public static ProjectInfo CurrentProject { get; private set; }
		public static bool Building => buildThread != null && buildThread.IsAlive;
		public static bool BuildSuccess { get; private set; }

		private static event Action OnSuccessfulBuild;

		public Dictionary<string, Scenes.LevelPack> VisualPacks;

		private Dictionary<string, string[]> LevelVisualPacks;

		public static void OpenProject(string filePath) {
			if (CurrentProject != null) {
				CurrentProject.Dispose();
			}
			CurrentProject = new ProjectInfo(filePath);
		}

		public static void BuildProject(bool release) {

			BuildSuccess = false;
			ReleaseBuild = release;

			Compiler.MainProgram.StandardOutput += Scenes.ConsoleScene.Log;
			Compiler.MainProgram.WarningOutput += Scenes.ConsoleScene.CompilerWarning;
			Compiler.MainProgram.ErrorOutput += Scenes.ConsoleScene.CompilerError;

			if (CurrentProject.justLoaded || CurrentProject.BuiltRelease != release) {
				CurrentProject.CleanProject();
				CurrentProject.BuiltRelease = release;
				CurrentProject.justLoaded = false;
			}

			buildThread = new Thread(BuildThread);
			buildThread.Start();
		}

		public static void RunBuild() {
			if (Building) {
				OnSuccessfulBuild += () => {
					Engine.OverloadGameLoop = () => {
						Emulation.EmulationHandler.LoadGame(Path.Combine(Directory.GetCurrentDirectory(), "output.gba"));
						Engine.OverloadGameLoop = null;
					};
				};
			}
			else {
				Engine.OverloadGameLoop = () => {
					Emulation.EmulationHandler.LoadGame(Path.Combine(Directory.GetCurrentDirectory(), "output.gba"));
					Engine.OverloadGameLoop = null;
				};
			}
		}

		public static void BuildRelease() {
			BuildProject(true);
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
				OnSuccessfulBuild = null;
			}
		}


		public readonly string ProjectDirectory;
		public string ProjectPath => Path.Combine(ProjectDirectory, projectFile + ".pxprj");
		private string projectFile;
		private Version formatVersion;
		private BinaryFileWriter nodes;

		public bool BuiltRelease;
		private bool justLoaded = true;

		private bool dirty;
		public bool Dirty {
			get { return dirty; }
			set {
				if (value)
					dirty = true;
			}
		}

		public string Name => projectFile;

		public string CurrentLevelPack { get; set; }

		private ProjectInfo(string path) {
			ProjectDirectory = Path.GetDirectoryName(path);
			projectFile = Path.GetFileNameWithoutExtension(path);

			FileUpdateManager.SetDirectory(ProjectDirectory);

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

		public Scenes.LevelPack GetPack(string level) {
			level = Path.ChangeExtension(level, null).Replace('\\', '/');
			foreach (var pair in LevelVisualPacks)
				if (pair.Value.Contains(level))
					foreach (var pack in VisualPacks.Values)
						if (pack.VisualData.LevelPacks != null && pack.VisualData.LevelPacks.Contains(pair.Key))
							return pack;
				

			return null;
		}

		public void LoadContent() {
			
			LevelVisualPacks = new Dictionary<string, string[]>();

			foreach (var file in Directory.EnumerateFiles(Path.Combine(ProjectDirectory, "levels", "_packs"))) {
				LevelVisualPacks.Add(Path.GetFileNameWithoutExtension(file), File.ReadAllLines(file));
			}

			var dictionary = JsonConvert.DeserializeObject<Dictionary<string, Compiler.VisualPackMetadata>>(File.ReadAllText(Path.Combine(ProjectDirectory, "levels", "meta_level.json")));

			VisualPacks = new Dictionary<string, Scenes.LevelPack>();
			var globalPack = dictionary["global"];
			foreach (var p in dictionary) {
				if (p.Value.Wrapping == null)
					continue;
				p.Value.Name = p.Key;

				foreach (char c in p.Value.Wrapping.Keys) {
					var wrap = p.Value.Wrapping[c];

					if (wrap.MappingCopy != null) {
						string[] split = wrap.MappingCopy.Split('/', '\\');

						var otherWrap = dictionary[split[0]].Wrapping[split[1][0]];

						wrap.Mapping = otherWrap.Mapping;
						wrap.TileMapping = otherWrap.TileMapping;
					}

					wrap.FinalizeMasks();
				}
				if (p.Value.EntityIndex == null)
					p.Value.EntityIndex = new Dictionary<string, int>();
				foreach (var pair in globalPack.EntityIndex) {
					p.Value.EntityIndex[pair.Key] = pair.Value;
				}
				if (p.Value.EntitySprites == null)
					p.Value.EntitySprites = new Dictionary<int, Compiler.VisualPackMetadata.EntityPreview>();
				foreach (var pair in globalPack.EntitySprites) {
					p.Value.EntitySprites[pair.Key] = pair.Value;
				}

				VisualPacks.Add(p.Key, new Scenes.LevelPack(p.Value));
			}
		}

		public void Dispose() {
			nodes.Dispose();
		}
	}
}