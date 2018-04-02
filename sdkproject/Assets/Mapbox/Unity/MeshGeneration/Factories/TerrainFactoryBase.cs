using Mapbox.Unity.MeshGeneration.Factories;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mapbox.Unity.MeshGeneration.Data;
using System;
using Mapbox.Unity.Map;
using Mapbox.Map;
using Mapbox.Utils;
using Mapbox.Unity.MeshGeneration.Enums;
using System.Collections.ObjectModel;
using Mapbox.Unity.Utilities;

public class TerrainFactoryBase : AbstractTileFactory
{
	public TerrainStrategy Strategy;
	[SerializeField]
	protected ElevationLayerProperties _elevationOptions = new ElevationLayerProperties();

	public override void SetOptions(LayerProperties options)
	{
		_elevationOptions = (ElevationLayerProperties)options;
	}

	internal override void OnInitialized()
	{
		Strategy.OnInitialized(_elevationOptions);
	}

	internal override void OnRegistered(UnityTile tile)
	{
		tile.HeightDataState = TilePropertyState.Loading;
		var pngRasterTile = new RawPngRasterTile();

		tile.AddTile(pngRasterTile);
		Progress++;

		pngRasterTile.Initialize(_fileSource, tile.CanonicalTileId, _elevationOptions.sourceOptions.Id, () =>
		{
			if (tile == null)
			{
				Progress--;
				return;
			}

			if (pngRasterTile.HasError)
			{
				OnErrorOccurred(new TileErrorEventArgs(tile.CanonicalTileId, pngRasterTile.GetType(), tile, pngRasterTile.Exceptions));
				tile.HeightDataState = TilePropertyState.Error;

				// Handle missing elevation from server (404)!
				// TODO: optimize this search!
				if (pngRasterTile.ExceptionsAsString.Contains("404"))
				{
					Strategy.OnFetchingError(pngRasterTile.Exceptions);
					//ResetToFlatMesh(tile);
				}
				Progress--;
				return;
			}

			tile.SetHeightData(pngRasterTile.Data, _elevationOptions.requiredOptions.exaggerationFactor, _elevationOptions.modificationOptions.useRelativeHeight);
			Strategy.OnRegistered(tile);
			//GenerateTerrainMesh(tile);
			Progress--;
		});
	}

	internal override void OnUnregistered(UnityTile tile)
	{
		Strategy.OnUnregistered(tile);
	}
}

public class TerrainStrategy
{
	[SerializeField]
	protected ElevationLayerProperties _elevationOptions = new ElevationLayerProperties();

	public virtual void OnInitialized(ElevationLayerProperties elOptions)
	{
		_elevationOptions = elOptions;
	}

	public virtual void OnRegistered(UnityTile tile)
	{

	}

	public virtual void OnUnregistered(UnityTile tile)
	{

	}

	internal void OnFetchingError(ReadOnlyCollection<Exception> exceptions)
	{

	}
}

public class ElevatedTerrainStrategy : TerrainStrategy
{
	Mesh _stitchTarget;

	protected Dictionary<UnwrappedTileId, Mesh> _meshData;
	private MeshData _currentTileMeshData;
	private MeshData _stitchTargetMeshData;

	private List<Vector3> _newVertexList;
	private List<Vector3> _newNormalList;
	private List<Vector2> _newUvList;
	private List<int> _newTriangleList;
	private Vector3 _newDir;
	private int _vertA, _vertB, _vertC;
	private int _counter;

	public override void OnInitialized(ElevationLayerProperties elOptions)
	{
		base.OnInitialized(elOptions);

		_meshData = new Dictionary<UnwrappedTileId, Mesh>();
		_currentTileMeshData = new MeshData();
		_stitchTargetMeshData = new MeshData();
		var sampleCountSquare = _elevationOptions.modificationOptions.sampleCount * _elevationOptions.modificationOptions.sampleCount;
		_newVertexList = new List<Vector3>(sampleCountSquare);
		_newNormalList = new List<Vector3>(sampleCountSquare);
		_newUvList = new List<Vector2>(sampleCountSquare);
		_newTriangleList = new List<int>();
	}

	public override void OnRegistered(UnityTile tile)
	{
		if (_elevationOptions.unityLayerOptions.addToLayer && tile.gameObject.layer != _elevationOptions.unityLayerOptions.layerId)
		{
			tile.gameObject.layer = _elevationOptions.unityLayerOptions.layerId;
		}

		if (tile.MeshRenderer == null)
		{
			var renderer = tile.gameObject.AddComponent<MeshRenderer>();
			renderer.material = _elevationOptions.requiredOptions.baseMaterial;
		}

		if (tile.MeshFilter == null)
		{
			tile.gameObject.AddComponent<MeshFilter>();
			CreateBaseMesh(tile);
		}

		if (_elevationOptions.requiredOptions.addCollider && tile.Collider == null)
		{
			tile.gameObject.AddComponent<MeshCollider>();
		}

		GenerateTerrainMesh(tile);
	}

	public override void OnUnregistered(UnityTile tile)
	{
		_meshData.Remove(tile.UnwrappedTileId);
	}

	//public override void OnErrorOccurred(TileErrorEventArgs e)
	//{
	//	base.OnErrorOccurred(e);
	//}

