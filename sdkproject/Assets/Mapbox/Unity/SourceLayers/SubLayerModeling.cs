using Mapbox.Unity.Map;

namespace Mapbox.Unity.SourceLayers
{
	public class SubLayerModeling : ISubLayerModeling
	{
		VectorSubLayerProperties _subLayerProperties;

		public SubLayerModeling(VectorSubLayerProperties subLayerProperties)
		{
			_subLayerProperties = subLayerProperties;
		}

		public ISubLayerCoreOptions CoreOptions
		{
			get { return _subLayerProperties.coreOptions; }
		}

		public ISubLayerExtrusionOptions ExtrusionOptions
		{
			get { return _subLayerProperties.extrusionOptions; }
		}

		public ISubLayerColliderOptions ColliderOptions
		{
			get { return _subLayerProperties.colliderOptions; }
		}

		public ISubLayerLineGeometryOptions LineOptions
		{
			get { return _subLayerProperties.lineGeometryOptions; }
		}

		/// <summary>
		/// Enable terrain snapping for features which sets vertices to terrain
		/// elevation before extrusion.
		/// </summary>
		/// <param name="isEnabled">Enabled terrain snapping</param>
		public virtual void EnableSnapingTerrain(bool isEnabled)
		{
			if (_subLayerProperties.coreOptions.snapToTerrain != isEnabled)
			{
				_subLayerProperties.coreOptions.snapToTerrain = isEnabled;
				_subLayerProperties.coreOptions.HasChanged = true;
			}
		}

		/// <summary>
		/// Enable combining individual features meshes into one to minimize gameobject
		/// count and draw calls.
		/// </summary>
		/// <param name="isEnabled"></param>
		public virtual void EnableCombiningMeshes(bool isEnabled)
		{
			if (_subLayerProperties.coreOptions.combineMeshes != isEnabled)
			{
				_subLayerProperties.coreOptions.combineMeshes = isEnabled;
				_subLayerProperties.coreOptions.HasChanged = true;
			}
		}


	}
}