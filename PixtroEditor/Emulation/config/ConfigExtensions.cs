﻿using System;
using Newtonsoft.Json.Linq;

namespace Pixtro.Emulation
{
	public static class ConfigExtensions
	{
		private class TypeNameEncapsulator
		{
			public object o;
		}
		private static JToken Serialize(object o)
		{
			var tne = new TypeNameEncapsulator { o = o };
			return JToken.FromObject(tne, ConfigService.Serializer)["o"];

			// Maybe todo:  This code is identical to the code above, except that it does not emit the legacy "$type"
			// parameter that we no longer need here.  Leaving that in to make bisecting during this dev phase easier, and such.
			// return JToken.FromObject(o, ConfigService.Serializer);
		}
		private static object Deserialize(JToken j, Type type)
		{
			try
			{
				return j?.ToObject(type, ConfigService.Serializer);
			}
			catch
			{
				// presumably some sort of config mismatch.  Anywhere we can expose this usefully?
				return null;
			}
		}

		/// <summary>
		/// Returns the core settings for a core
		/// </summary>
		/// <param name="config"></param>
		/// <param name="coreType"></param>
		/// <returns>null if no settings were saved, or there was an error deserializing</returns>
		public static object GetCoreSettings(this Config config, Type coreType, Type settingsType)
		{
			config.CoreSettings.TryGetValue(coreType.ToString(), out var j);
			return Deserialize(j, settingsType);
		}

		/// <summary>
		/// Returns the core settings for a core
		/// </summary>
		/// <returns>null if no settings were saved, or there was an error deserializing</returns>
		public static TSetting GetCoreSettings<TCore, TSetting>(this Config config)
			where TCore : IEmulator
		{
			return (TSetting)config.GetCoreSettings(typeof(TCore), typeof(TSetting));
		}

		/// <summary>
		/// saves the core settings for a core
		/// </summary>
		/// <param name="config"></param>
		/// <param name="o">null to remove settings for that core instead</param>
		/// <param name="coreType"></param>
		public static void PutCoreSettings(this Config config, object o, Type coreType)
		{
			if (o != null)
			{
				config.CoreSettings[coreType.ToString()] = Serialize(o);
			}
			else
			{
				config.CoreSettings.Remove(coreType.ToString());
			}
		}

		/// <summary>
		/// saves the core settings for a core
		/// </summary>
		/// <param name="config"></param>
		/// <param name="o">null to remove settings for that core instead</param>
		/// <typeparam name="TCore"></typeparam>
		public static void PutCoreSettings<TCore>(this Config config, object o)
			where TCore : IEmulator
		{
			config.PutCoreSettings(o, typeof(TCore));
		}

		/// <summary>
		/// Returns the core syncsettings for a core
		/// </summary>
		/// <param name="config"></param>
		/// <param name="coreType"></param>
		/// <returns>null if no settings were saved, or there was an error deserializing</returns>
		public static object GetCoreSyncSettings(this Config config, Type coreType, Type syncSettingsType)
		{
			config.CoreSyncSettings.TryGetValue(coreType.ToString(), out var j);
			return Deserialize(j, syncSettingsType);
		}

		/// <summary>
		/// Returns the core syncsettings for a core
		/// </summary>
		/// <returns>null if no settings were saved, or there was an error deserializing</returns>
		public static TSync GetCoreSyncSettings<TCore, TSync>(this Config config)
			where TCore : IEmulator
		{
			return (TSync)config.GetCoreSyncSettings(typeof(TCore), typeof(TSync));
		}

		/// <summary>
		/// saves the core syncsettings for a core
		/// </summary>
		/// <param name="config"></param>
		/// <param name="o">null to remove settings for that core instead</param>
		/// <param name="coreType"></param>
		public static void PutCoreSyncSettings(this Config config, object o, Type coreType)
		{
			if (o != null)
			{
				config.CoreSyncSettings[coreType.ToString()] = Serialize(o);
			}
			else
			{
				config.CoreSyncSettings.Remove(coreType.ToString());
			}
		}

		/// <summary>
		/// saves the core syncsettings for a core
		/// </summary>
		/// <param name="config"></param>
		/// <param name="o">null to remove settings for that core instead</param>
		/// <typeparam name="TCore"></typeparam>
		public static void PutCoreSyncSettings<TCore>(this Config config, object o)
			where TCore : IEmulator
		{
			config.PutCoreSyncSettings(o, typeof(TCore));
		}

		/// <param name="fileExt">file extension, including the leading period and in lowercase</param>
		/// <remarks><paramref name="systemID"/> will be <see langword="null"/> if returned value is <see langword="false"/></remarks>
		public static bool TryGetChosenSystemForFileExt(this Config config, string fileExt, out string systemID)
		{
			var b = config.PreferredPlatformsForExtensions.TryGetValue(fileExt, out var v);
			if (b && !string.IsNullOrEmpty(v))
			{
				systemID = v;
				return true;
			}
			systemID = null;
			return false;
		}
	}
}