	#region mesh gen
	private void CreateBaseMesh(UnityTile tile)
	{
		//TODO use arrays instead of lists
		_newVertexList.Clear();
		_newNormalList.Clear();
		_newUvList.Clear();
		_newTriangleList.Clear();

		var _sampleCount = _elevationOptions.modificationOptions.sampleCount;
		for (float y = 0; y < _sampleCount; y++)
		{
			var yrat = y / (_sampleCount - 1);
			for (float x = 0; x < _sampleCount; x++)
			{
				var xrat = x / (_sampleCount - 1);

				var xx = Mathd.Lerp(tile.Rect.Min.x, tile.Rect.Max.x, xrat);
				var yy = Mathd.Lerp(tile.Rect.Min.y, tile.Rect.Max.y, yrat);

				_newVertexList.Add(new Vector3(
					(float)(xx - tile.Rect.Center.x) * tile.TileScale,
					0,
					(float)(yy - tile.Rect.Center.y) * tile.TileScale));
				_newNormalList.Add(Mapbox.Unity.Constants.Math.Vector3Up);
				_newUvList.Add(new Vector2(x * 1f / (_sampleCount - 1), 1 - (y * 1f / (_sampleCount - 1))));
			}
		}

		int vertA, vertB, vertC;
		for (int y = 0; y < _sampleCount - 1; y++)
		{
			for (int x = 0; x < _sampleCount - 1; x++)
			{
				vertA = (y * _sampleCount) + x;
				vertB = (y * _sampleCount) + x + _sampleCount + 1;
				vertC = (y * _sampleCount) + x + _sampleCount;
				_newTriangleList.Add(vertA);
				_newTriangleList.Add(vertB);
				_newTriangleList.Add(vertC);

				vertA = (y * _sampleCount) + x;
				vertB = (y * _sampleCount) + x + 1;
				vertC = (y * _sampleCount) + x + _sampleCount + 1;
				_newTriangleList.Add(vertA);
				_newTriangleList.Add(vertB);
				_newTriangleList.Add(vertC);
			}
		}
		var mesh = tile.MeshFilter.mesh;
		mesh.SetVertices(_newVertexList);
		mesh.SetNormals(_newNormalList);
		mesh.SetUVs(0, _newUvList);
		mesh.SetTriangles(_newTriangleList, 0);
	}

	private void GenerateTerrainMesh(UnityTile tile)
	{
		tile.MeshFilter.mesh.GetVertices(_currentTileMeshData.Vertices);
		tile.MeshFilter.mesh.GetNormals(_currentTileMeshData.Normals);

		var _sampleCount = _elevationOptions.modificationOptions.sampleCount;
		for (float y = 0; y < _sampleCount; y++)
		{
			for (float x = 0; x < _sampleCount; x++)
			{
				_currentTileMeshData.Vertices[(int)(y * _sampleCount + x)] = new Vector3(
					_currentTileMeshData.Vertices[(int)(y * _sampleCount + x)].x,
					tile.QueryHeightData(x / (_sampleCount - 1), 1 - y / (_sampleCount - 1)),
					_currentTileMeshData.Vertices[(int)(y * _sampleCount + x)].z);
				_currentTileMeshData.Normals[(int)(y * _sampleCount + x)] = Mapbox.Unity.Constants.Math.Vector3Zero;
			}
		}

		tile.MeshFilter.mesh.SetVertices(_currentTileMeshData.Vertices);

		for (int y = 0; y < _sampleCount - 1; y++)
		{
			for (int x = 0; x < _sampleCount - 1; x++)
			{
				_vertA = (y * _sampleCount) + x;
				_vertB = (y * _sampleCount) + x + _sampleCount + 1;
				_vertC = (y * _sampleCount) + x + _sampleCount;
				_newDir = Vector3.Cross(_currentTileMeshData.Vertices[_vertB] - _currentTileMeshData.Vertices[_vertA], _currentTileMeshData.Vertices[_vertC] - _currentTileMeshData.Vertices[_vertA]);
				_currentTileMeshData.Normals[_vertA] += _newDir;
				_currentTileMeshData.Normals[_vertB] += _newDir;
				_currentTileMeshData.Normals[_vertC] += _newDir;

				_vertA = (y * _sampleCount) + x;
				_vertB = (y * _sampleCount) + x + 1;
				_vertC = (y * _sampleCount) + x + _sampleCount + 1;
				_newDir = Vector3.Cross(_currentTileMeshData.Vertices[_vertB] - _currentTileMeshData.Vertices[_vertA], _currentTileMeshData.Vertices[_vertC] - _currentTileMeshData.Vertices[_vertA]);
				_currentTileMeshData.Normals[_vertA] += _newDir;
				_currentTileMeshData.Normals[_vertB] += _newDir;
				_currentTileMeshData.Normals[_vertC] += _newDir;
			}
		}

		FixStitches(tile.UnwrappedTileId, _currentTileMeshData);

		tile.MeshFilter.mesh.SetNormals(_currentTileMeshData.Normals);
		tile.MeshFilter.mesh.SetVertices(_currentTileMeshData.Vertices);

		tile.MeshFilter.mesh.RecalculateBounds();

		if (!_meshData.ContainsKey(tile.UnwrappedTileId))
		{
			_meshData.Add(tile.UnwrappedTileId, tile.MeshFilter.mesh);
		}

		if (_elevationOptions.requiredOptions.addCollider)
		{
			var meshCollider = tile.Collider as MeshCollider;
			if (meshCollider)
			{
				meshCollider.sharedMesh = tile.MeshFilter.mesh;
			}
		}
	}

	private void ResetToFlatMesh(UnityTile tile)
	{
		tile.MeshFilter.mesh.GetVertices(_currentTileMeshData.Vertices);
		tile.MeshFilter.mesh.GetNormals(_currentTileMeshData.Normals);

		_counter = _currentTileMeshData.Vertices.Count;
		for (int i = 0; i < _counter; i++)
		{
			_currentTileMeshData.Vertices[i] = new Vector3(
				_currentTileMeshData.Vertices[i].x,
				0,
				_currentTileMeshData.Vertices[i].z);
			_currentTileMeshData.Normals[i] = Mapbox.Unity.Constants.Math.Vector3Up;
		}

		tile.MeshFilter.mesh.SetVertices(_currentTileMeshData.Vertices);
		tile.MeshFilter.mesh.SetNormals(_currentTileMeshData.Normals);

		tile.MeshFilter.mesh.RecalculateBounds();
	}

