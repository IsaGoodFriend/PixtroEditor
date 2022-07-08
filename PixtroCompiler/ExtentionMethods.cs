using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using Newtonsoft.Json;

namespace Pixtro.Compiler {
	public class PointConverter : JsonConverter<Point> {
		public override Point ReadJson(JsonReader reader, Type objectType, Point existingValue, bool hasExistingValue, JsonSerializer serializer) {

			var points = (reader.Value as string).Split(',');

			return new Point(int.Parse(points[0].Trim()), int.Parse(points[1].Trim()));
		}

		public override void WriteJson(JsonWriter writer, Point value, JsonSerializer serializer) {
		}
	}
	public static class ExtentionMethods
	{
		// Get all variables and methods
		// (?<=0x0{8})0(3|8)[0-9a-f]{6} +(?!(_start|__boot_method|__slave_number|_init))[A-Za-z_][0-9A-Za-z_]+(?=\n)
		// Regular expressions for finding a variable
		//
		// (?<=0x0{9}MEMORY_BUS[0-9a-f]{6} +0x[0-9a-f]+ [A-Za-z][0-9A-Za-z_]*.o\n[\n 0-9A-Za-z_]+0x0{9}MEMORY_BUS)[0-9a-f]{6} +VARIABLE_NAME\n

		public static void AddToList<K, T>(this Dictionary<K, List<T>> dictionary, K key, T value)
		{
			if (!dictionary.ContainsKey(key))
			{
				dictionary[key] = new List<T>();
			}
			dictionary[key].Add(value);
		}

		public static T GetXY<T>(this T[] array, int x, int y, int width) {
			return array[x + (y * width)];
		}
		public static void SetXY<T>(this T[] array, int x, int y, int width, T value) {
			array[x + (y * width)] = value;
		}
		public static void Flip<T>(this T[] array, bool X, int width) {
			int height = array.Length / width;
			
			// Only flip if array is rectangular based on width variable
			if (height * width != array.Length)
				return;

			if (X) {
				for (int x = 0; x < width / 2; ++x) {
					int otherX = (width - x) - 1;
					for (int y = 0; y < height; ++y) {

						T temp = array.GetXY(x, y, width);

						array.SetXY(x, y, width, array.GetXY(otherX, y, width));

						array.SetXY(otherX, y, width, temp);
					}
				}
			}
			else {
				for (int y = 0; y < height / 2; ++y) {
					int otherY = (height - y) - 1;
					for (int x = 0; x < width; ++x) {

						T temp = array.GetXY(x, y, width);

						array.SetXY(x, y, width, array.GetXY(x, otherY, width));

						array.SetXY(x, otherY, width, temp);
					}
				}
			}
		}
		public static void Flip<T>(this T[,] array, bool X)
		{
			int width = array.GetLength(0);
			int height = array.GetLength(1);

			// Only flip if array is rectangular based on width variable
			if (height * width != array.Length)
				return;

			if (X)
			{
				for (int x = 0; x < width / 2; ++x)
				{
					int otherX = (width - x) - 1;
					for (int y = 0; y < height; ++y)
					{
						T temp = array[x, y];

						array[x, y] = array[otherX, y];

						array[otherX, y] = temp;
					}
				}
			}
			else
			{
				for (int y = 0; y < height / 2; ++y)
				{
					int otherY = (height - y) - 1;
					for (int x = 0; x < width; ++x)
					{
						T temp = array[x, y];

						array[x, y] = array[x, otherY];

						array[x, otherY] = temp;
					}
				}
			}
		}
		public static uint GetWrapping<T>(this T[,] array, int x, int y, T[] check, params Point[] points) {

			int width = array.GetLength(0);
			int height = array.GetLength(1);

			uint retval = 0;

			foreach (var p in points) {
				retval <<= 1;

				Point ex = new Point(Clamp(x + p.X, 0, width - 1), Clamp(y + p.Y, 0, height - 1));

				if (check.Contains(array[ex.X, ex.Y]))
					retval |= 1;
			}

			return retval;
		}
		public static T GetValueWrapped<T>(this T[] array, int value)
		{
			return array[value % array.Length];
		}
		public static T GetRandom<T>(this T[] array, Random random) {
			return array[random.Next(0, array.Length)];
		}
		public static int Clamp(int value, int min, int max) {
			return Math.Min(Math.Max(value, min), max);
		}
		public static ushort ToGBA(this Color color, ushort _transparent = 0x8000) {
			if (color.R == 0 && color.G == 0 && color.B == 0)
				return _transparent;

			int r = (color.R & 0xF8) >> 3;
			int g = (color.G & 0xF8) >> 3;
			int b = (color.B & 0xF8) >> 3;


			return (ushort)(r | (g << 5) | (b << 10));
		}
		public static Color FromGBA(this ushort color) {
			return Color.FromArgb(255, (color & 0x1F) << 3, (color & 0x3E0) >> 2, (color & 0x7C00) >> 7);
		}

		public static int IndexOf<T>(this List<T> list, T compareTo, IEqualityComparer<T> compareWith)
		{
			int value = 0;
			foreach (var val in list)
			{
				if (compareWith.Equals(val, compareTo))
					return value;

				value++;
			}
			return -1;
		}

		public static bool IsInteger(this string str)
		{
			foreach (var c in str)
			{
				if (!char.IsNumber(c) || c == '-')
					return false;
			}
			return true;
		}
		public static bool IsDecimal(this string str)
		{
			foreach (var c in str)
			{
				if (!char.IsDigit(c))
					return false;
			}
			return true;
		}

		public static bool ContainsValue<T>(this IEnumerable<T?> list, T value) where T : struct
		{
			foreach (var item in list)
			{
				if (item == null)
				{
					continue;
				}
				if (item.Equals(value))
				{
					return true;
				}
			}
			return false;
		}
	}
}