using System.Collections.Generic;
using Mapbox.BaseModule.Data.Tiles;
using Mapbox.BaseModule.Utilities;
using UnityEngine;
using UnityEngine.UI;

namespace Mapbox.ImageModule.Minimap.Unity
{
    /// <summary>
    /// Draws the minimap as pooled <see cref="RawImage"/>s under a UI <see cref="RectTransform"/>,
    /// one per active tile. Positions and sizes are taken as pixels.
    ///
    /// Clip the minimap to a shape (the usual circle) by putting a <c>Mask</c> or
    /// <c>RectMask2D</c> on an ancestor of the content root - this renderer does not care, and
    /// deliberately does not own the chrome around it.
    /// </summary>
    public class RawImageMinimapRenderer : IMinimapRenderer
    {
        private readonly RectTransform _contentRoot;
        private readonly ObjectPool<RawImage> _pool;
        private readonly Dictionary<UnwrappedTileId, RawImage> _images = new Dictionary<UnwrappedTileId, RawImage>();
        private readonly Dictionary<RawImage, RectTransform> _transforms = new Dictionary<RawImage, RectTransform>();
        private readonly RectTransform _orientationRoot;

        /// <param name="contentRoot">Parent for the per-tile images. Tiles position themselves
        /// relative to its centre.</param>
        /// <param name="orientationRoot">What gets rotated when the minimap is not north-up.
        /// Usually <paramref name="contentRoot"/>'s parent, so rotating the map does not rotate any
        /// frame or compass drawn around it. Null leaves rotation unapplied.</param>
        public RawImageMinimapRenderer(RectTransform contentRoot, RectTransform orientationRoot = null)
        {
            _contentRoot = contentRoot;
            _orientationRoot = orientationRoot;
            _pool = new ObjectPool<RawImage>(CreateImage);

            // Adopt any RawImages already authored under the root as the pool's initial stock,
            // so a hand-built prefab does not allocate a fresh set at runtime and leave its own
            // children sitting visible behind them.
            foreach (var existing in contentRoot.GetComponentsInChildren<RawImage>(true))
            {
                RegisterTransform(existing);
                Release(existing);
            }
        }

        private RawImage CreateImage()
        {
            var gameObject = new GameObject("MinimapTile");
            gameObject.transform.SetParent(_contentRoot, false);
            var image = gameObject.AddComponent<RawImage>();
            RegisterTransform(image);
            return image;
        }

        private void RegisterTransform(RawImage image)
        {
            var rectTransform = image.GetComponent<RectTransform>();

            // Centre-anchored so a tile's position is a plain offset from the minimap centre,
            // which is what the projection produces. Without this, anchors inherited from a
            // hand-authored prefab child would offset every tile by the root's own size.
            rectTransform.anchorMin = rectTransform.anchorMax = rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.localScale = Constants.Math.Vector3One;
            rectTransform.localRotation = Quaternion.identity;

            _transforms[image] = rectTransform;
        }

        private void Release(RawImage image)
        {
            // Tiles can unload during scene teardown, after the images themselves are destroyed.
            if (image == null)
            {
                return;
            }

            image.texture = null;
            image.uvRect = new Rect(0, 0, 1, 1);
            image.gameObject.SetActive(false);
            _pool.Put(image);
        }

        public void AddTile(UnwrappedTileId tileId)
        {
            if (_images.ContainsKey(tileId))
            {
                return;
            }

            var image = _pool.GetObject();
            image.gameObject.SetActive(true);
            _images[tileId] = image;
        }

        public void UpdateTile(UnwrappedTileId tileId, MinimapTileRect rect, MinimapTileTexture texture)
        {
            if (!_images.TryGetValue(tileId, out var image) || image == null)
            {
                return;
            }

            var rectTransform = _transforms[image];
            rectTransform.sizeDelta = new Vector2(rect.Size, rect.Size);
            rectTransform.localPosition = new Vector3(rect.Position.x, rect.Position.y, 0f);

            image.texture = texture.Texture;
            image.uvRect = texture.UvRect;

            // Disabled rather than removed while a tile has no data: it is a real active tile and
            // will get its texture within a frame or two, and dropping it would mean tearing the
            // image down and building it again for no reason.
            image.enabled = texture.HasTexture;
        }

        public void RemoveTile(UnwrappedTileId tileId)
        {
            if (_images.TryGetValue(tileId, out var image))
            {
                Release(image);
                _images.Remove(tileId);
            }
        }

        public void SetRootOffset(Vector2 offset)
        {
            _contentRoot.localPosition = new Vector3(offset.x, offset.y, 0f);
        }

        public void SetOrientation(float degrees)
        {
            if (_orientationRoot != null)
            {
                _orientationRoot.localRotation = Quaternion.Euler(0f, 0f, degrees);
            }
        }

        public void Clear()
        {
            foreach (var pair in _images)
            {
                Release(pair.Value);
            }
            _images.Clear();
        }
    }
}