	/// <summary>
	/// Checkes all neighbours of the given tile and stitches the edges to achieve a smooth mesh surface.
	/// </summary>
	/// <param name="tileId">UnwrappedTileId of the tile being processed.</param>
	/// <param name="mesh"></param>
	private void FixStitches(UnwrappedTileId tileId, MeshData mesh)
	{
		var _sampleCount = _elevationOptions.modificationOptions.sampleCount;
		var meshVertCount = mesh.Vertices.Count;
		_stitchTarget = null;
		_meshData.TryGetValue(tileId.North, out _stitchTarget);
		if (_stitchTarget != null)
		{
			_stitchTarget.GetVertices(_stitchTargetMeshData.Vertices);
			_stitchTarget.GetNormals(_stitchTargetMeshData.Normals);

			for (int i = 0; i < _sampleCount; i++)
			{
				//just snapping the y because vertex pos is relative and we'll have to do tile pos + vertex pos for x&z otherwise
				mesh.Vertices[i] = new Vector3(
					mesh.Vertices[i].x,
					_stitchTargetMeshData.Vertices[meshVertCount - _sampleCount + i].y,
					mesh.Vertices[i].z);

				mesh.Normals[i] = new Vector3(_stitchTargetMeshData.Normals[meshVertCount - _sampleCount + i].x,
					_stitchTargetMeshData.Normals[meshVertCount - _sampleCount + i].y,
					_stitchTargetMeshData.Normals[meshVertCount - _sampleCount + i].z);
			}
		}

		_stitchTarget = null;
		_meshData.TryGetValue(tileId.South, out _stitchTarget);
		if (_stitchTarget != null)
		{
			_stitchTarget.GetVertices(_stitchTargetMeshData.Vertices);
			_stitchTarget.GetNormals(_stitchTargetMeshData.Normals);

			for (int i = 0; i < _sampleCount; i++)
			{
				mesh.Vertices[meshVertCount - _sampleCount + i] = new Vector3(
					mesh.Vertices[meshVertCount - _sampleCount + i].x,
					_stitchTargetMeshData.Vertices[i].y,
					mesh.Vertices[meshVertCount - _sampleCount + i].z);

				mesh.Normals[meshVertCount - _sampleCount + i] = new Vector3(
					_stitchTargetMeshData.Normals[i].x,
					_stitchTargetMeshData.Normals[i].y,
					_stitchTargetMeshData.Normals[i].z);
			}
		}

		_stitchTarget = null;
		_meshData.TryGetValue(tileId.West, out _stitchTarget);
		if (_stitchTarget != null)
		{
			_stitchTarget.GetVertices(_stitchTargetMeshData.Vertices);
			_stitchTarget.GetNormals(_stitchTargetMeshData.Normals);

			for (int i = 0; i < _sampleCount; i++)
			{
				mesh.Vertices[i * _sampleCount] = new Vector3(
					mesh.Vertices[i * _sampleCount].x,
					_stitchTargetMeshData.Vertices[i * _sampleCount + _sampleCount - 1].y,
					mesh.Vertices[i * _sampleCount].z);

				mesh.Normals[i * _sampleCount] = new Vector3(
					_stitchTargetMeshData.Normals[i * _sampleCount + _sampleCount - 1].x,
					_stitchTargetMeshData.Normals[i * _sampleCount + _sampleCount - 1].y,
					_stitchTargetMeshData.Normals[i * _sampleCount + _sampleCount - 1].z);
			}
		}

		_stitchTarget = null;
		_meshData.TryGetValue(tileId.East, out _stitchTarget);

		if (_stitchTarget != null)
		{
			_stitchTarget.GetVertices(_stitchTargetMeshData.Vertices);
			_stitchTarget.GetNormals(_stitchTargetMeshData.Normals);

			for (int i = 0; i < _sampleCount; i++)
			{
				mesh.Vertices[i * _sampleCount + _sampleCount - 1] = new Vector3(
					mesh.Vertices[i * _sampleCount + _sampleCount - 1].x,
					_stitchTargetMeshData.Vertices[i * _sampleCount].y,
					mesh.Vertices[i * _sampleCount + _sampleCount - 1].z);

				mesh.Normals[i * _sampleCount + _sampleCount - 1] = new Vector3(
					_stitchTargetMeshData.Normals[i * _sampleCount].x,
					_stitchTargetMeshData.Normals[i * _sampleCount].y,
					_stitchTargetMeshData.Normals[i * _sampleCount].z);
			}
		}

		_stitchTarget = null;
		_meshData.TryGetValue(tileId.NorthWest, out _stitchTarget);

		if (_stitchTarget != null)
		{
			_stitchTarget.GetVertices(_stitchTargetMeshData.Vertices);
			_stitchTarget.GetNormals(_stitchTargetMeshData.Normals);

			mesh.Vertices[0] = new Vector3(
				mesh.Vertices[0].x,
				_stitchTargetMeshData.Vertices[meshVertCount - 1].y,
				mesh.Vertices[0].z);

			mesh.Normals[0] = new Vector3(
				_stitchTargetMeshData.Normals[meshVertCount - 1].x,
				_stitchTargetMeshData.Normals[meshVertCount - 1].y,
				_stitchTargetMeshData.Normals[meshVertCount - 1].z);
		}

		_stitchTarget = null;
		_meshData.TryGetValue(tileId.NorthEast, out _stitchTarget);

		if (_stitchTarget != null)
		{
			_stitchTarget.GetVertices(_stitchTargetMeshData.Vertices);
			_stitchTarget.GetNormals(_stitchTargetMeshData.Normals);

			mesh.Vertices[_sampleCount - 1] = new Vector3(
				mesh.Vertices[_sampleCount - 1].x,
				_stitchTargetMeshData.Vertices[meshVertCount - _sampleCount].y,
				mesh.Vertices[_sampleCount - 1].z);

			mesh.Normals[_sampleCount - 1] = new Vector3(
				_stitchTargetMeshData.Normals[meshVertCount - _sampleCount].x,
				_stitchTargetMeshData.Normals[meshVertCount - _sampleCount].y,
				_stitchTargetMeshData.Normals[meshVertCount - _sampleCount].z);
		}

		_stitchTarget = null;
		_meshData.TryGetValue(tileId.SouthWest, out _stitchTarget);

		if (_stitchTarget != null)
		{
			_stitchTarget.GetVertices(_stitchTargetMeshData.Vertices);
			_stitchTarget.GetNormals(_stitchTargetMeshData.Normals);

			mesh.Vertices[meshVertCount - _sampleCount] = new Vector3(
				mesh.Vertices[meshVertCount - _sampleCount].x,
				_stitchTargetMeshData.Vertices[_sampleCount - 1].y,
				mesh.Vertices[meshVertCount - _sampleCount].z);

			mesh.Normals[meshVertCount - _sampleCount] = new Vector3(
				_stitchTargetMeshData.Normals[_sampleCount - 1].x,
				_stitchTargetMeshData.Normals[_sampleCount - 1].y,
				_stitchTargetMeshData.Normals[_sampleCount - 1].z);
		}

		_stitchTarget = null;
		_meshData.TryGetValue(tileId.SouthEast, out _stitchTarget);

		if (_stitchTarget != null)
		{
			_stitchTarget.GetVertices(_stitchTargetMeshData.Vertices);
			_stitchTarget.GetNormals(_stitchTargetMeshData.Normals);

			mesh.Vertices[meshVertCount - 1] = new Vector3(
				mesh.Vertices[meshVertCount - 1].x,
				_stitchTargetMeshData.Vertices[0].y,
				mesh.Vertices[meshVertCount - 1].z);

			mesh.Normals[meshVertCount - 1] = new Vector3(
				_stitchTargetMeshData.Normals[0].x,
				_stitchTargetMeshData.Normals[0].y,
				_stitchTargetMeshData.Normals[0].z);
		}
	}
	#endregion
}

