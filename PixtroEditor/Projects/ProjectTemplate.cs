using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace Pixtro.Projects
{
	public sealed class ProjectTemplate
	{
		public struct Metadata
		{
			[JsonProperty]
			public string Name { get; private set; }
		}

		private static Dictionary<string, ProjectTemplate> templates = new Dictionary<string, ProjectTemplate>();

		public static IReadOnlyDictionary<string, ProjectTemplate> Templates => templates;


		public static void LoadTemplates()
		{
			//foreach (string dir in Directory.GetDirectories(MainForm.Config.TemplatesFolder))
			//{
			//	string name = Path.GetFileName(dir);

			//	if (!File.Exists(Path.Combine(dir, "meta.json")))
			//		continue;

			//	templates.Add(name, new ProjectTemplate(dir));
			//}
			
		}

		public string WorkingPath { get; private set; }
		public Metadata meta { get; private set; }

		private ProjectTemplate(string path)
		{
			WorkingPath = path;
			meta = JsonConvert.DeserializeObject<Metadata>(File.ReadAllText(Path.Combine(path, "meta.json")));
		}

		public void CopyTo(string copyPath)
		{
			foreach (var dir in Directory.GetDirectories(WorkingPath, "*", SearchOption.AllDirectories))
			{
				string localDir = dir.Replace(WorkingPath, "");
				if (localDir.StartsWith("\\"))
					localDir = localDir.Substring(1);

				localDir = Path.Combine(copyPath, localDir);

				Directory.CreateDirectory(localDir);
			}
			foreach (var dir in Directory.GetFiles(WorkingPath, "*", SearchOption.AllDirectories))
			{
				string localDir = dir.Replace(WorkingPath, "");
				if (localDir.StartsWith("\\"))
					localDir = localDir.Substring(1);

				if (localDir == "meta.json")
				{
					continue;
				}

				localDir = Path.Combine(copyPath, localDir);

				File.Copy(dir, localDir);

			}
		}

	}
}
