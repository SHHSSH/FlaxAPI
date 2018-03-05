////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) 2012-2018 Flax Engine. All rights reserved.
////////////////////////////////////////////////////////////////////////////////////
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
	/// Represents a listener that hears audio sources. For spatial audio the volume and pitch of played audio is determined by the distance, orientation and velocity differences between the source and the listener.
	/// </summary>
	[Serializable]
	public sealed partial class AudioListener : Actor
	{
		/// <summary>
		/// Creates new <see cref="AudioListener"/> object.
		/// </summary>
		private AudioListener() : base()
		{
		}

		/// <summary>
		/// Creates new instance of <see cref="AudioListener"/> object.
		/// </summary>
		/// <returns>Created object.</returns>
#if UNIT_TEST_COMPILANT
		[Obsolete("Unit tests, don't support methods calls.")]
#endif
		[UnmanagedCall]
		public static AudioListener New() 
		{
#if UNIT_TEST_COMPILANT
			throw new NotImplementedException("Unit tests, don't support methods calls. Only properties can be get or set.");
#else
			return Internal_Create(typeof(AudioListener)) as AudioListener;
#endif
		}

#region Internal Calls
#if !UNIT_TEST_COMPILANT
#endif
#endregion
	}
}
