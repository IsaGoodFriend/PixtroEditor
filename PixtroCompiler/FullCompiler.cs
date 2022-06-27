using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.IO;
using System.Linq;
using YamlDotNet.Serialization;
using System.Drawing;
using System.Xml;

namespace Pixtro.Compiler
{
#pragma warning disable CS0649
	struct BackgroundMeta
	{
		public string[] ImportPalettes;
		public string[] Backgrounds;
	}
	class ImageMeta
	{
		public class AseMeta
		{
			public bool SeparateTags = false;
			public bool SeparateLayers = false;
		}
		public class PNGMeta
		{

		}

		public bool Animated { get; set; } = false;
		public int AnimatedWidth = 0, AnimatedHeight = 0;

		public AseMeta Ase;
		public PNGMeta PNG;

		public AsepriteReader.Tag[] SeparatedTags;

		public string[] Palettes;
		[JsonIgnore]
		public List<Color[]> ColorPalettes;

		public ImageMeta()
		{
			Ase = new AseMeta();
			PNG = new PNGMeta();
		}
	}
#pragma warning restore CS0649

	internal static class FullCompiler {
		struct ImageCompiledMeta {

		}
		private static string GetLocalPath(string file, string folder)
		{
			file = file.Replace('\\', '/');

			file = file.Replace(Path.Combine(Settings.ProjectPath, folder).Replace('\\', '/') + "/", "");

			return file;
		}
		private static string GetCompileName(string file, string folder)
		{
			file = GetLocalPath(file, folder);

			folder = Path.GetExtension(file);

			file = file.Replace(folder, "").Replace('/', '_');

			return file;
		}

		public static string[] CompilerErrorInfo = new string[5];

		private const string
			ArtPath = "art",
			LevelPath = "levels",
			BackgroundPath = ArtPath + "/backgrounds",
			PalettePath = ArtPath + "/palettes",
			ParticlePath = ArtPath + "/particles",
			SpritePath = ArtPath + "/sprites",
			TilesetPath = ArtPath + "/tilesets",
			TitleCardPath = ArtPath + "/titlecards",
			LevelPackPath = LevelPath + "/_packs",
			BuildToPath = "build/source";

		private static Dictionary<string, GBAImage[]> CompiledImages = new Dictionary<string, GBAImage[]>();
		private static Dictionary<string, Color[][]> CompiledPalettes = new Dictionary<string, Color[][]>();
		private static Dictionary<string, ImageMeta> CompiledMetadata = new Dictionary<string, ImageMeta>();
		private static Dictionary<string, List<string>> CompiledByFolder = new Dictionary<string, List<string>>();

		public static IReadOnlyDictionary<string, GBAImage[]> Sprites => CompiledImages;

		private static Dictionary<string, Color[]> palettesFromSprites = new Dictionary<string, Color[]>();

		static Dictionary<string, CompiledLevel> compiledLevels = new Dictionary<string, CompiledLevel>();
		static Dictionary<string, List<string>> levelPacks = new Dictionary<string, List<string>>();
		static List<string> usedLevels = new List<string>();

		private static string currentPack;

		internal static int currentType;

		internal static int entGlobalCount, entSectionCount, entLocalCount;

		internal static Dictionary<int, int> 
			typeGlobalCount = new Dictionary<int, int>(),
			typeSectionCount = new Dictionary<int, int>(),
			typeLocalCount = new Dictionary<int, int>();

		static void ClearDictionaries()
		{
			CompiledImages.Clear();
			CompiledPalettes.Clear();
			CompiledMetadata.Clear();
			CompiledByFolder.Clear();

			palettesFromSprites.Clear();
			compiledLevels.Clear();
			levelPacks.Clear();
			usedLevels.Clear();


			entLocalCount = 0;
			entGlobalCount = 0;
			entSectionCount = 0;
			typeLocalCount.Clear();
			typeGlobalCount.Clear();
			typeSectionCount.Clear();
		}

