using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pixtro.Editor
{
	public class EditorLayoutManager
	{
		public EditorLayoutManager()
		{
		}

		private List<(string name, EditorLayout layout)> currentLayouts = new List<(string name, EditorLayout layout)>();
		private int layoutIndex = -1;

		public EditorLayout Current => currentLayouts[layoutIndex].layout;

		public int CurrentLayoutIndex
		{
			get => layoutIndex;
			set
			{
				if (layoutIndex != value)
				{
					ChangeLayout(value);
				}
			}
		}
		public string CurrentLayoutName
		{
			get => currentLayouts[layoutIndex].name;
			set
			{
				if (currentLayouts[layoutIndex].name != value)
				{
					int index;
					for (index = 0; index < currentLayouts.Count; ++index)
						if (currentLayouts[index].name == value)
							break;

					if (index == currentLayouts.Count)
						return;

					CurrentLayoutIndex = index;
				}
			}
		}

		public void Add(string name, EditorLayout layout)
		{
			currentLayouts.Add((name, layout));

			if (layoutIndex < 0)
			{
				ChangeLayout(0);
			}
		}

		public void ChangeLayout(int index)
		{
			if (layoutIndex == index)
				return;
		}
	}
}
