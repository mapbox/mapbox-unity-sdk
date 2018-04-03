using UnityEngine;
using UnityARInterface;

public class ObjectShooter : ARBase
{
    [SerializeField]
    private GameObject m_Prefab;

    [SerializeField]
    private float m_Force = 5000f;

    public float minimumYValue = 0f;

    private bool m_WasFireRequested = false;
    private Vector2 m_ScreenPosition;

    public void RequestFire(Vector2 screenPosition)
    {
        m_WasFireRequested = true;
        m_ScreenPosition = screenPosition;
    }

    void Update()
    {
        if (m_WasFireRequested)
        {
            var camera = GetCamera();

            var ray = camera.ScreenPointToRay(m_ScreenPosition);
            var go = Instantiate(m_Prefab, ray.origin + ray.direction * 2f, Quaternion.identity);
            var rigidbody = go.GetComponent<Rigidbody>();
            if (rigidbody != null)
            {
                var force = ray.direction * m_Force;
                rigidbody.AddForce(force);
            }

            var remover = go.GetComponent<RemoveRigidbody>();
            remover.minYPosition = minimumYValue;
        }

        m_WasFireRequested = false;
    }
}
