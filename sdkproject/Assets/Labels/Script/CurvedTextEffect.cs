using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class CurvedTextEffect : BaseMeshEffect
{
    public float YOffset = 0;
    private float xOffsetVector;

    private static Camera _camera;
    private float _totalLength;
    private RectTransform _rect;
    private List<Vector3> _originalLine;
    private List<Vector3> _screenLine;
    [SerializeField] private bool _invertLine = false;
    [SerializeField] private Text _text;
    private float _textMeshLength = -1;
    private float _textHalfLength;
    private List<UIVertex> _uiVertexList = new List<UIVertex>();

    public RectTransform Rect
    {
        get
        {
            if (_rect == null)
            {
                _rect = GetComponent<RectTransform>();
            }

            return _rect;
        }
    }

    public void InitializeLine(List<Vector3> line, float xOffset)
    {
        if (_camera == null)
        {
            _camera = Camera.main;
        }

        xOffsetVector = xOffset;
        _rect = GetComponent<RectTransform>();
        _originalLine = line;
        _screenLine = new List<Vector3>(line.Count);
        _totalLength = 0;
        for (int i = 0; i < line.Count; i++)
        {
            _screenLine.Add(_camera.WorldToScreenPoint(_originalLine[i]) / transform.lossyScale.x);

            if (i > 0)
            {
                _totalLength += Vector3.Distance(_screenLine[i], _screenLine[i - 1]);
            }
        }
        
    }

    public override void ModifyMesh(VertexHelper helper)
    {
        if (!IsActive())
            return;

        helper.GetUIVertexStream(_uiVertexList);
        Modify(_uiVertexList);

        helper.Clear();
        helper.AddUIVertexTriangleStream(_uiVertexList);
    }

    private Vector3 QueryLine(float distance)
    {
        if (_screenLine == null || _screenLine.Count == 0)
        {
            return new Vector3(distance, 0, 0);
        }

        distance += xOffsetVector * _totalLength;

        for (int i = 0; i < _screenLine.Count - 1; i++)
        {
            var d = Vector3.Distance(_screenLine[i], _screenLine[i + 1]);
            if (distance > d)
            {
                distance -= d;
            }
            else
            {
                var v = (_screenLine[i] + (_screenLine[i + 1] - _screenLine[i]).normalized * distance);
                return v - _screenLine[0];
            }
        }

        return Vector3.zero;
    }

    private void Modify(List<UIVertex> verts)
    {
        if (_textMeshLength == -1)
        {
            _textMeshLength = (verts[4].position - verts[verts.Count - 3].position).magnitude;
            _textHalfLength = _textMeshLength / 2;
        }

        var textSize = verts[verts.Count - 3].position.x;
        if (!_invertLine)
        {
            for (int i = 0; i < verts.Count; i += 6)
            {
                var topLeft = verts[i];
                var topRight = verts[i + 1];
                var bottomRight = verts[i + 2];
                var bottomLeft = verts[i + 4];

                float width = (bottomRight.position - bottomLeft.position).magnitude;
                float height = (topLeft.position - bottomLeft.position).magnitude;

                var dir = (QueryLine(bottomRight.position.x - _textHalfLength) -
                           QueryLine(bottomLeft.position.x - _textHalfLength)).normalized;
                var normal = new Vector3(-dir.y, dir.x, 0);
                var position = QueryLine(bottomLeft.position.x - _textHalfLength) +
                               (bottomLeft.position.y + YOffset) * normal;

                topLeft.position = position + normal * height;
                topRight.position = position + dir * width + normal * height;
                bottomRight.position = position + dir * width;
                bottomLeft.position = position;

                verts[i] = topLeft;
                verts[i + 1] = topRight;
                verts[i + 2] = bottomRight;
                verts[i + 3] = bottomRight;
                verts[i + 4] = bottomLeft;
                verts[i + 5] = topLeft;
            }
        }
        else
        {
            for (int i = 0; i < verts.Count; i += 6)
            {
                var topLeft = verts[i];
                var topRight = verts[i + 1];
                var bottomRight = verts[i + 2];
                var bottomLeft = verts[i + 4];

                float width = (bottomRight.position - bottomLeft.position).magnitude;
                float height = (topLeft.position - bottomLeft.position).magnitude;

                var dir = (QueryLine(_textMeshLength - bottomRight.position.x - _textHalfLength) -
                           QueryLine(_textMeshLength - bottomLeft.position.x - _textHalfLength)).normalized;
                var normal = new Vector3(-dir.y, dir.x, 0);
                var position = QueryLine(_textMeshLength - bottomLeft.position.x - _textHalfLength) +
                               (bottomLeft.position.y + YOffset) * normal;

                topLeft.position = position + normal * height;
                topRight.position = position + dir * width + normal * height;
                bottomRight.position = position + dir * width;
                bottomLeft.position = position;

                verts[i] = topLeft;
                verts[i + 1] = topRight;
                verts[i + 2] = bottomRight;
                verts[i + 3] = bottomRight;
                verts[i + 4] = bottomLeft;
                verts[i + 5] = topLeft;
            }
        }
    }

    public void CheckForUpdate()
    {
        var needsUpdate = false;
        var fpo = _camera.WorldToScreenPoint(_originalLine[0]) / transform.lossyScale.x;
        if (fpo != _screenLine[0])
        {
            _totalLength = 0;
            for (int i = 0; i < _originalLine.Count; i++)
            {
                _screenLine[i] = _camera.WorldToScreenPoint(_originalLine[i]) / transform.lossyScale.x;

                if (i > 0)
                {
                    _totalLength += Vector3.Distance(_screenLine[i], _screenLine[i - 1]);
                }
            }
            needsUpdate = true;
        }

        if ((_camera.WorldToScreenPoint(_originalLine[1]) - _camera.WorldToScreenPoint(_originalLine[0])).x < 0)
        {
            if (_invertLine == false)
            {
                _invertLine = true;
                needsUpdate = true;
            }
        }
        else
        {
            if (_invertLine == true)
            {
                _invertLine = false;
                needsUpdate = true;
            }
        }

        if (needsUpdate)
        {
            _text.SetVerticesDirty();
        }
    }
}