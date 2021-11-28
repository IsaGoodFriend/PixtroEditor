#nullable enable

namespace Pixtro.Emulation
{
	public readonly struct FirmwareRecord
	{
		public readonly string Description;

		public readonly FirmwareID ID;

		public FirmwareRecord(FirmwareID id, string desc)
		{
			Description = desc;
			ID = id;
		}
	}
}
