using Pixtro.Emulation;

namespace Pixtro.Emulation.GBA
{
	public partial class MGBAHawk : IInputPollable
	{
		public int LagCount { get; set; }
		public bool IsLagFrame { get; set; }

		[FeatureNotImplemented]
		public IInputCallbackSystem InputCallbacks { get; private set; }
	}
}
