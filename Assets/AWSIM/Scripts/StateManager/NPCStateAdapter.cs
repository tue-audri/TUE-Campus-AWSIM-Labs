using AWSIM;
using UnityEngine;   
namespace CDT
{
    public class NPCStateAdapter
    {
        public static void ApplyState(AgentInternalState state)
        {
            // NPCVehicle vehicle = state.npcVehicle;

            // // State update function calls
            // ApplyPose(vehicle, state.pose);                 
        }

        private static void ApplyPose(NPCVehicle vehicle, UnityPose pose)
        {
            // // Vector3 position = new Vector3(pose.position.x, pose.position.y, pose.position.z);
            // // Quaternion rotation = new Quaternion(pose.orientation.x, pose.orientation.y, pose.orientation.z, pose.orientation.w);
            // vehicle.SetPosition(pose.position);
            // vehicle.SetRotation(pose.orientation);
        }

        public static void ApplyState(TrackedObjectInternalState state)
        {
            // PoseDrivenVehicle poseDrivenVehicle = state.poseDrivenVehicle;

            // // State update function calls
            // poseDrivenVehicle.SetTargetPose(state.pose);                 
        }
    }
}