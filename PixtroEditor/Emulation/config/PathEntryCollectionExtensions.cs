﻿using System;
using System.IO;

namespace Pixtro.Emulation
{
	public static class PathEntryExtensions
	{
		/// <summary>
		/// Returns the base path of the given system.
		/// If the system can not be found, an empty string is returned
		/// </summary>
		public static string BaseFor(this PathEntryCollection collection, string systemId)
		{
			return string.IsNullOrWhiteSpace(systemId)
				? ""
				: collection[systemId, "Base"]?.Path ?? "";
		}

		public static string GlobalBaseAbsolutePath(this PathEntryCollection collection)
		{
			var globalBase = collection["Global", "Base"].Path;

			// if %exe% prefixed then substitute exe path and repeat
			if (globalBase.StartsWith("%exe%", StringComparison.InvariantCultureIgnoreCase))
			{
				globalBase = PathUtils.ExeDirectoryPath + globalBase.Substring(5);
			}

			// rooted paths get returned without change
			// (this is done after keyword substitution to avoid problems though)
			if (Path.IsPathRooted(globalBase))
			{
				return globalBase;
			}

			// not-rooted things are relative to exe path
			globalBase = Path.Combine(PathUtils.ExeDirectoryPath, globalBase);
			return globalBase;
		}

		/// <summary>
		/// Returns an entry for the given system and pathType (ROM, screenshot, etc)
		/// but falls back to the base system or global system if it fails
		/// to find pathType or systemId
		/// </summary>
		public static PathEntry EntryWithFallback(this PathEntryCollection collection, string pathType, string systemId)
		{
			return (collection[systemId, pathType]
				?? collection[systemId, "Base"])
				?? collection["Global", "Base"];
		}

		public static string AbsolutePathForType(this PathEntryCollection collection, string systemId, string type)
		{
			var path = collection.EntryWithFallback(type, systemId).Path;
			return collection.AbsolutePathFor(path, systemId);
		}

		/// <summary>
		/// Returns an absolute path for the given relative path.
		/// If provided, the systemId will be used to generate the path.
		/// Wildcards are supported.
		/// Logic will fallback until an absolute path is found,
		/// using Global Base as a last resort
		/// </summary>
		public static string AbsolutePathFor(this PathEntryCollection collection, string path, string systemId)
		{
			// warning: supposedly Path.GetFullPath accesses directories (and needs permissions)
			// if this poses a problem, we need to paste code from .net or mono sources and fix them to not pose problems, rather than homebrew stuff
			return Path.GetFullPath(collection.AbsolutePathForInner(path, systemId));
		}

		private static string AbsolutePathForInner(this PathEntryCollection collection,  string path, string systemId)
		{
			// Hack
			if (systemId == "Global")
			{
				return collection.AbsolutePathForInner(path, systemId: null);
			}

			// This function translates relative path and special identifiers in absolute paths
			if (path.Length < 1)
			{
				return collection.GlobalBaseAbsolutePath();
			}

			if (path == "%recent%")
			{
				return Environment.SpecialFolder.Recent.ToString();
			}

			if (path.StartsWith("%exe%"))
			{
				return PathUtils.ExeDirectoryPath + path.Substring(5);
			}

			if (path.StartsWith("%rom%"))
			{
				return collection.LastRomPath + path.Substring(5);
			}

			if (path[0] == '.')
			{
				if (!string.IsNullOrWhiteSpace(systemId))
				{
					path = path.Remove(0, 1);
					path = path.Insert(0, collection.BaseFor(systemId));
				}

				if (path.Length == 1)
				{
					return collection.GlobalBaseAbsolutePath();
				}

				if (path[0] == '.')
				{
					path = path.Remove(0, 1);
					path = path.Insert(0, collection.GlobalBaseAbsolutePath());
				}

				return path;
			}

			if (Path.IsPathRooted(path))
			{
				return path;
			}

			//handling of initial .. was removed (Path.GetFullPath can handle it)
			//handling of file:// or file:\\ was removed  (can Path.GetFullPath handle it? not sure)

			// all bad paths default to EXE
			return PathUtils.ExeDirectoryPath;
		}

		public static string MovieAbsolutePath(this PathEntryCollection collection)
		{
			var path = collection["Global", "Movies"].Path;
			return collection.AbsolutePathFor(path, null);
		}

		public static string MovieBackupsAbsolutePath(this PathEntryCollection collection)
		{
			var path = collection["Global", "Movie backups"].Path;
			return collection.AbsolutePathFor(path, null);
		}

		public static string AvAbsolutePath(this PathEntryCollection collection)
		{
			var path = collection["Global", "A/V Dumps"].Path;
			return collection.AbsolutePathFor(path, null);
		}

		public static string FirmwareAbsolutePath(this PathEntryCollection collection)
		{
			return collection.AbsolutePathFor(collection.FirmwaresPathFragment, null);
		}

		public static string LogAbsolutePath(this PathEntryCollection collection)
		{
			var path = collection.ResolveToolsPath(collection["Global", "Debug Logs"].Path);
			return collection.AbsolutePathFor(path, null);
		}

		public static string WatchAbsolutePath(this PathEntryCollection collection)
		{
			var path = 	collection.ResolveToolsPath(collection["Global", "Watch (.wch)"].Path);
			return collection.AbsolutePathFor(path, null);
		}

