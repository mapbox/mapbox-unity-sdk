using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityARInterface;

public class DemoGUI : ARBase
{
    public float guiHeight { get; private set; }

    [SerializeField]
    private GameObject m_LevelGeometry;

    [SerializeField]
    private GUISkin m_GuiSkin;

    private ObjectShooter m_ObjectShooter;
    private ARController m_ARController;
    private float m_RotationAngle;

    void OnEnable()
    {
        m_ObjectShooter = GetComponent<ObjectShooter>();
        m_ARController = GetFirstEnabledControllerInChildren();
    }

    void OnGUI()
    {
        if (m_ARController == null || !m_ARController.enabled)
            return;

        guiHeight = Screen.height / 5;
        var buttonWidth = Screen.width / 2;

        if (GUI.Button(new Rect(Screen.width - buttonWidth, Screen.height - guiHeight, buttonWidth, guiHeight), "Fire!", m_GuiSkin.button))
            m_ObjectShooter.RequestFire(new Vector2(Screen.width / 2, Screen.height / 2));

        var sliderWidth = Screen.width / 2;
        var sliderHeight = guiHeight / 2;
        var angle = GUI.HorizontalSlider(
            new Rect(0, Screen.height - sliderHeight * 2, sliderWidth, sliderHeight),
            m_RotationAngle, 0f, 360f,
            m_GuiSkin.horizontalSlider,
            m_GuiSkin.horizontalSliderThumb);

        if (angle != m_RotationAngle)
        {
            m_ARController.rotation = Quaternion.AngleAxis(m_RotationAngle, Vector3.up);
            m_RotationAngle = angle;
        }

        var scale = GUI.HorizontalSlider(
            new Rect(0, Screen.height - sliderHeight, sliderWidth, sliderHeight),
            m_ARController.scale, 1f, 100f,
            m_GuiSkin.horizontalSlider,
            m_GuiSkin.horizontalSliderThumb);

        if (scale != m_ARController.scale)
            m_ARController.scale = scale;
    }

    void Update()
    {
        if (Input.GetKeyUp(KeyCode.Space))
            m_ObjectShooter.RequestFire(Input.mousePosition);

        if (Input.GetMouseButton(0) && Input.mousePosition.y > guiHeight)
        {
            var camera = GetCamera();

            Ray ray = camera.ScreenPointToRay(Input.mousePosition);

            var planeLayer = GetComponent<ARPlaneVisualizer>().planeLayer;
            int layerMask = 1 << planeLayer;

            RaycastHit rayHit;
            if (Physics.Raycast(ray, out rayHit, float.MaxValue, layerMask))
            {
                m_ARController.pointOfInterest = m_LevelGeometry.transform.position;
                m_ARController.AlignWithPointOfInterest(rayHit.point);
                m_ObjectShooter.minimumYValue = rayHit.point.y;
            }
        }
    }
}
