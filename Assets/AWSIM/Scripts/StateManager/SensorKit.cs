using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using System;
using AWSIM;
using RGLUnityPlugin;
namespace CDT
{
    public class SensorKit
    {
        private PoseDrivenEntity owner;
        private Transform mountRoot;
        private Dictionary<string, SensorHandle> sensors = new Dictionary<string, SensorHandle>();
        private bool isCalibrated = false;

        public SensorKit(PoseDrivenEntity owner)
        {
            this.owner = owner;
            this.mountRoot = owner.transform;
        }
        public SensorKit()
        {
            this.owner = null;
            this.mountRoot = null;
        }

        // ---------- Public API ----------
        public void CalibrateSensorKit(SensorTransform kitTransform)
        {   
            // Get transform from base_link ro sensor_kit_base link from kit transform and apply to sensor_kit_base_link
            try
            {
                GameObject sensorKitGO = new GameObject("URDF");                                    // Spawn a empty game object (URDF) as sensor kit parent with a child object(base_link)   
                sensorKitGO.transform.SetParent(mountRoot, false);
                GameObject baseLink = new GameObject(kitTransform.transform.parent);                // Create child of sensor kit to represent base_link
                baseLink.transform.SetParent(sensorKitGO.transform, false);
                GameObject sensorKitBaseLink = new GameObject(kitTransform.transform.child);        // Create another child of base_link to serve as mount point for all sensors (sensor_kit_base_link)
                sensorKitBaseLink.transform.SetParent(baseLink.transform, false);
                this.mountRoot = sensorKitBaseLink.transform;                                       // Set mountRoot to sensor_kit_base_link for future sensor spawns
                
                Pose sensorKitRosPose = new Pose
                {
                    position = kitTransform.transform.translation,
                    orientation = kitTransform.transform.rotation
                };
                // sensorKitRosPose.PrintPose();
                Vector3 sensorKitPos = StateManagerUtils.ConvertRos2UnityPosition(new Vector3(
                    sensorKitRosPose.position.x,
                    sensorKitRosPose.position.y,
                    sensorKitRosPose.position.z
                ));
                Quaternion sensorKitRot = StateManagerUtils.ConvertRos2UnityRotation(
                    new Quaternion(
                        sensorKitRosPose.orientation.x,
                        sensorKitRosPose.orientation.y,
                        sensorKitRosPose.orientation.z,
                        sensorKitRosPose.orientation.w
                    )
                );
                sensorKitBaseLink.transform.SetLocalPositionAndRotation(sensorKitPos, sensorKitRot);   // Apply transform to sensor_kit_base_link
                isCalibrated = true;
                Debug.Log($"[SensorKit] Sensor kit successfully calibrated.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SensorKit] Failed to calibrate sensor kit: {ex.Message}");
            }
        }
        public void AddSensor(string sensorID, string sensorType, SensorInitConfig config)
        {
            if (!isCalibrated)            
                throw new Exception("SensorKit must be calibrated before adding sensors.");
            
            if (sensors.ContainsKey(sensorID))
                throw new Exception($"Sensor with ID '{sensorID}' already exists in the kit.");

            Debug.Log($"[SensorKit] Adding sensor '{sensorID}' of type '{sensorType}' ");

            GameObject prefab = SensorRegistry.GetPrefab(sensorType);
            var adapter = SensorRegistry.GetAdapter(sensorType);

            if (prefab == null || adapter == null)
                throw new Exception($"Unknown prefab type '{sensorType}'");

            string[] agentID = sensorID.Split('/');
            string topicPrefix = agentID[1];

            SensorAdapter adapterInstance;

            switch (sensorType)
            {
                case "lidars":
                    Debug.Log($"[SensorKit] Instantiating LidarAdapter for sensor '{sensorID}' with topic prefix '{topicPrefix}'");
                    adapterInstance = new LidarAdapter(prefab); 
                    break;
                case "cameras":
                    Debug.Log($"[SensorKit] Instantiating CameraAdapter for sensor '{sensorID}' with topic prefix '{topicPrefix}'");
                    adapterInstance = new CameraAdapter(prefab);
                    break;
                case "imu":
                    Debug.Log($"[SensorKit] Instantiating ImuAdapter for sensor '{sensorID}' with topic prefix '{topicPrefix}'");
                    adapterInstance = new ImuAdapter(prefab);
                    break;
                case "gnss":
                    Debug.Log($"[SensorKit] Instantiating GnssAdapter for sensor '{sensorID}' with topic prefix '{topicPrefix}'");
                    adapterInstance = new GnssAdapter(prefab);
                    break;
                default:
                    throw new Exception($"Unknown sensor type '{sensorType}'");
            }

            GameObject sensorGO = adapterInstance.CreateAndInitialize(config, this.mountRoot, sensorID);
            SensorHandle handle = new SensorHandle(sensorID, sensorType, sensorGO, adapterInstance);
            sensors.Add(sensorID, handle);
             
            

            Debug.Log($"[SensorKit] Adding sensor '{sensorID}' of type '{sensorType}'");
        }

        
        // public SensorTransform[] GetSensorTransforms(string topic)
        // {
        //     if (sensorTransforms != null && sensorTransforms.ContainsKey(topic))
        //     {
        //         return sensorTransforms[topic];
        //     }
        //     else
        //     {
        //         Debug.LogWarning($"Sensor transforms for topic '{topic}' not found.");
        //         return null;
        //     }
        // }
        // 
    }    

    public class SensorHandle
    {
        public string SensorID { get; }
        public string SensorType { get; }
        public GameObject Instance { get; }
        public SensorAdapter Adapter { get; }

        public SensorHandle(string id, string type, GameObject instance, SensorAdapter adapter)
        {
            SensorID = id;
            SensorType = type;
            Instance = instance;
            Adapter = adapter;
        }
    }

    public static class SensorRegistry
    {
        private static Dictionary<string, GameObject> prefabs = new Dictionary<string, GameObject>();
        private static Dictionary<string, Func<GameObject, SensorAdapter>> factories = new Dictionary<string, Func<GameObject, SensorAdapter>>();

        public static void RegisterSensor(
            string type,
            GameObject prefab,
            Func<GameObject, SensorAdapter> factory)
        {
            if (prefab == null)
                throw new Exception( $"Prefab for sensor type '{type}' cannot be null.");
            if (factory == null)
                throw new Exception($"Factory for sensor type '{type}' cannot be null.");
            prefabs[type] = prefab;
            factories[type] = factory;            
        }

        public static GameObject GetPrefab(string type)
            => prefabs.TryGetValue(type, out var prefab) ? prefab : null;
        
        public static Func<GameObject, SensorAdapter> GetAdapter(string type)
            => factories.TryGetValue(type, out var factory) ? factory : null;
        
    }

}