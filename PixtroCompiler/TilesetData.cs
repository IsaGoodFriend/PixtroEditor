using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pixtro.Compiler
{
	[Flags]
	public enum FlipStyle { X = 1, Y = 2, Both = X | Y, None = 0 }
	public interface IFlippable<T>
	{
		void Flip(FlipStyle style);
		void Unflip();

		bool EqualTo(T other, FlipStyle flippable);
		bool EqualToFlipped(T other, FlipStyle flippable);
		FlipStyle GetFlipDifference(T other);
	}
	internal class CompareBricks : CompareFlippable<Brick> {
		public CompareBricks() {

		}
		public CompareBricks(FlipStyle style) {
			flipStyle = style;
		}

		public override bool Equals(Brick x, Brick y) {
			if (!base.Equals(x, y))
				return false;
			return
				x.collisionChar == y.collisionChar &&
				x.collisionShape == y.collisionShape &&
				x.collisionType == y.collisionType;
		}

		public override int GetHashCode(Brick obj) {
			return obj.GetHashCode();
		}
	}
	internal class CompareFlippable<T> : IEqualityComparer<T> where T : IFlippable<T>
	{
		public CompareFlippable()
		{

		}
		public CompareFlippable(FlipStyle style)
		{
			flipStyle = style;
		}
		public FlipStyle flipStyle = FlipStyle.Both;

		public virtual bool Equals(T x, T y)
		{
			return x.EqualTo(y, flipStyle);
		}

		public virtual int GetHashCode(T obj)
		{
			return obj.GetHashCode();
		}
	}
	internal class CompareFlippable_Static<T> : IEqualityComparer<T> where T : IFlippable<T> {
		public CompareFlippable_Static() {

		}
		public CompareFlippable_Static(FlipStyle style) {
			flipStyle = style;
		}
		public FlipStyle flipStyle = FlipStyle.Both;

		public bool Equals(T x, T y) {
			return x.EqualToFlipped(y, flipStyle);
		}

		public int GetHashCode(T obj) {
			return obj.GetHashCode();
		}
	}

	public class Tile : IFlippable<Tile> {
		private byte[] bitData;
		private uint[] rawData;
		private FlipStyle flipped;

		public bool IsAir { get; private set; }

		public uint[] RawData => rawData;

		public byte this[int x, int y] {
			get {
				return bitData[x + (y * 8)];
			}
		}

		public Tile() {
			IsAir = true;

			bitData = new byte[64];
			rawData = new uint[8];
		}
		public Tile(Tile copy) : this() {
			LoadInData(copy.rawData, 0);
		}

		public void LoadInData(uint[] data, int index) {
			IsAir = true;
			for (int i = 0; i < 8; ++i) {
				rawData[i] = data[i + index];

				IsAir &= rawData[i] == 0;

				int offset = i;
				offset += index;

				bitData[(i << 3) + 0]   = (byte)((data[offset] & 0x0000000F) >> 0);
				bitData[(i << 3) + 1]   = (byte)((data[offset] & 0x000000F0) >> 4);
				bitData[(i << 3) + 2]   = (byte)((data[offset] & 0x00000F00) >> 8);
				bitData[(i << 3) + 3]   = (byte)((data[offset] & 0x0000F000) >> 12);
				bitData[(i << 3) + 4]   = (byte)((data[offset] & 0x000F0000) >> 16);
				bitData[(i << 3) + 5]   = (byte)((data[offset] & 0x00F00000) >> 20);
				bitData[(i << 3) + 6]   = (byte)((data[offset] & 0x0F000000) >> 24);
				bitData[(i << 3) + 7]   = (byte)((data[offset] & 0xF0000000) >> 28);

			}
		}

		public void Flip(FlipStyle flip) {
			if (flip == FlipStyle.X) {
				flipped ^= FlipStyle.X;
				bitData.Flip(true, 8);
			}
			else if (flip == FlipStyle.Y) {
				flipped ^= FlipStyle.Y;
				bitData.Flip(false, 8);
			}
			else if (flip != FlipStyle.None) {
				Flip(FlipStyle.X);
				Flip(FlipStyle.Y);
			}
		}
		public void Unflip() {
			Flip(flipped);
		}

		public bool EqualTo(Tile other, FlipStyle flippable) {
			if (other == null)
				return false;

			Unflip();
			other.Unflip();

			return EqualToFlipped(other, flippable);
		}
		public bool EqualToFlipped(Tile other, FlipStyle flippable) {
			if (other == null)
				return false;

			if (Enumerable.SequenceEqual(bitData, other.bitData))
				return true;

			// Check mirrored on X axis
			if (flippable.HasFlag(FlipStyle.X)) {
				Flip(FlipStyle.X);

				if (Enumerable.SequenceEqual(bitData, other.bitData))
					return true;

				if (flippable.HasFlag(FlipStyle.Y)) {
					Flip(FlipStyle.Y);

					if (Enumerable.SequenceEqual(bitData, other.bitData))
						return true;
				}

				Unflip();
			}
			// Check mirrored on the y axis only
			if (flippable.HasFlag(FlipStyle.Y)) {
				Flip(FlipStyle.Y);

				if (Enumerable.SequenceEqual(bitData, other.bitData))
					return true;
			}

			return false;
		}

		public FlipStyle GetFlipDifference(Tile other) {
			Unflip();
			other.Unflip();

			if (Enumerable.SequenceEqual(bitData, other.bitData))
				return FlipStyle.None;

			Flip(FlipStyle.X);
			if (Enumerable.SequenceEqual(bitData, other.bitData))
				return FlipStyle.X;

			Flip(FlipStyle.Y);
			if (Enumerable.SequenceEqual(bitData, other.bitData))
				return FlipStyle.Both;

			Flip(FlipStyle.X);
			if (Enumerable.SequenceEqual(bitData, other.bitData))
				return FlipStyle.Y;

			throw new Exception();
		}
		public ushort GetFlipOffset(Tile other) {
			return (ushort)GetFlipDifference(other);
		}

		public byte RawBit(int x, int y) {
			x <<= 2;
			return (byte)((rawData[y] & (0xF << x)) >> x);
		}
		public override string ToString() {
			var flipNow = flipped;

			if (flipped != FlipStyle.None)
				Unflip();

			string retval = "";
			for (int i = 0; i < bitData.Length; ++i) {
				retval += bitData[i].ToString("X");
			}

			Flip(flipNow);

			return retval;
		}

	}
	public class LargeTile : IFlippable<LargeTile>
	{
		private FlipStyle flipped;
		private static IEnumerable<Tile> GetEmptyTiles(int size)
		{
			size *= size;
			for (int i = 0; i < size; ++i)
			{
				yield return new Tile();
			}
		}

		public int SizeOfTile { get; internal set; }

		internal byte[] bitData;
		internal Tile[,] tiles;

		public bool IsAir { get; private set; }

		public LargeTile(Tile[] tileArray, int tileSize)
		{
			SizeOfTile = tileSize;
			tileSize /= 8;
			tiles = new Tile[tileSize, tileSize];
			bitData = new byte[tileSize * tileSize * 64];

			int x = 0, y = 0;

			IsAir = true;
			for (int i = 0; i < tileSize * tileSize; ++i)
			{
				var currTile = tiles[x, y] = tileArray[i];

				for (int u = 0; u < 8; ++u) {
					for (int v = 0; v < 8; ++v) {
						bitData[(u + (x * 8)) + (v + (y * 8)) * 8] = currTile[u, v];
					}
				}

				IsAir &= currTile.IsAir;

				if (++x >= tileSize)
				{
					x = 0;
					++y;
				}
			}
		}
		public LargeTile(int widthInTiles) : this(GetEmptyTiles(widthInTiles).ToArray(), widthInTiles << 3) { }


		public void Flip(FlipStyle flip) {
			if ((flip & FlipStyle.X) != FlipStyle.None) {
				flipped ^= FlipStyle.X;

				bitData.Flip(true, SizeOfTile);
			}
			if ((flip & FlipStyle.Y) != FlipStyle.None) {
				flipped ^= FlipStyle.Y;

				bitData.Flip(false, SizeOfTile);
			}
		}
		public void Unflip()
		{
			Flip(flipped);
		}

		public bool EqualTo(LargeTile other, FlipStyle flippable) {
			if (other == null || other.SizeOfTile != SizeOfTile)
				return false;
			if (ReferenceEquals(this, other))
				return true;

			Unflip();
			other.Unflip();

			return EqualToFlipped(other, flippable);
		}
		public bool EqualToFlipped(LargeTile other, FlipStyle flippable)
		{
			if (other == null || other.SizeOfTile != SizeOfTile)
				return false;
			if (ReferenceEquals(this, other))
				return true;

			var comparer = new CompareFlippable<Tile>(){ flipStyle = flippable };

			if (Enumerable.SequenceEqual(bitData, other.bitData))
				return true;

			// Check mirrored on X axis
			if (flippable.HasFlag(FlipStyle.X))
			{
				Flip(FlipStyle.X);

				if (Enumerable.SequenceEqual(bitData, other.bitData))
					return true;

				if (flippable.HasFlag(FlipStyle.Y))
				{
					Flip(FlipStyle.Y);

					if (Enumerable.SequenceEqual(bitData, other.bitData))
						return true;
				}

				Unflip();
			}
			// Check mirrored on the y axis only
			if (flippable.HasFlag(FlipStyle.Y))
			{
				Flip(FlipStyle.Y);

				if (Enumerable.SequenceEqual(bitData, other.bitData))
					return true;
			}

			return false;
		}

		public FlipStyle GetFlipDifference(LargeTile other)
		{
			Unflip();
			other.Unflip();

			if (Enumerable.SequenceEqual(bitData, other.bitData))
				return FlipStyle.None;

			Flip(FlipStyle.X);
			if (Enumerable.SequenceEqual(bitData, other.bitData))
				return FlipStyle.X;

			Flip(FlipStyle.Y);
			if (Enumerable.SequenceEqual(bitData, other.bitData))
				return FlipStyle.Both;

			Flip(FlipStyle.X);
			if (Enumerable.SequenceEqual(bitData, other.bitData))
				return FlipStyle.Y;

			throw new Exception();
		}

		public ushort GetFlipOffset(LargeTile other)
		{
			return (ushort)GetFlipDifference(other);
		}

		public override string ToString() {
			var flipNow = flipped;

			if (flipped != FlipStyle.None)
				Unflip();

			string retval = "";
			for (int i = 0; i < bitData.Length; ++i) {
				retval += bitData[i].ToString("X");
			}

			Flip(flipNow);

			return retval;
		}
	}
	public class Brick : LargeTile, IFlippable<Brick>
	{
		public char collisionChar;
		public int collisionType, collisionShape, palette;

		public Brick(int widthInTiles) : base(widthInTiles) { }
		public Brick(Tile[] tileArray, int tileSize) : base(tileArray, tileSize) { }
		public Brick(LargeTile copy) : base(copy.tiles.Cast<Tile>().ToArray(), copy.SizeOfTile) { }

		public new void Flip(FlipStyle flip)
		{
			base.Flip(flip);
		}
		public new void Unflip()
		{
			base.Unflip();
		}

		public bool EqualTo(Brick other, FlipStyle flippable)
		{
			return base.EqualTo(other, flippable);
		}
		public bool EqualToFlipped(Brick other, FlipStyle flippable) {
			if (collisionChar != other.collisionChar)
				return false;
			return base.EqualToFlipped(other, flippable);
		}
		public FlipStyle GetFlipDifference(Brick other)
		{
			return base.GetFlipDifference(other);
		}
	}

	public class FlippableLayout<T> where T : IFlippable<T>
	{
		internal class FlippableCount<T> where T : IFlippable<T>
		{
			public T tile;
			public int count;

			public static implicit operator T(FlippableCount<T> c) => c.tile;
			public static implicit operator FlippableCount<T>(T t) => new FlippableCount<T>() { tile = t, count = 0 };
		}
		internal class FlipCountComparing<T> : IEqualityComparer<FlippableCount<T>> where T : IFlippable<T>
		{
			public FlipStyle flipStyle = FlipStyle.Both;

			public bool Equals(FlippableCount<T> x, FlippableCount<T> y)
			{
				return x.tile.EqualTo(y.tile, flipStyle);
			}

			public int GetHashCode(FlippableCount<T> obj)
			{
				return obj.tile.GetHashCode();
			}
		}

		private FlipCountComparing<T> comparing;

		public FlipStyle flipAcceptance;

		int width, height;
		private T[,] layout;
		internal List<FlippableCount<T>> tiles = new List<FlippableCount<T>>();

		public FlippableLayout(int width, int height)
		{
			comparing = new FlipCountComparing<T>();

			this.width = width;
			this.height = height;
			layout = new T[width, height];
		}
		public FlippableLayout(int width, int height, IEnumerator<T> collection) :this(width, height)
		{
			collection.MoveNext();

			int x = 0, y = 0;

			for (int i = 0; i < width * height; ++i)
			{
				Add(collection.Current, x, y);

				collection.MoveNext();

				if (++x >= width)
				{
					x = 0;
					++y;
				}
			}
		}

		private void Add(T obj, int x, int y)
		{
			layout[x, y] = obj;
			layout[x, y].Unflip();

			if (tiles.Contains<FlippableCount<T>>(obj, comparing))
			{
				tiles[tiles.IndexOf(obj, comparing)].count++;
			}
			else
			{
				tiles.Add(new FlippableCount<T>() { tile = obj, count = 1 });
			}
		}

		public T GetUniqueTile(T version)
		{

			foreach (var t in tiles)
			{
				if (t.tile.EqualTo(version, FlipStyle.Both))
					return t;
			}

			return default(T);
		}
		public T GetUniqueTile(int x, int y)
		{
			if (layout == null)
				return default(T);

			return GetUniqueTile(GetTile(x, y));
		}
		public T GetTile(int x, int y)
		{
			if (layout == null)
				return default(T);

			return layout[x, y];
		}
		public FlipStyle GetFlipIndex(int x, int y)
		{
			T ogTile = GetTile(x, y),
				uniqueTile = GetUniqueTile(x, y);

			return ogTile.GetFlipDifference(uniqueTile);
		}
		public virtual ushort GetIndex(T version)
		{
			return (ushort)tiles.IndexOf(GetUniqueTile(version));
		}
	}
}