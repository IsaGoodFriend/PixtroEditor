using Pixtro.Emulation;
using System.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Pixtro.Emulation
{
	public sealed class GameCommunicator
	{
		private struct EmulatorRange : Range<long> {
			public EmulatorRange(long start, long length) {
				Start = start;
				EndInclusive = start + length - 1;
			}
			public long Start { get; set; }

			public long EndInclusive { get; set; }
		}
		private const int 
			EWRam_Address = 2,
			IWRam_Address = 3,
			IORam_Address = 4,
			PalRam_Address = 5,
			VRam_Address = 6,
			OAM_Address = 7,
			ROM_Address = 8,
			SRam_Address = 0xE;
		private const bool BigEndian = false;

		public sealed class RomMapping
		{
			public int Address { get; private set; }
			public int Size { get; private set; }

			public RomMapping(int addr, int size)
			{
				Address = addr;
				Size = size;
			}
		}
		private class DontHotloadAttribute : Attribute
		{

		}
		public sealed class MemoryMap
		{
			public MemoryDomain domain;
			public int domainIndex { get; private set; }
			public int address { get; private set; }
			public int size { get; private set; }

			private byte[] state;

			public byte this[int index]
			{
				get
				{
					return GetByte(index);
				}
				set
				{
					SetByte(index, value);
				}
			}

			public byte GetByte(int index)
			{
				if (index > size || index < 0)
					throw new IndexOutOfRangeException();

				return domain.PeekByte(index + address);
			}
			public void SetByte(int index, byte val)
			{
				if (index > size || index < 0)
					throw new IndexOutOfRangeException();

				domain.PokeByte(index + address, val);
			}
			public byte[] GetByteArray(int index, int length) {
				if (index > size || index < 0)
					throw new IndexOutOfRangeException();

				byte[] retval = new byte[length];

				domain.BulkPeekByte(new EmulatorRange(index, length), retval);

				return retval;
			}
			public ushort GetUshort(int index)
			{
				if (index > size || index < 0)
					throw new IndexOutOfRangeException();

				return domain.PeekUshort(index + address, BigEndian);
			}
			public void SetUshort(int index, ushort val)
			{
				if (index > size || index < 0)
					throw new IndexOutOfRangeException();

				domain.PokeUshort(index + address, val, BigEndian);
			}
			public short GetShort(int index) {
				if (index > size || index < 0)
					throw new IndexOutOfRangeException();

				return (short)domain.PeekUshort(index + address, BigEndian);
			}
			public void SetShort(int index, short val) {
				if (index > size || index < 0)
					throw new IndexOutOfRangeException();

				domain.PokeUshort(index + address, (ushort)val, BigEndian);
			}
			public uint GetUint(int index)
			{
				if (index > size || index < 0)
					throw new IndexOutOfRangeException();

				return domain.PeekUint(index + address, BigEndian);
			}
			public void SetUint(int index, uint val)
			{
				if (index > size || index < 0)
					throw new IndexOutOfRangeException();

				domain.PokeUint(index + address, val, BigEndian);
			}
			public int GetInt(int index) {
				if (index > size || index < 0)
					throw new IndexOutOfRangeException();

				return (int)domain.PeekUint(index + address, BigEndian);
			}
			public void SetInt(int index, int val) {
				if (index > size || index < 0)
					throw new IndexOutOfRangeException();

				domain.PokeUint(index + address, (uint)val, BigEndian);
			}

			public bool GetFlag(int flag, int offset = 0)
			{
				uint val = GetUint(offset);

				return (val & (1 << flag)) > 0;
			}
			public bool GetFlag(Enum value, int offset = 0)
			{
				int parsed = Convert.ToInt32(value);

				for (int i = 0; i < 32; ++i)
				{
					if ((parsed & 0x1) > 0)
					{
						if (parsed == 1)
						{
							return GetFlag(i, offset);
						}

						throw new ArgumentException();
					}
					parsed >>= 1;
				}
				throw new Exception();
			}
			public void SetFlag(int flag, bool value, int offset = 0)
			{
				uint val = GetUint(offset);
				val &= (uint)~(1 << flag);
				if (value)
					val |= (uint)(1 << flag);

				SetUint(offset, val);
			}
			public void SetFlag(Enum flag, bool value, int offset = 0)
			{
				int parsed = Convert.ToInt32(flag);

				for (int i = 0; i < 32; ++i)
				{
					if ((parsed & 0x1) > 0)
					{
						if (parsed == 1)
						{
							SetFlag(i, value, offset);
							return;
						}

						throw new ArgumentException();
					}
					parsed >>= 1;
				}
				throw new Exception();
			}
			public void EnableFlags(Enum values, int offset = 0)
			{
				uint parsedValues = Convert.ToUInt32(values);

				for (int i = 0; i < 32; ++i)
				{
					if ((parsedValues & 1) != 0)
						SetFlag(i, true, offset);

					parsedValues >>= 1;
				}

			}
			public void DisableFlags(Enum values, int offset = 0)
			{
				uint parsedValues = Convert.ToUInt32(values);

				for (int i = 0; i < 32; ++i)
				{
					if ((parsedValues & 1) != 0)
						SetFlag(i, false, offset);

					parsedValues >>= 1;
				}


			}

			public void SaveState()
			{
				state = GetState();
			}
			public byte[] LoadState()
			{
				return state.ToArray(); // Just so you can't dig in and mess with the values
			}
			public byte[] GetState()
			{
				byte[] retval = new byte[size];

				for (int i = 0; i < size; ++i)
				{
					retval[i] = GetByte(i);
				}

				return retval;
			}
			public void SetState(byte[] bytes)
			{
				for (int i = 0; i < size; ++i)
				{
					SetByte(i, bytes[i]);
				}
			}

			public MemoryMap(int _domainIndex, int _addr, int _size)
			{
				domainIndex = _domainIndex;
				address = _addr;
				size = Math.Max(_size, 1);

			}

			public static implicit operator uint(MemoryMap map) => map.GetUint(0);
		}
		public static GameCommunicator Instance { get; private set; }

		[RequiredService]
		private IMemoryDomains MemoryDomains { get; set; }

		private MemoryDomain IWRam, EWRam, IORam, PalRam, VRam, OAM, ROM, SRam;

		public IReadOnlyDictionary<string, RomMapping> RomMap => romMap;

		#region Memory Mapping

		public MemoryMap debug_engine_flags { get; private set; }
		public MemoryMap debug_game_flags { get; private set; }
		public MemoryMap entities { get; private set; }
		public MemoryMap loaded_levels_a { get; private set; }
		public MemoryMap loaded_levels_b { get; private set; }
		public MemoryMap current_level_index { get; private set; }

		[DontHotload]
		public MemoryMap LevelRegion { get; private set; }
		[DontHotload]
		public MemoryMap Palettes { get; private set; }
		[DontHotload]
		public MemoryMap TileData { get; private set; }
		[DontHotload]
		public MemoryMap SpriteData { get; private set; }

		private MemoryMap[] maps;

		#endregion

		private Dictionary<string, RomMapping> romMap = new Dictionary<string, RomMapping>();

		public GameCommunicator(StreamReader memoryMap)
		{
			Instance = this;

			while (!memoryMap.ReadLine().StartsWith("Allocating common symbols")) ;

			string currentLine = memoryMap.ReadLine().Trim();

			List<MemoryMap> mapList = new List<MemoryMap>();

			do
			{

				while (!currentLine.StartsWith(".bss.completed") && !currentLine.StartsWith("*(.rodata)")) {
					currentLine = memoryMap.ReadLine().Trim();

					if (memoryMap.EndOfStream)
						break;
				}

				if (memoryMap.EndOfStream)
					break;

				do {
					currentLine = memoryMap.ReadLine().Trim();

					string[] split = currentLine.Split(new char[]{ ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
					if (split.Length == 1 || split[1].StartsWith("0x"))
						continue;

					int domain = int.Parse(split[0].Substring(11, 1));
					int parsed = Convert.ToInt32(split[0].Substring(12, 6), 16);

					switch (split[0][11]) {
						case '8':
							string name = split[1];

							try {
								int otherAddr = Convert.ToInt32(currentLine.Substring(12, 6), 16);

								romMap.Add(name, new RomMapping(parsed, otherAddr - parsed));
							}
							catch { }
							continue;
					}


					var propertyInfo = GetType().GetProperty(split[1], BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
					if (propertyInfo != null && propertyInfo.CustomAttributes.Where((item) => item.AttributeType == typeof(DontHotloadAttribute)).Count() != 0) {
						propertyInfo = null;
					}

					if (propertyInfo != null) {
						split = currentLine.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

						int otherAddr = Convert.ToInt32(split[0].Substring(12, 6), 16);

						var mapped = new MemoryMap(domain, parsed, otherAddr - parsed);

						mapList.Add(mapped);

						propertyInfo.GetSetMethod(true).Invoke(this, new object[] { mapped });

					}

				}
				while (!currentLine.StartsWith("*") || currentLine.StartsWith("*fill*"));

			} while (!memoryMap.EndOfStream);

			mapList.Add(LevelRegion = new MemoryMap(EWRam_Address, 0x20000, 0x10000));
			mapList.Add(Palettes = new MemoryMap(PalRam_Address, 0, 0x400));
			mapList.Add(TileData = new MemoryMap(VRam_Address, 0, 0x20000));
			mapList.Add(SpriteData = new MemoryMap(OAM_Address, 0, 0x400));

			maps = mapList.ToArray();
		}
		public GameCommunicator(StreamReader memoryMap, GameCommunicator previousState)
			:this(memoryMap)
		{
			GetStateFrom(previousState);
		}

		public void SaveState()
		{
			foreach (var map in maps)
			{
				map.SaveState();
			}
		}
		public void GetStateFrom(GameCommunicator other)
		{
			for (int i = 0; i < maps.Length; ++i)
			{
				if (maps[i].domainIndex == ROM_Address || maps[i].domainIndex == SRam_Address)
					continue;

				maps[i].SetState(other.maps[i].GetState());
			}
		}

		public MemoryMap CreateMemoryMap(int region, uint address, int size)
		{
			var retval = new MemoryMap(region, (int)address, size);

			SetDomain(retval);

			return retval;
		}
		public MemoryMap GetLevelMapping(bool regionA, int levelIndex)
		{
			var mapping = regionA ? loaded_levels_a : loaded_levels_b;
			uint address = mapping.GetUint(levelIndex);

			var tempMap = CreateMemoryMap(3, address, 0x500);

			int width = tempMap.GetUshort(0), height = tempMap.GetUshort(1);


			tempMap = CreateMemoryMap(3, address + (3 * 2) + (2), width * height * 2);

			return tempMap;
		}
		public MemoryMap GetTilePalette(int palette)
		{
			return CreateMemoryMap(5, (uint)(palette * 32), 32);
		}
		public MemoryMap GetSpritePalette(int palette)
		{
			return CreateMemoryMap(5, (uint)(palette * 32) + 256, 32);
		}

		public void RomLoaded()
		{
			IWRam = MemoryDomains["IWRAM"];
			EWRam = MemoryDomains["EWRAM"];
			IORam = MemoryDomains["IORAM"];
			PalRam = MemoryDomains["PALRAM"];
			VRam = MemoryDomains["VRAM"];
			OAM = MemoryDomains["OAM"];
			ROM = MemoryDomains["ROM"];
			SRam = MemoryDomains["SRAM"];
			
			foreach (var map in maps)
			{
				SetDomain(map);
			}
		}

		private void SetDomain(MemoryMap map)
		{
			switch (map.domainIndex)
			{
				case EWRam_Address:
					map.domain = EWRam;
					break;
				case IWRam_Address:
					map.domain = IWRam;
					break;
				case IORam_Address:
					map.domain = IORam;
					break;
				case PalRam_Address:
					map.domain = PalRam;
					break;
				case VRam_Address:
					map.domain = VRam;
					break;
				case OAM_Address:
					map.domain = OAM;
					break;
				case ROM_Address:
					map.domain = ROM;
					break;
				case SRam_Address:
					map.domain = SRam;
					break;
			}
		}
	}
}
