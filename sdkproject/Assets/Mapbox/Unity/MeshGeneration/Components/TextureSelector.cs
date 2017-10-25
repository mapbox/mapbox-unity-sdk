namespace Mapbox.Unity.MeshGeneration.Components
{
    using UnityEngine;
    using Random = UnityEngine.Random;
    using Mapbox.Unity.MeshGeneration.Data;

    [RequireComponent(typeof(MeshRenderer))]
    public class TextureSelector : MonoBehaviour
    {
        private bool _useSatelliteRoof;
        private bool _textureTop;
        private bool _textureSides;

        private UnityTile _tile;
        private MeshRenderer _meshRenderer;
        private Material[] _topTextures;
        private Material[] _sideTextures;

        public void Initialize(VectorEntity ve, bool doTextureTop, bool useSatelliteRoof, Material[] topTextures, bool doTextureSides, Material[] sideTextures)
        {
            _useSatelliteRoof = useSatelliteRoof;
            _textureTop = doTextureTop;
            _textureSides = doTextureSides;

            _tile = GetComponent<UnityTile>();
            var t = transform;
            while (_tile == null && t.parent != null)
            {
                t = t.parent;
                _tile = t.GetComponent<UnityTile>();
            }
            _topTextures = topTextures;
            _sideTextures = sideTextures;
            _meshRenderer = GetComponent<MeshRenderer>();

            if (_textureSides && _sideTextures.Length > 0)
            {
                _meshRenderer.materials = new Material[2]
                {
                _topTextures[Random.Range(0, _topTextures.Length)],
                _sideTextures[Random.Range(0, _sideTextures.Length)]
                };
            }
            else if (_textureTop)
            {
                _meshRenderer.materials = new Material[1]
               {
                _topTextures[Random.Range(0, _topTextures.Length)]
               };
            }

            if (_useSatelliteRoof)
            {
				_meshRenderer.materials[0].mainTexture = _tile.GetRasterData();
                _meshRenderer.materials[0].mainTextureScale = new Vector2(1f, 1f);
            }
        }

        //private void TextureTop(Material topmat)
        //{
        //    if (_useSatelliteRoof)
        //    {
        //        topmat.mainTexture = _tile.ImageData;
        //        topmat.mainTextureScale = new Vector2(1f, 1f);
        //    }
        //    else
        //    {
        //        topmat.mainTextureScale = new Vector2(0.07f, 0.07f);
        //        if (_topTextures.Any())
        //            _meshRenderer.materials[0].SetTexture("_MainTex", );
        //    }

        //    var c = 1 - (Random.Range(0, 10) / 40f);
        //    _meshRenderer.materials[0].color = new Color(c, c, c);
        //}

        //private void TextureSides(Material sidemat)
        //{
        //    var c = 1 - (Random.Range(0, 10) / 40f);
        //    _meshRenderer.materials[1].color = new Color(c, c, c);

        //    if (_sideTextures.Any())
        //        _meshRenderer.materials[1].SetTexture("_MainTex", _sideTextures[Random.Range(0, _sideTextures.Length)]);

        //}

    }

}