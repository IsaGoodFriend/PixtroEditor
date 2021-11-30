﻿namespace Pixtro.Emulation
{
	public interface IMemorySaveStateApi : IExternalApi
	{
		string SaveCoreStateToMemory();
		void LoadCoreStateFromMemory(string identifier);
		void DeleteState(string identifier);
		void ClearInMemoryStates();
	}
}
