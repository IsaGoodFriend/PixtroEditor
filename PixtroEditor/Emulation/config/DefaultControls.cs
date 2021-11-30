﻿using System.Collections.Generic;

namespace Pixtro.Emulation
{
	// Represents the defaults used in defctrl.json
	public class DefaultControls
	{
		public Dictionary<string, Dictionary<string, string>> AllTrollers { get; set; }
			= new Dictionary<string, Dictionary<string, string>>();

		public Dictionary<string, Dictionary<string, string>> AllTrollersAutoFire { get; set; }
			= new Dictionary<string, Dictionary<string, string>>();

		public Dictionary<string, Dictionary<string, AnalogBind>> AllTrollersAnalog { get; set; }
			= new Dictionary<string, Dictionary<string, AnalogBind>>();

		public Dictionary<string, Dictionary<string, FeedbackBind>> AllTrollersFeedbacks { get; set; }
			= new Dictionary<string, Dictionary<string, FeedbackBind>>();
	}
}
