using System;
using System.Collections.Generic;
using UnityEngine;
using Mapbox.Unity.MeshGeneration.Data;

namespace Mapbox.Unity.MeshGeneration.Modifiers
{
	[CreateAssetMenu(menuName = "Mapbox/Modifiers/Smooth Line Modifier")]
	public class SmoothLineModifier : MeshModifier
	{
		public override ModifierType Type { get { return ModifierType.Preprocess; } }

		[SerializeField]
		private int _maxEdgeSectionCount = 40;
		[SerializeField]
		private int _preferredEdgeSectionLength = 10;
		[SerializeField]
		private bool _centerSegments = true;

		public override void Run(VectorFeatureUnity feature, MeshData md, UnityTile tile = null)
		{
			float dist = 0;
			float step = 0;
			float dif = 0;
			var start = Constants.Math.Vector3Zero;
			var dir = Constants.Math.Vector3Zero;

			for (int i = 0; i < feature.Points.Count; i++)
			{
				var nl = new List<Vector3>();
				for (int j = 1; j < feature.Points[i].Count; j++)
				{
					dist = Vector3.Distance(feature.Points[i][j - 1], feature.Points[i][j]);
					step = Math.Min(_maxEdgeSectionCount, dist / _preferredEdgeSectionLength);

					start = feature.Points[i][j - 1];
					dir = (feature.Points[i][j] - feature.Points[i][j - 1]).normalized;
					if (_centerSegments && step > 1)
					{
						dif = dist - ((int)step * _preferredEdgeSectionLength);
						//prevent new point being to close to existing corner
						if (dif > 2)
						{
							//first step, original point or another close point if sections are centered
							start = feature.Points[i][j - 1] + (dir * (dif / 2));
							//to compansate step-1 below, so if there's more than 2m to corner, go one more step
							step++;
						}
						nl.Add(start);

						if (step > 1)
						{
							for (int s = 1; s < step - 1; s++)
							{
								nl.Add(start + dir * s * _preferredEdgeSectionLength);
							}
						}

					}

					//last step
					nl.Add(feature.Points[i][j]);
				}
				feature.Points[i] = nl;
			}
		}
	}
}
