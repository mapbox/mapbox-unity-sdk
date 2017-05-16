//-----------------------------------------------------------------------
// <copyright file="IObserver.cs" company="Mapbox">
//     Copyright (c) 2016 Mapbox. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Mapbox.Utils
{
	/// <summary>
	///     An observer interface, similar to .NET 4.0 own IObserver.
	/// </summary>
	/// <typeparam name="T">
	///     The data being observed.
	/// </typeparam>
	public interface IObserver<T>
	{
		/// <summary> The <see cref="T:Observable" /> has updated the data. </summary>
		/// <param name="next"> The data that has changed. </param>
		void OnNext(T next);
	}
}