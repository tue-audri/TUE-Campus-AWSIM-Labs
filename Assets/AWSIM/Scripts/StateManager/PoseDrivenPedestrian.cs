using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CDT
{
    public class PoseDrivenPedestrian : PoseDrivenEntity
    {
        [Header("Animation")]
        [SerializeField] private Animator animator;
        [SerializeField] private Transform referencePoint;

        [SerializeField] private float speedSmoothing = 0.1f;
        [SerializeField] private float turnSmoothing = 0.1f;

        [Header("Grounding")]
        [SerializeField] private bool followGround = true;
        [SerializeField] private float groundRayHeight = 1.0f;
        [SerializeField] private float groundRayDistance = 2.0f;
        [SerializeField] private LayerMask groundLayer;

        private Vector3 previousPosition;
        private Quaternion previousRotation;

        private float smoothedSpeed;
        private float smoothedTurnSpeed;

        protected override void Start()
        {
            base.Start();
            if (!animator)
            {
                animator = GetComponentInChildren<Animator>();
            }
            previousPosition = transform.position;
            previousRotation = transform.rotation;
        }
        
        protected override void FixedUpdate()
        {
            base.FixedUpdate();

            UpdateMotion();
            UpdateAnimation();
            UpdateGrounding();
        }

        private void UpdateMotion()
        {
            float dt = Time.fixedDeltaTime;
            if (dt <= Mathf.Epsilon)
                return;

            // Linear speed (m/s)
            Vector3 deltaPos = transform.position - previousPosition;
            float speed = deltaPos.magnitude / dt;

            // Angular speed (deg/s around Y)
            float yawDelta = Quaternion.Angle(previousRotation, transform.rotation);
            float turnSpeed = yawDelta / dt;

            smoothedSpeed = Mathf.Lerp(smoothedSpeed, speed, speedSmoothing * dt);
            smoothedTurnSpeed = Mathf.Lerp(smoothedTurnSpeed, turnSpeed, turnSmoothing * dt);

            previousPosition = transform.position;
            previousRotation = transform.rotation;
        }

        private void UpdateAnimation()
        {
            if (!animator)
                return;

            animator.SetFloat("moveSpeed", smoothedSpeed);
            animator.SetFloat("rotateSpeed", smoothedTurnSpeed);
        }

        private void UpdateGrounding()
        {
            if (!followGround)
                return;

            Ray ray = new Ray(
                transform.position + Vector3.up * groundRayHeight,
                Vector3.down
            );

            if (Physics.Raycast(ray, out RaycastHit hit, groundRayDistance, groundLayer))
            {
                Vector3 pos = transform.position;
                pos.y = hit.point.y;
                transform.position = pos;
            }
        }

    }
}
