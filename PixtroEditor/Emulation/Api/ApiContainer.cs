#nullable enable

using System;
using System.Collections.Generic;

namespace Pixtro.Emulation
{
	public sealed class ApiContainer : IDisposable
	{
		public readonly IReadOnlyDictionary<Type, IExternalApi> Libraries;

		public IEmuClientApi EmuClient => (IEmuClientApi) Libraries[typeof(IEmuClientApi)];
		public IEmulationApi Emulation => (IEmulationApi) Libraries[typeof(IEmulationApi)];
		public IGameInfoApi GameInfo => (IGameInfoApi) Libraries[typeof(IGameInfoApi)];
		public IGuiApi Gui => (IGuiApi) Libraries[typeof(IGuiApi)];
		public IInputApi Input => (IInputApi) Libraries[typeof(IInputApi)];
		public IJoypadApi Joypad => (IJoypadApi) Libraries[typeof(IJoypadApi)];
		public IMemoryApi Memory => (IMemoryApi) Libraries[typeof(IMemoryApi)];
		public IMemoryEventsApi MemoryEvents => (IMemoryEventsApi) Libraries[typeof(IMemoryEventsApi)];
		public IMemorySaveStateApi MemorySaveState => (IMemorySaveStateApi) Libraries[typeof(IMemorySaveStateApi)];
		public IMovieApi Movie => (IMovieApi) Libraries[typeof(IMovieApi)];
		public ISaveStateApi SaveState => (ISaveStateApi) Libraries[typeof(ISaveStateApi)];
		public IUserDataApi UserData => (IUserDataApi) Libraries[typeof(IUserDataApi)];
		public IToolApi Tool => (IToolApi) Libraries[typeof(IToolApi)];

		public ApiContainer(IReadOnlyDictionary<Type, IExternalApi> libs) => Libraries = libs;

		public void Dispose()
		{
			foreach (var lib in Libraries.Values) if (lib is IDisposable disposableLib) disposableLib.Dispose();
		}
	}
}