public class FlatTerrainStrategy : TerrainStrategy
{
	Mesh _cachedQuad;

	public override void OnInitialized(ElevationLayerProperties elOptions)
	{
		_elevationOptions = elOptions;
	}

	public override void OnRegistered(UnityTile tile)
	{
		if (_elevationOptions.unityLayerOptions.addToLayer && tile.gameObject.layer != _elevationOptions.unityLayerOptions.layerId)
		{
			tile.gameObject.layer = _elevationOptions.unityLayerOptions.layerId;
		}

		if (tile.MeshRenderer == null)
		{
			var renderer = tile.gameObject.AddComponent<MeshRenderer>();

			if (_elevationOptions.sideWallOptions.isActive)
			{
				renderer.materials = new Material[2]
				{
						_elevationOptions.requiredOptions.baseMaterial,
						_elevationOptions.sideWallOptions.wallMaterial
				};
			}
			else
			{
				renderer.material = _elevationOptions.requiredOptions.baseMaterial;
			}
		}

		if (tile.MeshFilter == null)
		{
			tile.gameObject.AddComponent<MeshFilter>();
		}

		// HACK: This is here in to make the system trigger a finished state.
		tile.MeshFilter.sharedMesh = GetQuad(tile, _elevationOptions.sideWallOptions.isActive);

		if (_elevationOptions.requiredOptions.addCollider && tile.Collider == null)
		{
			tile.gameObject.AddComponent<BoxCollider>();
		}
	}

	private Mesh GetQuad(UnityTile tile, bool buildSide)
	{
		if (_cachedQuad != null)
		{
			return _cachedQuad;
		}

		return buildSide ? BuildQuadWithSides(tile) : BuildQuad(tile);
	}

	Mesh BuildQuad(UnityTile tile)
	{
		var unityMesh = new Mesh();
		var verts = new Vector3[4];
		var norms = new Vector3[4];
		verts[0] = tile.TileScale * ((tile.Rect.Min - tile.Rect.Center).ToVector3xz());
		verts[1] = tile.TileScale * (new Vector3((float)(tile.Rect.Max.x - tile.Rect.Center.x), 0, (float)(tile.Rect.Min.y - tile.Rect.Center.y)));
		verts[2] = tile.TileScale * ((tile.Rect.Max - tile.Rect.Center).ToVector3xz());
		verts[3] = tile.TileScale * (new Vector3((float)(tile.Rect.Min.x - tile.Rect.Center.x), 0, (float)(tile.Rect.Max.y - tile.Rect.Center.y)));
		norms[0] = Mapbox.Unity.Constants.Math.Vector3Up;
		norms[1] = Mapbox.Unity.Constants.Math.Vector3Up;
		norms[2] = Mapbox.Unity.Constants.Math.Vector3Up;
		norms[3] = Mapbox.Unity.Constants.Math.Vector3Up;

		unityMesh.vertices = verts;
		unityMesh.normals = norms;

		var trilist = new int[6] { 0, 1, 2, 0, 2, 3 };
		unityMesh.SetTriangles(trilist, 0);

		var uvlist = new Vector2[4]
		{
					new Vector2(0,1),
					new Vector2(1,1),
					new Vector2(1,0),
					new Vector2(0,0)
		};
		unityMesh.uv = uvlist;
		tile.MeshFilter.sharedMesh = unityMesh;
		_cachedQuad = unityMesh;

		return unityMesh;
	}

	private Mesh BuildQuadWithSides(UnityTile tile)
	{
		var unityMesh = new Mesh();
		var verts = new Vector3[20];
		var norms = new Vector3[20];
		verts[0] = tile.TileScale * ((tile.Rect.Min - tile.Rect.Center).ToVector3xz());
		verts[1] = tile.TileScale * (new Vector3((float)(tile.Rect.Max.x - tile.Rect.Center.x), 0, (float)(tile.Rect.Min.y - tile.Rect.Center.y)));
		verts[2] = tile.TileScale * ((tile.Rect.Max - tile.Rect.Center).ToVector3xz());
		verts[3] = tile.TileScale * (new Vector3((float)(tile.Rect.Min.x - tile.Rect.Center.x), 0, (float)(tile.Rect.Max.y - tile.Rect.Center.y)));
		norms[0] = Mapbox.Unity.Constants.Math.Vector3Up;
		norms[1] = Mapbox.Unity.Constants.Math.Vector3Up;
		norms[2] = Mapbox.Unity.Constants.Math.Vector3Up;
		norms[3] = Mapbox.Unity.Constants.Math.Vector3Up;

		//verts goes
		//01
		//32
		unityMesh.subMeshCount = 2;
		Vector3 norm = Mapbox.Unity.Constants.Math.Vector3Up;
		for (int i = 0; i < 4; i++)
		{
			verts[4 * (i + 1)] = verts[i];
			verts[4 * (i + 1) + 1] = verts[i + 1];
			verts[4 * (i + 1) + 2] = verts[i] + new Vector3(0, -_elevationOptions.sideWallOptions.wallHeight, 0);
			verts[4 * (i + 1) + 3] = verts[i + 1] + new Vector3(0, -_elevationOptions.sideWallOptions.wallHeight, 0);

			norm = Vector3.Cross(verts[4 * (i + 1) + 1] - verts[4 * (i + 1) + 2], verts[4 * (i + 1)] - verts[4 * (i + 1) + 1]).normalized;
			norms[4 * (i + 1)] = norm;
			norms[4 * (i + 1) + 1] = norm;
			norms[4 * (i + 1) + 2] = norm;
			norms[4 * (i + 1) + 3] = norm;
		}

		unityMesh.vertices = verts;
		unityMesh.normals = norms;

		var trilist = new List<int>(6) { 0, 1, 2, 0, 2, 3 };
		unityMesh.SetTriangles(trilist, 0);

		trilist = new List<int>(8);
		for (int i = 0; i < 4; i++)
		{
			trilist.Add(4 * (i + 1));
			trilist.Add(4 * (i + 1) + 2);
			trilist.Add(4 * (i + 1) + 1);

			trilist.Add(4 * (i + 1) + 1);
			trilist.Add(4 * (i + 1) + 2);
			trilist.Add(4 * (i + 1) + 3);
		}
		unityMesh.SetTriangles(trilist, 1);

		var uvlist = new Vector2[20];
		uvlist[0] = new Vector2(0, 1);
		uvlist[1] = new Vector2(1, 1);
		uvlist[2] = new Vector2(1, 0);
		uvlist[3] = new Vector2(0, 0);
		for (int i = 4; i < 20; i += 4)
		{
			uvlist[i] = new Vector2(1, 1);
			uvlist[i + 1] = new Vector2(0, 1);
			uvlist[i + 2] = new Vector2(1, 0);
			uvlist[i + 3] = new Vector2(0, 0);
		}
		unityMesh.uv = uvlist;
		tile.MeshFilter.sharedMesh = unityMesh;
		_cachedQuad = unityMesh;

		return unityMesh;
	}
}

