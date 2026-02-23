using UnityEngine;
using System;
using AWSIM;

namespace CDT
{
    public class ImuAdapter : SensorAdapter
    {
        public ImuAdapter(GameObject instance) : base(instance)
        {
            capabilities =
                SensorCapabilities.EnableDisable |
                SensorCapabilities.ChangeTopic;
        } 
        public override GameObject CreateAndInitialize(SensorInitConfig config, Transform mountRoot, string sensorID = "default/defaultImu")
        {
            string[] idParts = sensorID.Split('/');
            string topicPrefix = idParts[0];
            GameObject sensorRoot = new GameObject(idParts[1]);
            sensorRoot.transform.SetParent(mountRoot, false);
            GameObject sensorInstance = GameObject.Instantiate(instance, sensorRoot.transform);
            sensorInstance.transform.localPosition = config.LocalPosition;
            sensorInstance.transform.localRotation = config.LocalRotation;
            ImuRos2Publisher publisher = sensorInstance.GetComponentInChildren<ImuRos2Publisher>();
            if (publisher == null)
            {
                throw new Exception($"ImuRos2Publisher component not found in imu sensor prefab for sensorID '{sensorID}'");
            }
            else
            {
                publisher.topic = $"{topicPrefix}{publisher.topic}";
                publisher.ReInitializePublisher();
            }
            return sensorInstance;
        }
          // Add imu-specific methods here, e.g., processing image data
    }
}