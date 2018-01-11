using Mapbox.Map;
using Mapbox.Unity;
using Mapbox.Utils;
using UnityEngine;

public class autorefreshmap : MonoBehaviour, Mapbox.Utils.IObserver<RasterTile>
{
	void Start()
	{
		var map = new Map<RasterTile>(MapboxAccess.Instance);
		map.Zoom = 2;
		map.Vector2dBounds = Vector2dBounds.World();
		map.MapId = "mapbox://styles/mapbox/streets-v10";
		map.Subscribe(this);
		map.Update();
	}

	public void OnNext(RasterTile tile)
	{
		if (tile.CurrentState == Tile.State.Loaded)
		{
			if (tile.HasError)
			{
				Debug.Log("RasterMap: " + tile.ExceptionsAsString);
				return;
			}

			var tileQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
			tileQuad.transform.SetParent(transform);
			tileQuad.name = tile.Id.ToString();
			tileQuad.transform.position = new Vector3(tile.Id.X, -tile.Id.Y, 0);
			var texture = new Texture2D(0, 0);
			texture.LoadImage(tile.Data);
			var material = new Material(Shader.Find("Unlit/Texture"));
			material.mainTexture = texture;
			tileQuad.GetComponent<MeshRenderer>().sharedMaterial = material;
		}
	}
}