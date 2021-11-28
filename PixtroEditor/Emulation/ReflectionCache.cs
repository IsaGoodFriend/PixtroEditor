using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Pixtro.Emulation
{
	public static class ReflectionCache
	{
		private static readonly Assembly Asm = typeof(ReflectionCache).Assembly;

		public static readonly Version AsmVersion = Asm.GetName().Version!;

		private static readonly Lazy<Type[]> _types = new Lazy<Type[]>(() => Asm.GetTypesWithoutLoadErrors().ToArray());

		public static Type[] Types => _types.Value;

		/// <exception cref="ArgumentException">not found</exception>
		public static Stream EmbeddedResourceStream(string embedPath)
		{
			var fullPath = $"Pixtro.Emulation.{embedPath}";

			var value = Asm.GetManifestResourceStream(fullPath);

			return value ?? throw new ArgumentException("resource at {fullPath} not found", nameof(embedPath));
			
		}
	}
}
