using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace CDT
{
    public class PoseDrivenVehicle : PoseDrivenEntity
    {
        // [Header("Vehicle Parameters")]
        // [Tooltip("Vehicle wheel base in meters")]
        // public float wheelBase = 2.7f;
        [Tooltip("Vehicle wheel radius in meters")]
        public float wheelRadius = 0.35f;

        [Header("Wheel Transforms")]
        public Transform frontLeftWheel;
        public Transform frontRightWheel;
        public Transform rearLeftWheel;
        public Transform rearRightWheel;

        float wheelRotation;
        Vector3 previousPosition;
        float previousYaw;
        float wheelBase;

        protected override void Start()
        {
            base.Start();
            previousPosition = transform.position;
            previousYaw = transform.eulerAngles.y;
            wheelBase = GetWheelBase();
        }
        protected override void FixedUpdate()
        {
            base.FixedUpdate();

            UpdateWheelVisuals();
            // UpdateLights();
        }

        void UpdateWheelVisuals()
        {
            Vector3 currentPosition = transform.position;
            float currentYaw = transform.eulerAngles.y;

            Vector3 deltaPosition = currentPosition - previousPosition;
            deltaPosition.y = 0; // Ignore vertical movement

            float distanceTraveled = deltaPosition.magnitude;
            float speed = distanceTraveled / Time.deltaTime;

            float deltaYaw = Mathf.DeltaAngle(previousYaw, currentYaw);
            float yawRate = deltaYaw * Mathf.Deg2Rad / Time.deltaTime;

            float steerAngle = 0f;
            if (speed > 0.1f)
            {
                steerAngle = Mathf.Atan(yawRate * wheelBase / speed) * Mathf.Rad2Deg;
            }

            wheelRotation += (distanceTraveled / (2f * Mathf.PI * wheelRadius)) * 360f;

            ApplyWheelTransforms(steerAngle,wheelRotation);

            previousPosition = currentPosition;
            previousYaw = currentYaw;
        }

        void ApplyWheelTransforms(float steerAngleDeg, float wheelRotationDeg)
        {
            // Front wheels: steering + rotation
            if (frontLeftWheel != null)
            {
                frontLeftWheel.localRotation =
                    Quaternion.Euler(wheelRotationDeg, steerAngleDeg, 0f);
            }

            if (frontRightWheel != null)
            {
                frontRightWheel.localRotation =
                    Quaternion.Euler(wheelRotationDeg, steerAngleDeg, 0f);
            }

            // Rear wheels: rotation only
            if (rearLeftWheel != null)
            {
                rearLeftWheel.localRotation =
                    Quaternion.Euler(wheelRotationDeg, 0f, 0f);
            }

            if (rearRightWheel != null)
            {
                rearRightWheel.localRotation =
                    Quaternion.Euler(wheelRotationDeg, 0f, 0f);
            }
        }

        public float GetWheelBase()
        {
            if (frontLeftWheel == null || rearLeftWheel == null)
            {
                Debug.LogWarning("WheelBase calculation failed: missing wheel transforms");
                return 0f;
            }

            float frontZ = frontLeftWheel.localPosition.z;
            float rearZ  = rearLeftWheel.localPosition.z;

            return Mathf.Abs(frontZ - rearZ);
        }

        void UpdateLights()
        {
            // Later: brake lights, indicators, etc.
        }
    }
}
