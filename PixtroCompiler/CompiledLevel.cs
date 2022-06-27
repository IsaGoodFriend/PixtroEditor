using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using System.Drawing;
using System.Collections;
using DSDecmp;

namespace Pixtro.Compiler {
	public class VisualPackMetadata {
		public class TileWrapping {

			public int Palette;
			public string Tileset;
			public string MappingCopy;
			public byte CollisionType = 1, CollisionShape;

			public char[] Connections;
			public Point[] Mapping;
			public string[] MappingSpecial;
			public Dictionary<string, Point[]> TileMapping;
			public Dictionary<string, Point[]> Offsets;

			[JsonIgnore]
			public Dictionary<string, uint> EnableMask, DisableMask;

			public Point[] GetWrapping(Func<int, int, char> checkTileset, int x, int y, int width, int height) {
				
				uint value = 0;

				foreach (var p in Mapping) {
					value <<= 1;

					Point ex = new Point(Math.Clamp(x + p.X, 0, width - 1), Math.Clamp(y + p.Y, 0, height - 1));

					if (Connections.Contains(checkTileset(ex.X, ex.Y)))
						value |= 1;
				}

				if (MappingSpecial != null)
					foreach (string str in MappingSpecial) {
						Match m = Regex.Match(str, @"([0-9\-]+), *([0-9\-]+) *; *(\w+)");
						if (!m.Success)
							continue;

						Point ex = new Point(Math.Clamp(x + int.Parse(m.Groups[1].Value), 0, width - 1), Math.Clamp(y + int.Parse(m.Groups[2].Value), 0, height - 1));
						char val = checkTileset(ex.X, ex.Y);

						value <<= 1;
						if (m.Groups[3].Value.Contains(val)) {
							value |= 1;
						}
					}

				foreach (var key in TileMapping.Keys) {
					var testValue = EnableMask[key];

					if ((testValue & value) != testValue)
						continue;

					testValue = DisableMask[key];

					if ((testValue & (value)) != 0)
						continue;

					return TileMapping[key];

				}
				
				return null;

			}

			public void FinalizeMasks() {
				EnableMask = new Dictionary<string, uint>();
				DisableMask = new Dictionary<string, uint>();

				if (TileMapping == null)
					return;

				foreach (string key in TileMapping.Keys) {
					EnableMask.Add(key, Convert.ToUInt32(key.Replace("*", "0"), 2));
					DisableMask.Add(key, ~Convert.ToUInt32(key.Replace("*", "1"), 2));
				}
				if (Offsets != null)
				foreach (string key in Offsets.Keys) {
					if (EnableMask.ContainsKey(key))
						continue;

					EnableMask.Add(key, Convert.ToUInt32(key.Replace("*", "0"), 2));
					DisableMask.Add(key, ~Convert.ToUInt32(key.Replace("*", "1"), 2));
				}
			}
		}
		public Dictionary<char, TileWrapping> Wrapping;


		[JsonIgnore]
		public LevelBrickset fullTileset = null;
		public Dictionary<string, FlippableLayout<LargeTile>> tilesetFound = new Dictionary<string, FlippableLayout<LargeTile>>();

		public Dictionary<string, int> EntityIndex;


		public string[] LevelPacks;

		[JsonIgnore]
		public List<string> levelsIncluded = new List<string>();

		public string Name;
	}
	public class LevelBrickset : IEnumerable<Brick>
	{
		List<Brick> bricks = new List<Brick>();
		List<Tile> rawTiles = new List<Tile>();

		public IReadOnlyList<Tile> RawTiles => rawTiles;

		public LevelBrickset()
		{

		}

		public bool Contains(Brick brick)
		{
			return bricks.Contains(brick, new CompareFlippable<Brick>() { flipStyle = FlipStyle.None } );
		}
		public void AddNewBrick(Brick brick)
		{
			if (bricks.Contains(brick, new CompareBricks() { flipStyle = FlipStyle.None }))
				return;

			bricks.Add(brick);

			foreach (var tile in brick.tiles) {
				if (!tile.IsAir && !rawTiles.Contains(tile, new CompareFlippable<Tile>())) {
					tile.Unflip();
					rawTiles.Add(new Tile(tile));
				}
			}
			
		}

		public ushort GetIndex(LargeTile tile, char type) => GetIndex(GetBrick(tile, type));

