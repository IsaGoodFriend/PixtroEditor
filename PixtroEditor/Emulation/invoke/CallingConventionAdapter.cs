﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Pixtro.Emulation;

namespace Pixtro.Emulation
{
	/// <summary>
	/// create interop delegates and function pointers for a particular calling convention
	/// </summary>
	public interface ICallingConventionAdapter
	{
		/// <summary>
		/// Like Marshal.GetFunctionPointerForDelegate(), but wraps a thunk around the returned native pointer
		/// to adjust the calling convention appropriately
		/// </summary>
		IntPtr GetFunctionPointerForDelegate(Delegate d);
		/// <summary>
		/// Like Marshal.GetFunctionPointerForDelegate, but only the unmanaged thunk-to-thunk part, with no
		/// managed wrapper involved.  Called "arrival" because it is to be used when the foreign code is calling
		/// back into host code.
		/// </summary>
		/// <returns></returns>
		IntPtr GetArrivalFunctionPointer(IntPtr p, ParameterInfo pp, object lifetime);

		/// <summary>
		/// Like Marshal.GetDelegateForFunctionPointer(), but wraps a thunk around the passed native pointer
		/// to adjust the calling convention appropriately
		/// </summary>
		/// <param name="p"></param>
		/// <param name="delegateType"></param>
		/// <returns></returns>
		Delegate GetDelegateForFunctionPointer(IntPtr p, Type delegateType);
		/// <summary>
		/// Like Marshal.GetDelegateForFunctionPointer(), but only the unmanaged thunk-to-thunk part, with no
		/// managed wrapper involved.static  Called "departure" beause it is to be used when first leaving host
		/// code for foreign code.
		/// </summary>
		/// <param name="p"></param>
		/// <param name="pp"></param>
		/// <param name="lifetime"></param>
		/// <returns></returns>
		IntPtr GetDepartureFunctionPointer(IntPtr p, ParameterInfo pp, object lifetime);
	}

	public static class CallingConventionAdapterExtensions
	{
		public static T GetDelegateForFunctionPointer<T>(this ICallingConventionAdapter a, IntPtr p)
			where T : Delegate
			=> (T) a.GetDelegateForFunctionPointer(p, typeof(T));
	}

	public class ParameterInfo
	{
		public Type ReturnType { get; }
		public IReadOnlyList<Type> ParameterTypes { get; }

		public ParameterInfo(Type returnType, IEnumerable<Type> parameterTypes)
		{
			ReturnType = returnType;
			ParameterTypes = parameterTypes.ToList().AsReadOnly();
		}

		/// <exception cref="InvalidOperationException"><paramref name="delegateType"/> does not inherit <see cref="Delegate"/></exception>
		public ParameterInfo(Type delegateType)
		{
			if (!typeof(Delegate).IsAssignableFrom(delegateType))
				throw new InvalidOperationException("Must be a delegate type!");
			var invoke = delegateType.GetMethod("Invoke")!;
			ReturnType = invoke.ReturnType;
			ParameterTypes = invoke.GetParameters().Select(p => p.ParameterType).ToList().AsReadOnly();
		}
	}

	/// <summary>
	/// Abstract over some waterbox functionality, sort of.  TODO: Would this ever make sense for anything else,
	/// or maybe it's just actually another CallingConventionAdapter and we're composing them?
	/// </summary>
	public interface ICallbackAdjuster
	{
		/// <summary>
		/// Returns a thunk over an arrival callback, for the given slot number.  Slots don't have much of
		/// any meaning to CallingConvention Adapter; it's just a unique key associated with the callback.
		/// </summary>
		IntPtr GetCallbackProcAddr(IntPtr exitPoint, int slot);
		/// <summary>
		/// Returns a thunk over a departure point.
		/// </summary>
		IntPtr GetCallinProcAddr(IntPtr entryPoint);
	}

	public static class CallingConventionAdapters
	{
		private class NativeConvention : ICallingConventionAdapter
		{
			public IntPtr GetArrivalFunctionPointer(IntPtr p, ParameterInfo pp, object lifetime)
			{
				return p;
			}

			public Delegate GetDelegateForFunctionPointer(IntPtr p, Type delegateType)
			{
				return Marshal.GetDelegateForFunctionPointer(p, delegateType);
			}

			public IntPtr GetDepartureFunctionPointer(IntPtr p, ParameterInfo pp, object lifetime)
			{
				return p;
			}

			public IntPtr GetFunctionPointerForDelegate(Delegate d)
			{
				return Marshal.GetFunctionPointerForDelegate(d);
			}
		}

		/// <summary>
		/// native (pass-through) calling convention
		/// </summary>
		public static ICallingConventionAdapter Native { get; } = new NativeConvention();

