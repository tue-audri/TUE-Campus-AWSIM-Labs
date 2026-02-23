using UnityEngine;
using System;
using Newtonsoft.Json.Linq;

namespace CDT 
{ 
    public abstract class SensorAdapter
    {
        protected GameObject instance;
        public SensorCapabilities capabilities { get; protected set; }

        protected SensorAdapter(GameObject instance)
        {
            this.instance = instance;
        }

        public abstract GameObject CreateAndInitialize(SensorInitConfig config, Transform mountRoot, string sensorID = "default");

        public virtual void Enable()
        {
            instance.SetActive(true);
        }
        
        public virtual void Disable()
        {
            instance.SetActive(false);
        }

        public virtual void Configure(SensorConfigUpdate update)
        {
            throw new NotSupportedException("Configure method not implemented for this sensor type.");
        }
        public virtual void Shutdown()
        {
            // optional override
        }

    }
    [Flags]
    public enum SensorCapabilities
    {
        None = 0,
        EnableDisable = 1,
        ChangeRate = 2,
        ChangeTopic = 4,
        Reconfigure = 8
    }

    public class SensorInitConfig
    {
        public Vector3 LocalPosition;
        public Quaternion LocalRotation;

        // flexible param bag
        public JObject RawConfig;
    }

    public class SensorConfigUpdate
    {
        public JObject Params;
    }
}