namespace Pixtro.Emulation
{
	public interface ICheatConfig
	{
		bool DisableOnLoad { get; }
		bool AutoSaveOnClose { get; }
	}

	public class CheatConfig : ICheatConfig
	{
		public bool DisableOnLoad { get; set; }
		public bool LoadFileByGame { get; set; } = true;
		public bool AutoSaveOnClose { get; set; } = true;
	}
}