		public ushort GetIndex(Brick brick)
		{
			return (ushort)bricks.IndexOf(brick);
		}
		public Brick GetBrick(LargeTile tile, char type)
		{
			foreach (var b in bricks)
			{
				if (b.collisionChar != type)
					continue;

				if (b.EqualTo(tile, FlipStyle.None))
					return b;
			}
			return null;
		}

		public IEnumerator<Brick> GetEnumerator()
		{
			return bricks.GetEnumerator();
		}
		IEnumerator IEnumerable.GetEnumerator()
		{
			return (IEnumerator<Brick>)bricks;
		}
	}
	public class CompiledLevel {

		private const int rng_value1 = 374761393, rng_value2 = 668265263, rng_value3 = 1274126177;

		public static int RNGSeed;
		public static int RandomFromPoint(int x, int y, int min, int max) {
			return RandomFromPoint(new Point(x, y), RNGSeed, min, max);
		}
		public static int RandomFromPoint(Point point, int min, int max)
		{
			return RandomFromPoint(point, RNGSeed, min, max);
		}
		public static int RandomFromPoint(Point point, int seed, int min, int max) {
			int h = seed + point.X*374761393 + point.Y*668265263; //all constants are prime
			h = (h^(h >> 13))*1274126177;
			h = h^(h >> 16);
			return h  % (max - min) - min;

		}

		private static string NextLine(StreamReader _reader, bool ignoreWhitespace = true) {

			string retval;

			if (ignoreWhitespace) {
				do
					retval = _reader.ReadLine();
				while (string.IsNullOrWhiteSpace(retval));
			}
			else {
				do
					retval = _reader.ReadLine();
				while (string.IsNullOrEmpty(retval));
			}

			return retval;
		}
		private static string[] SplitWithTrim(string str, char splitChar) {
			string[] split = str.Split(new char[]{ splitChar }, StringSplitOptions.RemoveEmptyEntries);

			for (int i = 0; i < split.Length; ++i) {
				split[i] = split[i].Trim();
			}

			return split;
		}
		public static CompiledLevel CompileLevelTxt(string fullPath) {

			CompiledLevel retval = new CompiledLevel();

			using (StreamReader reader = new StreamReader(File.Open(fullPath, FileMode.Open))) {
				string[] split = SplitWithTrim(NextLine(reader), '-');

				retval.Width = int.Parse(split[0]);
				retval.Height = int.Parse(split[1]);
				retval.Layers = int.Parse(split[2]);

				while (!reader.EndOfStream) {
					string dataType = NextLine(reader);
					split = SplitWithTrim(dataType, '-');

					switch (split[0]) {
						case "layer": {
							int layer = dataType.Contains('-') ? int.Parse(split[1]) : 0;

							for (int i = 0; i < retval.Height; ++i) {
								retval.AddLine(layer, i, NextLine(reader, false));
							}
							break;
						}
						case "entities":
							string ent = "";

							while (ent != "end") {
								ent = NextLine(reader);

								if (ent == "end")
									break;

								split = SplitWithTrim(ent, ';');

								var entity = new CompiledLevel.Entity();

								entity.x = int.Parse(split[1]);
								entity.y = int.Parse(split[2]);

								byte type;
								if (!byte.TryParse(split[0], out type)) {
									entity.type = CompiledLevel.DataParse.EntityIndex[split[0]];
								}
								var currentType = FullCompiler.currentType = entity.type;

								for (int i = 3; i < split.Length; ++i) {
									entity.data.Add(FullCompiler.ParseMetadata(split[i]));
								}


								retval.entities.Add(entity);

								FullCompiler.entLocalCount++;
								FullCompiler.entGlobalCount++;
								FullCompiler.entSectionCount++;

								if (!FullCompiler.typeLocalCount.ContainsKey(currentType))
									FullCompiler.typeLocalCount.Add(currentType, 0);
								if (!FullCompiler.typeGlobalCount.ContainsKey(currentType))
									FullCompiler.typeGlobalCount.Add(currentType, 0);
								if (!FullCompiler.typeSectionCount.ContainsKey(currentType))
									FullCompiler.typeSectionCount.Add(currentType, 0);

								FullCompiler.typeLocalCount[currentType]++;
								FullCompiler.typeGlobalCount[currentType]++;
								FullCompiler.typeSectionCount[currentType]++;


							}
							break;
						case "meta":
						case "metadata": {
							retval.metadata =new Dictionary<byte, byte>();

							string readLine = "";

							while (readLine != "end") {
								readLine = NextLine(reader);

								if (readLine == "end")
									break;

								split = readLine.Split(';');

								byte value = FullCompiler.ParseMetadata(split[1]);
								retval.metadata.Add(byte.Parse(split[0]), value);

							}
						}
						break;
					}

				}
			}

			return retval;
		}

