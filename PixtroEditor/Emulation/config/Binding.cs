﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;

// TODO [LARP] - It's pointless and annoying to store such a big structure filled with static information
// use this instead
// public class UserBinding
// {
//  public string DisplayName;
//  public string Bindings;
// }
// ...also. We should consider using something other than DisplayName for keying, maybe make a KEYNAME distinct from displayname.
// displayname is OK for now though.
namespace Pixtro.Emulation
{
	public class Binding
	{
		public string DisplayName { get; set; }
		public string Bindings { get; set; }
		public string DefaultBinding { get; set; }
		public string TabGroup { get; set; }
		public string ToolTip { get; set; }
		public int Ordinal { get; set; }
	}

	[Newtonsoft.Json.JsonObject]
	public class BindingCollection : IEnumerable<Binding>
	{
		public List<Binding> Bindings { get; }

		[Newtonsoft.Json.JsonConstructor]
		public BindingCollection(List<Binding> bindings)
		{
			Bindings = bindings;
		}

		public BindingCollection()
		{
			Bindings = new List<Binding>();
			Bindings.AddRange(DefaultValues);
		}

		public void Add(Binding b)
		{
			Bindings.Add(b);
		}

		public IEnumerator<Binding> GetEnumerator()
		{
			return Bindings.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public Binding this[string index] => Bindings.FirstOrDefault(b => b.DisplayName == index) ?? new Binding();

		private static Binding Bind(string tabGroup, string displayName, string bindings = "", string defaultBinding = "", string toolTip = "")
		{
			if (string.IsNullOrEmpty(defaultBinding))
			{
				defaultBinding = bindings;
			}

			return new Binding { DisplayName = displayName, Bindings = bindings, TabGroup = tabGroup, DefaultBinding = defaultBinding, ToolTip = toolTip };
		}

		public void ResolveWithDefaults()
		{
			// TODO - this method is potentially disastrously O(N^2) slow due to linear search nested in loop

			// Add missing entries
			foreach (Binding defaultBinding in DefaultValues)
			{
				var binding = Bindings.FirstOrDefault(b => b.DisplayName == defaultBinding.DisplayName);
				if (binding == null)
				{
					Bindings.Add(defaultBinding);
				}
				else
				{
					// patch entries with updated settings (necessary because of TODO LARP
					binding.Ordinal = defaultBinding.Ordinal;
					binding.DefaultBinding = defaultBinding.DefaultBinding;
					binding.TabGroup = defaultBinding.TabGroup;
					binding.ToolTip = defaultBinding.ToolTip;
					binding.Ordinal = defaultBinding.Ordinal;
				}
			}

			// Remove entries that no longer exist in defaults
			Bindings.RemoveAll(entry => DefaultValues.All(b => b.DisplayName != entry.DisplayName));
		}

		private static List<Binding> _defaultValues;

		public static List<Binding> DefaultValues
		{
			get
			{
				if (_defaultValues == null)
				{
					_defaultValues = new List<Binding>
					{
						Bind("General", "Frame Advance", "F"),
						Bind("General", "Pause", "Pause"),
						Bind("General", "Fast Forward", "Tab"),
						Bind("General", "Turbo", "Shift+Tab"),
						Bind("General", "Toggle Throttle"),
						Bind("General", "Soft Reset"),
						Bind("General", "Hard Reset"),
						//Bind("General", "Autohold"),
						Bind("General", "Clear Autohold"),
						Bind("General", "Screenshot", "F12"),
						Bind("General", "Full Screen", "Alt+Enter"),
						Bind("General", "Flush SaveRAM", "Ctrl+S"),
						Bind("General", "Display FPS"),
						Bind("General", "Frame Counter"),
						Bind("General", "Lag Counter"),
						Bind("General", "Toggle BG Input"),
						//Bind("General", "Toggle Menu"),
						Bind("General", "Volume Up"),
						Bind("General", "Volume Down"),
						Bind("General", "Larger Window", "Alt+Up"),
						Bind("General", "Smaller Window", "Alt+Down"),
						Bind("General", "Increase Speed", "Equals"),
						Bind("General", "Decrease Speed", "Minus"),
						Bind("General", "Reset Speed", "Shift+Equals"),
						Bind("General", "Reboot Core", "Ctrl+R"),
						Bind("General", "Toggle Sound"),
						Bind("General", "Exit Program"),
						Bind("General", "Screen Raw to Clipboard", "Ctrl+C"),
						Bind("General", "Screen Client to Clipboard", "Ctrl+Shift+C"),
						Bind("General", "Toggle Skip Lag Frame"),
						Bind("General", "Toggle Key Priority"),
						Bind("General", "Frame Inch"),

						Bind("Project", "Open Project", "Ctrl+O"),
						Bind("Project", "Close Project", "Ctrl+W"),
						Bind("Project", "Load Last Project"),
						Bind("Project", "Build Project", "Ctrl+B"),
						Bind("Project", "Run Project"),
						Bind("Project", "Build And Run", "F5"),

						Bind("Tools", "RAM Watch"),
						Bind("Tools", "RAM Search"),
						Bind("Tools", "Hex Editor"),
						//Bind("Tools", "Trace Logger"),
						//Bind("Tools", "Cheats"),
						//Bind("Tools", "ToolBox", "Shift+T"),
						//Bind("Tools", "Virtual Pad"),

						Bind("RAM Search", "New Search"),
						Bind("RAM Search", "Do Search"),
						Bind("RAM Search", "Previous Compare To"),
						Bind("RAM Search", "Next Compare To"),
						Bind("RAM Search", "Previous Operator"),
						Bind("RAM Search", "Next Operator"),
					};

					// set ordinals based on order in list
					for (int i = 0; i < _defaultValues.Count; i++)
					{
						_defaultValues[i].Ordinal = i;
					}
				} // if (s_DefaultValues == null)

				return _defaultValues;
			}
		}
	}
}
