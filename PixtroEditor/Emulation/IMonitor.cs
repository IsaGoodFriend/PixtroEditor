﻿using System;

namespace Pixtro.Emulation
{
	public interface IMonitor
	{
		void Enter();

		void Exit();
	}

	public static class MonitorExtensions
	{
		public static IDisposable EnterExit(this IMonitor m)
		{
			var ret = new EnterExitWrapper(m);
			m.Enter();
			return ret;
		}

		private class EnterExitWrapper : IDisposable
		{
			private readonly IMonitor _m;

			private bool _disposed;

			public EnterExitWrapper(IMonitor m)
			{
				_m = m;
			}

			public void Dispose()
			{
				if (!_disposed)
				{
					_m.Exit();
					_disposed = true;
				}
			}
		}
	}
}
