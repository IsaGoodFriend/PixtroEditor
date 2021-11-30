using System.Collections.Generic;

namespace Pixtro.Emulation
{
	public interface IGameInfoApi : IExternalApi
	{
		string GetRomName();
		string GetRomHash();
		bool InDatabase();
		string GetStatus();
		bool IsStatusBad();
		string GetBoardType();
		Dictionary<string, string> GetOptions();
	}
}
