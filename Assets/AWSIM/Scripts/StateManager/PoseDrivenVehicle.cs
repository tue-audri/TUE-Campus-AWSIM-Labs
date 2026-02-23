using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using System;
using AWSIM;
using RGLUnityPlugin;

namespace CDT
{
    public class PoseDrivenVehicle : PoseDrivenEntity
    {
        public enum TurnSignalState
        {
            OFF,
            LEFT,
            RIGHT,
            HAZARD,
        }

        [Serializable]
        public class EmissionMaterial
        {
            [SerializeField] MeshRenderer meshRenderer;
            [SerializeField] int materialIndex;
            [SerializeField] float lightingIntensity;
            [SerializeField] Color lightingColor;
            [SerializeField, Range(0, 1)] float lightingExposureWeight;

            Material material = null;
            Color defaultEmissionColor;
            float defaultExposureWeight;
            bool isOn = false;

            const string EmissionColor = "_EmissionColor";
            const string EmissionExposureWeight = "_EmissionExposureWeight";

            public void Initialize()
            {
                if (material == null)
                {
                    material = meshRenderer.materials[materialIndex];
                    material.EnableKeyword("_EMISSION");
                    defaultEmissionColor = material.GetColor(EmissionColor);
                    //  defaultExposureWeight = material.GetFloat(EmissionExposureWeight);
                }
            }

            public void Set(bool isLightOn)
            {
                if (this.isOn == isLightOn)
                    return;

                this.isOn = isLightOn;
                if (isLightOn)
                {
                    material.SetColor(EmissionColor, lightingColor * lightingIntensity);
                    material.SetFloat(EmissionExposureWeight, lightingExposureWeight);
                }
                else
                {
                    material.SetColor(EmissionColor, defaultEmissionColor);
                    material.SetFloat(EmissionExposureWeight, defaultExposureWeight);
                }
            }

            public void Destroy()
            {
                if (material != null)
                    UnityEngine.Object.Destroy(material);
            }
        }


        [Tooltip("Vehicle wheel radius in meters")]
        public float wheelRadius = 0.35f;

        [Header("Wheel Transforms")]
        public Transform frontLeftWheel;
        public Transform frontRightWheel;
        public Transform rearLeftWheel;
        public Transform rearRightWheel;

        [Header("Turn signal parameters")]
        [SerializeField] EmissionMaterial leftTurnSignalLight;
        [SerializeField] EmissionMaterial rightTurnSignalLight;

        float wheelRotation;
        Vector3 previousPosition;
        float previousYaw;
        float wheelBase;
        TurnSignalState turnSignalState = TurnSignalState.OFF;
        float turnSignalTimer = 0;
        bool currentTurnSignalOn = false;

        // light visual settings const values.
        const float turnSignalBlinkSec = 0.5f;             // seconds
        const float brakeLightAccelThreshold = -0.1f;      // m/s

        protected override void Start()
        {
            base.Start();
            // sensorKit = new SensorKit(this);
            previousPosition = transform.position;
            previousYaw = transform.eulerAngles.y;
            wheelBase = GetWheelBase();
            leftTurnSignalLight.Initialize();
            rightTurnSignalLight.Initialize();
        }

        protected override void Update()
        {
            base.Update();

            UpdateLights();
        }
        protected override void FixedUpdate()
        {
            base.FixedUpdate();

            UpdateWheelVisuals();
        }

        void UpdateWheelVisuals()
        {
            Vector3 currentPosition = transform.position;
            float currentYaw = transform.eulerAngles.y;

            Vector3 deltaPosition = currentPosition - previousPosition;
            deltaPosition.y = 0; // Ignore vertical movement

            float distanceTraveled = deltaPosition.magnitude;
            float speed = distanceTraveled / Time.fixedDeltaTime;

            float deltaYaw = Mathf.DeltaAngle(previousYaw, currentYaw);
            float yawRate = deltaYaw * Mathf.Deg2Rad / Time.fixedDeltaTime;

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
            UpdateTurnSignals(); // Update turn signals
            // Update brake lights
        }

        void UpdateTurnSignals()
        {
            if (IsAnyTurnSignalInputs() == false)
            {
                if (turnSignalTimer != 0)
                    turnSignalTimer = 0;

                if (currentTurnSignalOn != false)
                    currentTurnSignalOn = false;

                leftTurnSignalLight.Set(false);
                rightTurnSignalLight.Set(false);

                return;
            }

            turnSignalTimer -= Time.deltaTime;
            if (turnSignalTimer < 0f)
            {
                turnSignalTimer = turnSignalBlinkSec;
                currentTurnSignalOn = !currentTurnSignalOn;
            }

            var isLeftTurnSignalOn = IsLeftTurnSignalOn();
            leftTurnSignalLight.Set(isLeftTurnSignalOn);

            var isRightTurnSignalOn = IsRightTurniSignalOn();
            rightTurnSignalLight.Set(isRightTurnSignalOn);

            // --- inner functions ---

            bool IsAnyTurnSignalInputs()
            {
                return turnSignalState == TurnSignalState.LEFT
                    || turnSignalState == TurnSignalState.RIGHT
                    || turnSignalState == TurnSignalState.HAZARD;
            }

            bool IsLeftTurnSignalOn()
            {
                return (turnSignalState == TurnSignalState.LEFT
                    || turnSignalState == TurnSignalState.HAZARD)
                    && currentTurnSignalOn;
            }

            bool IsRightTurniSignalOn()
            {
                return (turnSignalState == TurnSignalState.RIGHT
                    || turnSignalState == TurnSignalState.HAZARD)
                    && currentTurnSignalOn;
            }
        }

