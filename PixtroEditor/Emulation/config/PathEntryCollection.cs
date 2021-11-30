using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace Pixtro.Emulation
{
	[JsonObject]
	public class PathEntryCollection : IEnumerable<PathEntry>
	{
		private static readonly Dictionary<string, string> _displayNameLookup = new Dictionary<string, string>()
		{
			["Global_NULL"] = "Global",
			["GBA"] = "GBA",
		};

		public static string GetDisplayNameFor(string sysID)
		{
			if (_displayNameLookup.TryGetValue(sysID, out var dispName)) return dispName;
			var newDispName = $"{sysID} (INTERIM)";
			_displayNameLookup[sysID] = newDispName;
			return newDispName;
		}

		public List<PathEntry> Paths { get; }

		[JsonConstructor]
		public PathEntryCollection(List<PathEntry> paths)
		{
			Paths = paths;
		}

		public PathEntryCollection() : this(new List<PathEntry>(DefaultValues)) {}

		public bool UseRecentForRoms { get; set; }
		public string LastRomPath { get; set; } = ".";

		public IEnumerator<PathEntry> GetEnumerator() => Paths.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		public PathEntry this[string system, string type] =>
			Paths.FirstOrDefault(p => p.IsSystem(system) && p.Type == type)
			?? TryGetDebugPath(system, type);

		private PathEntry TryGetDebugPath(string system, string type)
		{
			if (Paths.Any(p => p.IsSystem(system)))
			{
				// we have the system, but not the type.  don't attempt to add an unknown type
				return null;
			}

			// we don't have anything for the system in question.  add a set of stock paths
			Paths.AddRange(new PathEntry[]
			{
				new PathEntry(system, 0, "Base", Path.Combine(".", $"{system.RemoveInvalidFileSystemChars()}_INTERIM")),
				new PathEntry(system, 1, "ROM", "."),
				new PathEntry(system, 2, "Savestates", Path.Combine(".", "State")),
				new PathEntry(system, 3, "Save RAM", Path.Combine(".", "SaveRAM")),
				new PathEntry(system, 4, "Screenshots", Path.Combine(".", "Screenshots")),
				new PathEntry(system, 5, "Cheats", Path.Combine(".", "Cheats")),
			});

			return this[system, type];
		}

		public void ResolveWithDefaults()
		{
			// Add missing entries
			foreach (PathEntry defaultPath in DefaultValues)
			{
				var path = Paths.FirstOrDefault(p => p.System == defaultPath.System && p.Type == defaultPath.Type);
				if (path == null)
				{
					Paths.Add(defaultPath);
				}
			}

			var entriesToRemove = new List<PathEntry>();

			// Remove entries that no longer exist in defaults
			foreach (PathEntry pathEntry in Paths)
			{
				var path = DefaultValues.FirstOrDefault(p => p.System == pathEntry.System && p.Type == pathEntry.Type);
				if (path == null)
				{
					entriesToRemove.Add(pathEntry);
				}
			}

			foreach (PathEntry entry in entriesToRemove)
			{
				Paths.Remove(entry);
			}
		}

		[JsonIgnore]
		public string FirmwaresPathFragment => this["Global", "Firmware"].Path;

		[JsonIgnore]
		internal string TempFilesFragment => this["Global", "Temp Files"].Path;

		public static List<PathEntry> DefaultValues => new List<PathEntry>
		{
			//new("Global_NULL", 1, "Base", "."),
			//new("Global_NULL", 2, "ROM", "."),
			//new("Global_NULL", 3, "Firmware", Path.Combine(".", "Firmware")),
			//new("Global_NULL", 4, "Movies", Path.Combine(".", "Movies")),
			//new("Global_NULL", 5, "Movie backups", Path.Combine(".", "Movies", "backup")),
			//new("Global_NULL", 6, "A/V Dumps", "."),
			//new("Global_NULL", 7, "Tools", Path.Combine(".", "Tools")),
			//new("Global_NULL", 8, "Watch (.wch)", Path.Combine(".", ".")),
			//new("Global_NULL", 9, "Debug Logs", Path.Combine(".", "")),
			//new("Global_NULL", 10, "Macros", Path.Combine(".", "Movies", "Macros")),
			//new("Global_NULL", 11, "TAStudio states", Path.Combine(".", "Movies", "TAStudio states")),
			//new("Global_NULL", 12, "Multi-Disk Bundles", Path.Combine(".", "")),
			//new("Global_NULL", 13, "External Tools", Path.Combine(".", "ExternalTools")),
			//new("Global_NULL", 14, "Temp Files", ""),

			//new("GBA", 0, "Base", Path.Combine(".", "GBA")),
			//new("GBA", 1, "ROM", "."),
			//new("GBA", 2, "Savestates", Path.Combine(".", "State")),
			//new("GBA", 3, "Save RAM", Path.Combine(".", "SaveRAM")),
			//new("GBA", 4, "Screenshots", Path.Combine(".", "Screenshots")),
			//new("GBA", 5, "Cheats", Path.Combine(".", "Cheats")),
		};
	}
}
