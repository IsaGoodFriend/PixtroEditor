﻿using System.Collections.Generic;

namespace Pixtro.Emulation
{
	public interface IMovieApi : IExternalApi
	{
		bool StartsFromSavestate();
		bool StartsFromSaveram();
		string Filename();
		IDictionary<string, object> GetInput(int frame, int? controller = null);
		string GetInputAsMnemonic(int frame);
		bool GetReadOnly();
		ulong GetRerecordCount();
		bool GetRerecordCounting();
		bool IsLoaded();
		int Length();
		string Mode();
		void Save(string filename = "");
		void SetReadOnly(bool readOnly);
		void SetRerecordCount(ulong count);
		void SetRerecordCounting(bool counting);
		void Stop();
		double GetFps();
		Dictionary<string, string> GetHeader();
		List<string> GetComments();
		List<string> GetSubtitles();
	}
}
