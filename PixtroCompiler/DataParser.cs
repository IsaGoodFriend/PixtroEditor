using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using System.Text;
using System.Drawing;
using Dangl.Calculator;

namespace Pixtro.Compiler {

	public delegate double DataParseMethods(string[] args);
	public static class DataParser {

		static Regex findMethods = new Regex(@"({ *[A-Za-z][\w_: ]* *})");

		public static double EvaluateDouble(string algorithm, DataParseMethods getvalues = null)
		{
			int count = 0;
			List<string> methods = new List<string>();

			while (algorithm.Contains('{'))
			{
				methods.Add(findMethods.Match(algorithm).Value);

				algorithm = findMethods.Replace(algorithm, $"#m{count++}", 1);
			}
			

			var result = Calculator.Calculate(algorithm, substitute =>
			{
				if (getvalues == null)
					return 0;

				int methodValue = int.Parse(substitute.Substring(2));

				string[] split = methods[methodValue].Replace("{", "").Replace("}", "").Split(':');
				
				for (int i = 0; i < split.Length; ++i)
				{
					split[i] = split[i].Trim();
				}

				double value = getvalues(split);

				return value;
			});

			if (result.IsValid)
				return result.Result;

			return 0;
		}
		public static float EvaluateFloat(string value, DataParseMethods getvalues = null)
		{
			return (float)EvaluateDouble(value, getvalues);
		}
		public static byte EvaluateByte(string value, DataParseMethods getvalues = null)
		{
			return (byte)EvaluateDouble(value, getvalues);
		}
	}
}
