using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System.Collections.Generic;
using System;
using AWSIM;
namespace CDT
{


    [Serializable]
    public class DittoMessage
    {
        public string topic;
        public JObject headers;
        public string path;
        public JToken value; // If value is a complex object, use: public ValueType value;
        public int status;
    }
    
    [Serializable]
    public class ThingWrapper
    {
        public string thingID;
        public string policyID;
        public JToken attributes;
        public JToken features;
    }

    [Serializable]
    public class UnityPose
    {
        public Vector3 position;
        public Quaternion orientation;
    }

    [Serializable]
    public class AgentInternalState
    {
        // public NPCVehicle npcVehicle;
        public PoseDrivenVehicle poseDrivenVehicle;
        public TrackedObjectManager trackedObjectManager;
        public string agentID;
        public UnityPose pose;
        public int indicatorStatus;
        public int hazardLightStatus;
        // Add other state variables as needed
    }

    public class TrackedObjectInternalState
    {
        public PoseDrivenEntity poseDrivenEntity;
        public TrackedObjectManager trackedObjectManager;
        public string objectID;
        public string objClass;
        public UnityPose pose;
        // Add other state variables as needed
    }

    [Serializable]
    public class TrackedObject
    {
        public byte[] object_id;
        public float existance_probability;
        public List<Classification> classification;
        public Kinematics kinematics;
        public Shape shape;
        public string parent;
    }

    [Serializable]
    public class Classification
    {
        public int label;
        public float probability;
    }

    [Serializable]
    public class Kinematics
    {
        public PoseWithCovariance pose_with_covariance;
        public TwistWithCovariance twist_with_covariance;
        public AccelWithCovariance acceleration_with_covariance;
        public int orientation_availability;
        public bool is_stationary;
    }

    #region --- Pose ---

    [Serializable]
    public class PoseWithCovariance
    {
        public Pose pose;
        public List<double> covariariance;  // in your JSON "covariariance" is spelled this way!
    }

    [Serializable]
    public class SensorTransform
    {
        public string name;
        public TransformData transform;
        public void PrintSensorTransform()
        {
            Debug.Log($"Sensor Name: {name}, Parent: {transform.parent}, Child: {transform.child}");
            Debug.Log($"Sensor Pose - Position: ({transform.translation.x}, {transform.translation.y}, {transform.translation.z}), " +
                      $"Orientation: ({transform.rotation.x}, {transform.rotation.y}, {transform.rotation.z}, {transform.rotation.w})");
        }
    }

    [Serializable]
    public class TransformData
    {
        public string parent;
        public string child;
        public Position translation;
        public Orientation rotation;
    }

    [Serializable]
    public class Pose
    {
        public Position position;
        public Orientation orientation;
        public void PrintPose()
        {
            Debug.Log($"Position: ({position.x}, {position.y}, {position.z}), " +
                      $"Orientation: ({orientation.x}, {orientation.y}, {orientation.z}, {orientation.w})");
        }
    }

    [Serializable]
    public class Position
    {
        public float x;
        public float y;
        public float z;
    }

    [Serializable]
    public class Orientation
    {
        public float x;
        public float y;
        public float z;
        public float w;
    }

    #endregion

    #region --- Twist ---

    [Serializable]
    public class TwistWithCovariance
    {
        public Twist twist;
        public List<double> covariance;
    }

    [Serializable]
    public class Twist
    {
        public Linear linear;
        public Angular angular;
    }

    [Serializable]
    public class Linear
    {
        public double x;
        public double y;
        public double z;
    }

    [Serializable]
    public class Angular
    {
        public double x;
        public double y;
        public double z;
    }

    #endregion

    #region --- Acceleration ---

    [Serializable]
    public class AccelWithCovariance
    {
        public Accel accel;
        public List<double> covariance;
    }

    [Serializable]
    public class Accel
    {
        public Linear linear;
        public Angular angular;
    }

    #endregion

    #region --- Shape ---

    [Serializable]
    public class Shape
    {
        public int type;
        public Footprint footprint;
        public Dimensions dimensions;
    }

    [Serializable]
    public class Footprint
    {
        public List<object> points;  // empty array → leave as object[]
    }

    [Serializable]
    public class Dimensions
    {
        public double x;
        public double y;
        public double z;
    }

    #endregion
                                                    
}