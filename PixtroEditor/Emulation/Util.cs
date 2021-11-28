﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Threading;

public static unsafe class Util
{
	public static void CopyStream(Stream src, Stream dest, long len)
	{
		const int size = 0x2000;
		var buffer = new byte[size];
		while (len > 0)
		{
			var n = src.Read(buffer, 0, (int) Math.Min(len, size));
			dest.Write(buffer, 0, n);
			len -= n;
		}
	}

	/// <summary>equivalent to <see cref="Console.WriteLine()">Console.WriteLine</see> but is <c>#ifdef DEBUG</c></summary>
	[Conditional("DEBUG")]
	public static void DebugWriteLine() => Console.WriteLine();

	/// <summary>equivalent to <see cref="Console.WriteLine(string)">Console.WriteLine</see> but is <c>#ifdef DEBUG</c></summary>
	[Conditional("DEBUG")]
	public static void DebugWriteLine(string value) => Console.WriteLine(value);

	/// <summary>equivalent to <see cref="Console.WriteLine(object)">Console.WriteLine</see> but is <c>#ifdef DEBUG</c></summary>
	[Conditional("DEBUG")]
	public static void DebugWriteLine(object value) => Console.WriteLine(value);

	/// <summary>equivalent to <see cref="Console.WriteLine(string, object[])">Console.WriteLine</see> but is <c>#ifdef DEBUG</c></summary>
	[Conditional("DEBUG")]
	public static void DebugWriteLine(string format, params object[] arg) => Console.WriteLine(format, arg);

	/// <exception cref="InvalidOperationException">issues with parsing <paramref name="src"/></exception>
	/// <remarks>TODO use <see cref="MemoryStream(int)"/> and <see cref="MemoryStream.ToArray"/> instead of using <see cref="MemoryStream(byte[])"/> and keeping a reference to the array? --yoshi</remarks>
	public static byte[] DecompressGzipFile(Stream src)
	{
		var tmp = new byte[4];
		if (src.Read(tmp, 0, 2) != 2) throw new InvalidOperationException("Unexpected end of stream");
		if (tmp[0] != 0x1F || tmp[1] != 0x8B) throw new InvalidOperationException("GZIP header not present");
		src.Seek(-4, SeekOrigin.End);
		src.Read(tmp, 0, 4);
		src.Seek(0, SeekOrigin.Begin);
		using var gs = new GZipStream(src, CompressionMode.Decompress, true);
		var data = new byte[BitConverter.ToInt32(tmp, 0)];
		using var ms = new MemoryStream(data);
		gs.CopyTo(ms);
		return data;
	}

	/// <remarks>adapted from https://stackoverflow.com/a/3928856/7467292, values are compared using <see cref="EqualityComparer{T}.Default">EqualityComparer.Default</see></remarks>
	public static bool DictionaryEqual<TKey, TValue>(IDictionary<TKey, TValue> a, IDictionary<TKey, TValue> b)
		where TKey : notnull
	{
		if (a == b) return true;
		if (a.Count != b.Count) return false;
		var comparer = EqualityComparer<TValue>.Default;
		return a.All(kvp => b.TryGetValue(kvp.Key, out var bVal) && comparer.Equals(kvp.Value, bVal));
	}

	/// <param name="filesize">in bytes</param>
	/// <returns>human-readable filesize (converts units up to tebibytes)</returns>
	public static string FormatFileSize(long filesize)
	{
		if (filesize < 1024) return $"{filesize} B";
		if (filesize < 1048576) return $"{filesize / 1024.0:.##} KiB";
		if (filesize < 1073741824) return $"{filesize / 1048576.0:.##} MiB";
		if (filesize < 1099511627776) return $"{filesize / 1073741824.0:.##} GiB";
		return $"{filesize / 1099511627776.0:.##} TiB";
	}

	/// <returns>all <see cref="Type">Types</see> with the name <paramref name="className"/></returns>
	/// <remarks>adapted from https://stackoverflow.com/a/13727044/7467292</remarks>
	public static IList<Type> GetTypeByName(string className) => AppDomain.CurrentDomain.GetAssemblies()
		.SelectMany(asm => asm.GetTypesWithoutLoadErrors().Where(type => className.Equals(type.Name, StringComparison.InvariantCultureIgnoreCase))).ToList();

	/// <remarks>TODO replace this with GetTypes (i.e. the try block) when VB.NET dep is properly removed</remarks>
	public static IEnumerable<Type> GetTypesWithoutLoadErrors(this Assembly assembly)
	{
		try
		{
			return assembly.GetTypes();
		}
		catch (ReflectionTypeLoadException e)
		{
			return e.Types.Where(t => t != null);
		}
	}