		/// <summary>
		/// waterbox calling convention, including thunk handling for stack marshalling
		/// </summary>
		public static ICallingConventionAdapter MakeWaterbox(IEnumerable<Delegate> slots, ICallbackAdjuster waterboxHost)
		{
			return new WaterboxAdapter(slots, waterboxHost);
		}
		/// <summary>
		/// waterbox calling convention, including thunk handling for stack marshalling.  Can only do callins, not callouts
		/// </summary>
		public static ICallingConventionAdapter MakeWaterboxDepartureOnly(ICallbackAdjuster waterboxHost)
		{
			return new WaterboxAdapter(null, waterboxHost);
		}

		/// <summary>
		/// Get a waterbox calling convention adapater, except no wrapping is done for stack marshalling and callback support.
		/// This is very unsafe; any attempts by the guest to call syscalls will crash, and stack hygiene will be all wrong.
		/// DO NOT USE THIS.
		/// </summary>
		/// <returns></returns>
		public static ICallingConventionAdapter GetWaterboxUnsafeUnwrapped()
		{
			return WaterboxAdapter.WaterboxWrapper;
		}

		private class WaterboxAdapter : ICallingConventionAdapter
		{
			private class ReferenceEqualityComparer : IEqualityComparer<Delegate>
			{
				public bool Equals(Delegate x, Delegate y)
				{
					return x == y;
				}

				public int GetHashCode(Delegate obj)
				{
					return System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
				}
			}

			internal static readonly ICallingConventionAdapter WaterboxWrapper;
			static WaterboxAdapter()
			{
				WaterboxWrapper = OSTailoredCode.IsUnixHost
					? new NativeConvention()
					: (ICallingConventionAdapter)new MsHostSysVGuest();
			}

			private readonly Dictionary<Delegate, int>? _slots;
			private readonly ICallbackAdjuster _waterboxHost;

			public WaterboxAdapter(IEnumerable<Delegate>? slots, ICallbackAdjuster waterboxHost)
			{
				if (slots != null)
				{
					_slots = slots.Select((cb, i) => new { cb, i })
						.ToDictionary(a => a.cb, a => a.i, new ReferenceEqualityComparer());
				}
				_waterboxHost = waterboxHost;
			}

			public IntPtr GetArrivalFunctionPointer(IntPtr p, ParameterInfo pp, object lifetime)
			{
				if (_slots == null)
				{
					throw new InvalidOperationException("This calling convention adapter was created for departure only!  Pass known delegate slots when constructing to enable arrival");
				}
				if (!(lifetime is Delegate d))
				{
					throw new ArgumentException("For this calling convention adapter, lifetimes must be delegate so guest slot can be inferred");
				}
				if (!_slots.TryGetValue(d, out var slot))
				{
					throw new InvalidOperationException("All callback delegates must be registered at load");
				}
				return _waterboxHost.GetCallbackProcAddr(WaterboxWrapper.GetArrivalFunctionPointer(p, pp, lifetime), slot);
			}

			public Delegate GetDelegateForFunctionPointer(IntPtr p, Type delegateType)
			{
				p = _waterboxHost.GetCallinProcAddr(p);
				return WaterboxWrapper.GetDelegateForFunctionPointer(p, delegateType);
			}

			public IntPtr GetDepartureFunctionPointer(IntPtr p, ParameterInfo pp, object lifetime)
			{
				p = _waterboxHost.GetCallinProcAddr(p);
				return WaterboxWrapper.GetDepartureFunctionPointer(p, pp, lifetime);
			}

			public IntPtr GetFunctionPointerForDelegate(Delegate d)
			{
				if (_slots == null)
				{
					throw new InvalidOperationException("This calling convention adapter was created for departure only!  Pass known delegate slots when constructing to enable arrival");
				}
				if (!_slots.TryGetValue(d, out var slot))
				{
					throw new InvalidOperationException("All callback delegates must be registered at load");
				}
				return _waterboxHost.GetCallbackProcAddr(WaterboxWrapper.GetFunctionPointerForDelegate(d), slot);
			}
		}

		/// <summary>
		/// Calling Convention Adapter for where host code expects msabi and guest code is sysv.
		/// Does not handle anything Waterbox specific.
		/// </summary>
		private class MsHostSysVGuest : ICallingConventionAdapter
		{
			// This is implemented by using thunks defined in the waterbox native code, and putting stubs on top of them that close over the
			// function pointer parameter.

			private const int BlockSize = 32;
			private static readonly IImportResolver ThunkDll;

			static MsHostSysVGuest()
			{
				// If needed, these can be split out from waterboxhost.dll; they're not directly related to anything waterbox does.
				ThunkDll = new DynamicLibraryImportResolver(OSTailoredCode.IsUnixHost ? "libwaterboxhost.so" : "waterboxhost.dll", hasLimitedLifetime: false);
			}

			private readonly MemoryBlock _memory;
			private readonly object _sync = new object();
			private readonly WeakReference?[] _refs;

