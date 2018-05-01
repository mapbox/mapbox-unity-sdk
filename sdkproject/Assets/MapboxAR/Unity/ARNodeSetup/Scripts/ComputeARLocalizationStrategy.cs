namespace Mapbox.Unity.Ar
{
	using UnityEngine;
	using System;

	public interface IComputeARLocalizationStrategy
	{
		event Action<Alignment> OnLocalizationComplete;
		void ComputeLocalization(CentralizedARLocator centralizedARLocator);
	}

	/// <summary>
	/// Abstract class to compute localization strategy.
	/// Create concrete implementations of this class to define custom ways of computing localization in AR. 
	/// </summary>
	public abstract class ComputeARLocalizationStrategy : MonoBehaviour, IComputeARLocalizationStrategy
	{
		public virtual event Action<Alignment> OnLocalizationComplete;

		public virtual void ComputeLocalization(CentralizedARLocator centralizedARLocator)
		{
			throw new NotImplementedException();
		}
	}
}
