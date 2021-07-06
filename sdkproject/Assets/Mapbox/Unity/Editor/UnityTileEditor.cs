using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mapbox.Map;
using Mapbox.Unity.MeshGeneration.Data;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(UnityTile))]
public class UnityTileEditor : Editor
{
	private UnityTile _tile;
	private Tile _openDataTile;

	private bool _errorFold;

	private bool _tilesFold;

	//SerializedProperty lookAtPoint;
	private bool[] _tileTabs;

	void OnEnable()
	{
		//lookAtPoint = serializedObject.FindProperty("lookAtPoint");
		_tile = (UnityTile) target;
		_tileTabs = new bool[10];
	}

	public override void OnInspectorGUI()
	{
		serializedObject.Update();

		var zoom = _tile.CurrentZoom;

		var dataErrors = new List<string>();
		var dataTileDictionary = new Dictionary<Type, Tuple<int, int>>();
		foreach (var tile in _tile.Tiles)
		{
			if (tile.HasError)
			{
				foreach (var exception in tile.Exceptions)
				{
					dataErrors.Add(exception.ToString());
				}
			}

			var type = tile.GetType();
			var isFromCacheAdd = (tile.FromCache == CacheType.NoCache) ? 1 : 0;
			if (!dataTileDictionary.ContainsKey(type))
			{
				dataTileDictionary.Add(type, new Tuple<int, int>(1, isFromCacheAdd));
			}
			else
			{
				dataTileDictionary[type] = new Tuple<int, int>(dataTileDictionary[type].Item1 + 1, dataTileDictionary[type].Item2 + isFromCacheAdd);
			}
		}

		if (dataErrors.Count > 0)
		{
			_errorFold = EditorGUILayout.Foldout(_errorFold, "Errors");
			foreach (var error in dataErrors)
			{
				EditorGUILayout.LabelField(error, EditorStyles.miniLabel);
			}
		}

		EditorGUILayout.LabelField("Tile Id", _tile.CanonicalTileId.ToString(), EditorStyles.label);
		EditorGUILayout.LabelField("Tile Scale", _tile.TileScale.ToString(), EditorStyles.label);

		_tilesFold = EditorGUILayout.Foldout(_tilesFold, string.Format("{0} Data Tiles", _tile.Tiles.Count));
		if (_tilesFold)
		{
			EditorGUI.indentLevel++;
			foreach (var entry in dataTileDictionary)
			{
				GUILayout.Label(string.Format("{0}({1}) {2}", entry.Value.Item1, entry.Value.Item2, entry.Key), EditorStyles.miniLabel);
			}

			var index = 0;
			foreach (var dataTile in _tile.Tiles)
			{
				_tileTabs[index] = EditorGUILayout.Foldout(_tileTabs[index], dataTile.GetType().ToString());
				if (_tileTabs[index])
				{
					EditorGUI.indentLevel++;
					_openDataTile = dataTile;
					if (dataTile is RasterTile)
					{
						if ((dataTile as RasterTile).Texture2D != null)
						{
							EditorGUILayout.ObjectField(
								(dataTile as RasterTile).Texture2D,
								typeof(Texture2D));
						}
					}

					EditorGUILayout.LabelField(string.Format("{0} : {1}", "Tile Id", dataTile.Id), EditorStyles.label);
					EditorGUILayout.LabelField(string.Format("{0} : {1}", "Tileset", dataTile.TilesetId), EditorStyles.label);
					EditorGUILayout.LabelField(string.Format("{0} : {1}", "From", dataTile.FromCache), EditorStyles.label);
					EditorGUILayout.LabelField(string.Format("{0} : {1}", "State", dataTile.CurrentTileState), EditorStyles.label);
					EditorGUILayout.LabelField(string.Format("{0} : {1}", "ETag", dataTile.ETag), EditorStyles.label);
					EditorGUILayout.LabelField(string.Format("Is Mapbox : {0}", dataTile.IsMapboxTile), EditorStyles.label);

					if (dataTile.HasError)
					{
						EditorGUILayout.LabelField(string.Format("Error : {0}", dataTile.Exceptions[0].Message), EditorStyles.label);
					}

					EditorGUI.indentLevel--;
				}

				index++;
			}

			EditorGUI.indentLevel--;
		}

		serializedObject.ApplyModifiedProperties();
	}
}