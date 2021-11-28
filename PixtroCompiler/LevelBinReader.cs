using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Pixtro.Compiler {
	public class BinaryFileNode
	{
		public string Name;
		public Dictionary<string, object> Attributes = new Dictionary<string, object>();
		public List<BinaryFileNode> Children = new List<BinaryFileNode>();
		private List<string> compressedStrings = new List<string>();

		private uint boolBinary, boolIndex;

		public BinaryFileNode this[string name]
		{
			get
			{
				foreach (var node in Children)
				{
					if (node.Name == name)
						return node;
				}
				return null;
			}
		}

		public BinaryFileNode()
		{
			Name = "EmptyName!";
		}

		public BinaryFileNode GetChild(string _name)
		{
			foreach (var child in Children)
				if (child.Name == _name)
					return child;

			return null;
		}
		public BinaryFileNode AddChild(string _name)
		{
			var retval = new BinaryFileNode() { Name = _name };
			Children.Add(retval);
			return retval;
		}

		public string GetString(string _name)
		{
			var obj = Attributes[_name];

			return obj.ToString();
		}
		public int GetInteger(string _name)
		{
			var obj = Attributes[_name];

			switch (obj)
			{
				case int i:
					return i;
				case short i:
					return i;
				case byte i:
					return i;
				case long i:
					return (int)i;
				case uint i:
					return (int)i;
				case ushort i:
					return i;
				case sbyte i:
					return i;
				case ulong i:
					return (int)i;
				case float i:
					return (int)i;
				case double i:
					return (int)i;
			}

			throw new Exception();
		}
		public float GetFloat(string _name)
		{
			var obj = Attributes[_name];

			switch (obj)
			{
				case int i:
					return i;
				case short i:
					return i;
				case byte i:
					return i;
				case long i:
					return i;
				case uint i:
					return i;
				case ushort i:
					return i;
				case sbyte i:
					return i;
				case ulong i:
					return i;
				case float i:
					return i;
				case double i:
					return (float)i;
			}

			throw new Exception();
		}
		public long GetLong(string _name)
		{
			var obj = Attributes[_name];

			switch (obj)
			{
				case int i:
					return i;
				case short i:
					return i;
				case byte i:
					return i;
				case long i:
					return i;
				case uint i:
					return i;
				case ushort i:
					return i;
				case sbyte i:
					return i;
				case ulong i:
					return (long)i;
				case float i:
					return (long)i;
				case double i:
					return (long)i;
			}

			throw new Exception();
		}
		public double GetDouble(string _name)
		{
			var obj = Attributes[_name];

			switch (obj)
			{
				case int i:
					return i;
				case short i:
					return i;
				case byte i:
					return i;
				case long i:
					return i;
				case uint i:
					return i;
				case ushort i:
					return i;
				case sbyte i:
					return i;
				case ulong i:
					return i;
				case float i:
					return i;
				case double i:
					return i;
			}

			throw new Exception();
		}
		public bool[] GetBooleans()
		{

			List<bool> retval = new List<bool>();

			for (int i = 0; Attributes.ContainsKey($"booleans_{i}"); ++i)
			{
				uint value = (uint)Attributes[$"booleans_{i}"];
				for (int j = 0; j < 32; ++j)
				{
					retval.Add((value & 0x1) == 1);
					value >>= 1;
				}
			}

			return retval.ToArray();
		}
		public byte[] GetBytes(string _name)
		{
			return Attributes[_name] as byte[];
		}

		// Write Methods
		public void AddBoolean(bool _value)
		{
			int offset = (int)boolIndex & 0x1F;

			boolBinary |= (uint)((_value ? 1 : 0) << offset);
			Attributes[$"booleans_{boolIndex >> 5}"] = boolBinary;

			++boolIndex;
			if ((boolIndex & 0x1F) == 0)
			{
				boolBinary = 0;
			}
		}
		public void AddAttribute(string _name, object _value)
		{
			Attributes[_name] = _value;
			if (compressedStrings.Contains(_name))
				compressedStrings.Remove(_name);
		}
		public void AddCompressedString(string _name, string _value)
		{
			compressedStrings.Add(_name);
			Attributes[_name] = _value;
		}

		public override string ToString()
		{
			return Name;
		}
	}

	public class BinaryFileWriter : BinaryWriter
	{

		public const byte
			BOOLEAN = 0,
			BYTE = 1,
			SHORT = 2,
			INT = 3,
			FLOAT = 4,
			STRING = 5,
			LONG = 8,
			DOUBLE = 9,
			BYTE_ARRAY = 10,
			VAL_NULL = 255;

		public List<BinaryFileNode> Nodes;

		public BinaryFileNode this[string name]
		{
			get
			{
				foreach (var node in Nodes)
				{
					if (node.Name == name)
						return node;
				}
				return null;
			}
			set
			{
				BinaryFileNode node= null;

				foreach (var n in Nodes)
				{
					if (n.Name == name)
					{
						node = n;
						break;
					}
				}

				if (node != null)
					Nodes.Remove(node);
				
				Nodes.Add(value);
			}
		}

		public BinaryFileWriter() : base(new MemoryStream())
		{
			Nodes = new List<BinaryFileNode>();

			tempValues = BaseStream as MemoryStream;
			bodyWriter = new BinaryWriter(tempValues);
			stringLookup = new List<string>();
		}

		public enum StringType
		{
			LookUp,
			RunLength,
			Direct,
		}

		readonly BinaryWriter bodyWriter;
		readonly MemoryStream tempValues;

		readonly List<string> stringLookup;

		public override void Write(byte[] buffer)
		{
			bodyWriter.Write(buffer);
		}
		public override void Write(byte[] buffer, int index, int count)
		{
			bodyWriter.Write(buffer, index, count);
		}
		public override void Write(float value)
		{
			bodyWriter.Write(value);
		}
		public override void Write(bool value)
		{
			bodyWriter.Write(value);
		}
		public override void Write(byte value)
		{
			bodyWriter.Write(value);
		}
		public override void Write(char ch)
		{
			bodyWriter.Write(ch);
		}
		public override void Write(char[] chars)
		{
			bodyWriter.Write(chars);
		}
		public override void Write(char[] chars, int index, int count)
		{
			bodyWriter.Write(chars, index, count);
		}
		public override void Write(decimal value)
		{
			bodyWriter.Write(value);
		}
		public override void Write(double value)
		{
			bodyWriter.Write(value);
		}
		public override void Write(int value)
		{
			bodyWriter.Write(value);
		}
		public override void Write(long value)
		{
			bodyWriter.Write(value);
		}
		public override void Write(sbyte value)
		{
			bodyWriter.Write(value);
		}
		public override void Write(short value)
		{
			bodyWriter.Write(value);
		}
		public override void Write(uint value)
		{
			bodyWriter.Write(value);
		}
		public override void Write(ulong value)
		{
			bodyWriter.Write(value);
		}
		public override void Write(ushort value)
		{
			bodyWriter.Write(value);
		}

		public void WriteGeneric(object _obj)
		{
			switch (_obj)
			{
				default:
					if (_obj == null)
					{
						Write(VAL_NULL);
					}
					break;
				case bool value:
					Write(BOOLEAN);
					Write(value);

					break;
				case byte value:
					Write(BYTE);
					Write(value);

					break;
				case sbyte value:
					Write(BYTE);
					Write(value);

					break;
				case short value:
					Write(SHORT);
					Write(value);

					break;
				case ushort value:
					Write(SHORT);
					Write(value);

					break;
				case int value:
					Write(INT);
					Write(value);

					break;
				case uint value:
					Write(INT);
					Write(value);

					break;
				case float value:
					Write(FLOAT);
					Write(value);

					break;
				case string value:
					Write(STRING);
					Write(value);

					break;
				case long value:
					Write(LONG);
					Write(value);

					break;
				case ulong value:
					Write(LONG);
					Write(value);

					break;
				case double value:
					Write(DOUBLE);
					Write(value);

					break;
				case byte[] value:
					Write(BYTE_ARRAY);
					Write((uint)value.Length);
					foreach (var b in value)
						Write(b);

					break;
			}
		}
		public void WriteGenericString(string _value, StringType _type)
		{
			switch (_type)
			{
				case StringType.Direct:
					Write((byte)6);
					bodyWriter.Write(_value);
					break;
				case StringType.LookUp:
					Write((byte)5);
					Write(_value);
					break;
				case StringType.RunLength:
					Write((byte)7);


					char prev = _value[0];
					byte len = 255;

					List<byte> bytes = new List<byte>();

					for (int i = 0; i < _value.Length; ++i)
					{
						++len;
						if (prev != _value[i] || len == 255)
						{
							bytes.Add(len);
							bytes.Add((byte)prev);

							prev = _value[i];
							len = 0;
						}
					}
					if (len != 0)
					{
						bytes.Add(len);
						bytes.Add((byte)prev);
					}

					Write((short)bytes.Count);
					Write(bytes.ToArray());

					break;
			}
		}

		public override void Write(string value)
		{
			if (value == null)
				throw new Exception();

			if (!stringLookup.Contains(value))
				stringLookup.Add(value);

			bodyWriter.Write((ushort)stringLookup.IndexOf(value));
		}

		private long attrSeek = -1;
		private byte attrCount = 0;
		private void BeginAttributes()
		{
			if (attrSeek >= 0)
				throw new Exception();

			attrCount = 0;
			attrSeek = tempValues.Position;

			bodyWriter.Write((byte)0);
		}
		private void EndAttributes()
		{
			if (attrSeek == -1)
				throw new Exception();

			tempValues.Seek(attrSeek, SeekOrigin.Begin);

			bodyWriter.Write(attrCount);

			tempValues.Seek(0, SeekOrigin.End);

			attrSeek = -1;
		}
		public void WriteAttribute(string _name, object _value)
		{
			if (attrSeek == -1)
				throw new Exception();

			if (_value is long && ((long)_value) <= int.MaxValue && ((long)_value) >= int.MinValue)
			{
				_value = (int)(long)_value;
			}
			if (_value is double && ((double)_value) <= float.MaxValue && ((double)_value) >= float.MinValue)
			{
				_value = (float)(double)_value;
			}

			Write(_name);
			WriteGeneric(_value);

			++attrCount;
		}
		public void WriteAttribute(string _name, string _value, StringType _type)
		{
			if (attrSeek == -1)
				throw new Exception();

			Write(_name);
			WriteGenericString(_value, _type);

			++attrCount;
		}
		public BinaryFileNode AddNode(string _name)
		{
			var retval = new BinaryFileNode() { Name = _name };
			Nodes.Add(retval);
			return retval;
		}


		public void Save(string _filePath, string _header)
		{

			bodyWriter.Seek(0, SeekOrigin.Begin);
			Write((ushort)Nodes.Count);
			foreach (var node in Nodes)
				SaveNode(node);

			using (FileStream stream = File.Open(_filePath, FileMode.Create))
			{
				using (BinaryWriter writer = new BinaryWriter(stream))
				{
					writer.Write(_header);

					writer.Write((short)stringLookup.Count);

					foreach (string s in stringLookup)
					{
						writer.Write(s);
					}
					stream.Seek(0, SeekOrigin.End);

					tempValues.Seek(0, SeekOrigin.Begin);

					tempValues.CopyTo(stream);
				}

			}
		}

		private void SaveNode(BinaryFileNode _node)
		{
			Write(_node.Name);

			BeginAttributes();
			foreach (var attr in _node.Attributes)
			{
				WriteAttribute(attr.Key, attr.Value);
			}
			EndAttributes();

			Write((ushort)_node.Children.Count);
			foreach (var child in _node.Children)
				SaveNode(child);
		}
	}
	public class BinaryFileParser
	{

		string[ ] textLookup;
		public List<BinaryFileNode> Nodes;

		public BinaryFileNode this[string name]
		{
			get
			{
				foreach (var node in Nodes)
				{
					if (node.Name == name)
						return node;
				}
				return null;
			}
		}

		public string Header { get; private set; }

		public BinaryFileParser(string _path, string headerRequired = null)
		{
			BinaryReader reader = new BinaryReader(File.Open(_path, FileMode.Open));

			Header = reader.ReadString();

			if (headerRequired != null && Header != headerRequired)
			{
				reader.Dispose();
				throw new FileLoadException();
			}

			textLookup = new string[reader.ReadUInt16()];

			for (int i = 0; i < textLookup.Length; ++i)
			{
				textLookup[i] = reader.ReadString();
			}

			Nodes = new List<BinaryFileNode>(GetNodes(reader));

			reader.Dispose();
		}

		private IEnumerable<BinaryFileNode> GetNodes(BinaryReader reader)
		{
			int count = reader.ReadUInt16();

			for (int i = 0; i < count; ++i)
			{

				int v = reader.ReadUInt16();
				BinaryFileNode retval = new BinaryFileNode
				{
					Name = textLookup[v]
				};
				int attrCount = reader.ReadByte();

				for (int j = 0; j < attrCount; ++j)
				{
					var val = reader.ReadUInt16();
					var attrName = textLookup[val];

					byte b = reader.ReadByte();
					object value = null;

					switch (b)
					{
						case BinaryFileWriter.BOOLEAN:
							value = reader.ReadBoolean();
							break;
						case BinaryFileWriter.BYTE:
							value = reader.ReadByte();
							break;
						case BinaryFileWriter.SHORT:
							value = reader.ReadInt16();
							break;
						case BinaryFileWriter.INT:
							value = reader.ReadInt32();
							break;
						case BinaryFileWriter.FLOAT:
							value = reader.ReadSingle();
							break;
						case BinaryFileWriter.STRING:
							value = textLookup[reader.ReadInt16()];
							break;
						case 6:
							value = reader.ReadString();
							break;
						case 7:

							StringBuilder builder = new StringBuilder();
							short bytesCount = reader.ReadInt16();
							for (short ind = 0; ind < bytesCount; ind += 2)
							{
								byte repeatingCount = reader.ReadByte();
								char character = (char)reader.ReadByte(); // Direct cast
								builder.Append(character, repeatingCount);
							}
							value = builder.ToString();

							break;
						case BinaryFileWriter.LONG:
							value = reader.ReadInt64();
							break;
						case BinaryFileWriter.DOUBLE:
							value = reader.ReadDouble();
							break;
						case BinaryFileWriter.BYTE_ARRAY:
						{
							uint length = reader.ReadUInt32();
							var array = new byte[length];
							for (uint idx = 0; idx < length; ++idx)
							{
								array[idx] = reader.ReadByte();
							}
							value = array;
							break;
						}
					}
					retval.Attributes.Add(attrName, value);
				}

				retval.Children.AddRange(GetNodes(reader));


				yield return retval;
			}

			yield break;
		}

	}
}