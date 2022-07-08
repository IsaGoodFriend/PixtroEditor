using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace Monocle {
	public static class FileUpdateManager {

		static FileSystemWatcher updateWatcher;
		static string mainDirectory;

		public static event Action<string, string> fileAdded;
		public static event Action<string, string> fileModified;
		public static event Action<string, string> fileDeleted;

		public static void SetDirectory(string directory) {
			updateWatcher?.Dispose();

			updateWatcher = new FileSystemWatcher(directory);

			updateWatcher.IncludeSubdirectories = true;

			updateWatcher.BeginInit();
			updateWatcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.CreationTime | NotifyFilters.FileName;
			updateWatcher.EndInit();

			mainDirectory = directory;
			Start();
		}

		private static bool IgnoreFile(string path) {
			if (path.Contains("\\.git\\") || Path.GetExtension(path) == "")
				return true;
			string localPath = Path.GetRelativePath(mainDirectory, path);

			if (localPath.StartsWith("build\\"))
				return true;

			return false;
		}

		private static void UpdateWatcher_Renamed(object sender, RenamedEventArgs e) {
			if (IgnoreFile(e.FullPath))
				return;

			string mainfolder = e.Name.Split('\\')[0];

			updateWatcher.EnableRaisingEvents = false;
			fileDeleted(mainfolder, e.OldFullPath);
			fileAdded(mainfolder, e.FullPath);
			updateWatcher.EnableRaisingEvents = true;
		}

		private static void UpdateWatcher_Changed(object sender, FileSystemEventArgs e) {
			if (IgnoreFile(e.FullPath))
				return;

			string mainfolder = e.Name.Split('\\')[0];

			updateWatcher.EnableRaisingEvents = false;
			fileModified(mainfolder, e.FullPath);
			updateWatcher.EnableRaisingEvents = true;
		}

		private static void UpdateWatcher_Deleted(object sender, FileSystemEventArgs e) {
			if (IgnoreFile(e.FullPath))
				return;

			string mainfolder = e.Name.Split('\\')[0];

			updateWatcher.EnableRaisingEvents = false;
			fileDeleted(mainfolder, e.FullPath);
			updateWatcher.EnableRaisingEvents = true;
		}

		private static void UpdateWatcher_Created(object sender, FileSystemEventArgs e) {
			if (IgnoreFile(e.FullPath))
				return;

			string mainfolder = e.Name.Split('\\')[0];

			updateWatcher.EnableRaisingEvents = false;
			fileAdded(mainfolder, e.FullPath);
			updateWatcher.EnableRaisingEvents = true;
		}

		public static void Start() {

			updateWatcher.Created += UpdateWatcher_Created;
			updateWatcher.Deleted += UpdateWatcher_Deleted;
			updateWatcher.Changed += UpdateWatcher_Changed;
			updateWatcher.Renamed += UpdateWatcher_Renamed;

			updateWatcher.EnableRaisingEvents = true;
		}

	}
}
