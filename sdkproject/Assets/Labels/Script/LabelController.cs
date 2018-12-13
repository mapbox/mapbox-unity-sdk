using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using KdTree;
using KdTree.Math;
using Mapbox.Examples;
using Mapbox.Map;
using Mapbox.Unity.MeshGeneration;
using Mapbox.Unity.MeshGeneration.Data;
using Mapbox.Unity.MeshGeneration.Modifiers;
using UnityEngine;
using UnityEngine.UI;

public class LabelAnchor
{
    public GameObject WorldAnchor;
    public GameObject Label;
    public float[] Key;
    public CurvedTextEffect CurvedTextEffect;
}

public class LabelController : MonoBehaviour
{
    [SerializeField] private KdTree<float, VectorEntity> _collection;
    public RoadLabels LabelModule;
    public Camera _camera;
    public GameObject LabelPrefab;
    public Transform LabelContainer;
    public Dictionary<VectorEntity, List<LabelAnchor>> _positions;

    private float _collisionRadius = 20;
    private TextGenerator _generator = new TextGenerator();
    private TextGenerationSettings _textSettings;
    
    private void Start()
    {
        _collection = new KdTree<float, VectorEntity>(2, new FloatMath());
        _positions = new Dictionary<VectorEntity, List<LabelAnchor>>();
        LabelModule.LabelAdded += AddLabel;
        LabelModule.LabelRemoved += RemoveLabel;
        _textSettings = LabelPrefab.GetComponentInChildren<Text>().GetGenerationSettings(new Vector2(0, 0));
    }

    public void AddLabel(VectorEntity ve, UnityTile tile)
    {
        foreach (var lineSegment in ve.Feature.Points)
        {
            var lineLength = 0f;
            for (int i = 0; i < lineSegment.Count - 1; i++)
            {
                lineLength += Vector3.Distance(lineSegment[i], lineSegment[i + 1]);
            }
            
            if (ve.Feature.Properties.ContainsKey("name"))
            {
                var text = ve.Feature.Properties["name"].ToString();
                _generator.Populate(text, _textSettings);
                var textLength = (_generator.verts[4].position - _generator.verts[_generator.verts.Count - 3].position).magnitude;

                var totalScreenLength = 0f;
                var totalWorldLength = 0f;
                for (int i = 0; i < lineSegment.Count; i++)
                {
                    if (i > 0)
                    {
                        totalScreenLength += Vector3.Distance(_camera.WorldToScreenPoint(lineSegment[i]) / transform.lossyScale.x, _camera.WorldToScreenPoint(lineSegment[i - 1]) / transform.lossyScale.x);
                        totalWorldLength += Vector3.Distance(lineSegment[i], lineSegment[i - 1]);
                    }
                }

                var perc = (textLength * 4) / totalScreenLength;
                var worldLabelWidth = totalWorldLength / (int)(1 / perc);

                var distance = worldLabelWidth/2;
                var lastCorner = 0f;
                for (int i = 0; i < lineSegment.Count - 1; i++)
                {
                    var d = Vector3.Distance(lineSegment[i], lineSegment[i + 1]);
                    if (distance > d)
                    {
                        distance -= d;
                        lastCorner += d;
                    }
                    else
                    {
                        float j = 0;
                        for (j = distance; j < d; j += worldLabelWidth)
                        {
                            var pos = tile.transform.position +
                                      (lineSegment[i] + (lineSegment[i + 1] - lineSegment[i]) *
                                       (j / d));

                            var key = new float[2] {pos.x, pos.z};
                            var neighbors = _collection.RadialSearch(key, _collisionRadius);
                            if (!neighbors.Any())
                            {
                                CreateLabel(ve, lineSegment, text, (lastCorner + j) / lineLength, tile, key);
                                _collection.Add(key, ve);
                            }
                        }

                        distance = j - d;
                        lastCorner += d;
                    }
                }
            }
        }
    }

    private void CreateLabel(VectorEntity ve, List<Vector3> line, string text, float xOffset, UnityTile tile, float[] key)
    {
        var go = new GameObject();
        go.transform.SetParent(tile.transform);
        go.transform.position = tile.transform.position + line[0];

        var label = Instantiate(LabelPrefab);
        label.transform.SetParent(LabelContainer);
        label.transform.localScale = Vector3.one;
        label.GetComponentInChildren<Text>().text = text;
        
        var textComp = label.GetComponentInChildren<CurvedTextEffect>();
        textComp.InitializeLine(line, xOffset);

        if (!_positions.ContainsKey(ve))
        {
            _positions.Add(ve, new List<LabelAnchor>());
        }

        _positions[ve].Add(new LabelAnchor()
        {
            WorldAnchor = go,
            Label = label,
            CurvedTextEffect = textComp,
            Key = key
        });
        
        label.transform.position = _camera.WorldToScreenPoint(go.transform.position);
    }

    private void RemoveLabel(VectorEntity ve)
    {
        if (_positions.ContainsKey(ve))
        {
            foreach (var labelAnchor in _positions[ve])
            {
                Destroy(labelAnchor.Label);
                Destroy(labelAnchor.WorldAnchor);
                _collection.RemoveAt(labelAnchor.Key);
            }
            _positions[ve].Clear();
            _positions.Remove(ve);
        }
    }

    public void Update()
    {
        foreach (var tuple in _positions)
        {
            foreach (var labelAnchor in tuple.Value)
            {
                labelAnchor.Label.transform.position = _camera.WorldToScreenPoint(labelAnchor.WorldAnchor.transform.position);
                labelAnchor.CurvedTextEffect.CheckForUpdate();
            }
        }
    }
}