using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using System.Drawing;
using System.Collections;
using DSDecmp;

namespace Pixtro.Compiler {
	public class VisualPackMetadata {
		public class TileWrapping {
			// TODO: Create feature that lets users copy mapping data from one version to another

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
			return bricks.Contains(brick, new CompareFlippable<Brick>());
		}
		public void AddNewBrick(Brick brick)
		{
			bricks.Add(brick);
			int size = brick.SizeOfTile / 8;

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

				if (b.EqualTo(tile, FlipStyle.Both))
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

		private const int multValue = 57047;

		public static uint RNGSeed;
		private static int RandomFromPoint(Point point)
		{
			ulong tempVal = (ulong)(RNGSeed + (uint)point.X);
			tempVal = (tempVal * multValue) % int.MaxValue;
			tempVal += (ulong)point.Y;
			tempVal = (tempVal * multValue) % int.MaxValue;

			return (int)tempVal;
		}

		public static Random Randomizer;

		public static VisualPackMetadata DataParse;

		public class Entity {
			public int x, y, type;

			public List<byte> data = new List<byte>();
		}

		private int width, height, layers;

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

									//var dp = new DataParser(str);

									//value <<= 1;
									//value |= (uint)(dp.GetBoolean(getvalue) ? 1 : 0);
								}

							foreach (var key in wrapping.TileMapping.Keys)
							{
								testValue = wrapping.EnableMask[key];

								if ((testValue & value) != testValue)
									continue;

								testValue = wrapping.DisableMask[key];

								if ((testValue & (value)) != 0)
									continue;

								var point = wrapping.TileMapping[key].GetValueWrapped(RandomFromPoint(new Point(x, y)));

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
							tile = new LargeTile(Settings.BrickTileSize * 8);
						}

						if (mappedTile == null) {
							retval = 0;
						}
						else {
							retval = (ushort)(fullTileset.GetIndex(mappedTile, currentTile) + 1);
							ushort offset = (ushort)(tile.GetFlipOffset(mappedTile) << 10);
							retval |= offset;
							retval |= (ushort)(mappedTile.palette << 12);
						}
					}

					retvalArray[count + 1] = (byte)(retval >> 8);
					retvalArray[count] = (byte)(retval & 0xFF);

					count += 2;

				}
			}

			count = 0;
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