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
    }

}
