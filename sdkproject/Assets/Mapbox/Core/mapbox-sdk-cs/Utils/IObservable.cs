//-----------------------------------------------------------------------
// <copyright file="IObservable.cs" company="Mapbox">
//     Copyright (c) 2016 Mapbox. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Mapbox.Utils
{
	/// <summary>
	///     An observable interface, similar to .NET 4.0 own IObservable.
	/// </summary>
	/// <typeparam name="T">
	///     The data being observed.
	/// </typeparam>
	public interface IObservable<T>
	{
		/// <summary> Add an <see cref="T:IObserver" /> to the observer list. </summary>
		/// <param name="observer"> The object subscribing to events. </param>
		void Subscribe(Mapbox.Utils.IObserver<T> observer);

		/// <summary> Remove an <see cref="T:IObserver" /> to the observer list. </summary>
		/// <param name="observer"> The object unsubscribing to events. </param>
        void Unsubscribe(Mapbox.Utils.IObserver<T> observer);
	}
}