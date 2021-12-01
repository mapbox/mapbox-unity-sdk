using UnityEngine;

namespace Mapbox.Unity.QuadTree
{
    public class CursorController : MonoBehaviour
    {
        public QuadTreeCameraController CameraController;
        public GameObject RotatingImage;
        public GameObject PanImage;

        private void Update()
        {
            if (Input.GetMouseButton(1))
            {
                Cursor.visible = false;
                RotatingImage.SetActive(true);
                RotatingImage.transform.position = Input.mousePosition;
            }
            else
            {
                RotatingImage.SetActive(false);
            }

            if (Input.GetMouseButton(0))
            {
                Cursor.visible = false;
                PanImage.SetActive(true);
                PanImage.transform.position = Input.mousePosition;
            }
            else
            {

                PanImage.SetActive(false);
            }

            if (!(Input.GetMouseButton(0) || Input.GetMouseButton(1)))
            {
                Cursor.visible = true;
            }
        }
    }
}
