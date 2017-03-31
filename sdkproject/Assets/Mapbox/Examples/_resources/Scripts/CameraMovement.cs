namespace Mapbox.Examples
{
    using UnityEngine;

    public class CameraMovement : MonoBehaviour
    {
        private Transform _transform;
        public float Speed = 20;

        private Vector3 current_position;
        private Vector3 hit_position;
        private Vector3 camera_position;

        public void Start()
        {
            _transform = transform;
        }

        void Update()
        {
            if (Input.GetKey(KeyCode.A))
                _transform.Translate(-1 * Speed * Time.deltaTime, 0, 0, Space.World);

            if (Input.GetKey(KeyCode.W))
                _transform.Translate(0, 0, 1 * Speed * Time.deltaTime, Space.World);

            if (Input.GetKey(KeyCode.S))
                _transform.Translate(0, 0, -1 * Speed * Time.deltaTime, Space.World);

            if (Input.GetKey(KeyCode.D))
                _transform.Translate(1 * Speed * Time.deltaTime, 0, 0, Space.World);

            if (Input.GetMouseButtonDown(0))
            {
                hit_position = Input.mousePosition;
                camera_position = transform.position;

            }
            if (Input.GetMouseButton(0))
            {
                current_position = Input.mousePosition;
                LeftMouseDrag();
            }
        }

        void LeftMouseDrag()
        {
            // From the Unity3D docs: "The z position is in world units from the camera."  In my case I'm using the y-axis as height
            // with my camera facing back down the y-axis.  You can ignore this when the camera is orthograhic.
            current_position.z = hit_position.z = camera_position.y;

            // Get direction of movement.  (Note: Don't normalize, the magnitude of change is going to be Vector3.Distance(current_position-hit_position)
            // anyways.  
            var direction = Camera.main.ScreenToWorldPoint(current_position) - Camera.main.ScreenToWorldPoint(hit_position);

            // Invert direction to that terrain appears to move with the mouse.
            direction = new Vector3(direction.x * -1, 0, direction.y * -1);

            var position = camera_position + direction;

            transform.position = position;
        }
    }
}