		static void CompileAllImages()
		{
			void AddImageRange(string localPath, string name, GBAImage[] images)
			{
				CompiledByFolder.AddToList(localPath.Split('/')[0], name);
				CompiledImages.Add(name, images);
			}

			void AddFile(string file)
			{
				string ext = Path.GetExtension(file);
				if (!(ext == ".png" || ext == ".bmp" || ext == ".ase" || ext == ".aseprite"))
					return;

				string localPath = GetLocalPath(file, ArtPath);
				string name = GetCompileName(file, ArtPath);

				CompilerErrorInfo[0] = localPath;

				try
				{
					ImageMeta meta = null;

					if (File.Exists(Path.ChangeExtension(file, ".meta.yaml")))
						meta = MainProgram.ParseMeta<ImageMeta>(File.ReadAllText(Path.ChangeExtension(file, ".meta.yaml")));
					if (meta == null && File.Exists(Path.ChangeExtension(file, ".meta.yml")))
						meta = MainProgram.ParseMeta<ImageMeta>(File.ReadAllText(Path.ChangeExtension(file, ".meta.yml")));

					if (meta == null)
					{
						meta = new ImageMeta();
					}
					else {
						if (meta.Palettes != null) {
							meta.ColorPalettes = new List<Color[]>();
							foreach (var str in meta.Palettes) {
								meta.ColorPalettes.AddRange(CompiledPalettes[str]);
							}
						}
					}

					switch (localPath.Split('/')[0])
					{
						case "particles":
							meta.Animated = true;
							meta.AnimatedWidth = 8;
							meta.AnimatedHeight = 8;
							break;
					}

					switch (ext)
					{
						case ".ase":
							using (AsepriteReader reader = new AsepriteReader(file))
							{
								if (meta.Ase.SeparateTags)
								{
									foreach (var tag in reader.TagNames)
									{
										AddImageRange(localPath, $"{name}_{tag}", GBAImage.FromAsepriteProject(reader, tag: tag));
									}
								}
								else if (meta.Ase.SeparateLayers && reader.LayerNames.Length > 1)
								{
									foreach (var layer in reader.LayerNames.Distinct())
									{
										AddImageRange(localPath, $"{name}_{layer}", GBAImage.FromAsepriteProject(reader));
									}
								}
								else
								{
									AddImageRange(localPath, name, GBAImage.FromAsepriteProject(reader));

									meta.SeparatedTags = reader.Tags;
								}
							}
							break;
						case ".png":
							if (meta.Animated)
							{
								AddImageRange(localPath, name, GBAImage.AnimateFromFile(file, meta.AnimatedWidth, meta.AnimatedHeight, meta.ColorPalettes));
							}
							else
							{
								AddImageRange(localPath, name, new GBAImage[] { GBAImage.FromFile(file, meta.ColorPalettes) });
							}
							break;
						default:
							return;
					}

					CompiledMetadata.Add(name, meta);
				}
				catch (Exception e)
				{
					MainProgram.ErrorLog(e);
				}
			}

			foreach (var file in Directory.GetFiles(Path.Combine(Settings.ProjectPath, BackgroundPath), "*", SearchOption.AllDirectories))
				AddFile(file);
			foreach (var file in Directory.GetFiles(Path.Combine(Settings.ProjectPath, ParticlePath), "*", SearchOption.AllDirectories))
				AddFile(file);
			foreach (var file in Directory.GetFiles(Path.Combine(Settings.ProjectPath, SpritePath), "*", SearchOption.AllDirectories))
				AddFile(file);
			foreach (var file in Directory.GetFiles(Path.Combine(Settings.ProjectPath, TilesetPath), "*", SearchOption.AllDirectories))
				AddFile(file);
			foreach (var file in Directory.GetFiles(Path.Combine(Settings.ProjectPath, TitleCardPath), "*", SearchOption.AllDirectories))
				AddFile(file);
		}

