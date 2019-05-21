using UnityEngine;
using UnityEngine.Extensions;
using System.Collections;

public class StrateCamC : MonoBehaviour
{
    // Public fields
    public Terrain terrain;

    public float panSpeed = 15.0f;
    public float zoomSpeed = 100.0f;
    public float rotationSpeed = 50.0f;

    public float mousePanMultiplier = 0.1f;
    public float mouseRotationMultiplier = 0.2f;
    public float mouseZoomMultiplier = 5.0f;

    public float minZoomDistance = 20.0f;
    public float maxZoomDistance = 200.0f;
    public float smoothingFactor = 0.1f;
    public float goToSpeed = 0.1f;

    public bool useKeyboardInput = true;
    public bool useMouseInput = true;
    public bool adaptToTerrainHeight = true;
    public bool increaseSpeedWhenZoomedOut = true;
    public bool correctZoomingOutRatio = true;
    public bool smoothing = true;
    public bool allowDoubleClickMovement = false;
    private float doubleClickTimeWindow = 0.3f;

    public bool allowScreenEdgeMovement = true;
    public int screenEdgeSize = 10;
    public float screenEdgeSpeed = 1.0f;

    public GameObject objectToFollow;
    public Vector3 cameraTarget;

    // private fields
    private float currentCameraDistance;
    private Vector3 lastMousePos;
    private Vector3 lastPanSpeed = Vector3.zero;
    private Vector3 goingToCameraTarget = Vector3.zero;
    private bool doingAutoMovement = false;
    private DoubleClickDetectorC doubleClickDetector;


    // Use this for initialization
    public void Start()
    {
        currentCameraDistance = minZoomDistance + ((maxZoomDistance - minZoomDistance) / 2.0f);
        lastMousePos = Vector3.zero;
        doubleClickDetector = gameObject.AddComponent<DoubleClickDetectorC>();
        doubleClickDetector.doubleClickTimeWindow = doubleClickTimeWindow;
    }

    // Update is called once per frame
    public void Update()
    {
        if (allowDoubleClickMovement)
        {
            //doubleClickDetector.Update();
            UpdateDoubleClick();
        }
        UpdatePanning();
        UpdateRotation();
        UpdateZooming();
        UpdatePosition();
        UpdateAutoMovement();
        lastMousePos = Input.mousePosition;
    }

    public void GoTo(Vector3 position)
    {
        doingAutoMovement = true;
        goingToCameraTarget = position;
        objectToFollow = null;
    }

    public void Follow(GameObject gameObjectToFollow)
    {
        objectToFollow = gameObjectToFollow;
    }

    #region private functions
    private void UpdateDoubleClick()
    {
        if (doubleClickDetector.IsDoubleClick() && terrain && terrain.GetComponent<Collider>())
        {
            var cameraTargetY = cameraTarget.y;

            var collider = terrain.GetComponent<Collider>();
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit = new RaycastHit();
            Vector3 pos;

            if (collider.Raycast(ray, out hit, Mathf.Infinity))
            {
                pos = hit.point;
                pos.y = cameraTargetY;
                GoTo(pos);
            }
        }
    }