		public static string ToolsAbsolutePath(this PathEntryCollection collection)
		{
			var path = collection["Global", "Tools"].Path;
			return collection.AbsolutePathFor(path, null);
		}

		public static string TastudioStatesAbsolutePath(this PathEntryCollection collection)
		{
			var path = collection["Global", "TAStudio states"].Path;
			return collection.AbsolutePathFor(path, null);
		}

		public static string MultiDiskAbsolutePath(this PathEntryCollection collection)
		{
			var path = collection.ResolveToolsPath(collection["Global", "Multi-Disk Bundles"].Path);
			return collection.AbsolutePathFor(path, null);
		}

		public static string RomAbsolutePath(this PathEntryCollection collection, string systemId = null)
		{
			if (string.IsNullOrWhiteSpace(systemId))
			{
				return collection.AbsolutePathFor(collection["Global_NULL", "ROM"].Path, "Global_NULL");
			}

			if (collection.UseRecentForRoms)
			{
				return Environment.SpecialFolder.Recent.ToString();
			}

			var path = collection[systemId, "ROM"];

			if (!path.Path.PathIsSet())
			{
				path = collection["Global", "ROM"];

				if (path.Path.PathIsSet())
				{
					return collection.AbsolutePathFor(path.Path, null);
				}
			}

			return collection.AbsolutePathFor(path.Path, systemId);
		}

		public static string SaveRamAbsolutePath(this PathEntryCollection collection, IGameInfo game)
		{
			var name = game.FilesystemSafeName();

			var pathEntry = collection[game.System, "Save RAM"]
				?? collection[game.System, "Base"];

			return $"{Path.Combine(collection.AbsolutePathFor(pathEntry.Path, game.System), name)}.SaveRAM";
		}

		// Shenanigans
		public static string RetroSaveRamAbsolutePath(this PathEntryCollection collection, IGameInfo game)
		{
			var name = game.FilesystemSafeName();
			name = Path.GetDirectoryName(name);
			if (name == "")
			{
				name = game.FilesystemSafeName();
			}

			name ??= "";

			var pathEntry = collection[game.System, "Save RAM"]
				?? collection[game.System, "Base"];

			return Path.Combine(collection.AbsolutePathFor(pathEntry.Path, game.System), name);
		}

		// Shenanigans
		public static string RetroSystemAbsolutePath(this PathEntryCollection collection, IGameInfo game)
		{
			var name = game.FilesystemSafeName();
			name = Path.GetDirectoryName(name);
			if (string.IsNullOrEmpty(name))
			{
				name = game.FilesystemSafeName();
			}

			var pathEntry = collection[game.System, "System"]
				?? collection[game.System, "Base"];

			return Path.Combine(collection.AbsolutePathFor(pathEntry.Path, game.System), name);
		}

		public static string AutoSaveRamAbsolutePath(this PathEntryCollection collection, IGameInfo game)
		{
			var path = collection.SaveRamAbsolutePath(game);
			return path.Insert(path.Length - 8, ".AutoSaveRAM");
		}

		public static string CheatsAbsolutePath(this PathEntryCollection collection, string systemId)
		{
			var pathEntry = collection[systemId, "Cheats"]
				?? collection[systemId, "Base"];

			return collection.AbsolutePathFor(pathEntry.Path,systemId);
		}

		public static string SaveStateAbsolutePath(this PathEntryCollection collection, string systemId)
		{
			var pathEntry = collection[systemId, "Savestates"]
				?? collection[systemId, "Base"];

			return collection.AbsolutePathFor(pathEntry.Path, systemId);
		}

		public static string ScreenshotAbsolutePathFor(this PathEntryCollection collection, string systemId)
		{
			var entry = collection[systemId, "Screenshots"]
				?? collection[systemId, "Base"];

			return collection.AbsolutePathFor(entry.Path, systemId);
		}

		public static string PalettesAbsolutePathFor(this PathEntryCollection collection, string systemId)
		{
			return collection.AbsolutePathFor(collection[systemId, "Palettes"].Path, systemId);
		}

		/// <summary>
		/// Takes an absolute path and attempts to convert it to a relative, based on the system,
		/// or global base if no system is supplied, if it is not a subfolder of the base, it will return the path unaltered
		/// </summary>
		public static string TryMakeRelative(this PathEntryCollection collection, string absolutePath, string system = null) => absolutePath.MakeRelativeTo(
			string.IsNullOrWhiteSpace(system)
				? collection.GlobalBaseAbsolutePath()
				: collection.AbsolutePathFor(collection.BaseFor(system), system)
		);

		/// <summary>
		/// Puts the currently configured temp path into the environment for use as actual temp directory
		/// </summary>
		public static void RefreshTempPath(this PathEntryCollection collection)
		{
			if (string.IsNullOrWhiteSpace(collection.TempFilesFragment))
				return;
			var path = collection.AbsolutePathFor(collection.TempFilesFragment, null);
			TempFileManager.HelperSetTempPath(path);
		}

		private static string ResolveToolsPath(this PathEntryCollection collection, string subPath)
		{
			if (Path.IsPathRooted(subPath) || subPath.StartsWith("%"))
			{
				return subPath;
			}

			var toolsPath = collection["Global", "Tools"].Path;

			// Hack for backwards compatibility, prior to 1.11.5, .wch files were in .\Tools, we don't want that to turn into .Tools\Tools
			if (subPath == "Tools")
			{
				return toolsPath;
			}

			return Path.Combine(toolsPath, subPath);
		}
	}
}
