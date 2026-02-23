using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CDT
{
    public class PoseDrivenObject : PoseDrivenEntity
    {
        // [Header("Vehicle Visuals")]
        // wheel transforms, light references, etc.
        // public WheelController wheelController;
        // public VehicleLights lights;
        protected override void FixedUpdate()
        {
            base.FixedUpdate();

            // UpdateWheelVisuals();
            // UpdateLights();
        }

        void UpdateWheelVisuals()
        {
            // Later: derive wheel rotation from delta position / yaw rate
        }

        void UpdateLights()
        {
            // Later: brake lights, indicators, etc.
        }

        public override void ConfigureSensors(Dictionary<string, SensorTransform[]> sensorTransforms, string topicPrefix)
        {
            // Vehicles might have different sensor configurations, but for now we can just set the transforms
            // sensorKit.SetSensorTransforms(sensorTransforms);
        }
    }

}