		public static void Compile()
		{
			ClearDictionaries();

			var levelCompiler = new CompileToC();
			var artCompiler = new CompileToC();

			string
				artPath = Path.Combine(Settings.ProjectPath, ArtPath),
				levelPath = Path.Combine(Settings.ProjectPath, LevelPath),
				tilesetPath =  Path.Combine(Settings.ProjectPath, TilesetPath);

			MainProgram.Log("Compiling palettes");
			// Compile palettes first to be used if needed when importing images
			CompilePalettes(artCompiler);

			MainProgram.Log("Reading art assets");
			CompileAllImages();

			MainProgram.Log("Compiling sprites");
			CompileSprites(artCompiler);

			MainProgram.Log("Compiling particles");
			CompileParticles(artCompiler);

			//Compiler.DebugLog("Compiling backgrounds");

			MainProgram.Log("Compiling title cards");
			CompileTitleCards(artCompiler);


			string toSavePath = Path.Combine(Settings.ProjectPath, BuildToPath);


			Dictionary<string, VisualPackMetadata> metaLevelJson = JsonConvert.DeserializeObject<Dictionary<string, VisualPackMetadata>>(File.ReadAllText(levelPath + "\\meta_level.json"));

			// Get all the art from the tilesets and compile them into C# code for ease of access
			//CompileTilesets(tilesetPath);

			// Finalize tile mapping
			foreach (var p in metaLevelJson)
			{
				if (p.Value.Wrapping == null)
					continue;
				p.Value.Name = p.Key;

				foreach (char c in p.Value.Wrapping.Keys)
				{
					var wrap = p.Value.Wrapping[c];

					if (wrap.MappingCopy != null)
					{
						string[] split = wrap.MappingCopy.Split('/', '\\');

						var otherWrap = metaLevelJson[split[0]].Wrapping[split[1][0]];

						wrap.Mapping = otherWrap.Mapping;
						wrap.TileMapping = otherWrap.TileMapping;
					}

					wrap.FinalizeMasks();
				}
			}
			if (metaLevelJson.ContainsKey("global")) {
				var globalMeta = metaLevelJson["global"];

				foreach (var p in metaLevelJson.Values) {
					if (p == globalMeta)
						continue;
					if (p.EntityIndex == null)
						p.EntityIndex = new Dictionary<string, int>();
					foreach (var pair in globalMeta.EntityIndex) {
						if (!p.EntityIndex.ContainsKey(pair.Key))
							p.EntityIndex.Add(pair.Key, pair.Value);
					}
				}
			}

			// Go through each level pack and figure out which levels are used and where
			foreach (var pack in Directory.GetFiles(Path.Combine(Settings.ProjectPath, LevelPackPath)))
			{
				string name = Path.GetFileNameWithoutExtension(pack);

				List<string> levelList = new List<string>();

				foreach (var level in File.ReadAllLines(pack))
				{
					if (string.IsNullOrWhiteSpace(level))
						continue;

					levelList.Add(level);
				}

				for (int i = levelList.Count - 1; i >= 0; --i)
				{
					string lName = levelList[i];
					if (usedLevels.Contains(lName))
						levelList.RemoveAt(i);
					else
						usedLevels.Add(lName);
				}
				levelPacks.Add(name, levelList);

				foreach (var p in metaLevelJson.Values)
				{
					if (p.LevelPacks == null)
						continue;

					if (p.LevelPacks.Contains(name))
					{
						p.levelsIncluded.AddRange(levelList);
						break;
					}
				}
			}


			// Compile levels
			foreach (var pair in metaLevelJson)
			{
				var parse = pair.Value;

				if (parse.levelsIncluded.Count == 0)
					continue;

				currentPack = pair.Key;

				// Compile the visual pack's brickset before compiling levels
				MainProgram.Log($"Compiling Visual Pack {pair.Key}");

				// Clear out section's entity count
				typeSectionCount.Clear();
				entSectionCount = 0;

				LevelBrickset fullTileset = new LevelBrickset();

				List<string> found = new List<string>();

				foreach (var key in parse.Wrapping.Keys)
				{
					var wrap = parse.Wrapping[key];

					int collType = wrap.CollisionType;

					// Compile the tiles using the visual tileset desired
					
					if (CompiledImages.ContainsKey("tilesets_" + wrap.Tileset))
					{
						var usedTiles = wrap.TileMapping.Values.SelectMany(item => item).Distinct();

						if (!parse.tilesetFound.ContainsKey(wrap.Tileset)) {
							parse.tilesetFound[wrap.Tileset] = Sprites["tilesets_" + wrap.Tileset][0].GetLargeTileSet(Settings.BrickTileSize);
						}

						FlippableLayout<LargeTile> tiles = parse.tilesetFound[wrap.Tileset];

						// Iterate over the tilemapping points instead of the entire tileset so that the compiler only adds tiles that will likely be used.
						foreach (var point in usedTiles)
						{
							var tile = tiles.GetTile(point.X, point.Y);

							if (tile.IsAir && collType == 0)
								continue;

							var brick = new Brick(tile);
							brick.collisionType = collType;
							brick.collisionChar = key;
							brick.collisionShape = wrap.CollisionShape;
							brick.palette = wrap.Palette;

							fullTileset.AddNewBrick(brick);
						}
					}
					else // If the tileset doesn't exist, add an empty tile for the wrapping character
					{
						if (wrap.Tileset.ToLower() != "null")
							MainProgram.WarningLog($"Tileset {wrap.Tileset} does not exist.");

						var brick = new Brick(Settings.BrickTileSize);
						brick.collisionType = collType;
						brick.collisionChar = key;

						fullTileset.AddNewBrick(brick);
					}
				}
				int length = fullTileset.RawTiles.Count;

				List<Tile> rawTiles = new List<Tile>(fullTileset.RawTiles);
				// Compile all the raw visual tiles used by the brickset
				levelCompiler.BeginArray(CompileToC.ArrayType.UInt, "TILESET_" + parse.Name);

				foreach (var tile in rawTiles)
				{
					levelCompiler.AddRange(tile.RawData);
				}
				levelCompiler.EndArray();

				// Compile the collision types of each brick
				levelCompiler.BeginArray(CompileToC.ArrayType.UShort, "TILECOLL_" + parse.Name);
				foreach (var tile in fullTileset)
				{
					int index = fullTileset.GetIndex(tile, tile.collisionChar);

					levelCompiler.AddValue((tile.collisionType << 8) | tile.collisionShape);
				}
				levelCompiler.AddValue(0xFFFF);
				levelCompiler.EndArray();

				// Compile each brick's "uv" mapping, aka how each raw tile fits into this tileset
				levelCompiler.BeginArray(CompileToC.ArrayType.UShort, "TILE_MAPPING_" + parse.Name);
				int size = Settings.BrickTileSize;
				int count = 0;
				foreach (var tile in fullTileset)
				{
					++count;

					for (int i = 0; i < size * size; ++i)
					{
						var brickTile = tile.tiles[i % size, i / size];

						Tile mappedTile = null;

						foreach (var rt in rawTiles)
						{
							if (brickTile.EqualTo(rt, FlipStyle.Both))
							{
								mappedTile = rt;
								break;
							}
						}

						ushort value = 0;
						if (mappedTile != null) {
							value = (ushort)(rawTiles.IndexOf(mappedTile, new CompareFlippable<Tile>()) + 1);

							ushort flip = (ushort)(brickTile.GetFlipOffset(mappedTile) << 10);
							value |= flip;
						}
						
						levelCompiler.AddValue(value);
					}
				}
				levelCompiler.EndArray();

				// Define how many tiles are in the compiled tileset
				levelCompiler.AddValueDefine($"TILESET_{parse.Name}_len", length);
				levelCompiler.AddValueDefine($"TILESET_{parse.Name}_uvlen", count);

				parse.fullTileset = fullTileset;

				CompiledLevel.DataParse = parse;

				// Compile all the levels
				foreach (var level in parse.levelsIncluded)
				{
					var localPath = Path.Combine(Settings.ProjectPath, LevelPath, level);
					var ext = "";

					if (File.Exists(localPath + ".bin"))
						ext = ".bin";
					else if (File.Exists(localPath + ".json"))
						ext = ".json";
					else if (File.Exists(localPath + ".tmx"))
						ext = ".tmx";
					else if (File.Exists(localPath + ".txt"))
						ext = ".txt";

					// Troubled.  Level doesn't exist with the accepted extensions
					if (ext == "")
						throw new FileNotFoundException($"The level file {localPath} was unable to be found.");

					MainProgram.DebugLog($"Compiling Level {level}");

					localPath = localPath.Replace('\\', '/') + ext;

					CompiledLevel compressed = null;

					entLocalCount = 0;
					typeLocalCount.Clear();

					CompiledLevel.Randomizer = new Random(new Random(localPath.GetHashCode()).Next(int.MaxValue >> 16, int.MaxValue));

					switch (ext)
					{
						case ".txt":
							compressed = CompileLevelTxt(level + ext);
							break;
						case ".json":
							throw new NotImplementedException();
						case ".tmx":
							//compressed = CompileLevelTiled(level + ext);
							break;
						default:
							compressed = CompileLevelBin(level + ext);
							break;
					}

					// Troubled.  Unable to compile level
					if (compressed == null)
						throw new Exception();

					long editTime = File.GetLastWriteTime(localPath).Ticks;

					var compiledPath = Path.Combine(Settings.ProjectPath, "build/levels", level + ".comp");

					localPath = $"LVL_{Path.GetFileNameWithoutExtension(level.Replace('/', '_').Replace('\\', '_'))}";

					compiledLevels.Add(localPath, compressed);

					levelCompiler.BeginArray(CompileToC.ArrayType.Char, localPath);

					void compileLevel() {
						byte[] data = compressed.BinaryData();
						levelCompiler.AddRange(data);

						if (!Directory.Exists(Path.GetDirectoryName(compiledPath))) {
							Directory.CreateDirectory(Path.GetDirectoryName(compiledPath));
						}
						using (var write = new BinaryWriter(File.Open(compiledPath, FileMode.OpenOrCreate, FileAccess.Write))) {
							write.Write(editTime);
							write.Write(data.Length);
							write.Write(data);
						}
					}

					if (File.Exists(compiledPath)) {

						compileLevel();
						//try {
						//	using (var read = new BinaryReader(File.Open(compiledPath, FileMode.Open, FileAccess.Read))) {
						//		if (read.ReadInt64() == editTime) {
						//			int len = read.Read();
						//			levelCompiler.AddRange(read.ReadBytes(len));
						//		}
						//		else {
						//			compileLevel();
						//		}
						//	}
						//}
						//catch (Exception e) {
						//	compileLevel();
						//}
					}
					else {
						compileLevel();
					}
					levelCompiler.EndArray();
				}
			}

			MainProgram.Log("Compiling Level Packs");
			// Compile Level Packs
			foreach (var pack in levelPacks)
			{
				currentPack = pack.Key;

				levelCompiler.BeginArray(CompileToC.ArrayType.UInt, "PACK_" + currentPack);

				List<string> levelList = new List<string>();

				foreach (var level in pack.Value)
				{
					levelList.Add("LVL_" + level.Replace('/', '_').Replace('\\', '_'));
				}

				for (int i = 0; i < levelList.Count; ++i)
				{
					if (i != 0)
					{
						levelCompiler.AddValue(1);
					}
					levelCompiler.AddValue("&" + levelList[i]);

					CompiledLevel level = compiledLevels[levelList[i].Replace('/', '_').Replace('\\', '_')];

					//for (int j = 0; j < level.Layers; ++j)
					//{
					//	levelCompiler.AddValue((2) | (j << 4));
					//}
					//levelCompiler.AddValue(3);
				}

				levelCompiler.AddValue(0);

				levelCompiler.EndArray();
			}


			MainProgram.Log("Compiling Backgrounds");
			CompileBackgrounds(artCompiler);

			MainProgram.Log("Saving source files");
			levelCompiler.SaveTo(toSavePath, "levels");
			artCompiler.SaveTo(toSavePath, "sprites");

			ClearDictionaries();
		}