			public MsHostSysVGuest()
			{
				int size = 4 * 1024 * 1024;
				_memory = new MemoryBlock((ulong)size);
				_refs = new WeakReference[size / BlockSize];
			}

			private int FindFreeIndex()
			{
				for (int i = 0; i < _refs.Length; i++)
				{
					if (!_refs[i].IsAlive) return i; // return index of first null or dead
				}
				throw new InvalidOperationException("Out of Thunk memory");
			}

			private int FindUsedIndex(object lifetime)
			{
				for (int i = 0; i < _refs.Length; i++)
				{
					if (_refs[i]?.Target == lifetime)
						return i;
				}
				return -1;
			}

			private static void VerifyParameter(Type type)
			{
				if (type == typeof(float) || type == typeof(double))
					throw new NotSupportedException("floating point not supported");
				if (type == typeof(void) || type.IsPrimitive || type.IsEnum)
					return;
				if (type.IsPointer || typeof(Delegate).IsAssignableFrom(type))
					return;
				if (type.IsByRef || type.IsClass)
					return;
				throw new NotSupportedException($"Unknown type {type}. Possibly supported?");
			}

			private static int VerifyDelegateSignature(ParameterInfo pp)
			{
				VerifyParameter(pp.ReturnType);
				foreach (var ppp in pp.ParameterTypes)
					VerifyParameter(ppp);
				var ret = pp.ParameterTypes.Count;
				if (ret >= 7)
					throw new InvalidOperationException("Too many parameters to marshal!");
				return ret;
			}

			private void WriteThunk(IntPtr thunkFunctionAddress, IntPtr calleeAddress, int index)
			{
				_memory.Protect(_memory.Start, _memory.Size, MemoryBlock.Protection.RW);
				var ss = _memory.GetStream(_memory.Start + (ulong)index * BlockSize, BlockSize, true);
				var bw = new BinaryWriter(ss);

				// The thunks all take the expected parameters in the expected places, but additionally take the parameter
				// of the function to call as a hidden extra parameter in rax.

				// mov r10, thunkFunctionAddress
				bw.Write((byte)0x49);
				bw.Write((byte)0xba);
				bw.Write((long)thunkFunctionAddress);
				// mov rax, calleeAddress
				bw.Write((byte)0x48);
				bw.Write((byte)0xb8);
				bw.Write((long)calleeAddress);
				// jmp r10
				bw.Write((byte)0x41);
				bw.Write((byte)0xff);
				bw.Write((byte)0xe2);

				_memory.Protect(_memory.Start, _memory.Size, MemoryBlock.Protection.RX);
			}

			private IntPtr GetThunkAddress(int index)
			{
				return Z.US(_memory.Start + (ulong)index * BlockSize);
			}

			private void SetLifetime(int index, object lifetime)
			{
				_refs[index] ??= new WeakReference(null);
				_refs[index]!.Target = lifetime;
			}

			public IntPtr GetFunctionPointerForDelegate(Delegate d)
			{
				// for this call only, the expectation is that it can be called multiple times
				// on the same delegate and not leak extra memory, so the result has to be cached
				lock (_sync)
				{
					var index = FindUsedIndex(d);
					if (index != -1)
					{
						return GetThunkAddress(index);
					}
					else
					{
						return GetArrivalFunctionPointer(
							Marshal.GetFunctionPointerForDelegate(d), new ParameterInfo(d.GetType()), d);
					}
				}
			}

			public IntPtr GetArrivalFunctionPointer(IntPtr p, ParameterInfo pp, object lifetime)
			{
				lock (_sync)
				{
					var index = FindFreeIndex();
					var count = VerifyDelegateSignature(pp);
					WriteThunk(ThunkDll.GetProcAddrOrThrow($"arrive{count}"), p, index);
					SetLifetime(index, lifetime);
					return GetThunkAddress(index);
				}
			}

			public Delegate GetDelegateForFunctionPointer(IntPtr p, Type delegateType)
			{
				lock (_sync)
				{
					var index = FindFreeIndex();
					var count = VerifyDelegateSignature(new ParameterInfo(delegateType));
					WriteThunk(ThunkDll.GetProcAddrOrThrow($"depart{count}"), p, index);
					var ret = Marshal.GetDelegateForFunctionPointer(GetThunkAddress(index), delegateType);
					SetLifetime(index, ret);
					return ret;
				}
			}

			public IntPtr GetDepartureFunctionPointer(IntPtr p, ParameterInfo pp, object lifetime)
			{
				lock (_sync)
				{
					var index = FindFreeIndex();
					var count = VerifyDelegateSignature(pp);
					WriteThunk(ThunkDll.GetProcAddrOrThrow($"depart{count}"), p, index);
					SetLifetime(index, lifetime);
					return GetThunkAddress(index);
				}
			}

		}
	}
}
