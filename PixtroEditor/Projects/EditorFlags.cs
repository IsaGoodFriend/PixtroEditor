using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pixtro.Editor
{
	[Flags]
	public enum BaseDebugFlags : uint
	{
		Ready = 0x1,
		PauseUpdates = 0x2,
	}
	[Flags]
	public enum GameDebugFlags : uint
	{
		Waiting = 0x1,
	}
}
