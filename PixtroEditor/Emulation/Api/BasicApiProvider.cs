﻿#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

namespace Pixtro.Emulation
{
	public class BasicApiProvider : IExternalApiProvider
	{
		public IReadOnlyCollection<Type> AvailableApis => Container.Libraries.Keys.ToList();

		public ApiContainer Container { get; }

		public BasicApiProvider(ApiContainer apiContainer) => Container = apiContainer;

		public object? GetApi(Type t) => Container.Libraries.TryGetValue(t, out var api) ? api : null;

		public bool HasApi(Type t) => Container.Libraries.ContainsKey(t);
	}
}