		public static Random Randomizer;

		public static VisualPackMetadata DataParse;

		public class Entity {
			public int x, y, type;

			public List<byte> data = new List<byte>();
		}

		private int width, height, layers;

		public char[,,] LevelData => levelData;
		private char[,,] levelData;

		public Dictionary<byte, byte> metadata = new Dictionary<byte, byte>();
		public List<Entity> entities = new List<Entity>();

		public int Width {
			get { return width; }
			set {
				if (levelData == null) {
					width = value;

					if (width != 0 && height != 0 && layers != 0) {
						levelData = new char[layers, width, height];
					}
				}
			}
		}
		public int Height {
			get { return height; }
			set {
				if (levelData == null) {
					height = value;

					if (width != 0 && height != 0 && layers != 0) {
						levelData = new char[layers, width, height];
					}
				}
			}
		}
		public int Layers {
			get { return layers; }
			set {
				if (levelData == null) {
					layers = Math.Max(Math.Min(value, 3), 1); // can only be between one and three

					if (width != 0 && height != 0 && layers != 0) {
						levelData = new char[layers, width, height];
					}
				}
			}
		}

		public void AddLine(int layer, int line, string data) {
			if (layer >= layers)
				return;
			for (int i = 0; i < width; ++i) {
				levelData[layer, i, line] = data[i];
			}
		}

		public byte[] BinaryData() {
			List<byte> bytes = new List<byte>(Enumerable.ToArray(GetBinary()));

			while ((bytes.Count & 0x3) != 0)
				bytes.Add(0xFF);

			return bytes.ToArray();
		}
		private IEnumerable<byte> GetBinary() {
			foreach (var b in Header())
				yield return b;

			if ((metadata.Count & 0x1) == 1)
			{
				yield return 3;
				yield return 0xFF;
				yield return 0xFF;
			}
			else
			{
				yield return 1;
			}

			for (int i = 0; i < layers; ++i) {

				var array = VisualLayer(i);
				int len = array.Length;
				int offset = 4 - ((array.Length + 3) & 0x3);
				if (i == layers - 1)
					offset = 0;
				len += offset;

				yield return (byte)(len & 0xFF);
				yield return (byte)(len >> 8);

				foreach (var b in array) {
					yield return b;
				}


				if (i == layers - 1)
					break;

				for (int j = 0; j < offset; ++j) {
					yield return 0xFF;
				}

				yield return 0x01;
			}

			foreach (var b in Entities())
				yield return b;
			
			yield break;
		}

