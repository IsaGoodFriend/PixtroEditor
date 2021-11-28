#nullable enable

namespace Pixtro.Emulation
{
	public interface AxisConstraint
	{
		public string? Class { get; }

		public string? PairedAxis { get; }
	}
}
