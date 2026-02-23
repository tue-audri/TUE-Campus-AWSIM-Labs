using AWSIM;
using Unity.VisualScripting;
using UnityEngine;
using System;

namespace CDT
{
    public class LidarAdapter : SensorAdapter
    {
        public LidarAdapter(GameObject instance) : base(instance)
        {
            // lidarDriver = instance.GetComponentInChildren<MonoBehaviour>(); // replace
            // rosPublisher = instance.GetComponentInChildren<MonoBehaviour>(); // replace

            capabilities =
                SensorCapabilities.EnableDisable |
                SensorCapabilities.ChangeTopic;
        } 
        public override GameObject CreateAndInitialize(SensorInitConfig config, Transform mountRoot, string sensorID = "default/defaultLidar")
        {
            string[] idParts = sensorID.Split('/');
            string topicPrefix = idParts[0];
            GameObject sensorRoot = new GameObject(idParts[1]);
            sensorRoot.transform.SetParent(mountRoot, false);
            GameObject sensorInstance = GameObject.Instantiate(instance, sensorRoot.transform);
            sensorInstance.SetActive(false);
            RglLidarPublisher publisher = sensorInstance.GetComponentInChildren<RglLidarPublisher>();
            sensorInstance.transform.SetLocalPositionAndRotation(config.LocalPosition, config.LocalRotation);
            if (publisher == null)            {
                throw new Exception($"RglLidarPublisher component not found in LiDAR sensor prefab for sensorID '{sensorID}'");
            }
            else
            {
                foreach (var pub in publisher.pointCloud2Publishers)
                {
                    pub.topic = $"{topicPrefix}/{pub.topic}";
                }
                sensorInstance.SetActive(true);
            } 
            // replace with actual prefab instantiation
            // Additional LiDAR-specific initialization can be done here using config.RawConfig
            return sensorInstance;
        }
          // Add LiDAR-specific methods here, e.g., processing point cloud data
    }
}
 