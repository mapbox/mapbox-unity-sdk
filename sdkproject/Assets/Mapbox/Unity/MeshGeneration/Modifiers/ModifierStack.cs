using System.Threading;
using System.Threading.Tasks;
using Mapbox.Map;

namespace Mapbox.Unity.MeshGeneration.Modifiers
{
	using UnityEngine;
	using System.Collections.Generic;
	using Mapbox.Unity.MeshGeneration.Data;
	using Mapbox.Unity.MeshGeneration.Components;
	using System;

	public enum PositionTargetType
	{
		TileCenter,
		FirstVertex,
		CenterOfVertices
	}

	/// <summary>
	/// Modifier Stacks
	/// Modifier Stack can be thought as styles as as they contain all the data/settings for how the feature will be visualized.
	/// They also create the game objects in default implementations in the sdk.
	/// Currently there's two implementations of this; Modifier Stack and Merged Modifier Stack.They work almost exactly same
	/// (logically) with one difference; modifier stacks creates a game object for each feature while merged modifier stack,
	/// merges them up as the name suggest and create one game object for multiple(as many as possible) features.Both have
	/// their advantages but the main factor here is the performance.Regular modifier stack creates individual game object so
	/// it's easier to interact, move, animate etc features.But if you want to visualize whole San Francisco, that would mean
	/// just 200k-300k buildings which would hit performance really hard. In such a case, especially if you don't need
	/// individual interaction or something, you can use merged modifier stack, which will probably be able to create whole
	/// SF around a few hundred game objects.
	/// They contain two lists; mesh modifier list and game object modifier list.These modifiers are used to create and
	/// decorate game objects.
	/// Mesh modifiers generate data required for the game objects mesh. I.e.polygon mesh modifier triangulates the polygn,
	/// height modifier extrudes the polygon and adds volume etc, uv modifier changes UV mapping etc.
	/// Game object modifiers decorate created game objects, like settings material, interaction scripts, animations etc.
	/// i.e.Material modifier sets materials to mesh and submeshes, highlight modifier adds mouse highlight to features,
	/// feature behaviour adds a script to keep feature data on game objects etc.
	/// So the idea here is; run all mesh modifiers first, generate all the data required for mesh.Create game object
	/// using that mesh data.Run all game object modifiers to decorate that game object.
	/// </summary>
	[CreateAssetMenu(menuName = "Mapbox/Modifiers/Modifier Stack")]
	public class ModifierStack : ModifierStackBase
	{
		[SerializeField] public PositionTargetType moveFeaturePositionTo;

		public override void Initialize()
		{
			base.Initialize();

			var counter = MeshModifiers.Count;
			for (int i = 0; i < counter; i++)
			{
				MeshModifiers[i].Initialize();
			}

			counter = GoModifiers.Count;
			for (int i = 0; i < counter; i++)
			{
				GoModifiers[i].Initialize();
			}
		}

		public override MeshData RunMeshModifiers(UnityTile tile, VectorFeatureUnity feature, MeshData meshData, float scaler)
		{
			// var tempPoint = Constants.Math.Vector3Zero;
			// var counter = feature.Points.Count;
			// var secondCounter = 0;
			// if (moveFeaturePositionTo != PositionTargetType.TileCenter)
			// {
			//
			// 	if (moveFeaturePositionTo == PositionTargetType.FirstVertex)
			// 	{
			// 		tempPoint = feature.Points[0][0];
			// 	}
			// 	else if (moveFeaturePositionTo == PositionTargetType.CenterOfVertices)
			// 	{
			// 		//this is not precisely the center because of the duplicates  (first/last vertex) but close to center
			// 		tempPoint = feature.Points[0][0];
			// 		var vertexIndex = 1;
			//
			// 		for (int i = 0; i < counter; i++)
			// 		{
			// 			secondCounter = feature.Points[i].Count;
			// 			for (int j = 0; j < secondCounter; j++)
			// 			{
			// 				tempPoint += feature.Points[i][j];
			// 				vertexIndex++;
			// 			}
			// 		}
			//
			// 		tempPoint /= vertexIndex;
			// 	}
			//
			// 	for (int i = 0; i < counter; i++)
			// 	{
			// 		secondCounter = feature.Points[i].Count;
			// 		for (int j = 0; j < secondCounter; j++)
			// 		{
			// 			feature.Points[i][j] = new Vector3(feature.Points[i][j].x - tempPoint.x, 0, feature.Points[i][j].z - tempPoint.z);
			// 		}
			// 	}
			//
			// 	meshData.PositionInTile = tempPoint;
			// }

			//meshData.PositionInTile = tempPoint;
			var counter = MeshModifiers.Count;
			for (int i = 0; i < counter; i++)
			{
				if (MeshModifiers[i] != null && MeshModifiers[i].Active)
				{
					if (MeshModifiers[i] is ICoreWrapper)
					{
						(MeshModifiers[i] as ICoreWrapper).GetAsycCore().Run(feature, meshData, tile);
					}
					//MeshModifiers[i].Run(feature, meshData, tile);
				}
			}

			return meshData;
		}

		public override void RunGoModifiers(VectorEntity entity, UnityTile tile)
		{
			var counter = GoModifiers.Count;
			for (int i = 0; i < counter; i++)
			{
				if (GoModifiers[i].Active)
				{
					GoModifiers[i].Run(entity, tile);
				}
			}
		}

		public override void Clear()
		{

		}
	}
}
