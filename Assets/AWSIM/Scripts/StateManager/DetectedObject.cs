using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DetectedObject : MonoBehaviour
{
    // Add hover information to fields
    [Header("Smooth movement Options")]
    [Tooltip("Speed at which the object moves towards the target position")]
    public float lerpSpeed = 10f;
    [Tooltip("Time in seconds to move to the target position")]
    public float moveTime = 0.1f; // Time in seconds to move to the target position
    [Tooltip("Toggle for using SmoothDamp instead of Lerp")]
    public bool useLerp = true; // Toggle for using SmoothDamp instead of Lerp

    [SerializeField]
    private Vector3 targetPosition;
    private Vector3 velocity = Vector3.zero; // Used for SmoothDamp

    public float timeToDeath = 5f; // Time in seconds before the object is destroyed
    // Start is called before the first frame update
    void Start()
    {

    }

    public void updatePosition(Vector3 newPosition)
    {
        // Update the position of the detected object
        targetPosition = newPosition;
        timeToDeath = 5f; // Reset the time to death when the position is updated
    }

    // Update is called once per frame
    void Update()
    {
        // Decrease the time to death
        timeToDeath -= Time.deltaTime;

        // If time to death is less than or equal to zero, destroy the object
        if (timeToDeath <= 0)
        {
            Destroy(gameObject);
        }

        // Option 1: Smoothly lerp the object towards the target position (linear)
        // if (!useLerp)
        // {
        //     transform.position = Vector3.Lerp(transform.position, targetPosition, lerpSpeed * Time.deltaTime);
        // }
        // else
        // { 
        //     transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, moveTime);
        // }
        

    }
}
