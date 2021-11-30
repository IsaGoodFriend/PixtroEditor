#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Pixtro.Emulation;


namespace Pixtro.Emulation
{
	public static class ApiManager
	{
		private static readonly IReadOnlyList<(Type ImplType, Type InterfaceType, ConstructorInfo Ctor, Type[] CtorTypes)> _apiTypes;

		static ApiManager()
		{
			var list = new List<(Type, Type, ConstructorInfo, Type[])>();

			var typeList = new List<Type>()
			{
				typeof(EmulationApi),
				typeof(MemoryApi),
				typeof(MemoryEventsApi),
				typeof(MemorySaveStateApi),
			};

			foreach (var implType in typeList.Where(t => /*t.IsClass &&*/t.IsSealed)) // small optimisation; api impl. types are all sealed classes
			{
				var interfaceType = implType.GetInterfaces().FirstOrDefault(t => typeof(IExternalApi).IsAssignableFrom(t) && t != typeof(IExternalApi));
				if (interfaceType == null) continue; // if we couldn't determine what it's implementing, then it's not an api impl. type
				var ctor = implType.GetConstructors().Single();
				list.Add((implType, interfaceType, ctor, ctor.GetParameters().Select(pi => pi.ParameterType).ToArray()));
			}
			_apiTypes = list.ToArray();
		}

		private static ApiContainer? _container;

		private static ApiContainer Register(
			IEmulatorServiceProvider serviceProvider,
			Action<string> logCallback,
			Config config,
			IEmulator emulator,
			IGameInfo game)
		{
			var avail = new Dictionary<Type, object>
			{
				[typeof(Action<string>)] = logCallback,
				[typeof(Config)] = config,
				[typeof(IEmulator)] = emulator,
				[typeof(IGameInfo)] = game,
			};
			return new ApiContainer(_apiTypes.Where(tuple => ServiceInjector.IsAvailable(serviceProvider, tuple.ImplType))
				.ToDictionary(
					tuple => tuple.InterfaceType,
					tuple =>
					{
						var instance = tuple.Ctor.Invoke(tuple.CtorTypes.Select(t => avail[t]).ToArray());
						ServiceInjector.UpdateServices(serviceProvider, instance);
						return (IExternalApi) instance;
					}));
		}

		public static IExternalApiProvider Restart(
			IEmulatorServiceProvider serviceProvider,
			Config config,
			IEmulator emulator,
			IGameInfo game)
		{
			_container?.Dispose();
			_container = Register(serviceProvider, Console.WriteLine,config, emulator, game);
			return new BasicApiProvider(_container);
		}
	}
}
