﻿namespace Pixtro.Emulation
{
	/// <summary>
	/// Specifies an interface for returning the state of a LED drive light such as on Disk and CD Drives,
	/// If available the client will display a light that turns on and off based on the drive light status
	/// </summary>
	public interface IDriveLight : IEmulatorService
	{
		/// <summary>
		/// Gets a value indicating whether there is currently a Drive light available
		/// </summary>
		bool DriveLightEnabled { get; }

		/// <summary>
		/// Gets a value indicating whether the light is currently lit
		/// </summary>
		bool DriveLightOn { get; }
	}
}