public class LowPolyTerrainStrategy : TerrainStrategy
{
	protected Dictionary<UnwrappedTileId, Mesh> _meshData;
	private Mesh _stitchTarget;
	private MeshData _currentTileMeshData;
	private MeshData _stitchTargetMeshData;
	private List<Vector3> _newVertexList;
	private List<Vector3> _newNormalList;
	private List<Vector2> _newUvList;
	private List<int> _newTriangleList;
	private Vector3 _newDir;
	private int _vertA, _vertB, _vertC;
	private int _counter;


	public override void OnInitialized(ElevationLayerProperties elOptions)
	{
		_elevationOptions = elOptions;
		_meshData = new Dictionary<UnwrappedTileId, Mesh>();
		_currentTileMeshData = new MeshData();
		_stitchTargetMeshData = new MeshData();
		var sampleCountSquare = _elevationOptions.modificationOptions.sampleCount * _elevationOptions.modificationOptions.sampleCount;
		_newVertexList = new List<Vector3>(sampleCountSquare);
		_newNormalList = new List<Vector3>(sampleCountSquare);
		_newUvList = new List<Vector2>(sampleCountSquare);
		_newTriangleList = new List<int>();
	}

	public override void OnUnregistered(UnityTile tile)
	{
		_meshData.Remove(tile.UnwrappedTileId);
	}

	public override void OnRegistered(UnityTile tile)
	{
		if (_elevationOptions.unityLayerOptions.addToLayer && tile.gameObject.layer != _elevationOptions.unityLayerOptions.layerId)
		{
			tile.gameObject.layer = _elevationOptions.unityLayerOptions.layerId;
		}

		if (tile.MeshRenderer == null)
		{
			var renderer = tile.gameObject.AddComponent<MeshRenderer>();
			renderer.material = _elevationOptions.requiredOptions.baseMaterial;
		}

		if (tile.MeshFilter == null)
		{
			tile.gameObject.AddComponent<MeshFilter>();
			CreateBaseMesh(tile);
		}

		if (_elevationOptions.requiredOptions.addCollider && tile.Collider == null)
		{
			tile.gameObject.AddComponent<MeshCollider>();
		}

		GenerateTerrainMesh(tile);
	}

	private void CreateBaseMesh(UnityTile tile)
	{
		//TODO use arrays instead of lists
		_newVertexList.Clear();
		_newNormalList.Clear();
		_newUvList.Clear();
		_newTriangleList.Clear();

		var cap = (_elevationOptions.modificationOptions.sampleCount - 1);
		for (float y = 0; y < cap; y++)
		{
			for (float x = 0; x < cap; x++)
			{
				var x1 = tile.TileScale * (float)(Mathd.Lerp(tile.Rect.Min.x, tile.Rect.Max.x, x / cap) - tile.Rect.Center.x);
				var y1 = tile.TileScale * (float)(Mathd.Lerp(tile.Rect.Min.y, tile.Rect.Max.y, y / cap) - tile.Rect.Center.y);
				var x2 = tile.TileScale * (float)(Mathd.Lerp(tile.Rect.Min.x, tile.Rect.Max.x, (x + 1) / cap) - tile.Rect.Center.x);
				var y2 = tile.TileScale * (float)(Mathd.Lerp(tile.Rect.Min.y, tile.Rect.Max.y, (y + 1) / cap) - tile.Rect.Center.y);

				var triStart = _newVertexList.Count;
				_newVertexList.Add(new Vector3(x1, 0, y1));
				_newVertexList.Add(new Vector3(x2, 0, y1));
				_newVertexList.Add(new Vector3(x1, 0, y2));
				//--
				_newVertexList.Add(new Vector3(x2, 0, y1));
				_newVertexList.Add(new Vector3(x2, 0, y2));
				_newVertexList.Add(new Vector3(x1, 0, y2));

				_newNormalList.Add(Mapbox.Unity.Constants.Math.Vector3Up);
				_newNormalList.Add(Mapbox.Unity.Constants.Math.Vector3Up);
				_newNormalList.Add(Mapbox.Unity.Constants.Math.Vector3Up);
				//--
				_newNormalList.Add(Mapbox.Unity.Constants.Math.Vector3Up);
				_newNormalList.Add(Mapbox.Unity.Constants.Math.Vector3Up);
				_newNormalList.Add(Mapbox.Unity.Constants.Math.Vector3Up);


				_newUvList.Add(new Vector2(x / cap, 1 - y / cap));
				_newUvList.Add(new Vector2((x + 1) / cap, 1 - y / cap));
				_newUvList.Add(new Vector2(x / cap, 1 - (y + 1) / cap));
				//--
				_newUvList.Add(new Vector2((x + 1) / cap, 1 - y / cap));
				_newUvList.Add(new Vector2((x + 1) / cap, 1 - (y + 1) / cap));
				_newUvList.Add(new Vector2(x / cap, 1 - (y + 1) / cap));

				_newTriangleList.Add(triStart);
				_newTriangleList.Add(triStart + 1);
				_newTriangleList.Add(triStart + 2);
				//--
				_newTriangleList.Add(triStart + 3);
				_newTriangleList.Add(triStart + 4);
				_newTriangleList.Add(triStart + 5);
			}
		}


		var mesh = tile.MeshFilter.mesh;
		mesh.SetVertices(_newVertexList);
		mesh.SetNormals(_newNormalList);
		mesh.SetUVs(0, _newUvList);
		mesh.SetTriangles(_newTriangleList, 0);
		mesh.RecalculateBounds();
	}


