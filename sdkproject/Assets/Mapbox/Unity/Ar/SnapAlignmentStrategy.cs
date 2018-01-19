namespace Mapbox.Unity.Ar
{
	using UnityEngine;

	public class SnapAlignmentStrategy : AbstractAlignmentStrategy
	{
		public override void OnAlignmentAvailable(Alignment alignment)
		{
			_transform.rotation = Quaternion.Euler(0, alignment.Rotation, 0);
			_transform.localPosition = alignment.Position;
		}
	}
}