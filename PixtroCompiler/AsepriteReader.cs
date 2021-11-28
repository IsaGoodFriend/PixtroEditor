using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace Pixtro.Compiler {
	// TODO: Add support for tilesets, since that's a feature now (yay)

	public enum BlendType : ushort {
		Normal = 0,
		Multiply = 1,
		Screen = 2,
		Overlay = 3,
		Darken = 4,
		Lighten = 5,
		Color_Dodge = 6,
		Color_Burn = 7,
		Hard_Light = 8,
		Soft_Light = 9,
		Difference = 10,
		Exclusion = 11,
		Hue = 12,
		Saturation = 13,
		Color = 14,
		Luminosity = 15,
		Addition = 16,
		Subtract = 17,
		Divide = 18,
	}

	/// <summary>
	/// Created using this guide https://github.com/aseprite/aseprite/blob/master/docs/ase-file-specs.md
	/// </summary>
	public class AsepriteReader : BinaryReader {

		private static byte[] DecompressBytes(BinaryReader reader, int compressedSize, int decompressedSize = int.MaxValue)
		{
			reader.BaseStream.Seek(2, SeekOrigin.Current);

			var bits = reader.ReadBytes(compressedSize - 2);

			using (BinaryReader read = new BinaryReader(new DeflateStream(new MemoryStream(bits, false), CompressionMode.Decompress)))
			{
				return read.ReadBytes(decompressedSize);
			}
		}
		private static FloatColor[,] BytesToFloatArray(byte[] bytes, int width, int height, int bpp, FloatColor[] palette)
		{
			FloatColor[,] colors = new FloatColor[width, height];
			int x = 0, y = 0;
			
			switch (bpp)
			{
				case 8:

					for (int i = 0; i < bytes.Length; ++i)
					{
						colors[x, y] = palette[bytes[i]];
						if (++x >= width)
						{
							x = 0;
							++y;
						}
					}
					break;
				case 16:
					for (int i = 0; i < bytes.Length; ++i)
					{
						byte val = bytes[i];
						colors[x, y] = new FloatColor(val, val, val, bytes[i + 1]);
						if (++x >= width)
						{
							x = 0;
							++y;
						}
					}
					break;
				case 32:
					for (int i = 0; i < bytes.Length; i += 4)
					{
						colors[x, y] = new FloatColor(bytes[i], bytes[i + 1], bytes[i + 2], bytes[i + 3]);
						if (++x >= width)
						{
							x = 0;
							++y;
						}
					}
					break;
			}


			return colors;
		}

		public class Layer {
			public bool visible = true, tileset = false;
			public BlendType blending;
			public string Name;

			public int Type { get; private set; }

			public TilePalette Palette { get; private set; }

			public Cel[] cels;

			public Layer(ushort flags, ushort blend, int celCount, int type, string name, TilePalette tilePalette)
			{
				Name = name;

				Palette = tilePalette;

				Type = type;

				visible = (flags & 0x1) != 0;
				blending = (BlendType)blend;

				cels = new Cel[celCount];
			}

		}
		public class Tag {
			public string name;
			public int start, end;

		}
		public class Cel {
			public int X, Y;

			int width, height;

			public int Width { get; private set; }
			public int Height { get; private set; }

			public byte opacity;

			public readonly bool useTiles;

			private Layer parentlayer;

			private uint[,] tiles;
			private FloatColor[,] colors;

			public FloatColor[,] ColorValues
			{
				get
				{
					if (colors != null)
						return colors;

					var tileset = parentlayer.Palette;
					int tileWidth = tileset.Width, tileHeight = tileset.Height;

					int boundWidth = width * tileWidth,
						boundHeight = height * tileHeight;

					FloatColor[,] retval = new FloatColor[boundWidth, boundHeight];

					for (int ty = 0; ty < boundHeight; ++ty)
					{
						for (int tx = 0; tx < boundWidth; ++tx)
						{
							var currTile = tileset.tiles[(int)tiles[tx, ty]];

							for (int y = 0; y < tileHeight; ++y)
							{
								for (int x = 0; x < tileWidth; ++x)
								{
									retval[x + tx, y + ty] = currTile.colors[x, y];
								}
							}
						}
					}

					return retval;
				}
			}

			public Cel(AsepriteReader reader, Layer layer, FloatColor[] palette, int bpp, int bitSize) {
				X = reader.ReadInt16();
				Y = reader.ReadInt16();
				opacity = reader.ReadByte();

				parentlayer = layer;

				int celType = reader.ReadUInt16();

				reader.BaseStream.Seek(7, SeekOrigin.Current);

				if (celType == 3)
				{
					useTiles = true;
					bitSize -= 32;

					width = reader.ReadInt16();
					height = reader.ReadInt16();

					Width = width * layer.Palette.Width;
					Height = height * layer.Palette.Height;

					tiles = new uint[width, height];

					reader.BaseStream.Seek(28, SeekOrigin.Current);

					byte[] bits = DecompressBytes(reader, bitSize, width * height * 4);


					using (BinaryReader localReader = new BinaryReader(new MemoryStream(bits)))
					{
						int x = 0, y = 0;
						for (int i = 0; i < tiles.Length; ++i)
						{
							tiles[x++, y] = localReader.ReadUInt32();
							if (x >= width)
							{
								x = 0;
								++y;
							}
						}
					}
				}
				else
				{
					useTiles = false;
					bitSize -= 4;

					width = reader.ReadInt16();
					height = reader.ReadInt16();

					Width = width;
					Height = height;

					int colorSize = width * height;

					byte[] bits = null;

					if (celType == 2)
					{
						bits = DecompressBytes(reader, bitSize, width * height * bpp / 8);
					}
					else
						bits = reader.ReadBytes(width * height * bpp / 8);

					colors = BytesToFloatArray(bits, width, height, bpp, palette);

				}
			}
		}
		public class Tile
		{
			public int Width, Height;
			public FloatColor[,] colors;

			public Tile(byte[] bytes, int bpp, int width, int height, FloatColor[] palette)
			{
				colors = BytesToFloatArray(bytes, width, height, bpp, palette);
			}

		}
		public class TilePalette
		{
			public int Width, Height;
			public List<Tile> tiles;

			public TilePalette(IEnumerable<Tile> value)
			{
				tiles = new List<Tile>(value);
				Width = tiles[0].Width;
				Height = tiles[0].Height;
			}
		}

		int BPP;

		public int FrameCount { get; private set; }
		public int Width { get; private set; }
		public int Height { get; private set; }
			
		public bool IndexedColors => BPP == 8;

		private int transparent;


		private List<FloatColor> colorPalette;
		private List<Layer> layers = new List<Layer>();
		private Dictionary<string, Layer> layerDictionary = new Dictionary<string, Layer>();
		private List<Tag> tags = new List<Tag>();
		private List<TilePalette> tilePalettes = new List<TilePalette>();

		public string[] TagNames {
			get {
				List<string> t = new List<string>();
				foreach (var tag in tags)
					t.Add(tag.name);

				return t.Distinct().ToArray();
			}
		}
		public Tag[] Tags => tags.ToArray();
		public string[] LayerNames {
			get {
				string[] retval = new string[layers.Count];
				for (int i = 0; i < layers.Count; ++i)
					retval[i] = layers[i].Name;
				return retval;
			}
		}
		public FloatColor[] ColorPalette => colorPalette.ToArray();

		public override float ReadSingle() {
			int value = ReadInt32();

			return value / (0x10000f);
		}
		public override string ReadString() {
			byte[] array = ReadBytes(ReadUInt16());

			return Encoding.UTF8.GetString(array);
		}
		public FloatColor ReadColor(int _x, int _y, int _frame = 0, string _layer = null) {
			FloatColor retval = new FloatColor();

			foreach (var layer in layers) {
				if (_layer != null && layer.Name != _layer)
					continue;

				var cel = layer.cels[_frame];

				if (!layer.visible || cel == null)
					continue;

				if (_x < cel.X || _x >= cel.X + cel.Width || _y < cel.Y || _y >= cel.Y + cel.Height)
					continue;

				retval = FloatColor.FlattenColor(retval, cel.ColorValues[_x - cel.X, _y - cel.Y], layer.blending);
			}

			return retval;
		}

		public AsepriteReader(string _filePath) : base(File.Open(_filePath, FileMode.Open)) {
			BaseStream.Seek(4, SeekOrigin.Begin);
			if (ReadUInt16() != 0xA5E0)
				throw new FileLoadException();

			FrameCount = ReadUInt16();
			Width = ReadUInt16();
			Height = ReadUInt16();

			BPP = ReadUInt16();

			BaseStream.Seek(14, SeekOrigin.Current);

			transparent = ReadByte();

			BaseStream.Seek(3, SeekOrigin.Current);

			colorPalette = new List<FloatColor>(new FloatColor[ReadUInt16()]);

			// Seek to the end of the header
			BaseStream.Seek(128, SeekOrigin.Begin);

			for (int i = 0; i < FrameCount; ++i) {
				BaseStream.Seek(6, SeekOrigin.Current);

				uint chunkCount = ReadUInt16();
				BaseStream.Seek(4, SeekOrigin.Current);

				if (chunkCount == 0xFFFF)
					chunkCount = ReadUInt32();
				else
					BaseStream.Seek(4, SeekOrigin.Current);

				bool usesNewPal = false;

				for (int chunk = 0; chunk < chunkCount; ++chunk) {
					long pos = BaseStream.Position;

					uint size = ReadUInt32();
					ushort type = ReadUInt16();

					if (type == 0x2019)
						usesNewPal = true;

					if (type != 0x0004 || !usesNewPal)
						ReadChunk(type, i, size);

					BaseStream.Seek(pos + size, SeekOrigin.Begin);
				}
			}

		}

		private void ReadChunk(uint type, int frameIndex, uint size) {


			string value = BaseStream.Position.ToString("X");

			switch (type) {
				case 0x2018: // Tag data
					{
					ushort count = ReadUInt16();
					BaseStream.Seek(8, SeekOrigin.Current);
					for (int i = 0; i < count; ++i) {
						int from = ReadUInt16(), to = ReadUInt16();
						BaseStream.Seek(13, SeekOrigin.Current);

						string name = ReadString();

						tags.Add(new Tag() { start = from, end = to, name = name });
					}
				}
				break;
				case 0x2004: // Layer Data
					{
					ushort flags = ReadUInt16();
					ushort layerType = ReadUInt16();
					BaseStream.Seek(6, SeekOrigin.Current);
					ushort blend = ReadUInt16();
					BaseStream.Seek(4, SeekOrigin.Current);
					string name = ReadString();

					var layerNew = new Layer(flags, blend, FrameCount, layerType, name, layerType == 2 ? tilePalettes[ReadInt32()] : null);
					layers.Add(layerNew);
					layerDictionary[name] = layerNew;
				}
				break;
				case 0x2005: // Cel Data
					{
					var layer = layers[ReadUInt16()];

					long idx = BaseStream.Position;
					BaseStream.Seek(5, SeekOrigin.Current);

					Cel cel;

					var b = ReadUInt16();
					switch (b) {
						case 1:
							BaseStream.Seek(7, SeekOrigin.Current);

							cel = layer.cels[ReadUInt16()];
							break;
						default:
							BaseStream.Seek(idx, SeekOrigin.Begin);
							cel = new Cel(this, layer, colorPalette.ToArray(), BPP, (int)size - 16);
							break;
					}


					layer.cels[frameIndex] = cel;
				}
				break;
				case 0x2007:

					break;
				case 0x2019: // Palette Data
					{
					int palSize = ReadInt32();

					if (palSize != colorPalette.Count) {
						if (palSize > colorPalette.Count)
							colorPalette.AddRange(new FloatColor[palSize - colorPalette.Count]);
						else
							while (colorPalette.Count > palSize)
								colorPalette.RemoveAt(colorPalette.Count - 1);
					}

					int start = ReadInt32();
					int end = ReadInt32();

					BaseStream.Seek(8, SeekOrigin.Current);

					for (; start <= end; ++start) {
						bool hasName = ReadInt16() == 1;

						FloatColor c = new FloatColor(ReadByte(), ReadByte(), ReadByte(), ReadByte());

						colorPalette[start] = c;

						if (hasName)
							BaseStream.Seek(ReadUInt16(), SeekOrigin.Current);

					}

					colorPalette[transparent] = new FloatColor();
				}
				break;
				
				case 0x2023: // Tile Palette Data

					int tilePaletteIndex = ReadInt32();

					BaseStream.Seek(4, SeekOrigin.Current);

					int tileCount = ReadInt32(),
						tileWidth = ReadUInt16(),
						tileHeight = ReadUInt16();

					BaseStream.Seek(18, SeekOrigin.Current);

					int compressedSize = ReadInt32();

					byte[] bits = DecompressBytes(this, compressedSize, tileWidth * tileHeight * tileCount * BPP / 8);

					List<Tile> tiles = new List<Tile>();

					using (BinaryReader read = new BinaryReader(new MemoryStream(bits)))
					{
						for (int i = 0; i < tileCount; ++i)
						{
							tiles.Add(new Tile(read.ReadBytes(tileWidth * tileHeight * BPP / 8), BPP, tileWidth, tileHeight, colorPalette.ToArray()));
						}
					}

					while (tilePalettes.Count <= tilePaletteIndex)
						tilePalettes.Add(null);

					tilePalettes[tilePaletteIndex] = new TilePalette(tiles);

					break;
				default:

					break;
			}
		}

		public FloatColor[,] GetFrameValue(int frame, bool onlyVisible, params string[] layerNames)
		{
			FloatColor[,] retval = new FloatColor[Width, Height];

			void AddLayer(Layer layer)
			{
				Cel cel = layer.cels[frame];

				if (cel == null || (!layer.visible && onlyVisible))
					return;

				FloatColor[,] celColor = cel.ColorValues;

				for (int y = Math.Max(-cel.Y, 0); y < cel.Height && (cel.Y + y) < Height; ++y)
				{
					for (int x = Math.Max(-cel.X, 0); x < cel.Width && (cel.X + x) < Width; ++x)
					{
						retval[cel.X + x, cel.Y + y] = FloatColor.FlattenColor(retval[cel.X + x, cel.Y + y], celColor[x, y], layer.blending);
					}
				}
			}

			if (layerNames == null || layerNames.Length == 0)
			{
				foreach (var layer in layers)
				{
					AddLayer(layer);
				}
			}
			else
			{
				foreach (var name in layerNames)
				{
					if (!layerDictionary.ContainsKey(name))
						continue;

					AddLayer(layerDictionary[name]);
				}
			}


			return retval;
		}

		public Tag GetTag(string name)
		{
			foreach (var tag in tags)
			{
				if (tag.name == name)
					return tag;
			}
			return null;
		}
	}
}