    private void UpdatePanning()
    {
        Vector3 moveVector = new Vector3(0, 0, 0);
        if (useKeyboardInput)
        {
            //! rewrite to adress xyz seperatly
            if (Input.GetKey(KeyCode.A))
            {
                moveVector.x -= 1;
            }
            if (Input.GetKey(KeyCode.S))
            {
                moveVector.z -= 1;
            }
            if (Input.GetKey(KeyCode.D))
            {
                moveVector.x += 1;
            }
            if (Input.GetKey(KeyCode.W))
            {
                moveVector.z += 1;
            }
        }
        if (allowScreenEdgeMovement)
        {
            if (Input.mousePosition.x < screenEdgeSize)
            {
                moveVector.x -= screenEdgeSpeed;
            }
            else if (Input.mousePosition.x > Screen.width - screenEdgeSize)
            {
                moveVector.x += screenEdgeSpeed;
            }
            if (Input.mousePosition.y < screenEdgeSize)
            {
                moveVector.z -= screenEdgeSpeed;
            }
            else if (Input.mousePosition.y > Screen.height - screenEdgeSize)
            {
                moveVector.z += screenEdgeSpeed;
            }
        }

        if (useMouseInput)
        {
            if (Input.GetMouseButton(2) && Input.GetKey(KeyCode.LeftShift))
            {
                Vector3 deltaMousePos = (Input.mousePosition - lastMousePos);
                moveVector += new Vector3(-deltaMousePos.x, 0, -deltaMousePos.y) * mousePanMultiplier;
            }
        }

        if (moveVector != Vector3.zero)
        {
            objectToFollow = null;
            doingAutoMovement = false;
        }

        var effectivePanSpeed = moveVector;
        if (smoothing)
        {
            effectivePanSpeed = Vector3.Lerp(lastPanSpeed, moveVector, smoothingFactor);
            lastPanSpeed = effectivePanSpeed;
        }

        var oldXRotation = transform.localEulerAngles.x;

        // Set the local X rotation to 0;
        transform.SetLocalEulerAngles(0.0f);

        float panMultiplier = increaseSpeedWhenZoomedOut ? (Mathf.Sqrt(currentCameraDistance)) : 1.0f;
        cameraTarget = cameraTarget + transform.TransformDirection(effectivePanSpeed) * panSpeed * panMultiplier * Time.deltaTime;

        // Set the old x rotation.
        transform.SetLocalEulerAngles(oldXRotation);
    }

    private void UpdateRotation()
    {
        float deltaAngleH = 0.0f;
        float deltaAngleV = 0.0f;

        if (useKeyboardInput)
        {
            if (Input.GetKey(KeyCode.Q))
            {
                deltaAngleH = 1.0f;
            }
            if (Input.GetKey(KeyCode.E))
            {
                deltaAngleH = -1.0f;
            }
        }

        if (useMouseInput)
        {
            if (Input.GetMouseButton(2) && !Input.GetKey(KeyCode.LeftShift))
            {
                var deltaMousePos = (Input.mousePosition - lastMousePos);
                deltaAngleH += deltaMousePos.x * mouseRotationMultiplier;
                deltaAngleV -= deltaMousePos.y * mouseRotationMultiplier;
            }
        }

        transform.SetLocalEulerAngles(
            Mathf.Min(80.0f, Mathf.Max(5.0f, transform.localEulerAngles.x + deltaAngleV * Time.deltaTime * rotationSpeed)),
            transform.localEulerAngles.y + deltaAngleH * Time.deltaTime * rotationSpeed
        );
    }

    private void UpdateZooming()
    {
        float deltaZoom = 0.0f;
        if (useKeyboardInput)
        {
            if (Input.GetKey(KeyCode.F))
            {
                deltaZoom = 1.0f;
            }
            if (Input.GetKey(KeyCode.R))
            {
                deltaZoom = -1.0f;
            }
        }
        if (useMouseInput)
        {
            var scroll = Input.GetAxis("Mouse ScrollWheel");
            deltaZoom -= scroll * mouseZoomMultiplier;
        }
        var zoomedOutRatio = correctZoomingOutRatio ? (currentCameraDistance - minZoomDistance) / (maxZoomDistance - minZoomDistance) : 0.0f;
        currentCameraDistance = Mathf.Max(minZoomDistance, Mathf.Min(maxZoomDistance, currentCameraDistance + deltaZoom * Time.deltaTime * zoomSpeed * (zoomedOutRatio * 2.0f + 1.0f)));
    }

    private void UpdatePosition()
    {
        if (objectToFollow != null)
        {
            cameraTarget = Vector3.Lerp(cameraTarget, objectToFollow.transform.position, goToSpeed);
        }

        transform.position = cameraTarget;
        transform.Translate(Vector3.back * currentCameraDistance);

        if (adaptToTerrainHeight && terrain != null)
        {
            transform.SetPosition(
                null,
                Mathf.Max(terrain.SampleHeight(transform.position) + terrain.transform.position.y + 10.0f, transform.position.y)
            );
        }
    }

    private void UpdateAutoMovement()
    {
        if (doingAutoMovement)
        {
            cameraTarget = Vector3.Lerp(cameraTarget, goingToCameraTarget, goToSpeed);
            if (Vector3.Distance(goingToCameraTarget, cameraTarget) < 1.0f)
            {
                doingAutoMovement = false;
            }
        }
    }
    #endregion
}