		private IEnumerable<byte> Header() {
			foreach (byte b in BitConverter.GetBytes((short)width))
				yield return b;
			foreach (byte b in BitConverter.GetBytes((short)height))
				yield return b;

			foreach (var b in metadata.Keys) {
				yield return b;
				yield return metadata[b];
			}
			yield return 0xFF;
			
			yield break;
		}
		private byte[] VisualLayer(int layer) {
			
			int x, y;
			
			List<char> characters = new List<char>(DataParse.Wrapping.Keys);
			Dictionary<char, uint[]> connect = new Dictionary<char, uint[]>();
			LevelBrickset fullTileset = DataParse.fullTileset;


			//if (DataParse.fullTileset != null)
			//	fullTileset = DataParse.fullTileset;

			//// If there isn't a brickset created, make a new one
			//else {
			//	fullTileset = new LevelBrickset();

			//	List<string> found = new List<string>();

			//	// Foreach tile type ('M', 'N' or whatever)
			//	foreach (var key in DataParse.Wrapping.Keys) {

			//		int collType = DataParse.Wrapping[key].CollisionType;

			//		if (FullCompiler.Sprites.ContainsKey("tilesets_" + DataParse.Wrapping[key].Tileset))
			//		{
			//			var tileset = FullCompiler.Sprites["tilesets_" + DataParse.Wrapping[key].Tileset][0].CreateTileset(Settings.BrickTileSize);

			//			foreach (var tile in tileset.tiles)
			//			{
			//				if (tile.tile.IsAir && collType == 0)
			//					continue;

			//				var brick = new Brick(tile);
			//				brick.collisionType = collType;
			//				brick.collisionChar = key;

			//				fullTileset.AddNewBrick(brick);
			//			}
			//		}
			//		else
			//		{
			//			var brick = new Brick(Settings.BrickTileSize * 8);
			//			brick.collisionType = collType;
			//			brick.collisionChar = key;

			//			fullTileset.AddNewBrick(brick);
			//		}
						
			//		found.Add(DataParse.Wrapping[key].Tileset);
			//	}

			//	DataParse.fullTileset = fullTileset;
			//}

			foreach (var tile in DataParse.Wrapping.Keys) {

				if (DataParse.Wrapping[tile].Connections == null)
					continue;

				List<uint> conns = new List<uint>();

				foreach (var name in DataParse.Wrapping[tile].Connections)
					conns.Add((uint)characters.IndexOf(name) + 1);

				connect.Add(tile, conns.ToArray());
			}

			uint[,] data = new uint[width, height];

			for (y = 0; y < height; ++y) {
				for (x = 0; x < width; ++x) {
					data[x, y] = levelData[layer, x, y] == ' ' ? 0 : (uint)characters.IndexOf(levelData[layer, x, y]) + 1;
				}
			}

			int count = 0;

			byte[] retvalArray = new byte[width * height * 2];

			for (y = 0; y < height; ++y)
			{
				for (x = 0; x < width; ++x)
				{
					ushort retval;

					char currentTile = levelData[layer, x, y];

					if (currentTile == ' ')
					{
						retval = 0;
					}
					else
					{
						var wrapping = DataParse.Wrapping[currentTile];
						Brick mappedTile;
						LargeTile tile = null;

						if (DataParse.tilesetFound.ContainsKey(wrapping.Tileset))
						{
							var tileset = DataParse.tilesetFound[wrapping.Tileset];

							uint value = data.GetWrapping(x, y, connect[currentTile], wrapping.Mapping),
								testValue;

							if (wrapping.MappingSpecial != null)
								foreach (string str in wrapping.MappingSpecial)
								{
									Match m = Regex.Match(str, @"([0-9\-]+), *([0-9\-]+) *; *(\w+)");
									if (!m.Success)
										continue;

									Point ex = new Point(Math.Clamp(x + int.Parse(m.Groups[1].Value), 0, width - 1), Math.Clamp(y + int.Parse(m.Groups[2].Value), 0, height - 1));
									char val = levelData[layer, ex.X, ex.Y];

									value <<= 1;
									if (m.Groups[3].Value.Contains(val)) {
										value |= 1;
									}
								}

							foreach (var key in wrapping.TileMapping.Keys)
							{
								testValue = wrapping.EnableMask[key];

								if ((testValue & value) != testValue)
									continue;

								testValue = wrapping.DisableMask[key];

								if ((testValue & (value)) != 0)
									continue;

								var point = wrapping.TileMapping[key][RandomFromPoint(new Point(x, y), 0, wrapping.TileMapping[key].Length)];

								if (wrapping.Offsets != null)
									foreach (var o in wrapping.Offsets.Keys)
									{

										testValue = wrapping.EnableMask[o];

										if ((testValue & value) != testValue)
											continue;

										testValue = wrapping.DisableMask[o];

										if ((testValue & (value)) != 0)
											continue;

										foreach (var exPoint in wrapping.Offsets[o])
											point = new Point(point.X + exPoint.X, point.Y + exPoint.Y);
									}

								tile = tileset.GetTile(point.X, point.Y);
								break;
							}

							mappedTile = fullTileset.GetBrick(tile, currentTile);// tileset.GetUniqueTile(tile??tileset.GetTile(0, 0));
						}
						else
						{
							mappedTile = fullTileset.GetBrick(new LargeTile(Settings.BrickTileSize * 8), currentTile);
						}

						if (mappedTile == null) {
							retval = 0;
						}
						else {
							retval = (ushort)(fullTileset.GetIndex(mappedTile, currentTile) + 1);
							retval |= (ushort)(mappedTile.palette << 12);
						}
					}

					retvalArray[count + 1] = (byte)(retval >> 8);
					retvalArray[count] = (byte)(retval & 0xFF);

					count += 2;

				}
			}

			return LZUtil.Compress(retvalArray);
		}
		private IEnumerable<byte> Entities() {
			foreach (var ent in entities) {
				yield return (byte)ent.type;
				yield return (byte)ent.x;
				yield return (byte)ent.y;

				foreach (var b in ent.data)
					yield return b;

				yield return 0xFF;
			}
			yield return 0xFF;
			yield break;
		}

	}
}