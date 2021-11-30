using System;
using System.Collections.Generic;

namespace Pixtro.Emulation
{
	public interface IToolApi : IExternalApi
	{
		IEnumerable<Type> AvailableTools { get; }

		object CreateInstance(string name);

		void OpenCheats();

		void OpenHexEditor();

		void OpenRamSearch();

		void OpenRamWatch();

		void OpenTasStudio();

		void OpenToolBox();

		void OpenTraceLogger();
	}
}
