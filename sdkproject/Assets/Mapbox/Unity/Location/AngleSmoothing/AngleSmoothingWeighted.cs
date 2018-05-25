namespace Mapbox.Unity.Location
{


	using UnityEngine;


	/// <summary>
	/// <para>NOT IMPLEMENTED YET</para>
	/// <para>Smooths angles via a weighted algorithm.</para>
	/// </summary>
	public class AngleSmoothingWeighted : AngleSmoothingAbstractBase
	{


		///// <summary>weights for calculating 'UserHeading'. hardcoded for now. TODO: auto-calc based on time, distance, ...</summary>
		//private float[] _headingWeights = new float[]{
		//	0,
		//	-0.5f,
		//	-1.0f,
		//	-1.5f
		//};


		public override double Calculate() {
			Debug.LogWarning("AngleSmoothingWeighted: not implemented");
			return 0.0;
		}


	}
}