	/// <summary>
	/// Creates the non-flat terrain mesh, using a grid by defined resolution (_sampleCount). Vertex order goes right & up. Normals are calculated manually and UV map is fitted/stretched 1-1.
	/// Any additional scripts or logic, like MeshCollider or setting layer, can be done here.
	/// </summary>
	/// <param name="tile"></param>
	// <param name="heightMultiplier">Multiplier for queried height value</param>
	private void GenerateTerrainMesh(UnityTile tile)
	{
		tile.MeshFilter.mesh.GetVertices(_currentTileMeshData.Vertices);
		tile.MeshFilter.mesh.GetNormals(_currentTileMeshData.Normals);

		var cap = (_elevationOptions.modificationOptions.sampleCount - 1);
		for (float y = 0; y < cap; y++)
		{
			for (float x = 0; x < cap; x++)
			{
				_currentTileMeshData.Vertices[(int)(y * cap + x) * 6] = new Vector3(
					_currentTileMeshData.Vertices[(int)(y * cap + x) * 6].x,
					tile.QueryHeightData(x / cap, 1 - y / cap),
					_currentTileMeshData.Vertices[(int)(y * cap + x) * 6].z);

				_currentTileMeshData.Vertices[(int)(y * cap + x) * 6 + 1] = new Vector3(
					_currentTileMeshData.Vertices[(int)(y * cap + x) * 6 + 1].x,
					tile.QueryHeightData((x + 1) / cap, 1 - y / cap),
					_currentTileMeshData.Vertices[(int)(y * cap + x) * 6 + 1].z);

				_currentTileMeshData.Vertices[(int)(y * cap + x) * 6 + 2] = new Vector3(
					_currentTileMeshData.Vertices[(int)(y * cap + x) * 6 + 2].x,
					tile.QueryHeightData(x / cap, 1 - (y + 1) / cap),
					_currentTileMeshData.Vertices[(int)(y * cap + x) * 6 + 2].z);

				//-- 

				_currentTileMeshData.Vertices[(int)(y * cap + x) * 6 + 3] = new Vector3(
					_currentTileMeshData.Vertices[(int)(y * cap + x) * 6 + 3].x,
					tile.QueryHeightData((x + 1) / cap, 1 - y / cap),
					_currentTileMeshData.Vertices[(int)(y * cap + x) * 6 + 3].z);

				_currentTileMeshData.Vertices[(int)(y * cap + x) * 6 + 4] = new Vector3(
					_currentTileMeshData.Vertices[(int)(y * cap + x) * 6 + 4].x,
					tile.QueryHeightData((x + 1) / cap, 1 - (y + 1) / cap),
					_currentTileMeshData.Vertices[(int)(y * cap + x) * 6 + 4].z);

				_currentTileMeshData.Vertices[(int)(y * cap + x) * 6 + 5] = new Vector3(
				   _currentTileMeshData.Vertices[(int)(y * cap + x) * 6 + 5].x,
				   tile.QueryHeightData(x / cap, 1 - (y + 1) / cap),
				   _currentTileMeshData.Vertices[(int)(y * cap + x) * 6 + 5].z);



				_newDir = Vector3.Cross(_currentTileMeshData.Vertices[(int)(y * cap + x) * 6 + 1] - _currentTileMeshData.Vertices[(int)(y * cap + x) * 6], _currentTileMeshData.Vertices[(int)(y * cap + x) * 6 + 2] - _currentTileMeshData.Vertices[(int)(y * cap + x) * 6]);
				_currentTileMeshData.Normals[(int)(y * cap + x) * 6 + 0] = _newDir;
				_currentTileMeshData.Normals[(int)(y * cap + x) * 6 + 1] = _newDir;
				_currentTileMeshData.Normals[(int)(y * cap + x) * 6 + 2] = _newDir;
				//--
				_newDir = Vector3.Cross(_currentTileMeshData.Vertices[(int)(y * cap + x) * 6 + 4] - _currentTileMeshData.Vertices[(int)(y * cap + x) * 6 + 3], _currentTileMeshData.Vertices[(int)(y * cap + x) * 6 + 5] - _currentTileMeshData.Vertices[(int)(y * cap + x) * 6 + 3]);
				_currentTileMeshData.Normals[(int)(y * cap + x) * 6 + 3] = _newDir;
				_currentTileMeshData.Normals[(int)(y * cap + x) * 6 + 4] = _newDir;
				_currentTileMeshData.Normals[(int)(y * cap + x) * 6 + 5] = _newDir;
			}
		}
		FixStitches(tile.UnwrappedTileId, _currentTileMeshData);
		tile.MeshFilter.mesh.SetVertices(_currentTileMeshData.Vertices);
		tile.MeshFilter.mesh.SetNormals(_currentTileMeshData.Normals);
		tile.MeshFilter.mesh.RecalculateBounds();

		if (!_meshData.ContainsKey(tile.UnwrappedTileId))
		{
			_meshData.Add(tile.UnwrappedTileId, tile.MeshFilter.mesh);
		}

		if (_elevationOptions.requiredOptions.addCollider)
		{
			var meshCollider = tile.Collider as MeshCollider;
			if (meshCollider)
			{
				meshCollider.sharedMesh = tile.MeshFilter.mesh;
			}
		}
	}

	private void ResetToFlatMesh(UnityTile tile)
	{
		tile.MeshFilter.mesh.GetVertices(_currentTileMeshData.Vertices);
		tile.MeshFilter.mesh.GetNormals(_currentTileMeshData.Normals);

		_counter = _currentTileMeshData.Vertices.Count;
		for (int i = 0; i < _counter; i++)
		{
			_currentTileMeshData.Vertices[i] = new Vector3(
				_currentTileMeshData.Vertices[i].x,
				0,
				_currentTileMeshData.Vertices[i].z);
			_currentTileMeshData.Normals[i] = Mapbox.Unity.Constants.Math.Vector3Up;
		}

		tile.MeshFilter.mesh.SetVertices(_currentTileMeshData.Vertices);
		tile.MeshFilter.mesh.SetNormals(_currentTileMeshData.Normals);
	}

