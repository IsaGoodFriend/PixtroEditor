using System.Collections.Generic;

namespace Pixtro.Emulation
{
	public interface IInputApi : IExternalApi
	{
		Dictionary<string, bool> Get();
		Dictionary<string, object> GetMouse();
	}
}
