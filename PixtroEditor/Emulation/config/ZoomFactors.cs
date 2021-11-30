﻿using System.Collections.Generic;

namespace Pixtro.Emulation
{
	public class ZoomFactors : Dictionary<string, int>
	{
		public new int this[string index]
		{
			get
			{
				if (!ContainsKey(index))
				{
					Add(index, 2);
				}

				return base[index];
			}

			set => base[index] = value;
		}
	}
}