	/// <summary>
	/// Checkes all neighbours of the given tile and stitches the edges to achieve a smooth mesh surface.
	/// </summary>
	/// <param name="tileId"></param>
	/// <param name="mesh"></param>
	private void FixStitches(UnwrappedTileId tileId, MeshData mesh)
	{
		var meshVertCount = mesh.Vertices.Count;
		_stitchTarget = null;
		_meshData.TryGetValue(tileId.North, out _stitchTarget);
		var cap = _elevationOptions.modificationOptions.sampleCount - 1;
		if (_stitchTarget != null)
		{
			_stitchTarget.GetVertices(_stitchTargetMeshData.Vertices);
			_stitchTarget.GetNormals(_stitchTargetMeshData.Normals);

			for (int i = 0; i < cap; i++)
			{
				mesh.Vertices[6 * i] = new Vector3(
					mesh.Vertices[6 * i].x,
					_stitchTargetMeshData.Vertices[6 * cap * (cap - 1) + 6 * i + 2].y,
					mesh.Vertices[6 * i].z);
				mesh.Vertices[6 * i + 1] = new Vector3(
					mesh.Vertices[6 * i + 1].x,
					_stitchTargetMeshData.Vertices[6 * cap * (cap - 1) + 6 * i + 4].y,
					mesh.Vertices[6 * i + 1].z);
				mesh.Vertices[6 * i + 3] = new Vector3(
					mesh.Vertices[6 * i + 3].x,
					_stitchTargetMeshData.Vertices[6 * cap * (cap - 1) + 6 * i + 4].y,
					mesh.Vertices[6 * i + 3].z);
			}
		}

		_stitchTarget = null;
		_meshData.TryGetValue(tileId.South, out _stitchTarget);
		if (_stitchTarget != null)
		{
			_stitchTarget.GetVertices(_stitchTargetMeshData.Vertices);
			_stitchTarget.GetNormals(_stitchTargetMeshData.Normals);

			for (int i = 0; i < cap; i++)
			{
				mesh.Vertices[6 * cap * (cap - 1) + 6 * i + 2] = new Vector3(
					mesh.Vertices[6 * cap * (cap - 1) + 6 * i + 2].x,
					_stitchTargetMeshData.Vertices[6 * i].y,
					mesh.Vertices[6 * cap * (cap - 1) + 6 * i + 2].z);
				mesh.Vertices[6 * cap * (cap - 1) + 6 * i + 5] = new Vector3(
					mesh.Vertices[6 * cap * (cap - 1) + 6 * i + 5].x,
					_stitchTargetMeshData.Vertices[6 * i].y,
					mesh.Vertices[6 * cap * (cap - 1) + 6 * i + 5].z);
				mesh.Vertices[6 * cap * (cap - 1) + 6 * i + 4] = new Vector3(
					mesh.Vertices[6 * cap * (cap - 1) + 6 * i + 4].x,
					_stitchTargetMeshData.Vertices[6 * i + 3].y,
					mesh.Vertices[6 * cap * (cap - 1) + 6 * i + 4].z);
			}
		}

		_stitchTarget = null;
		_meshData.TryGetValue(tileId.West, out _stitchTarget);
		if (_stitchTarget != null)
		{
			_stitchTarget.GetVertices(_stitchTargetMeshData.Vertices);
			_stitchTarget.GetNormals(_stitchTargetMeshData.Normals);

			for (int i = 0; i < cap; i++)
			{
				mesh.Vertices[6 * cap * i] = new Vector3(
					mesh.Vertices[6 * cap * i].x,
					_stitchTargetMeshData.Vertices[6 * cap * i + 6 * cap - 5].y,
					mesh.Vertices[6 * cap * i].z);

				mesh.Vertices[6 * cap * i + 2] = new Vector3(
					mesh.Vertices[6 * cap * i + 2].x,
					_stitchTargetMeshData.Vertices[6 * cap * i + 6 * cap - 2].y,
					mesh.Vertices[6 * cap * i + 2].z);

				mesh.Vertices[6 * cap * i + 5] = new Vector3(
					mesh.Vertices[6 * cap * i + 5].x,
					_stitchTargetMeshData.Vertices[6 * cap * i + 6 * cap - 2].y,
					mesh.Vertices[6 * cap * i + 5].z);
			}
		}

		_stitchTarget = null;
		_meshData.TryGetValue(tileId.East, out _stitchTarget);

		if (_stitchTarget != null)
		{
			_stitchTarget.GetVertices(_stitchTargetMeshData.Vertices);
			_stitchTarget.GetNormals(_stitchTargetMeshData.Normals);

			for (int i = 0; i < cap; i++)
			{
				mesh.Vertices[6 * cap * i + 6 * cap - 5] = new Vector3(
					mesh.Vertices[6 * cap * i + 6 * cap - 5].x,
					_stitchTargetMeshData.Vertices[6 * cap * i].y,
					mesh.Vertices[6 * cap * i + 6 * cap - 5].z);

				mesh.Vertices[6 * cap * i + 6 * cap - 3] = new Vector3(
					mesh.Vertices[6 * cap * i + 6 * cap - 3].x,
					_stitchTargetMeshData.Vertices[6 * cap * i].y,
					mesh.Vertices[6 * cap * i + 6 * cap - 3].z);

				mesh.Vertices[6 * cap * i + 6 * cap - 2] = new Vector3(
					mesh.Vertices[6 * cap * i + 6 * cap - 2].x,
					_stitchTargetMeshData.Vertices[6 * cap * i + 5].y,
					mesh.Vertices[6 * cap * i + 6 * cap - 2].z);
			}
		}

		_stitchTarget = null;
		_meshData.TryGetValue(tileId.NorthWest, out _stitchTarget);

		if (_stitchTarget != null)
		{
			_stitchTarget.GetVertices(_stitchTargetMeshData.Vertices);
			_stitchTarget.GetNormals(_stitchTargetMeshData.Normals);

			mesh.Vertices[0] = new Vector3(
				mesh.Vertices[0].x,
				_stitchTargetMeshData.Vertices[meshVertCount - 2].y,
				mesh.Vertices[0].z);
		}

		_stitchTarget = null;
		_meshData.TryGetValue(tileId.NorthEast, out _stitchTarget);

		if (_stitchTarget != null)
		{
			_stitchTarget.GetVertices(_stitchTargetMeshData.Vertices);
			_stitchTarget.GetNormals(_stitchTargetMeshData.Normals);

			mesh.Vertices[6 * cap - 5] = new Vector3(
				mesh.Vertices[6 * cap - 5].x,
				_stitchTargetMeshData.Vertices[6 * (cap - 1) * cap + 2].y,
				mesh.Vertices[6 * cap - 5].z);

			mesh.Vertices[6 * cap - 3] = new Vector3(
				mesh.Vertices[6 * cap - 3].x,
				_stitchTargetMeshData.Vertices[6 * (cap - 1) * cap + 2].y,
				mesh.Vertices[6 * cap - 3].z);
		}

		_stitchTarget = null;
		_meshData.TryGetValue(tileId.SouthWest, out _stitchTarget);

		if (_stitchTarget != null)
		{
			_stitchTarget.GetVertices(_stitchTargetMeshData.Vertices);
			_stitchTarget.GetNormals(_stitchTargetMeshData.Normals);

			mesh.Vertices[6 * (cap - 1) * cap + 2] = new Vector3(
				mesh.Vertices[6 * (cap - 1) * cap + 2].x,
				_stitchTargetMeshData.Vertices[6 * cap - 5].y,
				mesh.Vertices[6 * (cap - 1) * cap + 2].z);

			mesh.Vertices[6 * (cap - 1) * cap + 5] = new Vector3(
				mesh.Vertices[6 * (cap - 1) * cap + 5].x,
				_stitchTargetMeshData.Vertices[6 * cap - 5].y,
				mesh.Vertices[6 * (cap - 1) * cap + 5].z);
		}

		_stitchTarget = null;
		_meshData.TryGetValue(tileId.SouthEast, out _stitchTarget);

		if (_stitchTarget != null)
		{
			_stitchTarget.GetVertices(_stitchTargetMeshData.Vertices);
			_stitchTarget.GetNormals(_stitchTargetMeshData.Normals);

			mesh.Vertices[6 * cap * cap - 2] = new Vector3(
				mesh.Vertices[6 * cap * cap - 2].x,
				_stitchTargetMeshData.Vertices[0].y,
				mesh.Vertices[6 * cap * cap - 2].z);
		}
	}
}

