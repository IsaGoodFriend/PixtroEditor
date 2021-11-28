﻿using System;
using System.Collections.Generic;

namespace Pixtro.Emulation
{
	/// <summary>
	/// This is a property of <seealso cref="IInputPollable"/>, and defines the means by which a client
	/// gets and sets input callbacks in the core.  An input callback should fire any time input is
	/// polled by the core
	/// </summary>
	/// <seealso cref="IInputPollable"/>
	public interface IInputCallbackSystem : ICollection<Action>
	{
		/// <summary>
		/// Will iterate and call every callback
		/// </summary>
		void Call();

		/// <summary>
		/// Will remove the given list of callbacks
		/// </summary>
		void RemoveAll(IEnumerable<Action> actions);
	}
}
