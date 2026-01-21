using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using AWSIM;
namespace CDT
{
    public class AgentManager : MonoBehaviour
    {
        // Inputs and Declarations
        [SerializeField] private GameObject vehiclePrefab; // Assign agent vehicle prefab in Inspector   
        [SerializeField] private Transform mapOrigin;

        private ConcurrentQueue<DittoMessage> eventQueue; 
        private ConcurrentQueue<DittoMessage> liveMessageQueue;
        private List<DittoMessage> spawnRequests;
        private Dictionary<string, AgentInternalState> agentStates;


        // Lifecycle Methods
        void Start()
        {
            eventQueue = new ConcurrentQueue<DittoMessage>();
            liveMessageQueue = new ConcurrentQueue<DittoMessage>();
            spawnRequests = new List<DittoMessage>();
            agentStates = new Dictionary<string, AgentInternalState>();
        }

        void Update()
        {
            // Dequeue messages and update relevant agent state
            while(eventQueue.TryDequeue(out DittoMessage msg))
            {
                // Debug.Log("Processing Ditto Event with topic: " + msg.topic + ". Remaining queue size: " + eventQueue.Count);
                // Process the message and update agentStates accordingly
                try
                {
                    HandleEvent(msg);
                }
                catch (System.Exception e)
                {
                    Debug.LogError("Error updating agent state from Ditto Event: " + e.Message);
                }
            }
            while(liveMessageQueue.TryDequeue(out DittoMessage msg))
            {
                // Debug.Log("Processing Ditto Live Message with topic: " + msg.topic + ". Remaining queue size: " + liveMessageQueue.Count);
                // Process the message and update agentStates accordingly
                try
                {
                    HandleMessage(msg);
                }
                catch (System.Exception e)
                {
                    Debug.LogError("Error updating agent state from Ditto Live Message: " + e.Message);
                }
            }
        }

        void FixedUpdate()
        {
            foreach (var agentState in agentStates.Values)
            {
                // NPCStateAdapter.ApplyState(agentState);
                agentState.poseDrivenVehicle.ApplyPose(agentState.pose);
            }
        }

        void LateUpdate()
        {
            // Process spawn requests
            foreach (var msg in spawnRequests)
            {
                try
                {
                    SpawnAgent(msg);
                }
                catch (System.Exception e)
                {
                    Debug.LogError("Error spawning agent from Ditto Event: " + e.Message);
                }
            }
            spawnRequests.Clear();

            foreach (var agentState in agentStates.Values)
            {
                if (agentState.hazardLightStatus <= 1 & agentState.indicatorStatus <= 1)
                {
                    agentState.poseDrivenVehicle.SetTurnSignalState(PoseDrivenVehicle.TurnSignalState.OFF);
                }
                else if (agentState.indicatorStatus == 2 & agentState.hazardLightStatus <= 1)
                {
                    agentState.poseDrivenVehicle.SetTurnSignalState(PoseDrivenVehicle.TurnSignalState.LEFT);
                }
                else if (agentState.indicatorStatus == 3 & agentState.hazardLightStatus <= 1)
                {
                    agentState.poseDrivenVehicle.SetTurnSignalState(PoseDrivenVehicle.TurnSignalState.RIGHT);
                }
                else
                {
                    agentState.poseDrivenVehicle.SetTurnSignalState(PoseDrivenVehicle.TurnSignalState.HAZARD);
                }
            }
        }      

        // Other Methods
        // ----- Event handler -----
        public void HandleEvent(DittoMessage msg)
        {
            string[] topicParts = msg.topic?.Split('/');
            switch (topicParts.Last())
            {
                case "created":
                    // Debug.Log("Created event Detected for Agent: " + topicParts[1]);
                    // SpawnAgent(msg);
                    spawnRequests.Add(msg);
                    break;
                case "modified":
                    // Debug.Log("Modified event Detected for Agent: " + topicParts[1]);
                    ModifyAgent(msg);
                    break;
                case "deleted":
                    DeleteAgent(msg);
                    break;
                default:
                    throw new System.Exception("Unrecognized Ditto event type: " + topicParts.Last());
            }
        }

        public void HandleMessage(DittoMessage msg)
        {
            string [] topicParts = msg.topic?.Split('/');
            string thingID = topicParts[0] + ":" + topicParts[1];
            if (!agentStates.ContainsKey(thingID))
            {
                throw new System.Exception("Received Ditto Live Message for non-existent Agent: " + thingID);
            }
            // Add the message to the TrackedObjectManager of the relevant agent
            agentStates[thingID].trackedObjectManager?.EnqueueMessage(msg);
            // Debug.Log("Received Ditto Live Message for Agent: " + msg.topic);
        }

        public void SpawnAgent(DittoMessage msg)
        {
            AgentInternalState newAgent = new AgentInternalState();
            ThingWrapper thing = JsonConvert.DeserializeObject<ThingWrapper>(msg.value.ToString());
            Pose rosPose = StateManagerUtils.GetPoseFromMessage(thing.features?["status"]?["properties"]?["kinematics"]?["pose"]);

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
            pose.position = StateManagerUtils.FollowGround(pose.position);
            // Debug.Log("Extracted Pose - Position: " + pose.position + ", Orientation: " + pose.orientation);
            
            var obj = Object.Instantiate(vehiclePrefab, pose.position, pose.orientation);           // Instantiate the visual GameObject in the world           
            obj.name = thing.thingID;                                                               // Rename the new object in the Hierarchy window for clarity
            obj.transform.parent = this.transform;                                                  // Organize the object in the Unity Hierarchy under a parent object
            // var agent = obj.GetComponent<NPCVehicle>();                                          
            var agent = obj.GetComponent<PoseDrivenVehicle>();                                      // CRITICAL STEP: Get the specific script instance attached to the new GameObject
            // agent.VehicleID = thing.thingID;                                                     // Initialize the C# data within that retrieved script instance
            // agent.enabled = true;                                                                    
            newAgent.poseDrivenVehicle = agent;                                                     // Set the npcVehicle reference in the AgentInternalState
            // newAgent.npcVehicle = agent;
            newAgent.trackedObjectManager = obj.GetComponent<TrackedObjectManager>();               // Set the trackedObjectManager reference in the AgentInternalState
            newAgent.trackedObjectManager.mapOrigin = this.mapOrigin;                                        // Set the mapOrigin reference in the TrackedObjectManager
            newAgent.agentID = thing.thingID;                                                       // Set the agentID in the AgentInternalState
            newAgent.pose = pose;                                                                   // Set the pose in the AgentInternalState
            agentStates.Add(thing.thingID, newAgent);                                               // Add the new agent to the agentStates list
            Debug.Log("Entry added to agentStates for AgentID: " + thing.thingID);
        }

