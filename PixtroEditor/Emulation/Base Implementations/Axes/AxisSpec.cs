#nullable enable

using System;

using Pixtro.Emulation;

namespace Pixtro.Emulation
{
	public readonly struct AxisSpec
	{
		/// <summary>
		/// Gets the axis constraints that apply artificial constraints to float values
		/// For instance, a N64 controller's analog range is actually larger than the amount allowed by the plastic that artificially constrains it to lower values
		/// Axis constraints provide a way to technically allow the full range but have a user option to constrain down to typical values that a real control would have
		/// </summary>
		public readonly AxisConstraint? Constraint;

		public readonly bool IsReversed;

		public int Max => Range.EndInclusive;

		/// <value>maximum decimal digits analog input can occupy with no-args ToString</value>
		/// <remarks>does not include the extra char needed for a minus sign</remarks>
		public int MaxDigits => Math.Max(Math.Abs(Min).ToString().Length, Math.Abs(Max).ToString().Length);

		public readonly int Neutral;

		public int Min => Range.Start;

		public string? PairedAxis => Constraint?.PairedAxis;

		public readonly Range<int> Range;

		public AxisSpec(Range<int> range, int neutral, bool isReversed = false, AxisConstraint? constraint = null)
		{
			Constraint = constraint;
			IsReversed = isReversed;
			Neutral = neutral;
			Range = range;
		}
	}
}
