using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Pixtro.Emulation
{
	[Description("A library for interacting with the currently loaded emulator core")]
	public sealed class EmulationApi : IEmulationApi
	{
		[RequiredService]
		private IEmulator Emulator { get; set; }

		[OptionalService]
		private IBoardInfo BoardInfo { get; set; }

		[OptionalService]
		private IDebuggable DebuggableCore { get; set; }

		[OptionalService]
		private IDisassemblable DisassemblableCore { get; set; }

		[OptionalService]
		private IInputPollable InputPollableCore { get; set; }

		[OptionalService]
		private IMemoryDomains MemoryDomains { get; set; }

		//private readonly Config _config;

		private readonly IGameInfo _game;

		private readonly Action<string> LogCallback;


		public Action FrameAdvanceCallback { get; set; }

		public bool ForbiddenConfigReferenceUsed { get; private set; }

		public Action YieldCallback { get; set; }

		public EmulationApi(Action<string> logCallback, IGameInfo game)
		{
			//_config = config;
			_game = game;
			LogCallback = logCallback;
		}

		public void FrameAdvance() => FrameAdvanceCallback();

		public int FrameCount() => Emulator.Frame;

		public object Disassemble(uint pc, string name = "")
		{
			try
			{
				if (DisassemblableCore != null)
				{
					return new {
						disasm = DisassemblableCore.Disassemble(
							string.IsNullOrEmpty(name) ? MemoryDomains.SystemBus : MemoryDomains[name],
							pc,
							out var l
						),
						length = l
					};
				}
			}
			catch (NotImplementedException) {}
			LogCallback($"Error: Emulation does not yet implement {nameof(IDisassemblable.Disassemble)}()");
			return null;
		}

		public ulong? GetRegister(string name)
		{
			try
			{
				if (DebuggableCore != null)
				{
					var registers = DebuggableCore.GetCpuFlagsAndRegisters();
					return registers.ContainsKey(name) ? registers[name].Value : (ulong?) null;
				}
			}
			catch (NotImplementedException) {}
			LogCallback($"Error: Emulation does not yet implement {nameof(IDebuggable.GetCpuFlagsAndRegisters)}()");
			return null;
		}

		public Dictionary<string, ulong> GetRegisters()
		{
			try
			{
				if (DebuggableCore != null)
				{
					var table = new Dictionary<string, ulong>();
					foreach (var kvp in DebuggableCore.GetCpuFlagsAndRegisters()) table[kvp.Key] = kvp.Value.Value;
					return table;
				}
			}
			catch (NotImplementedException) {}
			LogCallback($"Error: Emulation does not yet implement {nameof(IDebuggable.GetCpuFlagsAndRegisters)}()");
			return new Dictionary<string, ulong>();
		}

		public void SetRegister(string register, int value)
		{
			try
			{
				if (DebuggableCore != null)
				{
					DebuggableCore.SetCpuRegister(register, value);
					return;
				}
			}
			catch (NotImplementedException) {}
			LogCallback($"Error: Emulation does not yet implement {nameof(IDebuggable.SetCpuRegister)}()");
		}

		public long TotalExecutedCycles()
		{
			try
			{
				if (DebuggableCore != null) return DebuggableCore.TotalExecutedCycles;
			}
			catch (NotImplementedException) {}
			LogCallback($"Error: Emulation does not yet implement {nameof(IDebuggable.TotalExecutedCycles)}()");
			return default;
		}

		public string GetSystemId() => _game.System;

		public bool IsLagged()
		{
			if (InputPollableCore != null) return InputPollableCore.IsLagFrame;
			LogCallback($"Can not get lag information, Emulation does not implement {nameof(IInputPollable)}");
			return false;
		}

		public void SetIsLagged(bool value = true)
		{
			if (InputPollableCore != null) InputPollableCore.IsLagFrame = value;
			else LogCallback($"Can not set lag information, Emulation does not implement {nameof(IInputPollable)}");
		}

		public int LagCount()
		{
			if (InputPollableCore != null) return InputPollableCore.LagCount;
			LogCallback($"Can not get lag information, Emulation does not implement {nameof(IInputPollable)}");
			return default;
		}

		public void SetLagCount(int count)
		{
			if (InputPollableCore != null) InputPollableCore.LagCount = count;
			else LogCallback($"Can not set lag information, Emulation does not implement {nameof(IInputPollable)}");
		}

		public void LimitFramerate(bool enabled) { }

		public void MinimizeFrameskip(bool enabled) { }

		public void Yield() => YieldCallback();

		public string GetDisplayType() => "";

		public string GetBoardName() => BoardInfo?.BoardName ?? "";

		public object GetSettings() => Emulator switch
		{
			_ => null
		};

		public PutSettingsDirtyBits PutSettings(object settings) => Emulator switch
		{
			_ => PutSettingsDirtyBits.None
		};

		public void SetRenderPlanes(params bool[] args)
		{
		}

		public void DisplayVsync(bool enabled) { }
	}
}
