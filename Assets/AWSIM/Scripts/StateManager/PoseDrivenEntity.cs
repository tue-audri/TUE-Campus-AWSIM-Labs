using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;

namespace CDT
{   
    public abstract class PoseDrivenEntity : MonoBehaviour
    {
    
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

        [Header("Lifecycle")]
        public float timeToDeath = 1f; // Time in seconds before the object is destroyed
        [SerializeField] private Bounds localBounds;

        protected Vector3 targetPosition;
        protected Quaternion targetRotation;
        protected Vector3 velocity = Vector3.zero; // Used for SmoothDamp
        protected float rotationVelocity = 0f; // Used for SmoothDamp on yaw
        // protected float rayCastOriginOffset = 1f;
        // protected float rayCastMaxDistance = 100f;
        protected TrackedObjectManager trackedObjectManager;

        // Start is called before the first frame update
        protected virtual void Start()
        {
            // Initialize target position and rotation to current transform values
            targetPosition = transform.position;
            // targetPosition = FollowGround(transform.position);
            targetRotation = transform.rotation;
        }

        // Update is called once per frame
        protected virtual void Update()
        {
            timeToDeath -= Time.deltaTime;
            if (timeToDeath <= 0)
            {
                trackedObjectManager.RemoveTrackedObject(this.name);
            }
        }

        protected virtual void FixedUpdate()
        {
            Vector3 currentPos = transform.position;            
            Vector3 targetPos2D = new Vector3(
                targetPosition.x,
                currentPos.y, 
                targetPosition.z);
            Vector3 currentEuler = transform.eulerAngles;
            float targetYaw = targetRotation.eulerAngles.y;
            float currentYaw = currentEuler.y;

            if (useLerp)
            {
                // Smoothly move towards the target position using Lerp
                currentPos = Vector3.Lerp(currentPos, targetPos2D, lerpSpeed * Time.fixedDeltaTime);
                // Smoothly rotate towards the target yaw using Lerp
                currentYaw = Mathf.Lerp(currentYaw, targetYaw, rotationLerpSpeed * Time.fixedDeltaTime);
            }
            else
            {
                // Smoothly move towards the target position using SmoothDamp
                currentPos = Vector3.SmoothDamp(currentPos, targetPos2D, ref velocity, moveTime);
                
                // Smoothly rotate towards the target yaw using SmoothDamp
                currentYaw = Mathf.SmoothDamp(currentYaw, targetYaw, ref rotationVelocity, rotationMoveTime);
            }

            transform.position = currentPos;
            transform.eulerAngles = new Vector3(currentEuler.x, currentYaw, currentEuler.z);
        }

        public virtual void ApplyPose(UnityPose rootPose)
        {
            SetTargetPose(rootPose);
        }

        public virtual void ApplyTrackedObjectPose(UnityPose centroidPose)
        {
            UnityPose rootPose = ConvertCentroidPoseToRoot(centroidPose);
            SetTargetPose(rootPose);
        }

        protected void SetTargetPose(UnityPose pose)
        {
            targetPosition = pose.position;
            // targetPosition = FollowGround(targetPosition);
            targetRotation = pose.orientation;
            timeToDeath = 5f; // Reset time to death on pose update
        }
        // protected Vector3 FollowGround(Vector3 position)
        // {
        //     var origin = position + Vector3.up * rayCastOriginOffset;
        //     var groundLayerMask = LayerMask.GetMask("Ground");
        //     var groundExists = Physics.Raycast(origin, Vector3.down, out var hitInfo, rayCastMaxDistance, groundLayerMask);
        //     // return groundExists ? hitInfo.point : position;
        //     Debug.DrawRay(origin, Vector3.down * rayCastMaxDistance, groundExists ? Color.green : Color.red, 2f);

        //     if (groundExists)
        //     {
        //         Debug.Log($"[FollowGround] Hit ground at {hitInfo.point} | Collider: {hitInfo.collider.name}");
        //         return hitInfo.point;
        //     }
        //     else
        //     {
        //         Debug.LogWarning($"[FollowGround] No ground hit from {origin}, distance {rayCastMaxDistance}, mask {groundLayerMask}");
        //         return position;
        //     }
        // }

        // protected virtual UnityPose ConvertCentroidPoseToRoot(UnityPose centroidPose)
        // {
        //     // Debug.Log($"Base ConvertCentroidPoseToRoot used on {name}");
        //     Vector3 localOffset = -localBounds.center;
        //     Vector3 worldOffset = centroidPose.orientation * localOffset;
        //     return new UnityPose
        //     {
        //         position = centroidPose.position + worldOffset,
        //         orientation = centroidPose.orientation
        //     };
        // }

        public UnityPose ConvertCentroidPoseToRoot(UnityPose centroidPose)
        {
            // Debug.Log($"Base ConvertCentroidPoseToRoot used on {name}");
            Vector3 localOffset = -localBounds.center;
            Vector3 worldOffset = centroidPose.orientation * localOffset;
            return new UnityPose
            {
                position = centroidPose.position + worldOffset,
                orientation = centroidPose.orientation
            };
        }

        public virtual void Register(TrackedObjectManager manager)
        {
            trackedObjectManager = manager;
        }
        private void OnDrawGizmos()
        {
            // Cache Gizmos default values.
            var cacheColor = Gizmos.color;
            var cacheMatrix = Gizmos.matrix;

            // Apply color and matrix.
            Gizmos.color = Color.white;
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);

            // Draw wire cube.
            Gizmos.DrawWireCube(localBounds.center, localBounds.size);

            // Return to default value.
            Gizmos.color = cacheColor;
            Gizmos.matrix = cacheMatrix;
        }
        
    }
}
