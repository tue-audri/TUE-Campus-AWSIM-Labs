using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System.Collections.Generic;
using AWSIM;

namespace CDT
{
    // Class Definitions
    

    public class StateManagerUtils
    {
        public static Vector3 ConvertRos2UnityPosition(Vector3 rosPos)
        {
            return new Vector3(-rosPos.y,rosPos.z, rosPos.x);
        }
        public static Quaternion ConvertRos2UnityRotation(Quaternion rosQuat)
        {
            Vector3 rosEuler = RosQuaternionToEuler(rosQuat);

            Vector3 unityEuler = new Vector3(rosEuler.y * Mathf.Rad2Deg, -rosEuler.z * Mathf.Rad2Deg, -rosEuler.x * Mathf.Rad2Deg);
            
            return Quaternion.Euler(unityEuler);
        }
        public static Vector3 RosQuaternionToEuler(Quaternion q)
        {
            // Unity’s Quaternion.eulerAngles gives degrees, but we want radians first
            Vector3 eulerDeg = q.eulerAngles;
            return new Vector3(
                eulerDeg.x * Mathf.Deg2Rad,
                eulerDeg.y * Mathf.Deg2Rad,
                eulerDeg.z * Mathf.Deg2Rad
            );
        }
        public static Pose GetPoseFromMessage(JToken poseToken)
        {
            Pose pose = new Pose();
            pose.position = new Position();
            pose.orientation = new Orientation();
            
            pose.position.x = poseToken["position"]["x"].Value<float>();
            pose.position.y = poseToken["position"]["y"].Value<float>();
            pose.position.z = poseToken["position"]["z"].Value<float>();
            pose.orientation.x = poseToken["orientation"]["x"].Value<float>();
            pose.orientation.y = poseToken["orientation"]["y"].Value<float>();
            pose.orientation.z = poseToken["orientation"]["z"].Value<float>();
            pose.orientation.w = poseToken["orientation"]["w"].Value<float>();
            // Debug.Log("Thing pose recieved: " + poseToken.ToString());

            return pose;
        }

        public static JToken DecodeBase64ToJToken(JToken base64Token)
        {
            if (base64Token == null || base64Token.Type != JTokenType.String)
            {
                throw new System.Exception("Invalid base64 token");
            }

            string base64String = base64Token.ToString();
            byte[] data = System.Convert.FromBase64String(base64String);
            string jsonString = System.Text.Encoding.UTF8.GetString(data);
            JToken decodedToken = JToken.Parse(jsonString);
            return decodedToken;
        }

        public static readonly Dictionary<string, string> classMap = new Dictionary<string, string>
        {
            { "0", "unknown" }, { "1", "car" }, { "2", "truck" },
            { "3", "bus" }, { "4", "trailer" }, { "5", "motorcycle" },
            { "6", "bicycle" }, { "7", "pedestrian" }
        };
        public static string GetObjectClass(List<Classification> classification)
        {
            string classID = classification != null && classification.Count > 0
                ? classification[0].label.ToString()
                : "0";
            return classMap.TryGetValue(classID, out string objClass) ? objClass : "unknown";
        }
    }

    
}