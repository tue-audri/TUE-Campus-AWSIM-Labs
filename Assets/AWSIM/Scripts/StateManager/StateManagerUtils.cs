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
        public static Vector3 FollowGround(Vector3 position)
        {
            float rayCastOriginOffset = 1f;
            float rayCastMaxDistance = 100f;
            var origin = position + Vector3.up * rayCastOriginOffset;
            var groundLayerMask = LayerMask.GetMask("Ground");
            var groundExists = Physics.Raycast(origin, Vector3.down, out var hitInfo, rayCastMaxDistance, groundLayerMask);
            return groundExists ? hitInfo.point : position;
            // Debug.DrawRay(origin, Vector3.down * rayCastMaxDistance, groundExists ? Color.green : Color.red, 2f);

            // if (groundExists)
            // {
            //     Debug.Log($"[FollowGround] Hit ground at {hitInfo.point} | Collider: {hitInfo.collider.name}");
            //     return hitInfo.point;
            // }
            // else
            // {
            //     Debug.LogWarning($"[FollowGround] No ground hit from {origin}, distance {rayCastMaxDistance}, mask {groundLayerMask}");
            //     return position;
            // }
        }
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

        public static Dictionary<string, SensorTransform[]> GetSensorKitFromMessage(JToken sensorsToken)
        {
            Dictionary<string, SensorTransform[]> sensorKit = new Dictionary<string, SensorTransform[]>();
            if (sensorsToken == null || sensorsToken.Type != JTokenType.Object)
                throw new System.Exception("SensorKit is null or incompatible type");

            var obj = (JObject)sensorsToken;
            Debug.Log("[SensorParse]Recieved sensorkit: " + sensorsToken.ToString()); 

            foreach (var property in obj.Properties())
            {
                if (property.Value is JArray transformArray)
                {
                    SensorTransform[] transforms = property.Value.ToObject<SensorTransform[]>();
                    sensorKit[property.Name] = transforms;
                    // sensorKit[property.Name][0].PrintSensorTransform();
                    // Debug.Log($"[SensorParse] Category '{property.Name}' → {transforms.Length} entries");
                }
                else
                {
                    throw new System.Exception($"[SensorParse] '{property.Name}' is not an array — skipped.");
                }
            }
                       
            return sensorKit;
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