	/// <exception cref="ArgumentException"><paramref name="str"/> has an odd number of chars or contains a char not in <c>[0-9A-Fa-f]</c></exception>
	public static byte[] HexStringToBytes(this string str)
	{
		if (str.Length % 2 != 0) throw new ArgumentException();
		static int CharToNybble(char c)
		{
			if ('0' <= c && c <= '9') return c - 0x30;
			if ('A' <= c && c <= 'F') return c - 0x37;
			if ('a' <= c && c <= 'f') return c - 0x57;
			throw new ArgumentException();
		}
		using var ms = new MemoryStream();
		for (int i = 0, l = str.Length / 2; i != l; i++) ms.WriteByte((byte) ((CharToNybble(str[2 * i]) << 4) + CharToNybble(str[2 * i + 1])));
		return ms.ToArray();
	}

	public static int Memcmp(void* a, void* b, int len)
	{
		var ba = (byte*) a;
		var bb = (byte*) b;
		for (var i = 0; i != len; i++)
		{
			var _a = ba[i];
			var _b = bb[i];
			var c = _a - _b;
			if (c != 0) return c;
		}
		return 0;
	}

	public static void Memset(void* ptr, int val, int len)
	{
		var bptr = (byte*) ptr;
		for (var i = 0; i != len; i++) bptr[i] = (byte) val;
	}

	public static byte[]? ReadByteBuffer(this BinaryReader br, bool returnNull)
	{
		var len = br.ReadInt32();
		if (len == 0 && returnNull) return null;
		var ret = new byte[len];
		var ofs = 0;
		while (len > 0)
		{
			var done = br.Read(ret, ofs, len);
			ofs += done;
			len -= done;
		}
		return ret;
	}

	/// <remarks>Any non-zero element is interpreted as <see langword="true"/>.</remarks>
	public static bool[] ToBoolBuffer(this byte[] buf)
	{
		var ret = new bool[buf.Length];
		for (int i = 0, len = buf.Length; i != len; i++) ret[i] = buf[i] != 0;
		return ret;
	}

	public static double[] ToDoubleBuffer(this byte[] buf)
	{
		var len = buf.Length;
		var ret = new double[len / 8];
		Buffer.BlockCopy(buf, 0, ret, 0, len);
		return ret;
	}

	public static float[] ToFloatBuffer(this byte[] buf)
	{
		var len = buf.Length;
		var ret = new float[len / 4];
		Buffer.BlockCopy(buf, 0, ret, 0, len);
		return ret;
	}

	/// <remarks>Each set of 4 elements in <paramref name="buf"/> becomes 1 element in the returned buffer. The first of each set is interpreted as the LSB, with the 4th being the MSB. Elements are used as raw bits without regard for sign.</remarks>
	public static int[] ToIntBuffer(this byte[] buf)
	{
		var len = buf.Length / 4;
		var ret = new int[len];
		unchecked
		{
			for (var i = 0; i != len; i++) ret[i] = (buf[4 * i + 3] << 24) | (buf[4 * i + 2] << 16) | (buf[4 * i + 1] << 8) | buf[4 * i];
		}
		return ret;
	}

	/// <remarks>Each pair of elements in <paramref name="buf"/> becomes 1 element in the returned buffer. The first of each pair is interpreted as the LSB. Elements are used as raw bits without regard for sign.</remarks>
	public static short[] ToShortBuffer(this byte[] buf)
	{
		var len = buf.Length / 2;
		var ret = new short[len];
		unchecked
		{
			for (var i = 0; i != len; i++) ret[i] = (short) ((buf[2 * i + 1] << 8) | buf[2 * i]);
		}
		return ret;
	}

	public static byte[] ToUByteBuffer(this bool[] buf)
	{
		var ret = new byte[buf.Length];
		for (int i = 0, len = buf.Length; i != len; i++) ret[i] = buf[i] ? (byte) 1 : (byte) 0;
		return ret;
	}

	public static byte[] ToUByteBuffer(this double[] buf)
	{
		var len = buf.Length * 8;
		var ret = new byte[len];
		Buffer.BlockCopy(buf, 0, ret, 0, len);
		return ret;
	}

	public static byte[] ToUByteBuffer(this float[] buf)
	{
		var len = buf.Length * 4;
		var ret = new byte[len];
		Buffer.BlockCopy(buf, 0, ret, 0, len);
		return ret;
	}