		private static CompiledLevel CompileLevelTxt(string localPath) {
			return CompiledLevel.CompileLevelTxt(Path.Combine(Settings.ProjectPath, LevelPath, localPath));
		}

		private static CompiledLevel CompileLevelTiled(string path) {
			path = Path.Combine(Settings.ProjectPath, LevelPath, path);

			CompiledLevel retval = new CompiledLevel();

			XmlDocument doc = new XmlDocument();
			doc.Load(path);



			return retval;
		}

		public static byte ParseMetadata(string algorithm)
		{
			byte retval;

			if (byte.TryParse(algorithm, out retval))
				return retval;

			double getvals(string[] args)
			{
				string pack = currentPack.ToLower();
				int i;

				switch (args[0].ToLower())
				{
					case "entglobalcount":
						return entGlobalCount;
					case "entlocalcount":
						return entLocalCount;
					case "entsectioncount":
						return entSectionCount;

					case "typeglobalcount":
						if (!typeGlobalCount.TryGetValue(currentType, out i))
							return 0;
						return i;
					case "typelocalcount":
						if (!typeLocalCount.TryGetValue(currentType, out i))
							return 0;
						return i;
					case "typesectioncount":
						if (!typeSectionCount.TryGetValue(currentType, out i))
							return 0;
						return i;

					case "packsize":
						if (args.Length >= 2) {
							pack = args[1];
						}

						return levelPacks[pack].Count;

					case "levelindex":
					{
						if (args.Length >= 3) {
							pack = args[2];
						}

						return levelPacks[pack].IndexOf($"{pack}/{args[1]}");
					}
				}

				return 0;
			}

			return DataParser.EvaluateByte(algorithm, getvals);
		}

