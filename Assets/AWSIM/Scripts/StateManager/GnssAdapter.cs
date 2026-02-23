using AWSIM;
using System;
using UnityEngine;

namespace CDT
{
    public class GnssAdapter : SensorAdapter
    {
        public GnssAdapter(GameObject instance) : base(instance)
        {
            capabilities =
                SensorCapabilities.EnableDisable |
                SensorCapabilities.ChangeTopic;
        } 
        public override GameObject CreateAndInitialize(SensorInitConfig config, Transform mountRoot, string sensorID = "default/defaultGnss")
        {
            string[] idParts = sensorID.Split('/');
            string topicPrefix = idParts[0];
            GameObject sensorRoot = new GameObject(idParts[1]);
            sensorRoot.transform.SetParent(mountRoot, false);
            GameObject sensorInstance = GameObject.Instantiate(instance, sensorRoot.transform);
            sensorInstance.transform.localPosition = config.LocalPosition;
            sensorInstance.transform.localRotation = config.LocalRotation;
            GnssRos2Publisher publisher = sensorInstance.GetComponentInChildren<GnssRos2Publisher>();
            if (publisher == null)
            {
                throw new Exception($"GnssRos2Publisher component not found in gnss sensor prefab for sensorID '{sensorID}'");
            }
            else
            {
                publisher.poseTopic = $"{topicPrefix}{publisher.poseTopic}";
                publisher.poseWithCovarianceStampedTopic = $"{topicPrefix}{publisher.poseWithCovarianceStampedTopic}";
                publisher.ReInitializePublisher();
            }
            return sensorInstance;
        }
          // Add gnss-specific methods here, e.g., processing image data
    }
}