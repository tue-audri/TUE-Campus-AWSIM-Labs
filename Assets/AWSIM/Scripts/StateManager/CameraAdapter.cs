using AWSIM;
using UnityEngine;
using System;

namespace CDT
{
    public class CameraAdapter : SensorAdapter
    {
        public CameraAdapter(GameObject instance) : base(instance)
        {
            capabilities =
                SensorCapabilities.EnableDisable |
                SensorCapabilities.ChangeTopic;
        } 
        public override GameObject CreateAndInitialize(SensorInitConfig config, Transform mountRoot, string sensorID = "default/defaultCamera")
        {
            string[] idParts = sensorID.Split('/');
            string topicPrefix = idParts[0];
            GameObject sensorRoot = new GameObject(idParts[1]);
            sensorRoot.transform.SetParent(mountRoot, false);
            // CameraRos2Publisher publisher = instance.GetComponentInChildren<CameraRos2Publisher>();
            GameObject sensorInstance = GameObject.Instantiate(instance, sensorRoot.transform);
            sensorInstance.transform.localPosition = config.LocalPosition;
            CameraRos2Publisher publisher = sensorInstance.GetComponentInChildren<CameraRos2Publisher>();
            if (publisher == null)
            {
                throw new Exception($"CameraRos2Publisher component not found in camera sensor prefab for sensorID '{sensorID}'");
            }
            else
            {
                publisher.imageTopic = $"{topicPrefix}{publisher.imageTopic}";
                publisher.cameraInfoTopic = $"{topicPrefix}{publisher.cameraInfoTopic}";
                publisher.ReInitializePublisher();
            }
            // Additional camera-specific initialization can be done here using config.RawConfig
            return sensorInstance;
        }
          // Add camera-specific methods here, e.g., processing image data
    }
}