		private static CompiledLevel CompileLevelBin(string path)
		{
			path = Path.Combine(Settings.ProjectPath, LevelPath, path);

			// TODO: Add support for this at some point.

			var reader = new BinaryFileParser(path, "PIXTRO_LVL");

			string baseName = Path.GetFileNameWithoutExtension(path);

			foreach (var node in reader.Nodes)
			{
				switch (node.Name)
				{
					case "level":
						if (node.Children[0].Name != "meta")
							continue;

						CompiledLevel level = new CompiledLevel();
						string levelName = null;

						foreach (var child in node.Children)
						{
							switch (child.Name)
							{
								case "meta":
									level.Width = child.GetInteger("width");
									level.Height = child.GetInteger("height");
									level.Layers = child.GetInteger("layers");
									levelName = child.GetString("name");
									break;
								case "layer":
									int layerIndex = child.GetInteger("index");
									string[] values = (child.GetString("data")).Split('\n');

									for (int i = 0; i < values.Length; ++i)
									{
										level.AddLine(layerIndex, i, values[i]);
									}
									break;
								case "entity":

									var ent = new CompiledLevel.Entity();

									ent.x = child.GetInteger("x");
									ent.y = child.GetInteger("y");
									if (child.Attributes["type"] is string)
									{
										ent.type = CompiledLevel.DataParse.EntityIndex[child.Attributes["type"] as string];
									}
									else
									{
										ent.type = (byte)child.GetInteger("type");
									}
									currentType = ent.type;

									level.entities.Add(ent);

									foreach (var attr in child.Attributes.Keys)
									{
										switch (attr)
										{
											case "x":
											case "y":
											case "name":
											case "type":
												break;
											default:
												if (child.Attributes[attr] is string)
												{
													ent.data.Add(ParseMetadata(child.Attributes[attr] as string));
												}
												else
												{
													ent.data.Add((byte)child.GetInteger(attr));
												}

												break;
										}

									}

									entLocalCount++;
									entGlobalCount++;
									entSectionCount++;


									if (!typeLocalCount.ContainsKey(currentType))
									{
										typeLocalCount.Add(currentType, 0);
										typeGlobalCount.Add(currentType, 0);
										typeSectionCount.Add(currentType, 0);
									}
									typeLocalCount[currentType]++;
									typeGlobalCount[currentType]++;
									typeSectionCount[currentType]++;


									break;
							}
						}

						break;

					case "meta":
						break;
				}
			}

			return null;
		}

