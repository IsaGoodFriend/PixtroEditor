//#define BINARY

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.IO;

namespace Pixtro.Compiler {

	public class HeaderFile : IDisposable {

		public bool IsDirty { get; private set; }
		private string filePath;

		private StreamWriter fileWriter;
		private StringBuilder builder;
		private List<string> defines;

		public HeaderFile(string file) {

			SwitchFiles(file);
		}

		public void SetDirty() {

			if (IsDirty)
				return;

			fileWriter = new StreamWriter(filePath);
			IsDirty = true;

			fileWriter.WriteLine("#pragma once");

			fileWriter.Write(builder.ToString());
		}

		private void Write(string data) {
			if (IsDirty) {
				fileWriter.Write(data);
			}
			else {
				builder.Append(data);
			}
		}
		private void WriteLine(string data) {
			if (IsDirty) {
				fileWriter.WriteLine(data);
			}
			else {
				builder.AppendLine(data);
			}
		}
		private void WriteLine() {
			if (IsDirty) {
				fileWriter.WriteLine();
			}
			else {
				builder.AppendLine();
			}
		}

		public void AddArrayDefinition(string name, int size, SourceFile.ArrayType arrayType) {
			string valueType = "";
			string ptr = "";

			switch (arrayType) {
				case SourceFile.ArrayType.CharPtr:
					ptr = "*";
					goto case SourceFile.ArrayType.Char;
				case SourceFile.ArrayType.UShortPtr:
					ptr = "*";
					goto case SourceFile.ArrayType.UShort;
				case SourceFile.ArrayType.UIntPtr:
					ptr = "*";
					goto case SourceFile.ArrayType.UInt;

				case SourceFile.ArrayType.Char:
					valueType = "unsigned char";
					break;
				case SourceFile.ArrayType.Short:
					valueType = "short";
					break;
				case SourceFile.ArrayType.UShort:
					valueType = "unsigned short";
					break;
				case SourceFile.ArrayType.Int:
					valueType = "int";
					break;
				case SourceFile.ArrayType.UInt:
					valueType = "unsigned int";
					break;
			}
			WriteLine($"extern const {valueType}{ptr} {name}[{size}];");
		}
		public void AddValueDefine(string name, int value) {
			AddValueDefine(name, value.ToString());
		}
		public void AddValueDefine(string name, string value) {
			if (name == value)
				return;

			defines.Add($"#define {name}%{value}");
		}

		public void SwitchFiles(string file) {
			Dispose();

			builder = new StringBuilder();
			defines = new List<string>();

			filePath = file;
			builder.Clear();

			if (!File.Exists(file)) {
				SetDirty();
			}
		}
		public void Dispose() {
			if (IsDirty) {
				fileWriter.WriteLine();

				int longest = 0;
				foreach (var item in defines) {
					longest = Math.Max(item.Split('%')[0].Length, longest);
				}
				longest += 4 - (longest % 4);

				foreach (var item in defines) {
					string[] split = item.Split('%');

					int len = split[0].Length;

					while (len < longest) {
						split[0] += '\t';
						len += 4;
					}

					fileWriter.WriteLine($"{split[0]}{split[1]}");
				}

				fileWriter.Dispose();
			}

			IsDirty = false;
		}
	}
	public class SourceFile : IDisposable {
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
		public enum CompileOptions {
			None = 0,

			CompileEmptyArrays = 1,

			Compact = 2,

			AddDefine = 4
		}

		CompileOptions options = CompileOptions.AddDefine;

		public HeaderFile headerFile;

		public bool IsDirty { get; private set; }
		private string filePath;

		private bool inArray;
		private StreamWriter fileWriter;
		private StringBuilder builder;
		private List<string> arrayContents;
		private int arrayCount;

		private string arrayHeader;
		ArrayType arrayType;

		public SourceFile(string file, HeaderFile header, CompileOptions options) {

			filePath = file;
			headerFile = header;

			SwitchFiles(file, options);
		}

		public void AddValue(string value){
			if (long.TryParse(value, out long resultval)){
				AddValue(resultval);
			}

			arrayCount++;
			arrayContents.Add($"{value}, ");
		}
		public void AddValue(long value) {

			int length = 8;

			switch (arrayType) {
				case ArrayType.Char:
					length = 1;
					break;
				case ArrayType.Short:
				case ArrayType.UShort:
					length = 2;
					break;
				case ArrayType.UIntPtr:
				case ArrayType.UShortPtr:
				case ArrayType.CharPtr:
				case ArrayType.Int:
				case ArrayType.UInt:
					length = 4;
					break;
			}

			if (length < 8) {

				value &= ((long)1 << (length * 8)) - 1;
			}

			arrayCount++;
			arrayContents.Add($"0x{value.ToString("X" + (length * 2))},");
		}
		public void AddRange(long[] value) {

			foreach (var val in value)
				AddValue(val);
		}
		public void AddRange(ushort[] value) {

			foreach (var val in value)
				AddValue(val);
		}
		public void AddRange(uint[] value) {

			foreach (var val in value)
				AddValue(val);
		}
		public void AddRange(byte[] value) {

			foreach (var val in value)
				AddValue(val);
		}

