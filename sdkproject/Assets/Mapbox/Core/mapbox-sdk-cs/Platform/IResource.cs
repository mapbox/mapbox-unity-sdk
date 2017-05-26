//-----------------------------------------------------------------------
// <copyright file="IResource.cs" company="Mapbox">
//     Copyright (c) 2016 Mapbox. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Mapbox.Platform
{
	using System;

	/// <summary> 
	/// Interface representing a Mapbox resource URL. Used to build request strings
	/// and return full URLs to a Mapbox Web Service API resource. 
	/// </summary>
	public interface IResource
	{
		/// <summary>Builds a complete, valid URL string.</summary>
		/// <returns>URL string.</returns>
		string GetUrl();
	}
}