	/// <remarks>Each element of <paramref name="buf"/> becomes 4 elements in the returned buffer, with the LSB coming first. Elements are used as raw bits without regard for sign.</remarks>
	public static byte[] ToUByteBuffer(this int[] buf)
	{
		var len = buf.Length;
		var ret = new byte[4 * len];
		unchecked
		{
			for (var i = 0; i != len; i++)
			{
				ret[4 * i] = (byte) buf[i];
				ret[4 * i + 1] = (byte) (buf[i] >> 8);
				ret[4 * i + 2] = (byte) (buf[i] >> 16);
				ret[4 * i + 3] = (byte) (buf[i] >> 24);
			}
		}
		return ret;
	}

	/// <remarks>Each element of <paramref name="buf"/> becomes 2 elements in the returned buffer, with the LSB coming first. Elements are used as raw bits without regard for sign.</remarks>
	public static byte[] ToUByteBuffer(this short[] buf)
	{
		var len = buf.Length;
		var ret = new byte[2 * len];
		unchecked
		{
			for (var i = 0; i != len; i++)
			{
				ret[2 * i] = (byte) buf[i];
				ret[2 * i + 1] = (byte) (buf[i] >> 8);
			}
		}
		return ret;
	}

	/// <inheritdoc cref="ToUByteBuffer(int[])"/>
	public static byte[] ToUByteBuffer(this uint[] buf)
	{
		var len = buf.Length;
		var ret = new byte[4 * len];
		unchecked
		{
			for (var i = 0; i != len; i++)
			{
				ret[4 * i] = (byte) buf[i];
				ret[4 * i + 1] = (byte) (buf[i] >> 8);
				ret[4 * i + 2] = (byte) (buf[i] >> 16);
				ret[4 * i + 3] = (byte) (buf[i] >> 24);
			}
		}
		return ret;
	}

	/// <inheritdoc cref="ToUByteBuffer(short[])"/>
	public static byte[] ToUByteBuffer(this ushort[] buf)
	{
		var len = buf.Length;
		var ret = new byte[2 * len];
		unchecked
		{
			for (var i = 0; i != len; i++)
			{
				ret[2 * i] = (byte) buf[i];
				ret[2 * i + 1] = (byte) (buf[i] >> 8);
			}
		}
		return ret;
	}

	/// <inheritdoc cref="ToIntBuffer"/>
	public static uint[] ToUIntBuffer(this byte[] buf)
	{
		var len = buf.Length / 4;
		var ret = new uint[len];
		unchecked
		{
			for (var i = 0; i != len; i++) ret[i] = (uint) ((buf[4 * i + 3] << 24) | (buf[4 * i + 2] << 16) | (buf[4 * i + 1] << 8) | buf[4 * i]);
		}
		return ret;
	}

	/// <inheritdoc cref="ToShortBuffer"/>
	public static ushort[] ToUShortBuffer(this byte[] buf)
	{
		var len = buf.Length / 2;
		var ret = new ushort[len];
		unchecked
		{
			for (var i = 0; i != len; i++) ret[i] = (ushort) ((buf[2 * i + 1] << 8) | buf[2 * i]);
		}
		return ret;
	}

	/// <summary>Tries really hard to keep the contents of <paramref name="desiredPath"/> saved (as <paramref name="backupPath"/>) while freeing that path to be used for a new file.</summary>
	/// <remarks>If both <paramref name="desiredPath"/> and <paramref name="backupPath"/> exist, <paramref name="backupPath"/> is always deleted.</remarks>
	public static bool TryMoveBackupFile(string desiredPath, string backupPath)
	{
		if (!File.Exists(desiredPath)) return true; // desired path already free

		// delete any existing backup
		try
		{
			if (File.Exists(backupPath)) File.Delete(backupPath);
		}
		catch
		{
			return false; // if Exists or Delete threw, there's not much we can do -- the caller will either overwrite the file or fail itself
		}

		// deletions are asynchronous, so wait for a while and then give up
		static bool TryWaitForFileToVanish(string path)
		{
			for (var i = 25; i != 0; i--)
			{
				if (!File.Exists(path)) return true;
				Thread.Sleep(10);
			}
			return false;
		}
		if (!TryWaitForFileToVanish(backupPath)) return false;

		// the backup path is available now, so perform the backup and then wait for it to finish
		try
		{
			File.Move(desiredPath, backupPath);
			return TryWaitForFileToVanish(desiredPath);
		}
		catch
		{
			return false; // this will be hit in the unlikely event that something else wrote to the backup path after we checked it was okay
		}
	}

	public static void WriteByteBuffer(this BinaryWriter bw, byte[]? data)
	{
		if (data == null)
		{
			bw.Write(0);
		}
		else
		{
			bw.Write(data.Length);
			bw.Write(data);
		}
	}
}
