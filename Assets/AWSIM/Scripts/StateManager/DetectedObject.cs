using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace CDT
{
    public class DetectedObject : MonoBehaviour
    {
        // Add hover information to fields
        [Header("Smooth movement Options")]
        [Tooltip("Speed at which the object moves towards the target position")]
        public float lerpSpeed = 10f;
        [Tooltip("Time in seconds to move to the target position")]
        public float moveTime = 0.1f; // Time in seconds to move to the target position
        [Tooltip("Toggle for using SmoothDamp instead of Lerp")]
        

        [Header("Smooth rotation Options")]
        public float rotationLerpSpeed = 10f;
        [Tooltip("Time in seconds to rotate to the target yaw")]
        public float rotationMoveTime = 0.1f;
        [Tooltip("Toggle for using SmoothDamp instead of Lerp for rotation")]
        public bool useLerp = true; // Toggle for using SmoothDamp instead of Lerp

        public float timeToDeath = 5f; // Time in seconds before the object is destroyed

        private Vector3 targetPosition;
        private Vector3 velocity = Vector3.zero; // Used for SmoothDamp
        private Quaternion targetRotation = Quaternion.identity;
        private float rotationVelocity = 0f; // Used for SmoothDamp on yaw

        private TrackedObjectManager trackedObjectManager;
        
        void Start()
        {
            // Initialize target position and rotation to current transform values
            // so the object doesn't lerp from its spawn location to origin on first frame
            targetPosition = transform.position;
            targetRotation = transform.rotation;
        }
       
        // Update is called once per frame
        void Update()
        {
            // Decrease the time to death
            timeToDeath -= Time.deltaTime;

            // If time to death is less than or equal to zero, destroy the object
            if (timeToDeath <= 0)
            {
                trackedObjectManager.RemoveTrackedObject(this.name);
            }     

        }

        void FixedUpdate()
        {
            // Gravity-affected movement
            Vector3 currentPos = transform.position;            
            Vector3 targetPos2D = new Vector3(targetPosition.x, currentPos.y, targetPosition.z);
            Vector3 currentEuler = transform.eulerAngles;
            float targetYaw = targetRotation.eulerAngles.y;
            float currentYaw = currentEuler.y;

            if (useLerp)
            {
                currentPos = Vector3.Lerp(currentPos, targetPos2D, lerpSpeed * Time.fixedDeltaTime);
                currentYaw = Mathf.Lerp(currentYaw, targetYaw, rotationLerpSpeed * Time.fixedDeltaTime);
            }
            else
            {
                currentPos = Vector3.SmoothDamp(currentPos, targetPos2D, ref velocity, moveTime);
                currentYaw = Mathf.SmoothDamp(currentYaw, targetYaw, ref rotationVelocity, rotationMoveTime);
            }
            
            transform.position = currentPos;
            transform.eulerAngles = new Vector3(currentEuler.x, currentYaw, currentEuler.z);
        }
        
        public void ApplyPose(UnityPose newpose)
        {
            targetPosition = newpose.position;
            targetRotation = newpose.orientation;
            timeToDeath = 5f; // Reset the time to death when the pose is updated
        }

        public void Register(TrackedObjectManager manager)
        {
            trackedObjectManager = manager;
        }
        public void updatePosition(Vector3 newPosition)
        {
            // Update the position of the detected object
            targetPosition = newPosition;
            timeToDeath = 5f; // Reset the time to death when the position is updated
        }

        public void updateRotation(Quaternion newRotation)
        {
            // Update the target rotation (yaw will be interpolated; pitch/roll from gravity)
            targetRotation = newRotation;
            timeToDeath = 5f; // Reset the time to death when the rotation is updated
        }
    }   
}