		private static void CompilePalettes(CompileToC compiler)
		{
			string[] getFiles = Directory.GetFiles(Path.Combine(Settings.ProjectPath, PalettePath));

			List<string> addedIn = new List<string>();

			foreach (string file in getFiles)
			{
				string ext = Path.GetExtension(file);

				string localPath = GetCompileName(file, ArtPath);
				string cName = Regex.Replace(localPath, "^palettes_", "PAL_");
				localPath = Regex.Replace(localPath, "^palettes_", "");

				// Only compile one version of the palette
				if (addedIn.Contains(localPath))
					continue;

				List<Color> fullPalette = new List<Color>();

				switch (ext)
				{
					case ".bmp": // Palettes are okay with .bmp
					case ".png":
						unsafe
						{
							using (Bitmap map = GBAImage.GetFormattedBitmap(file))
							{
								if (map.Width % 16 != 0)
									throw new Exception();

								var bits = map.LockBits(new Rectangle(0, 0, map.Width, map.Height), System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

								byte* bytes = (byte*) bits.Scan0;

								for (int i = 0; i < map.Height * bits.Stride; i += 4)
								{
									fullPalette.Add(Color.FromArgb(255, bytes[i + 2], bytes[i + 1], bytes[i + 0]));
								}

								map.UnlockBits(bits);


								break;
							}
						}
					case ".pal":
						using (var sr = new StreamReader(File.Open(file, FileMode.Open)))
						{
							sr.ReadLine();
							sr.ReadLine();

							int count = int.Parse(sr.ReadLine());

							for (int i = 0; i < count / 16; ++i)
							{
								sr.ReadLine();

								fullPalette.Add(Color.FromArgb(0, 0, 0, 0));

								for (int j = 1; j < 16; ++j)
								{
									string[] read = sr.ReadLine().Split(' ');

									byte r = (byte)(int.Parse(read[0]) & 0xF8);
									byte g = (byte)(int.Parse(read[1]) & 0xF8);
									byte b = (byte)(int.Parse(read[2]) & 0xF8);

									fullPalette.Add(Color.FromArgb(255, r, g, b));
								}

							}
						}
						break;
				}

				List<Color[]> exported = new List<Color[]>();

				// Put the found colors into individual palettes
				for (int p = 0; p < fullPalette.Count; p += 16)
				{
					List<Color> pal = new List<Color>();
					for (int c = 0; c < 16; ++c)
					{	
						pal.Add(fullPalette[p + c]);
					}

					exported.Add(pal.ToArray());
				}

				// Add the compiled palettes
				CompiledPalettes.Add(localPath, exported.ToArray());

				addedIn.Add(localPath);

				// Ignore any folders that start with an underscore
				if (localPath.Contains("palettes__"))
					continue;

				// Compile the palettes 
				for (int i = 0; i < exported.Count; ++i)
				{
					if (exported.Count > 1)
						compiler.BeginArray(CompileToC.ArrayType.UShort, $"{cName}_{i}");
					else
						compiler.BeginArray(CompileToC.ArrayType.UShort, cName);

					for (int c = 0; c < 16; ++c)
					{
						if (c == 0)
							compiler.AddValue(0);
						else
						{
							var color = exported[i][c];
							byte r = (byte)(color.R >> 3);
							byte g = (byte)(color.G >> 3);
							byte b = (byte)(color.B >> 3);

							compiler.AddValue((ushort)(r | (g << 5) | (b << 10)));
						}
					}

					compiler.EndArray();
				}
			}
		}
		private static void CompileSprites(CompileToC compiler)
		{
			foreach (string localPath in CompiledByFolder["sprites"])
			{
				GBAImage[] images = CompiledImages[localPath];

				int height = images[0].Height;

				switch (images[0].Width)
				{
					case 8:
					case 16:
						if (height != 8 && height != 16 && height != 32)
							throw new Exception();
						break;
					case 32:
						if (height != 8 && height != 16 && height != 32 && height != 64)
							throw new Exception();
						break;
					case 64:
						if (height != 64)
							throw new Exception();
						break;

					default:
						throw new Exception();
				}

				string cName = Regex.Replace(localPath, "^sprites_", "SPR_");

				MainProgram.DebugLog(cName);

				compiler.BeginArray(CompileToC.ArrayType.UInt, cName);

				for (int i = 0; i < images.Length; ++i)
				{
					compiler.AddRange(images[i].GetSpriteData().ToArray());
				}

				compiler.EndArray();
			}
		}
		private static void CompileParticles(CompileToC compiler)
		{
			int index = 0;

			compiler.BeginArray(CompileToC.ArrayType.UInt, "particles");

			foreach (string str in CompiledByFolder["particles"])
			{
				GBAImage[] images = CompiledImages[str];

				string cName = Regex.Replace(str, "^palettes_", "PAL_");
				int length = Math.Min(CompiledImages[str].Length, 16) - 1;

				compiler.AddValueDefine($"PART_{cName}", index | (length << 12));

				for (int i = length; i >= 0; --i)
				{
					GBAImage img = images[i];

					compiler.AddRange(img.GetTile().RawData);
				}

				index += length;
			}

			compiler.EndArray();
		}
		private static void CompileBackgrounds(CompileToC compiler)
		{
			void CompileBG(string name, GBAImage image, List<Tile> tiles, List<Color[]> palettes)
			{
				if (tiles == null)
					tiles = new List<Tile>(image.GetTiles());
				if (palettes == null)
					palettes = image.Palettes.ToList();

				compiler.BeginArray(CompileToC.ArrayType.UShort, $"BG_{name}");

				var compare = new CompareFlippable<Tile>();

				int x = 0, y = 0;
				foreach (var tile in image.GetTiles())
				{
					int index = tiles.IndexOf(tile, compare);

					compiler.AddValue(index | image.GetPaletteIndex(x, y));

					if (++x >= image.Width >> 3)
					{
						x = 0;
						++y;
					}
				}

				compiler.EndArray();
			}
			void CompileBGPack(string name, GBAImage[] images)
			{
				List<Tile> tiles = new List<Tile>();
				List<Color[]> colors = new List<Color[]>();

				foreach (var img in images)
				{
					tiles.AddRange(img.GetTiles());

					foreach (var palette in img.Palettes)
					{
						int paletteIndex = 0;
						List<Color> found = new List<Color>(palette);

						// Check if any palette contains all the colors used
						foreach (var pal in colors)
						{
							var foundPalette = pal;

							foreach (var col in palette)
							{
								if (!pal.Contains(col))
								{
									foundPalette = null;
									break;
								}
							}

							if (foundPalette != null)
							{
								found = new List<Color>(foundPalette.Where(value => value != null).Select(value => (Color)value));
								break;
							}
							paletteIndex++;
						}

						// If there's no palette that would fit in, try and find a palette to blend with
						if (paletteIndex == colors.Count)
						{
							paletteIndex = 0;

							bool selectedPalette = false;
							foreach (var pal in colors)
							{
								int nullCount = 0;
								// Count how many null slots are in current palette
								for (int i = 0; i < 16; ++i)
								{
									if (pal[i] == null)
										nullCount++;
								}
								// Find and count every color current palette doesn't have
								List<Color> toAdd = new List<Color>();
								foreach (var color in palette)
								{
									if (!pal.Contains(color))
									{
										nullCount--;
										toAdd.Add(color);
									}
								}

								// If there's enough null slots to add, then add them and stop checking palettes
								if (nullCount >= 0)
								{
									for (int i = 0; i < 16 && toAdd.Count > 0; ++i)
									{
										if (pal[i] == null)
										{
											pal[i] = toAdd[0];
											toAdd.RemoveAt(0);
										}
									}
									selectedPalette = true;

									found = new List<Color>(pal.Where(value => value != null).Select(value => (Color)value));
									break;
								}

								paletteIndex++;
							}

							// If there's no palette to blend with, then just add the new palette
							if (!selectedPalette)
							{
								colors.Add(palette);
							}

						}

					}
				}

				// Recompile images to better fit palettes
				foreach (var img in images)
					img.RecompileColors(colors);

				compiler.BeginArray(CompileToC.ArrayType.UInt, $"BGTILE_{name}");

				foreach (var tile in tiles.Distinct(new CompareFlippable<Tile>() { flipStyle = FlipStyle.Both }))
				{
					compiler.AddRange(tile.RawData);
				}

				compiler.EndArray();

				if (images.Length > 1)
				{
					for (int i = 0; i < images.Length; ++i)
						CompileBG($"{name}_{i}", images[i], tiles, colors);
					
					compiler.BeginArray(CompileToC.ArrayType.UInt, $"BGPACK_{name}");

					for (int i = 0; i < images.Length; ++i)
						compiler.AddValue($"&BG_{name}_{i}");
				}
				else
				{
					CompileBG(name, images[0], tiles, colors);

					compiler.BeginArray(CompileToC.ArrayType.UInt, $"BGPACK_{name}");

					compiler.AddValue($"&BG_{name}");
				}

				compiler.AddValue($"&BGTILE_{name}");
				compiler.EndArray();
			}

			// Start of the actual code
			if (File.Exists(Path.Combine(Settings.ProjectPath, BackgroundPath, "backgrounds.yaml")))
			{
				var backgrounds = MainProgram.ParseMeta<Dictionary<string, string[]>>(File.ReadAllText(Path.Combine(Settings.ProjectPath, BackgroundPath, "backgrounds.yaml")));

				foreach (var key in backgrounds.Keys)
				{
					List<GBAImage> images = new List<GBAImage>();
					foreach (var str in backgrounds[key])
					{
						string safeName = str.Replace('/', '_').Replace('\\', '_');

						images.Add(CompiledImages[$"backgrounds_{safeName}"][0]);
					}

					CompileBGPack(key, images.ToArray());
				}
			}
			else
			{
				if (!CompiledByFolder.ContainsKey("backgrounds"))
					return;

				foreach (var key in CompiledByFolder["backgrounds"])
				{
					CompileBG(key, CompiledImages[key][0], null, null);
				}

			}
		}
		private static void CompileTitleCards(CompileToC _compiler)
		{
			if (!Directory.Exists(Path.Combine(Settings.ProjectPath, TitleCardPath)))
				return;

			// Get all the cards that the user wants to use, and compile only those.
			string[] allCards = File.ReadAllLines(Path.Combine(Settings.ProjectPath, TitleCardPath, "order.txt"));

			List<string> addedCards = new List<string>();

			// foreach card, search for it and compile it if it exists.
			foreach (var cardName in allCards)
			{
				if (string.IsNullOrWhiteSpace(cardName))
					continue;

				string name, tag = null;
				if (cardName.Contains(":"))
				{
					tag = cardName.Split(':')[1];
					name = cardName.Split(':')[0];
				}
				else
					name = cardName;

				//foreach (string s in Directory.GetFiles(_path))
				//{
				//	// Found card, now compile it and stop searching for others of the same name
				//	if (Path.GetFileNameWithoutExtension(s) == name)
				//	{
				//		addedCards.Add(name);
				//		CompileTitleCard(s, _compiler);
				//		break;
				//	}
				//}
			}

			_compiler.BeginArray(CompileToC.ArrayType.UShortPtr, "INTRO_CARDS");

			foreach (var str in addedCards)
			{
				//_compiler.AddValue($"(unsigned short*)CARD_{str}");
				//_compiler.AddValue($"(unsigned short*)CARDTILE_{str}");
			}
			//_compiler.AddValue(0);

			_compiler.EndArray();
		}
		private static void CompileTitleCard(string name, CompileToC _compiler)
		{
			string localName = GetCompileName(name, ArtPath),
				compileName = GetCompileName(name, TitleCardPath);

			var images = CompiledImages[localName];

			_compiler.BeginArray(CompileToC.ArrayType.UShort, "CARD_" + compileName);

			//_compiler.AddRange(Enumerable.ToArray(bg.Data()));

			_compiler.EndArray();

			_compiler.BeginArray(CompileToC.ArrayType.UShort, "CARDTILE_" + compileName);

			//_compiler.AddRange(Enumerable.ToArray(bg.tileset.Data(compileName)));

			_compiler.EndArray();
		}
	}
}
