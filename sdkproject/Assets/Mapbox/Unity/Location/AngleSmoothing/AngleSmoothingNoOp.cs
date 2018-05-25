namespace Mapbox.Unity.Location
{


	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;
	using UnityEngine;


	public class AngleSmoothingNoOp : AngleSmoothingAbstractBase
	{


		public override double Calculate() { return _angles[0]; }
		

	}
}