        public void SetTurnSignalState(TurnSignalState turnSignalState)
        {
            if (this.turnSignalState != turnSignalState)
                this.turnSignalState = turnSignalState;
        }

        // public void ConfigureSensors(Dictionary<string, SensorTransform[]> sensorTransforms)
        // {
        //     sensorKit.SetSensorTransforms(sensorTransforms);
        // }

        public override void ConfigureSensors(Dictionary<string, SensorTransform[]> sensorTransforms, string topicPrefix)
        {
            sensorKit = new SensorKit(this); // Re-initialize SensorKit with the current vehicle context
            foreach (var kv in sensorTransforms)
            {
                string type = kv.Key;
                var sensors = kv.Value;

                // Debug.Log($"[SensorDict] {type} → {sensors?.Length ?? 0} sensors");

                if (sensors == null) continue;

                if (type == "sensor_kit")
                {
                    // Implement logic to position sensor_kit_base link wrt base_link
                    sensorKit.CalibrateSensorKit(sensors[0]);
                }
                else
                {
                    for (int i = 0; i < sensors.Length; i++)
                    {
                        var s = sensors[i];
                        string sensorID = topicPrefix + "/" + s.name;
                        Pose sensorRosPose = new Pose
                        {
                            position = s.transform.translation,
                            orientation = s.transform.rotation
                        };

                        SensorInitConfig config = new SensorInitConfig
                        {
                            LocalPosition = StateManagerUtils.ConvertRos2UnityPosition(new Vector3(
                                sensorRosPose.position.x, 
                                sensorRosPose.position.y, 
                                sensorRosPose.position.z
                            )),
                            LocalRotation = StateManagerUtils.ConvertRos2UnityRotation(new Quaternion(
                                sensorRosPose.orientation.x, 
                                sensorRosPose.orientation.y, 
                                sensorRosPose.orientation.z, 
                                sensorRosPose.orientation.w
                            )),
                            RawConfig = null
                        };

                        sensorKit.AddSensor(sensorID, type, config);

                        // Debug.Log($"[SensorParse] Processing sensor '{sensorID}'");

                        // Debug.Log(
                        //     $"  [{type} #{i}] name={s.name} parent={s.parent} child={s.child} " +
                        //     $"T=({s.sensorPose.position.x},{s.sensorPose.position.y},{s.sensorPose.position.z}) " +
                        //     $"R=({s.sensorPose.orientation.x},{s.sensorPose.orientation.y},{s.sensorPose.orientation.z},{s.sensorPose.orientation.w})"
                        // );
                    }
                }                
            }
        }
        public void ConfigureSensors(string topicPrefix)
        {
            // List<RglLidarPublisher> lidars = new List<RglLidarPublisher>();
            var lidars = new List<RglLidarPublisher>(GetComponentsInChildren<RglLidarPublisher>());
            var imus = new List<ImuRos2Publisher>(GetComponentsInChildren<ImuRos2Publisher>());
            var cameras = new List<CameraRos2Publisher>(GetComponentsInChildren<CameraRos2Publisher>());
            var gnssSensors = new List<GnssRos2Publisher>(GetComponentsInChildren<GnssRos2Publisher>());

            foreach (var camera in cameras)
            {
                camera.imageTopic = $"{topicPrefix}{camera.imageTopic}";
                camera.cameraInfoTopic = $"{topicPrefix}{camera.cameraInfoTopic}";
                camera.ReInitializePublisher();
            }
            foreach (var lidar in lidars)
            {
                var publishers = lidar.pointCloud2Publishers;
                foreach (var publisher in publishers)
                {
                    publisher.topic = $"{topicPrefix}{publisher.topic}";
                }
            }

            foreach (var imu in imus)
            {
                imu.topic = $"{topicPrefix}{imu.topic}";
                imu.ReInitializePublisher();
            }

            foreach (var gnss in gnssSensors)
            {
                gnss.poseTopic = $"{topicPrefix}{gnss.poseTopic}";
                gnss.poseWithCovarianceStampedTopic = $"{topicPrefix}{gnss.poseWithCovarianceStampedTopic}";
                gnss.ReInitializePublisher();
                // gnss.RefreshPublisher(topicPrefix);
            }
        }
    }
}
