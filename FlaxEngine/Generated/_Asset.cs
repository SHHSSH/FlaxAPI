//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------
using System;
using System.Runtime.CompilerServices;

namespace FlaxEngine
{

		/// <summary>
		/// Assets objects base class
		/// </summary>
		public partial class Asset : Object
		{
			/// <summary>
			/// Gets asset name
			/// </summary>
			[UnmanagedCall]
			public String Name
			{
#if UNIT_TEST_COMPILANT
				get; set;
#else
				get { return Internal_GetName(unmanagedPtr); }
#endif
			}

			/// <summary>
			/// Gets amount of references to that asset
			/// </summary>
			[UnmanagedCall]
			public int RefCount
			{
#if UNIT_TEST_COMPILANT
				get; set;
#else
				get { return Internal_GetRefCount(unmanagedPtr); }
#endif
			}

#region Internal Calls
#if !UNIT_TEST_COMPILANT
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal static extern String Internal_GetName(IntPtr obj);
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal static extern int Internal_GetRefCount(IntPtr obj);
#endif
#endregion
	}
}

