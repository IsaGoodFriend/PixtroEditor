using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.IO;

namespace Pixtro.Compiler {
	public struct FloatColor {
		public float R, G, B, A;

		public FloatColor(byte r, byte g, byte b, byte a) {
			R = r / 255f;
			G = g / 255f;
			B = b / 255f;
			A = a / 255f;

			if (A == 0) {
				R = 0;
				G = 0;
				B = 0;
			}
		}
		public FloatColor(Color color) : this(color.R, color.G, color.B, color.A) {
		}

		public static FloatColor FlattenColor(FloatColor colorA, FloatColor colorB, BlendType blend) {
			FloatColor color = colorA;

			if (colorB.A <= 0)
				return colorA;

			switch (blend) {
				case BlendType.Normal:
					color.R = colorB.R;
					color.G = colorB.G;
					color.B = colorB.B;
					color.A = Math.Max(colorA.A, colorB.A);
					break;
			}

			return color;
		}

		public ushort ToGBA(ushort _transparent = 0x8000) {
			if (A <= 0)
				return _transparent;

			int r = (int)(R * 255);
			int g = (int)(G * 255);
			int b = (int)(B * 255);

			r = (r & 0xF8) >> 3;
			g = (g & 0xF8) >> 3;
			b = (b & 0xF8) >> 3;

			return (ushort)(r | (g << 5) | (b << 10));
		}
		public Color ToGBAColor() {
			if (A <= 0.5f) {
				return Color.FromArgb(0, 0, 0, 0);
			}

			byte r = (byte)(Math.Floor(R * 255/8f) * 8);
			byte g = (byte)(Math.Floor(G * 255/8f) * 8);
			byte b = (byte)(Math.Floor(B * 255/8f) * 8);

			return Color.FromArgb(255, r, g, b);
		}

