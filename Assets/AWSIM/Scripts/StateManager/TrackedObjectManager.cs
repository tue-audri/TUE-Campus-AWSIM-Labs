using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
namespace CDT{
    public class TrackedObjectManager : MonoBehaviour
    {
        // public AgentManager agentManager;
        public Transform mapOrigin;
        // Class Implementation
        private ConcurrentQueue<DittoMessage> msgQueue = new ConcurrentQueue<DittoMessage>();
        private List<TrackedObject> spawnRequests = new List<TrackedObject>();
        private Dictionary<string, AgentInternalState> trackedObjectStates = new Dictionary<string, AgentInternalState>();

        // Lifecycle Methods
        void Update()
        {
            // Dequeue messages and update relevant tracked object state
            while(msgQueue.TryDequeue(out DittoMessage msg))
            {
                // Debug.Log("Processing Ditto Live Message with topic: " + msg.topic + ". Remaining queue size: " + msgQueue.Count);
                try
                {
                    UpdateTrackedObjectStates(msg);
                }
                catch (System.Exception e)
                {
                    Debug.LogError("Error updating tracked object state from Ditto Live Message: " + e.Message);
                }
            }
        }

        void FixedUpdate()
        {
            // loop through the tracked object states and apply the states
        }

        void LateUpdate()
        {
            Debug.Log("Spawning " + spawnRequests.Count + " new tracked objects.");
            foreach (var trackedObject in spawnRequests)
            {
                // Spawn the tracked object in the Unity scene
                Debug.Log("Spawning tracked object with ID: " + trackedObject.object_id.ToString());
                // Implementation of spawning logic goes here
            }
            spawnRequests.Clear();                                      
        }

        // Other Methods
        // ----- Event handler -----
        public void EnqueueMessage(DittoMessage liveMessage)
        {
            msgQueue.Enqueue(liveMessage);
            // Debug.Log("Added Ditto Live Message with topic: " + liveMessage.topic + " to Message queue. Queue size: " + msgQueue.Count);
        }

        public void UpdateTrackedObjectStates(DittoMessage msg)
        {
            msg.value = StateManagerUtils.DecodeBase64ToJToken(msg.value);    // decode the  value field of live message which is base64 encoded JSON
            string[] topicParts = msg.topic?.Split('/');      
            Debug.Log("Processing tracked object message for Agent: " + this.gameObject.name + " with topic: " + msg.topic);   

            if (msg.value["objects"] == null || msg.value["objects"].Type != JTokenType.Array)
            {
                throw new System.Exception("Invalid or missing 'objects' field in tracked object message");
            }

            JArray objectsArray = msg.value["objects"] as JArray;                                               // Extract the list of tracked objects from the recieved tracked message

            foreach (JObject objToken in objectsArray)                                                          // loop through the list
            {
                TrackedObject trackedObject = objToken.ToObject<TrackedObject>();
                string objectID = topicParts[1] + ":" + trackedObject.object_id.ToString();
                trackedObject.parent = topicParts[1];
                // string objectID = new Guid(objToken["object_id"]?.ToObject<byte[]>()).ToString();
                Debug.Log("Processing tracked object: " + objectID);

                if (!trackedObjectStates.ContainsKey(objectID))
                {
                    spawnRequests.Add(trackedObject);
                    continue;
                }

                // Update existing tracked object state
                ModifyTrackedObject(trackedObject);
                
            }
        }

        public void SpawnTrackedObject(TrackedObject trackedObject)
        {
            AgentInternalState newAgent = new AgentInternalState();
            string objectID = trackedObject.parent + ":" + trackedObject.object_id.ToString();
            Pose rosPose = StateManagerUtils.GetPoseFromMessage(trackedObject.kinematics.pose_with_covariance.pose);
            UnityPose pose = new UnityPose();
            pose.position = StateManagerUtils.ConvertRos2UnityPosition(new Vector3(
                rosPose.position.x,
                rosPose.position.y,
                rosPose.position.z
            ));
            pose.orientation = StateManagerUtils.ConvertRos2UnityRotation(new Quaternion(
                rosPose.orientation.x,
                rosPose.orientation.y,
                rosPose.orientation.z,
                rosPose.orientation.w
            ));
            pose.position = mapOrigin.TransformPoint(pose.position);
            string objClass = StateManagerUtils.GetObjectClass(objToken);

            // retrieve the prefab for this object class from a predefined dictionary or list
                // Add prefabs as serialized fields in AgentManager?
            // Spawning Sequence
            // Pedestrian prefab are different from vehicle. How to handle?

            // var obj = Object.Instantiate(vehiclePrefab, pose.position, pose.orientation);           // Instantiate the visual GameObject in the world           
            // obj.name = objectID;                                                                    // Rename the new object in the Hierarchy window for clarity
            // obj.transform.parent = this.transform;                                                  // Organize the object in the Unity Hierarchy under a parent object
            // var agent = obj.GetComponent<NPCVehicle>();                                             // CRITICAL STEP: Get the specific script instance attached to the new GameObject

            Debug.Log("detected tracked object with class: " + objClass);
        }

        public void ModifyTrackedObject(TrackedObject trackedObject)
        {
            string objectID = trackedObject.parent + ":" + trackedObject.object_id.ToString();
            Debug.Log("Modifying tracked object state for object ID: " + objectID);
            // Update the relevant tracked object state variables here
        }

    }
}