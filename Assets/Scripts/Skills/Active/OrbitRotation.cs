using UnityEngine;

public class OrbitRotation : MonoBehaviour
{
    [SerializeField] float rotationSpeed = 180f;

    void Update()
    {
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
    }
}