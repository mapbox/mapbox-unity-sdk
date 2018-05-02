namespace Mapbox.Unity.Ar
{
	using UnityEngine;
	using System;

	public abstract class AbstractAlignmentStrategy : MonoBehaviour
	{
		[SerializeField]
		protected Transform _transform;
		public Action OnAlignmentApplicationComplete = delegate { };

		public void Register(ISynchronizationContext context)
		{
			context.OnAlignmentAvailable += OnAlignmentAvailable;
		}

		public void Unregister(ISynchronizationContext context)
		{
			context.OnAlignmentAvailable -= OnAlignmentAvailable;
		}

		public abstract void OnAlignmentAvailable(Alignment alignment);
	}
}