		public void SetDirty() {

			headerFile.SetDirty();

			if (IsDirty)
				return;

			fileWriter = new StreamWriter(filePath);
			IsDirty = true;

			fileWriter.WriteLine("#pragma once");

			fileWriter.Write(builder.ToString());
		}

		private void Write(string data) {
			if (IsDirty) {
				fileWriter.Write(data);
			}
			else {
				builder.Append(data);
			}
		}
		private void WriteLine(string data) {
			if (IsDirty) {
				fileWriter.WriteLine(data);
			}
			else {
				builder.AppendLine(data);
			}
		}
		private void WriteLine() {
			if (IsDirty) {
				fileWriter.WriteLine();
			}
			else {
				builder.AppendLine();
			}
		}

		public void BeginArray(ArrayType type, string name) {
			if (inArray)
				throw new Exception();
			inArray = true;

			arrayCount = 0;
			arrayHeader = name;
			arrayType = type;
			arrayContents = new List<string>();

			
		}
		public void EndArray(bool hideFromHeader = false) {
			if (!inArray)
				return;

			string valueType = "";
			string ptr = "";

			switch (arrayType) {
				case SourceFile.ArrayType.CharPtr:
					ptr = "*";
					goto case SourceFile.ArrayType.Char;
				case SourceFile.ArrayType.UShortPtr:
					ptr = "*";
					goto case SourceFile.ArrayType.UShort;
				case SourceFile.ArrayType.UIntPtr:
					ptr = "*";
					goto case SourceFile.ArrayType.UInt;

				case SourceFile.ArrayType.Char:
					valueType = "unsigned char";
					break;
				case SourceFile.ArrayType.Short:
					valueType = "short";
					break;
				case SourceFile.ArrayType.UShort:
					valueType = "unsigned short";
					break;
				case SourceFile.ArrayType.Int:
					valueType = "int";
					break;
				case SourceFile.ArrayType.UInt:
					valueType = "unsigned int";
					break;
			}

			Write($"const {valueType}{ptr} {arrayHeader}[{arrayCount}] = {{");

			foreach (var item in arrayContents) {
				Write(item);
			}

			WriteLine("};");

			if (!hideFromHeader)
				headerFile.AddArrayDefinition(arrayHeader, arrayCount, arrayType);

			inArray = false;

			//if (arrayValues.Count == 0 && (options & CompileOptions.CompileEmptyArrays) == CompileOptions.None)
			//	return;

			//string end = (options & CompileOptions.Compact) == CompileOptions.Compact ? "" : "\n";
			//string valueType = "";

			//switch (arrayType) {
			//	case ArrayType.Char:
			//	case ArrayType.CharPtr:
			//		valueType = "unsigned char";
			//		break;
			//	case ArrayType.Int:
			//	case ArrayType.UInt:
			//	case ArrayType.UIntPtr:
			//		valueType = "int";
			//		break;
			//	case ArrayType.Short:
			//	case ArrayType.UShort:
			//	case ArrayType.UShortPtr:
			//		valueType = "short";
			//		break;
			//}

			//if (arrayType.ToString().StartsWith("U"))
			//	valueType = "unsigned " + valueType;
			//if (arrayType.ToString().EndsWith("Ptr")){
			//	valueType += "*";
			//}

			//source.Add($"const {valueType} {arrayHeader}[{arrayValues.Count}] = {{ {end}");

			//for (int i = 0; i < arrayValues.Count; ++i) {
				
			//	string addVal = arrayValues[i] is long ? CompileToString((long)arrayValues[i]) : (string)arrayValues[i];
				
			//	source.Add(addVal + ", " + ((i & 0xF) == 0xF ? $"{end}" : ""));

			//}
			//if (arrayValues.Count % 16 != 0)
			//	source.Add("\n");
			//source.Add($"}}; {end}");

			//header.Add($"extern const {valueType} {arrayHeader}[{arrayValues.Count}];\n");
		}

		public void SwitchFiles(string file, CompileOptions options) {
			Dispose();

			filePath = file;
			builder = new StringBuilder();

			this.options = options;

			if (!File.Exists(file)) {
				SetDirty();
			}
		}
		public void Dispose() {
			if (IsDirty) {
				fileWriter.Dispose();
			}

			IsDirty = false;
		}
	}
}