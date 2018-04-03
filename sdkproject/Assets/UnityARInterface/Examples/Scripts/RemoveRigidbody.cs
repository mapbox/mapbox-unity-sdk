using UnityEngine;

// Removes a rigid body if it goes to sleep or falls
// below a minimum vertical threshold.

public class RemoveRigidbody : MonoBehaviour
{
    public float minYPosition;

    void Update ()
    {
        if (transform.position.y < minYPosition)
        {
            Destroy(gameObject);
        }
        else
        {
            var rigidbody = GetComponent<Rigidbody>();
            if (rigidbody != null && rigidbody.IsSleeping())
                Destroy(gameObject);
        }
    }
}
