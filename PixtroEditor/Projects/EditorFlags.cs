using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pixtro.Emulation
{
	[Flags]
	public enum EditorToGameFlags : uint
	{
		PauseUpdates = 0x1,
	}
	[Flags]
	public enum GameToEditorFlags : uint
	{
		PrintLevelData = 0x1,
	}
}
