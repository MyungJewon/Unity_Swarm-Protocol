using UnityEngine;

public class FoodBehaviour : MonoBehaviour
{
    [Header("Settings")]
    public float sinkSpeed = 2.0f;
    public float bottomLimit = -15.0f;

    void Update()
    {
        if (transform.position.y > bottomLimit)
        {
            transform.Translate(Vector3.down * sinkSpeed * Time.deltaTime, Space.World);
        }
    }
}