public class FlatSphereTerrainStrategy : TerrainStrategy
{
	public float Radius { get { return _elevationOptions.modificationOptions.earthRadius; } }

	public override void OnInitialized(ElevationLayerProperties elOptions)
	{
		_elevationOptions = elOptions;
	}

	public override void OnRegistered(UnityTile tile)
	{
		if (_elevationOptions.unityLayerOptions.addToLayer && tile.gameObject.layer != _elevationOptions.unityLayerOptions.layerId)
		{
			tile.gameObject.layer = _elevationOptions.unityLayerOptions.layerId;
		}

		if (tile.MeshRenderer == null)
		{
			var renderer = tile.gameObject.AddComponent<MeshRenderer>();
			renderer.material = _elevationOptions.requiredOptions.baseMaterial;
		}

		if (tile.MeshFilter == null)
		{
			tile.gameObject.AddComponent<MeshFilter>();
		}

		GenerateTerrainMesh(tile);

		if (_elevationOptions.requiredOptions.addCollider && tile.Collider == null)
		{
			tile.gameObject.AddComponent<MeshCollider>();
		}
	}

	void GenerateTerrainMesh(UnityTile tile)
	{
		var verts = new List<Vector3>();
		var _sampleCount = _elevationOptions.modificationOptions.sampleCount;
		var _radius = _elevationOptions.modificationOptions.earthRadius;
		for (float x = 0; x < _sampleCount; x++)
		{
			for (float y = 0; y < _sampleCount; y++)
			{
				var xx = Mathf.Lerp((float)tile.Rect.Min.x, ((float)tile.Rect.Min.x + (float)tile.Rect.Size.x),
					x / (_sampleCount - 1));
				var yy = Mathf.Lerp((float)tile.Rect.Max.y, ((float)tile.Rect.Max.y + (float)tile.Rect.Size.y),
					y / (_sampleCount - 1));

				var ll = Conversions.MetersToLatLon(new Vector2d(xx, yy));

				var latitude = (float)(Mathf.Deg2Rad * ll.x);
				var longitude = (float)(Mathf.Deg2Rad * ll.y);

				float xPos = (_radius) * Mathf.Cos(latitude) * Mathf.Cos(longitude);
				float zPos = (_radius) * Mathf.Cos(latitude) * Mathf.Sin(longitude);
				float yPos = (_radius) * Mathf.Sin(latitude);

				var pp = new Vector3(xPos, yPos, zPos);
				verts.Add(pp);
			}
		}

		var trilist = new List<int>();
		for (int y = 0; y < _sampleCount - 1; y++)
		{
			for (int x = 0; x < _sampleCount - 1; x++)
			{
				trilist.Add((y * _sampleCount) + x);
				trilist.Add((y * _sampleCount) + x + _sampleCount + 1);
				trilist.Add((y * _sampleCount) + x + _sampleCount);

				trilist.Add((y * _sampleCount) + x);
				trilist.Add((y * _sampleCount) + x + 1);
				trilist.Add((y * _sampleCount) + x + _sampleCount + 1);
			}
		}

		var uvlist = new List<Vector2>();
		var step = 1f / (_sampleCount - 1);
		for (int i = 0; i < _sampleCount; i++)
		{
			for (int j = 0; j < _sampleCount; j++)
			{
				uvlist.Add(new Vector2(i * step, (j * step)));
			}
		}

		tile.MeshFilter.mesh.SetVertices(verts);
		tile.MeshFilter.mesh.SetTriangles(trilist, 0);
		tile.MeshFilter.mesh.SetUVs(0, uvlist);
		tile.MeshFilter.mesh.RecalculateBounds();
		tile.MeshFilter.mesh.RecalculateNormals();

		tile.transform.localPosition = Mapbox.Unity.Constants.Math.Vector3Zero;
	}

	public override void OnUnregistered(UnityTile tile)
	{

	}
}
