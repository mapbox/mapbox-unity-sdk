namespace Mapbox.Unity.MeshGeneration.Modifiers
{
	using Mapbox.Unity.MeshGeneration.Data;
	using UnityEngine;
	using Mapbox.Unity.MeshGeneration.Components;
	using System.Collections.Generic;
	using System;

	[CreateAssetMenu(menuName = "Mapbox/Modifiers/Collider Modifier")]
	public class ColliderModifier : GameObjectModifier
	{
		[SerializeField]
		private ColliderType _colliderType;
		private IColliderStrategy _colliderStrategy;


		public override void Initialize()
		{
			//no need to reset strategy objects on map reinit as we're caching feature game objects as well
			//creating a new one iff we don't already have one. if you want to reset/recreate you have to clear stuff inside current/old one first.
			if (_colliderStrategy == null)
			{
				switch (_colliderType)
				{
					case ColliderType.None:
						_colliderStrategy = null;
						break;
					case ColliderType.BoxCollider:
						_colliderStrategy = new BoxColliderStrategy();
						break;
					case ColliderType.MeshCollider:
						_colliderStrategy = new MeshColliderStrategy();
						break;
					case ColliderType.SphereCollider:
						_colliderStrategy = new SphereColliderStrategy();
						break;
					default:
						_colliderStrategy = null;
						break;
				}
			}
		}

		public override void Run(VectorEntity ve, UnityTile tile)
		{
			if(_colliderStrategy != null)
			{
				_colliderStrategy.AddColliderTo(ve);
			}

		}

		public enum ColliderType
		{
			None,
			BoxCollider,
			MeshCollider,
			SphereCollider
		}

		public class BoxColliderStrategy : IColliderStrategy
		{
			private Dictionary<GameObject, BoxCollider> _colliders;

			public BoxColliderStrategy()
			{
				_colliders = new Dictionary<GameObject, BoxCollider>();
			}

			public void AddColliderTo(VectorEntity ve)
			{
				if (_colliders.ContainsKey(ve.GameObject))
				{
					_colliders[ve.GameObject].center = ve.Mesh.bounds.center;
					_colliders[ve.GameObject].size = ve.Mesh.bounds.size;
				}
				else
				{
					_colliders.Add(ve.GameObject, ve.GameObject.AddComponent<BoxCollider>());
				}
			}
		}

		public class MeshColliderStrategy : IColliderStrategy
		{
			private Dictionary<GameObject, MeshCollider> _colliders;

			public MeshColliderStrategy()
			{
				_colliders = new Dictionary<GameObject, MeshCollider>();
			}

			public void AddColliderTo(VectorEntity ve)
			{
				if (_colliders.ContainsKey(ve.GameObject))
				{
					_colliders[ve.GameObject].sharedMesh = ve.Mesh;
				}
				else
				{
					_colliders.Add(ve.GameObject, ve.GameObject.AddComponent<MeshCollider>());
				}
			}
		}

		public class SphereColliderStrategy : IColliderStrategy
		{
			private Dictionary<GameObject, SphereCollider> _colliders;

			public SphereColliderStrategy()
			{
				_colliders = new Dictionary<GameObject, SphereCollider>();
			}

			public void AddColliderTo(VectorEntity ve)
			{
				if (_colliders.ContainsKey(ve.GameObject))
				{
					_colliders[ve.GameObject].center = ve.Mesh.bounds.center;
					_colliders[ve.GameObject].radius = ve.Mesh.bounds.extents.magnitude;
				}
				else
				{
					_colliders.Add(ve.GameObject, ve.GameObject.AddComponent<SphereCollider>());
				}
			}
		}

		public interface IColliderStrategy
		{
			void AddColliderTo(VectorEntity ve);
		}
	}
}
