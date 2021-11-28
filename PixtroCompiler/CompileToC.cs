//#define BINARY

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Pixtro.Compiler {

	public class CompileToC {
		public enum ArrayType {
			Char,
			UShort,
			Short,
			UInt,
			Int,
			CharPtr,
			UShortPtr,
			UIntPtr,
		}
#if BINARY
		public enum CompileOptions
		{
			None = 0,

			CompileEmptyArrays = 1,

			Pretty = 0,
			Compact = 2,

			AddDefine = 4
		}

		public CompileOptions options = CompileOptions.AddDefine;

		private bool inArray;
		private List<string> source, header;

		private List<long> arrayValues;
		private string arrayHeader;
		ArrayType arrayType;

		private Dictionary<string, byte[]> binaryFiles;

		public int ArrayLength { get { return arrayValues.Count; } }

		public CompileToC()
		{
			source = new List<string>();
			header = new List<string>();
			arrayValues = new List<long>();

			binaryFiles = new Dictionary<string, byte[]>();
		}

		public void AddValue(long _value)
		{
			if (!inArray)
				throw new Exception();

			arrayValues.Add(_value);
		}
		public void AddRange(long[] _value)
		{
			if (!inArray)
				throw new Exception();

			arrayValues.AddRange(_value);
		}
		public void AddRange(ushort[] _value)
		{
			if (!inArray)
				throw new Exception();

			arrayValues.AddRange(_value.Select(x => (long)x).ToArray());
		}
		public void AddRange(uint[] _value)
		{
			if (!inArray)
				throw new Exception();

			arrayValues.AddRange(_value.Select(x => (long)x).ToArray());
		}

		public void AddValueDefine(string _name, int _value)
		{
			AddValueDefine(_name, _value.ToString());
		}
		public void AddValueDefine(string _name, string _value)
		{
			string def = $"#define {_name}";

			int len = def.Length;

			while (len < 40)
			{
				def += '\t';
				len = (len & 0xFFFC) + 4;
			}
			def += $"{_value}\n";

			header.Add(def);
		}

		public void BeginArray(ArrayType _type, string _name)
		{
			if (inArray)
				throw new Exception();
			inArray = true;

			arrayValues.Clear();

			arrayHeader = _name;
			arrayType = _type;
		}
		public void EndArray()
		{
			if (!inArray)
				throw new Exception();

			inArray = false;

			if (arrayValues.Count == 0 && (options & CompileOptions.CompileEmptyArrays) == CompileOptions.None)
				return;

			string end = (options & CompileOptions.Compact) == CompileOptions.Compact ? "" : "\n";
			string valueType = "";

			switch (arrayType)
			{
				case ArrayType.Char:
					valueType = "unsigned char";
					break;
				case ArrayType.Int:
				case ArrayType.UInt:
					valueType = "int";
					break;
				case ArrayType.Short:
				case ArrayType.UShort:
					valueType = "short";
					break;
			}
			if (arrayType.ToString().StartsWith("U"))
				valueType = "unsigned " + valueType;

			source.Add($"const {valueType} {arrayHeader}[{arrayValues.Count}] = {{ {end}");

			List<byte> binFile = new List<byte>();

			for (int i = 0; i < arrayValues.Count; ++i)
			{
				source.Add(CompileToString(arrayValues[i]) + ", " + ((i & 0xF) == 0xF ? $"{end}" : ""));
				AddToBin(arrayValues[i], binFile);
			}

			binaryFiles.Add(arrayHeader, binFile.ToArray());

			source.Add($"}}; {end}");

			header.Add($"extern const {valueType} {arrayHeader}[{arrayValues.Count}];\n");
		}

		public void SaveTo(string _path, string _name)
		{
			foreach (var path in binaryFiles.Keys)
			{
				var exportTo = Path.Combine(_path, path) + "";
				File.WriteAllBytes(exportTo, binaryFiles[path]);
			}
		}

		private string CompileToString(long obj)
		{
			bool neg = obj < 0 && (arrayType == ArrayType.Short || arrayType == ArrayType.Int);
			obj = Math.Abs(obj);

			string retval = "";

			switch (arrayType)
			{
				case ArrayType.Char:
					obj = (byte)obj;
					retval = obj.ToString("X2");
					break;
				case ArrayType.Short:
					obj = (short)obj & 0x7FFF;
					retval = obj.ToString("X4");
					break;
				case ArrayType.UShort:
					obj = (ushort)obj & 0xFFFF;
					retval = obj.ToString("X4");
					break;
				case ArrayType.Int:
					obj = (int)obj & 0x7FFFFFFF;
					retval = obj.ToString("X8");
					break;
				case ArrayType.UInt:
					obj = (uint)obj & 0xFFFFFFFF;
					retval = obj.ToString("X8");
					break;
			}

			return (neg ? "-" : "") + $"0x{retval}";
		}
		private void AddToBin(long obj, List<byte> bin)
		{
			int neg = Math.Sign(obj);

			obj = Math.Abs(obj);

			switch (arrayType)
			{
				case ArrayType.Char:
					bin.Add((byte)obj);
					break;
				case ArrayType.Short:
					bin.AddRange(BitConverter.GetBytes((short)((obj & 0x7FFF) * neg)));
					break;
				case ArrayType.UShort:
					bin.AddRange(BitConverter.GetBytes((ushort)(obj & 0xFFFF)));
					break;
				case ArrayType.Int:
					bin.AddRange(BitConverter.GetBytes((int)((obj & 0x7FFFFFFF) * neg)));
					break;
				case ArrayType.UInt:
					bin.AddRange(BitConverter.GetBytes((uint)(obj & 0xFFFFFFFF)));
					break;
			}

		}


#else
		public enum CompileOptions {
			None = 0,

			CompileEmptyArrays = 1,

			Pretty = 0,
			Compact = 2,

			AddDefine = 4
		}

		public CompileOptions options = CompileOptions.AddDefine;

		private bool inArray;
		private List<string> source, header;

		private List<object> arrayValues;
		private string arrayHeader;
		ArrayType arrayType;

		public int ArrayLength { get { return arrayValues.Count; } }

		public CompileToC() {
			source = new List<string>();
			header = new List<string>();
			arrayValues = new List<object>();
		}

		private void CheckForErrors(bool addInteger){
			if (!inArray)
				throw new Exception();
		}

		public void AddValue(string _value){
			if (long.TryParse(_value, out long resultval)){
				AddValue(_value);
			}
			CheckForErrors(false);

			arrayValues.Add(_value);
		}
		public void AddValue(long _value) {
			CheckForErrors(true);

			arrayValues.Add(_value);
		}
		public void AddRange(long[] _value) {
			CheckForErrors(true);

			foreach (var val in _value)
				arrayValues.Add(val);
		}
		public void AddRange(ushort[] _value) {
			CheckForErrors(true);

			foreach (var val in _value)
				arrayValues.Add((long)val);
		}
		public void AddRange(uint[] _value) {
			CheckForErrors(true);

			foreach (var val in _value)
				arrayValues.Add((long)val);
		}
		public void AddRange(byte[] _value) {
			CheckForErrors(true);

			foreach (var val in _value)
				arrayValues.Add((long)val);
		}

		public void AddValueDefine(string _name, int _value) {
			AddValueDefine(_name, _value.ToString());
		}
		public void AddValueDefine(string _name, string _value) {
			if (_name == _value)
				return;

			string def = $"#define {_name}";

			int len = def.Length;

			while (len < 40) {
				def += '\t';
				len = (len & 0xFFFC) + 4;
			}
			def += $"{_value}\n";

			header.Add(def);
		}

		public void BeginArray(ArrayType _type, string _name) {
			if (inArray)
				throw new Exception();
			inArray = true;

			arrayValues.Clear();

			arrayHeader = _name;
			arrayType = _type;
		}
		public void EndArray() {
			if (!inArray)
				throw new Exception();
			inArray = false;

			if (arrayValues.Count == 0 && (options & CompileOptions.CompileEmptyArrays) == CompileOptions.None)
				return;

			string end = (options & CompileOptions.Compact) == CompileOptions.Compact ? "" : "\n";
			string valueType = "";

			switch (arrayType) {
				case ArrayType.Char:
				case ArrayType.CharPtr:
					valueType = "unsigned char";
					break;
				case ArrayType.Int:
				case ArrayType.UInt:
				case ArrayType.UIntPtr:
					valueType = "int";
					break;
				case ArrayType.Short:
				case ArrayType.UShort:
				case ArrayType.UShortPtr:
					valueType = "short";
					break;
			}

			if (arrayType.ToString().StartsWith("U"))
				valueType = "unsigned " + valueType;
			if (arrayType.ToString().EndsWith("Ptr")){
				valueType += "*";
			}

			source.Add($"const {valueType} {arrayHeader}[{arrayValues.Count}] = {{ {end}");

			for (int i = 0; i < arrayValues.Count; ++i) {
				
				string addVal = arrayValues[i] is long ? CompileToString((long)arrayValues[i]) : (string)arrayValues[i];
				
				source.Add(addVal + ", " + ((i & 0xF) == 0xF ? $"{end}" : ""));

			}
			if (arrayValues.Count % 16 != 0)
				source.Add("\n");
			source.Add($"}}; {end}");

			header.Add($"extern const {valueType} {arrayHeader}[{arrayValues.Count}];\n");
		}

		public void SaveTo(string _path, string _name) {
			using (var sw = new StreamWriter(File.Open($"{_path}\\{_name}.c", FileMode.Create))) {
				sw.WriteLine($"#include \"{Path.GetFileName(_name)}.h\" ");
				foreach (var str in source) {
					sw.Write(str);
				}
			}
			using (var sw = new StreamWriter(File.Open($"{_path}\\{_name}.h", FileMode.Create))) {
				if ((options & CompileOptions.AddDefine) != CompileOptions.None) {
					sw.WriteLine($"#pragma once");
				}

				foreach (var str in header) {
					sw.Write(str);
				}
			}
		}

		private string CompileToString(long obj) {
			bool neg = obj < 0 && (arrayType == ArrayType.Short || arrayType == ArrayType.Int);
			obj = Math.Abs(obj);

			string retval = "";

			switch (arrayType) {
				case ArrayType.Char:
					obj = (char)obj;
					retval = obj.ToString("X2");
					break;
				case ArrayType.Short:
					obj = (short)obj & 0x7FFF;
					retval = (obj).ToString("X4");
					break;
				case ArrayType.UShort:
					obj = (ushort)obj & 0xFFFF;
					retval = (obj).ToString("X4");
					break;
				case ArrayType.Int:
					obj = (int)obj & 0x7FFFFFFF;
					retval = (obj).ToString("X8");
					break;
				case ArrayType.UInt:
					obj = (uint)obj & 0xFFFFFFFF;
					retval = (obj).ToString("X8");
					break;
			}

			return (neg ? "-" : "") + $"0x{retval}";
		}
#endif
	}
}