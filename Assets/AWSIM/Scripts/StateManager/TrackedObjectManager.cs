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
        private List<JObject> spawnRequests = new List<JObject>();
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
                    UpdateTrackedObjectState(msg);
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

        // Other Methods
        // ----- Event handler -----
        public void EnqueueMessage(DittoMessage liveMessage)
        {
            msgQueue.Enqueue(liveMessage);
            // Debug.Log("Added Ditto Live Message with topic: " + liveMessage.topic + " to Message queue. Queue size: " + msgQueue.Count);
        }

        public void UpdateTrackedObjectState(DittoMessage msg)
        {
            msg.value = StateManagerUtils.DecodeBase64ToJToken(msg.value);    // decode the  value field of live message which is base64 encoded JSON
            string[] topicParts = msg.topic?.Split('/');
            // string parentName = topicParts[0] + ":" + topicParts[1];    
            // GameObject parentGO;       
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
                // string objectID = new Guid(objToken["object_id"]?.ToObject<byte[]>()).ToString();
                Debug.Log("Processing tracked object: " + objectID);

                if (!trackedObjectStates.ContainsKey(objectID))
                {
                    spawnRequests.Add(objToken);
                    continue;
                }

                // If the tracked object already exists, update its state in trackedObjectStates list
                // If the tracked object is new, create a new AgentInternalState and add to trackedObjectStates list               
                // SpawnTrackedObject(objToken, parentGO);
                

                // // Find existing tracked object state or create a new one
                // AgentInternalState trackedObjectState = trackedObjectStates.Find(state => state.agentID == objectID);
                // if (trackedObjectState == null)
                // {
                //     trackedObjectState = new AgentInternalState();
                //     trackedObjectState.agentID = objectID;
                //     trackedObjectStates.Add(trackedObjectState);
                // }

                // // Update the pose
                // trackedObjectState.pose = pose;
                
            }
        }

        // public void SpawnTrackedObject(JObject objToken, GameObject parentGO)
        // {
        //     AgentInternalState newAgent = new AgentInternalState();
        //     string objectID = topicParts[1] + ":" + objToken["object_id"]?.ToString(Formatting.None);
        //     Pose pose = StateManagerUtils.GetPoseFromMessage(objToken?["kinematics"]?["pose_with_covariance"]?["pose"]);
        //     pose.position = mapOrigin.TransformPoint(pose.position);
        //     string objClass = StateManagerUtils.GetObjectClass(objToken);

        //     // var obj = Object.Instantiate(vehiclePrefab, pose.position, pose.orientation);           // Instantiate the visual GameObject in the world           
        //     // obj.name = objectID;                                                                    // Rename the new object in the Hierarchy window for clarity
        //     // obj.transform.parent = this.transform;                                                  // Organize the object in the Unity Hierarchy under a parent object
        //     // var agent = obj.GetComponent<NPCVehicle>();                                             // CRITICAL STEP: Get the specific script instance attached to the new GameObject

        //     Debug.Log("detected tracked object with class: " + objClass);
        // }

        public void UpdateTrackedObject(JObject objToken)
        {
            // Implementation
        }

    }
}