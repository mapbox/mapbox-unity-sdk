using UnityEngine;

public class UIDirectionControl : MonoBehaviour
{
    // This class is used to make sure world space UI
    // elements such as the health bar face the correct direction.


    public bool m_UseRelativePosition = true;     // Use relative position should be used for this gameobject?
    public bool m_UseRelativeRotation = true;     // Use relative rotation should be used for this gameobject?


    private Vector3 m_RelativePosition;         // The local position at the start of the scene.
    private Quaternion m_RelativeRotation;      // The local rotatation at the start of the scene.


    private void Start()
    {
        m_RelativePosition = transform.localPosition;
        m_RelativeRotation = transform.localRotation;
    }


    private void Update()
    {
        if (m_UseRelativeRotation)
            transform.rotation = m_RelativeRotation;

        if (m_UseRelativePosition)
            transform.position = transform.parent.position + m_RelativePosition;
    }
}