        public void ModifyAgent(DittoMessage msg)
        {
            // Identify modified thing and Extract path to determine nature of modification
            string[] topicParts = msg.topic?.Split('/');
            string thingID = topicParts[0] + ":" + topicParts[1];
            
            if (!agentStates.ContainsKey(thingID))
            {
                throw new System.Exception("ModifyAgent called for non-existent Agent: " + thingID);
            }

            string path = msg.path; 
            string[] pathParts = path?.Split('/');
            // Debug.Log("ModifyAgent called with path: " + path + " Path length: " + pathParts.Length);

            // Update concerned AgentInternalState based on path
            switch (path)
            {
                case "/features/status/properties/kinematics":                                      // Kinematic Update
                    Pose rosPose = StateManagerUtils.GetPoseFromMessage(msg.value?["pose"]);           // Extract pose
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
                    agentStates[thingID].pose = pose;                                               // Update agentStates for appropriate entry
                    // Debug.Log("Kinematics Update received for ThingID: " + thingID + " New Position: " + pose.position + " New Orientation: " + pose.orientation);
                    break;
                case "/features/status/properties/hazard_lights_status":                            // Hazard Lights State Update
                    agentStates[thingID].hazardLightStatus = msg.value.Value<int>();
                    // Debug.Log("Hazard Lights Status Update received for ThingID: " + thingID + " New Status: " + agentStates[thingID].hazardLightStatus);
                    // Extract hazard lights state
                    // Update agentStates for appropriate entry
                    break;
                case "/features/status/properties/turn_indicators_status":                           // Turn Indicator State Update
                    agentStates[thingID].indicatorStatus = msg.value.Value<int>();
                    // Debug.Log("Turn Indicator Status Update received for ThingID: " + thingID + " New Status: " + agentStates[thingID].indicatorStatus);
                    // Extract turn indicator state
                    // Update agentStates for appropriate entry
                    break;
                default:
                    throw new System.Exception("Unrecognized modification path: " + path);
                    // break;
            }  
        }

        public void DeleteAgent(DittoMessage msg)
        {
            string[] topicParts = msg.topic?.Split('/');
            string thingID = topicParts[0] + ":" + topicParts[1];
            if (agentStates.TryGetValue(thingID, out AgentInternalState agentState))
            {
                GameObject agent = agentState.poseDrivenVehicle.gameObject;
                agentStates.Remove(thingID);
                Destroy(agent);
                Debug.Log($"Destroyed Agent GameObject : {thingID}");
            }
            else
            {
                Debug.LogWarning($"No agent found with ID : {thingID}");
            }
            // Debug.Log("Deleted event Detected for Agent: " + topicParts[1]);
        }

        public bool IsAgentLive(string agentID, out GameObject agentGO)
        {
            if (agentStates.ContainsKey(agentID))
            {
                agentGO = agentStates[agentID].poseDrivenVehicle.gameObject;
                return true;
            }
            agentGO = null;
            return false;
        }
        public bool IsAgentLive(string agentID)
        {
            return agentStates.ContainsKey(agentID);
        }


        // -----Methods to enqueue messages -----
        public void EnqueueMessage(DittoMessage eventMessage)
        {
            liveMessageQueue.Enqueue(eventMessage);
            // Debug.Log("Added Ditto Event with topic: " + eventMessage.topic + " to Event queue. Queue size: " + eventQueue.Count);
        }

        public void EnqueueEvent(DittoMessage eventMessage)
        {
            eventQueue.Enqueue(eventMessage);
            // Debug.Log("Added Ditto Event with topic: " + eventMessage.topic + " to Event queue. Queue size: " + eventQueue.Count);
        }
    }
}

// Sample code to spawn NPC from NPCSpawner

// public NPCVehicle Spawn(GameObject prefab, uint vehicleID, NPCVehicleSpawnPoint npcVehicleSpawnPoint)
// {
//     // 1. Instantiate the visual GameObject in the world
//     var obj = Object.Instantiate(prefab, npcVehicleSpawnPoint.Position, Quaternion.identity);

//     // 2. Rename the new object in the Hierarchy window for clarity
//     obj.name = obj.name + "_" + vehicleID.ToString();

//     // 3. Orient the object correctly using the spawn point data
//     obj.transform.forward = npcVehicleSpawnPoint.Forward;

//     // 4. Organize the object in the Unity Hierarchy under a parent object
//     obj.transform.parent = NPCVehicleParentsObj.transform;
    
//     // 5. CRITICAL STEP: Get the specific script instance attached to the new GameObject
//     var vehicle = obj.GetComponent<NPCVehicle>();

//     // 6. Initialize the C# data within that retrieved script instance
//     vehicle.VehicleID = vehicleID;

//     // 7. Return the C# script reference to the calling code
//     return vehicle;
// }