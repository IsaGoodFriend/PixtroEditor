using System;
using System.Collections.Generic;
using System.Linq;

namespace Pixtro.Emulation
{

	[AttributeUsage(AttributeTargets.Class)]
	public sealed class ExternalToolAttribute : Attribute
	{
		public string Description { get; set; }

		public string[] LoadAssemblyFiles { get; set; }

		public readonly string Name;

		public ExternalToolAttribute(string name)
		{
			Name = string.IsNullOrWhiteSpace(name) ? Guid.NewGuid().ToString() : name;
		}

		public class MissingException : Exception {}
	}

	[AttributeUsage(AttributeTargets.Class)]
	public sealed class ExternalToolEmbeddedIconAttribute : Attribute
	{
		/// <remarks>The full path, including the assembly name.</remarks>
		public readonly string ResourcePath;

		/// <param name="resourcePath">The full path, including the assembly name.</param>
		public ExternalToolEmbeddedIconAttribute(string resourcePath)
		{
			ResourcePath = resourcePath;
		}
	}
}