		public override string ToString() {
			return $"{{{R:0.00} - {G:0.00} - {B:0.00} :: {A:0.00}}}";
		}
	}
	public class GBAImage
	{
		public static Bitmap GetFormattedBitmap(string path) {
			Bitmap map = new Bitmap(path);

			if (map.PixelFormat != System.Drawing.Imaging.PixelFormat.Format32bppArgb) {

				Bitmap clone = new Bitmap(map.Width, map.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

				using (Graphics gr = Graphics.FromImage(clone)) {
					gr.DrawImage(map, new Rectangle(0, 0, clone.Width, clone.Height));
				}
				return clone;
			}
			else
				return map;

		}

		private unsafe static GBAImage FromBitmap(Bitmap map, Rectangle section, List<Color[]> palettes)
		{
			FloatColor[,] values = new FloatColor[section.Width, section.Height];

			var data = map.LockBits(section, System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

			byte* ptr = (byte*)data.Scan0;

			for (int y = section.Top; y < section.Bottom; ++y)
			{
				for (int x = section.Left; x < section.Right; ++x)
				{
					int i = (x * 4) + (y * data.Stride);

					values[x - section.X, y - section.Y] = new FloatColor(ptr[i + 2], ptr[i + 1], ptr[i], ptr[i + 3]);
				}
			}
			
			map.UnlockBits(data);

			return new GBAImage(values, palettes);
		}
		private unsafe static GBAImage FromBitmap(byte* ptr, int stride, Rectangle section, List<Color[]> palettes) {
			FloatColor[,] values = new FloatColor[section.Width, section.Height];

			for (int y = section.Top; y < section.Bottom; ++y) {
				for (int x = section.Left; x < section.Right; ++x) {
					int i = (x * 4) + (y * stride);

					values[x - section.X, y - section.Y] = new FloatColor(ptr[i + 2], ptr[i + 1], ptr[i], ptr[i + 3]);
				}
			}

			return new GBAImage(values, palettes);
		}

		public static GBAImage FromFile(string path, List<Color[]> palettes)
		{
			Bitmap map = GetFormattedBitmap(path);

			if (map.Width % 8 != 0 || map.Height % 8 != 0)
				throw new FormatException("Image is not the correct size.  Make sure all your images width/height are divisible by 8");

			var image = FromBitmap(map, new Rectangle(0, 0, map.Width, map.Height), palettes);

			map.Dispose();

			return image;
		}
		public unsafe static GBAImage[] AnimateFromFile(string path, int width, int height, List<Color[]> palettes)
		{
			if (width % 8 != 0 || height % 8 != 0)
				throw new FormatException("Image is not the correct size.  Make sure all your images width/height are divisible by 8");

			Bitmap map = GetFormattedBitmap(path);

			// Throw error if file can't be divided evenly
			if (map.Width % width != 0 || map.Width % height != 0)
				throw new FormatException("Image cannot be divided properly for animation.  Make sure your animated images have all frames in full");

			int frameX = map.Width / width,
				frameY = map.Height / height;

			List<GBAImage> images = new List<GBAImage>();

			MainProgram.DebugLog(Path.GetFileName(path));

			var data = map.LockBits(new Rectangle(0, 0, map.Width, map.Height), System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

			byte* ptr = (byte*)data.Scan0;

			for (int y = 0; y < frameY; ++y)
			{
				for (int x = 0; x < frameX; ++x)
				{
					images.Add(FromBitmap(ptr, data.Stride, new Rectangle(x * width, y * height, width, height), palettes));
				}
			}

			map.UnlockBits(data);

			map.Dispose();

			return images.ToArray();
		}
		public static GBAImage[] FromAsepriteProject(string path, string tag = null, string layer = null)
		{
			using (AsepriteReader reader = new AsepriteReader(path))
			{
				return FromAsepriteProject(reader, tag, layer);
			}
		}
		public static GBAImage[] FromAsepriteProject(AsepriteReader reader, string tag = null, string layer = null)
		{
			List<Color[]> palettes = null;
			if (reader.IndexedColors)
			{
				var colors = reader.ColorPalette;

				palettes = new List<Color[]>();

				int paletteCount = (colors.Length + 14) >> 4;

				for (int i = 0; i < paletteCount << 4; i += 16)
				{
					Color[] pal = new Color[16];

					pal[0] = Color.FromArgb(0, 0, 0, 0);
					int index;
					for (index = 1; index < 16 && (index + i) < colors.Length; ++index)
					{
						pal[index] = colors[index + i].ToGBAColor();
					}
					for (; index < 16; ++index)
					{
						pal[index] = Color.FromArgb(0, 0, 0, 0);
					}

					palettes.Add(pal);
				}
			}

			// Angry.  You didn't feed me a properly formatted image
			if (reader.Width % 8 != 0 || reader.Height % 8 != 0)
				throw new FormatException("Aseprite image is not the correct size.  Make sure all your images width/height are divisible by 8");


			int start = 0, end = reader.FrameCount;

			if (tag != null && reader.TagNames.Contains(tag))
			{
				var t = reader.GetTag(tag);
				start = t.start;
				end = t.end + 1;
			}

			GBAImage[] retval = new GBAImage[end - start];

			for (int i = start; i < end; ++i)
			{
				if (layer != null)
					retval[i - start] = new GBAImage(reader.GetFrameValue(i, true, layer), palettes);
				else
					retval[i - start] = new GBAImage(reader.GetFrameValue(i, true), palettes);
			}

			return retval;
			
		}

		public int Width { get; private set; }
		public int Height { get; private set; }

		private int[,] baseValues;
		private List<Color[]> finalPalettes;
		private FloatColor[,] originalData;

		public IReadOnlyList<Color[]> Palettes => finalPalettes;

		private bool palettesLocked;

		private GBAImage(FloatColor[,] colors, List<Color[]> exportPalettes = null)
		{
			Width = colors.GetLength(0);
			Height = colors.GetLength(1);
			baseValues = new int[Width, Height];

			originalData = colors;

			RecompileColors(exportPalettes);
		}
		public GBAImage(string path) {
			using (var reader = new BinaryReader(File.OpenRead(path))) {
				Width = reader.Read();
				Height = reader.Read();

				baseValues = new int[Width, Height];

				int paletteCount = reader.ReadByte();

				reader.BaseStream.Seek(16, SeekOrigin.Begin);

				for (int i = 0; i < paletteCount; ++i) {
					List<Color> palette = new List<Color>();
					for (int c = 0; c < 16; ++c) {
						Color col = Color.FromArgb(reader.ReadByte(), reader.ReadByte(), reader.ReadByte(), reader.ReadByte());

						palette.Add(col);
					}
					finalPalettes.Add(palette.ToArray());
				}

				for (int y = 0; y < Height; ++y)
					for (int x = 0; x < Width; ++x)
						baseValues[x, y] = reader.Read();

			}
		}
		public void Save(string path) {
			using (var writer = new BinaryWriter(File.Open(path, FileMode.OpenOrCreate, FileAccess.Write))) {
				writer.Write(Width);
				writer.Write(Height);

				writer.Write((byte)finalPalettes.Count);

				writer.BaseStream.Seek(16, SeekOrigin.Begin);

				for (int i = 0; i < finalPalettes.Count; ++i) {
					for (int c = 0; c < 16; ++c) {
						writer.Write(finalPalettes[i][c].A);
						writer.Write(finalPalettes[i][c].R);
						writer.Write(finalPalettes[i][c].G);
						writer.Write(finalPalettes[i][c].B);
					}
				}

				for (int y = 0; y < Height; ++y)
					for (int x = 0; x < Width; ++x)
						writer.Write(baseValues[x, y]);
			}
		}

		public void RecompileColors(List<Color[]> exportPalettes = null)
		{
			List<Color?[]> palettes = new List<Color?[]>();

			if (exportPalettes != null)
			{
				palettes.Clear();
				foreach (var pal in exportPalettes)
				{
					pal[0] = Color.Transparent;
					var newPal = pal.Select(val => {
						var c = new FloatColor(val).ToGBAColor();
						return (Color?)c;
						}
					).ToArray();
					palettes.Add(newPal);
				}
				palettesLocked = true;
			}

			for (int ty = 0; ty < Height; ty += 8)
			{
				for (int tx = 0; tx < Width; tx += 8)
				{
					List<Color> palette = new List<Color>();

					Color[,] rawData = new Color[8, 8];

					for (int y = 0; y < 8; ++y)
					{
						for (int x = 0; x < 8; ++x)
						{
							rawData[x, y] = originalData[x + tx, y + ty].ToGBAColor();
							if (!palette.Contains(rawData[x, y]))
								palette.Add(rawData[x, y]);
						}
					}

					int paletteIndex = 0;

					foreach (var pal in palettes)
					{
						var foundPalette = pal;

						foreach (var col in palette)
						{
							int i = 0;
							for (; i < pal.Length; ++i) {
								if (pal[i].HasValue && (pal[i].Value == col || (pal[i].Value.A == 0 && col.A == 0))) {
									break;
								}
							}
							if (i == pal.Length)
							{
								foundPalette = null;
								break;
							}
						}

						if (foundPalette != null)
						{
							palette = new List<Color>(foundPalette.Where(value => value != null).Select(value => (Color)value));
							break;
						}
						paletteIndex++;
					}
					if (paletteIndex == palettes.Count)
					{
						if (palettesLocked) // TODO: allow compiler to find closest palette instead of throwing error?
							throw new Exception($"Error compiling image {FullCompiler.CompilerErrorInfo[0]}. The image's palettes have already been chosen, but no palette given is suitable to import the image.  Make sure the color are exact.");

						paletteIndex = 0;

						bool selectedPalette = false;
						foreach (var pal in palettes)
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

								palette = new List<Color>(pal.Where(value => value != null).Select(value => (Color)value));
								break;
							}

							paletteIndex++;
						}

						if (!selectedPalette)
						{
							List<Color?> addPal = new List<Color?>(palette.Select(value => (Color?)value));

							while (addPal.Count < 16)
								addPal.Add(null);
							palettes.Add(addPal.ToArray());
						}

					}

					paletteIndex <<= 12;
					for (int y = 0; y < 8; ++y)
					{
						for (int x = 0; x < 8; ++x)
						{
							baseValues[x + tx, y + ty] = palette.IndexOf(rawData[x, y]) | paletteIndex;
						}
					}
				}
			}

			finalPalettes = new List<Color[]>();
			foreach (var pal in palettes)
			{
				finalPalettes.Add(pal.Where(value => value != null).Select(val => (Color)val).ToArray());
			}
			palettesLocked = true;
		}

		public FlippableLayout<LargeTile> GetLargeTileSet(int widthInTiles)
		{
			return new FlippableLayout<LargeTile>((Width >> 3) / widthInTiles, (Height >> 3) / widthInTiles, GetLargeTiles(widthInTiles).GetEnumerator());

		}
		public Tile GetTile()
		{
			return GetTiles().First();
		}

		public int GetPaletteIndex(int x, int y)
		{
			return baseValues[x << 3, y << 3] & 0xF000;
		}

		public IEnumerable<Tile> GetTiles()
		{
			uint[] array = GetTileData().ToArray();

			int size = Width * Height / 64;

			for (int i = 0; i < size; ++i)
			{
				var tile = new Tile();

				tile.LoadInData(array, i * 8);

				yield return tile;
			}
		}
		public IEnumerable<LargeTile> GetLargeTiles(int widthInTiles)
		{
			Tile[] array = GetTiles().ToArray();

			int tileWidth = Width / 8, tileHeight = Height / 8;

			for (int y = 0; y < tileHeight; y += widthInTiles)
			{
				for (int x = 0; x < tileWidth; x += widthInTiles)
				{
					List<Tile> tileList = new List<Tile>();
					for (int ly = 0; ly < widthInTiles; ++ly)
					{
						for (int lx = 0; lx < widthInTiles; ++lx)
						{
							tileList.Add(array[lx + x + (ly + y) * tileWidth]);
						}
					}

					yield return new LargeTile(tileList.ToArray(), widthInTiles * 8);
				}
			}
		}
		public IEnumerable<uint> GetTileData()
		{
			const int tileSize = 8;

			uint dumpValue = 0;

			for (int ty = 0; ty < Height; ty += tileSize)
			{
				for (int tx = 0; tx < Width; tx += tileSize)
				{
					for (int y = 0; y < tileSize; ++y)
					{
						for (int x = tileSize - 1; x >= 0; --x)
						{
							dumpValue <<= 4;
							dumpValue |= (uint)baseValues[tx + x, ty + y] & 0xF;

						}
						yield return dumpValue;
					}
				}
			}

			yield break;
		}
		public IEnumerable<uint> GetSpriteData()
		{
			const int tileSize = 8;

			// Can't have sprites that have multiple palettes
			if (finalPalettes.Count > 1)
				throw new Exception();

			uint dumpValue = 0;

			for (int ty = 0; ty < Height; ty += tileSize)
			{
				for (int tx = 0; tx < Width; tx += tileSize)
				{
					for (int y = 0; y < tileSize; ++y)
					{
						for (int x = tileSize - 1; x >= 0; --x)
						{
							dumpValue <<= 4;
							dumpValue |= (uint)baseValues[tx + x, ty + y] & 0xF;
							
						}
						yield return dumpValue;
					}
				}
			}

			yield break;
		}
	}
}
