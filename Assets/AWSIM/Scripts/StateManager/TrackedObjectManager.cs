using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine.Rendering;

namespace CDT{
    public class TrackedObjectManager : MonoBehaviour
    {
        public Transform mapOrigin;
        [Header("Tracked Object Prefabs")]
        [SerializeField] private GameObject[] unknownPrefabs;                  // Default prefab for unknown class
        // [SerializeField] private GameObject trackedCar;                     // Assign tracked car prefab in Inspector
        [SerializeField] private GameObject[] trackedCarPrefabs;
        [SerializeField] private GameObject[] trackedTruckPrefabs;                   // Assign tracked truck prefab in Inspector
        [SerializeField] private GameObject[] trackedPedestrianPrefabs;              // Assign tracked pedesrian prefab in Inspector
        
        private ConcurrentQueue<DittoMessage> msgQueue = new ConcurrentQueue<DittoMessage>();
        private List<TrackedObject> spawnRequests = new List<TrackedObject>();
        private Dictionary<string, TrackedObjectInternalState> trackedObjectStates = new Dictionary<string, TrackedObjectInternalState>();
        private Dictionary<string, (TrackedObjectInternalState objstate,float lastSeen)> disabledObjectStates = new Dictionary<string, (TrackedObjectInternalState,float)>();
        private Dictionary<string, GameObject[]> prefabMap = new Dictionary<string, GameObject[]>();

        // Lifecycle Methods
        void Awake()
        {
            // Initialize prefab map
            prefabMap["car"] = trackedCarPrefabs;
            prefabMap["truck"] = trackedTruckPrefabs;
            prefabMap["pedestrian"] = trackedPedestrianPrefabs;
            prefabMap["unknown"] = unknownPrefabs;

            foreach (var pair in prefabMap)
            {
                if (pair.Value == null)
                {
                    Debug.LogWarning($"Prefab for class {pair.Key} is not assigned in Inspector.");
                }
            }
        }
        void Update()
        {
            // Dequeue messages and update relevant tracked object state
            while(msgQueue.TryDequeue(out DittoMessage msg))
            {
                // Debug.Log("Processing Ditto Live Message with topic: " + msg.topic + ". Remaining queue size: " + msgQueue.Count);
                try
                {
                    UpdateTrackedObjectStatesV2(msg);
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
            // if (spawnRequests.Count > 0)
            // {
            //     Debug.Log("Identified " + spawnRequests.Count + " new tracked objects in LateUpdate().");
            // }
            int spawnCount = 0;
            List<string> objIDsToremove = new List<string>();
            
            foreach (var trackedObject in spawnRequests)
            {
                string objectID = trackedObject.parent + ":" + new Guid(trackedObject.object_id).ToString("B");
                // Spawn the tracked object in the Unity scene
                if (trackedObjectStates.ContainsKey(objectID))
                {
                    Debug.LogWarning("Tracked object with ID: " + objectID + " already exists. Skipping spawn.");
                    continue;
                }
                Debug.Log("Spawning tracked object with ID: " + objectID);
                SpawnTrackedObject(trackedObject);
                spawnCount += 1;
                // Implementation of spawning logic goes here
            }
            if (spawnCount > 0)
            {
                Debug.Log("Spawned " + spawnCount + " new tracked objects in LateUpdate().");
            }    
            spawnRequests.Clear();

            foreach (var disabledObj in disabledObjectStates)
            {
                string disabbledObjectID = disabledObj.Key;
                TrackedObjectInternalState disabledObjectstate = disabledObj.Value.objstate;
                float timeSinceDisabled = Time.time - disabledObj.Value.lastSeen;
                if (timeSinceDisabled > 30f) // If an object has been disabled for more than 30 seconds, remove it from the scene and from disabledObjectStates
                {
                    Debug.Log("Removing tracked object with ID: " + disabbledObjectID + " as it has been disabled for " + timeSinceDisabled + " seconds.");
                    UnityEngine.Object.Destroy(disabledObjectstate.poseDrivenEntity.gameObject);
                    objIDsToremove.Add(disabbledObjectID);
                }
            }
            if (objIDsToremove.Count > 0)
            {
                foreach (string objID in objIDsToremove)
                {
                   disabledObjectStates.Remove(objID);
                }
                objIDsToremove.Clear();
            }                                      
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
            List<TrackedObject> msgObjects = RetrieveMessageObjects(msg);
            // Debug.Log("Number of tracked objects in message: " + objectsArray.Count);
            int newObjectsCount = 0;
            int objectIndex = 0;
            foreach (TrackedObject objToken in msgObjects)                                                          // loop through the list
            {
                TrackedObjectInternalState matchedState = null;                
                string matchedOldId = null;
                float distanceFromCandidate = float.MaxValue;

                // string objectID = topicParts[1] + ":" + ByteArrayToIdString(trackedObject.object_id);
                // string objectID = trackedObject.parent + ":" + BitConverter.ToString(trackedObject.object_id).Replace("-", "");
                
                string objectID = objToken.parent + ":" + new Guid(objToken.object_id).ToString("B");
                // Debug.Log("Processing tracked object: " + objectID);

                // if (!trackedObjectStates.ContainsKey(objectID))
                // {
                //     spawnRequests.Add(trackedObject);
                //     newObjectsCount += 1;
                //     continue;
                // }

                if (!trackedObjectStates.ContainsKey(objectID)) // check if new object token is already being tracked (if-not)
                {                    
                    string objClass = StateManagerUtils.GetObjectClass(objToken.classification);
                    UnityPose incomingPose = new UnityPose();
                    // Try to find a similar object based on proximity (simple heuristic)
                    foreach (var kvp in trackedObjectStates)  
                    {
                        string candidateID = kvp.Key;
                        TrackedObjectInternalState candidateState = kvp.Value;
                        distanceFromCandidate = float.MaxValue;

                        // Object class matching
                        if (candidateState.objClass != objClass)
                        {
                            continue;
                        }

                        foreach (var to in msgObjects)  // Check if the identified candidate is also in the incoming message
                        {
                            string toID = to.parent + ":" + new Guid(to.object_id).ToString("B");
                            if (toID == candidateID)
                            {
                                continue;
                            }
                        }

                        // Proximity check
                        Pose incomingRosPose = objToken.kinematics.pose_with_covariance.pose;
                        UnityPose inCentPose = new UnityPose();
                        UnityPose canCentPose = new UnityPose();
                        incomingPose.position = StateManagerUtils.ConvertRos2UnityPosition(new Vector3(
                            incomingRosPose.position.x,
                            incomingRosPose.position.y,
                            incomingRosPose.position.z
                        ));
                        incomingPose.orientation = StateManagerUtils.ConvertRos2UnityRotation(new Quaternion(
                            incomingRosPose.orientation.x,
                            incomingRosPose.orientation.y,
                            incomingRosPose.orientation.z,
                            incomingRosPose.orientation.w
                        ));
                        incomingPose.position = mapOrigin.TransformPoint(incomingPose.position);
                        incomingPose = candidateState.poseDrivenEntity.ConvertCentroidPoseToRoot(incomingPose);
                        incomingPose.position = StateManagerUtils.FollowGround(incomingPose.position);
                        inCentPose = candidateState.poseDrivenEntity.ConvertRootPoseToCentroid(incomingPose);
                        canCentPose = candidateState.poseDrivenEntity.ConvertRootPoseToCentroid(candidateState.pose);
                        

                        // float distanceFromCandidate = Vector3.Distance(inCentPose.position, canCentPose.position);
                        distanceFromCandidate = Vector3.Distance(inCentPose.position, canCentPose.position);
                        Bounds candidateBounds = candidateState.poseDrivenEntity.GetLocalBounds();
                        // float distanceFromCandidateX = Mathf.Abs(inCentPose.position.x - canCentPose.position.x);
                        // float distanceFromCandidateZ = Mathf.Abs(inCentPose.position.z - canCentPose.position.z);
                        float distanceTolerance = Mathf.Max(candidateBounds.extents.x, candidateBounds.extents.z) * 0.8f;
                    

                        if (distanceFromCandidate <= distanceTolerance)
                        {
                            matchedState = candidateState;
                            matchedOldId = candidateID;
                            Debug.Log("Matched new tracked object " + objectID + " to existing object " + matchedOldId + " based on proximity.");
                            break;
                        }
                    }
                    if (matchedState != null)
                    {
                        Debug.Log("Matched state is not null, reassigning ID.");
                        matchedState.poseDrivenEntity.gameObject.name = objectID; // Update GameObject name
                        trackedObjectStates.Remove(matchedOldId);
                        matchedState.objectID = objectID;
                        matchedState.pose = incomingPose;
                        trackedObjectStates.Add(objectID, matchedState);
                        matchedState = null;
                        matchedOldId = null;
                        // Debug.Log("Reassigned matched object from old ID: " + matchedOldId + " to new ID: " + objectID);
                    }
                    else
                    {
                        // Debug.Log("No match found for tracked object " + objectID + "distnce from nearest.");
                        spawnRequests.Add(objToken);
                        newObjectsCount += 1;
                        continue;
                    }
                }              

                // Update existing tracked object state
                ModifyTrackedObject(objToken);
                objectIndex += 1;
                
            }
            // Debug.Log("Spawning" + newObjectsCount + " new tracked objects, and updated " + (
                    // objectIndex) + " existing tracked objects.");
        }

        public void UpdateTrackedObjectStatesV2(DittoMessage msg)
        {
            List<TrackedObject> msgObjects = RetrieveMessageObjects(msg);
            // Debug.Log("Number of tracked objects in message: " + objectsArray.Count);
            // int newObjectsCount = 0;
            // int objectIndex = 0;
            List<string> objIDsToremove = new List<string>();
            

            foreach (var objToken in trackedObjectStates)    // For each tracked object in trackedObjects
            {
                string objectID = objToken.Key;
                TrackedObjectInternalState trackedObject = objToken.Value;
                bool objectFoundInMessage = false;
                
                foreach (TrackedObject obj in msgObjects) // Check if the object is still in the message
                {
                    if (obj.parent + ":" + new Guid(obj.object_id).ToString("B") == objectID) // identify match in msgObjects not efficient to resolve neame everytime like this
                    {
                        // Update the state tracked object from message
                        ModifyTrackedObject(obj); // Update the state tracked object from message
                        msgObjects.Remove(obj); // remove entry from msgObjects
                        objectFoundInMessage = true;
                        break;
                    }
                }
                if (!objectFoundInMessage)
                {
                    // Disable the tracked object
                    objIDsToremove.Add(objectID);
                }        
            }

            if (objIDsToremove.Count > 0)
            {
                foreach (string objID in objIDsToremove)
                {
                    DisableTrackedObject(objID);
                }
                objIDsToremove.Clear();
            }

            foreach (TrackedObject obj in msgObjects) // for every remaining object in msgObjects
            {
                TrackedObjectInternalState matchedState = null;                
                string matchedOldId = null;
                float distanceFromCandidate = float.MaxValue;
                string objectID = obj.parent + ":" + new Guid(obj.object_id).ToString("B");
                string objClass = StateManagerUtils.GetObjectClass(obj.classification);
                UnityPose incomingPose = new UnityPose();
                UnityPose inCentPose = null;
                // remaining objects in the message are either new objects or disabled objects with a new ID
                
                // Check if the incoming object matches any disabled objects based on proximity and obj class
                if (!disabledObjectStates.ContainsKey(objectID))
                {
                    // Debug.Log("Object ID" + objectID + " not found in disabled objects list, checking for proximity matches in " + disabledObjectStates.Count + " disabled objects");
                    foreach(var disabledObj in disabledObjectStates)
                    {
                        string disabledObjectID = disabledObj.Key;
                        TrackedObjectInternalState disabledObjectState = disabledObj.Value.objstate;

                        // Object class matching
                        if (disabledObjectState.objClass != objClass)
                        {
                            // Debug.Log("Skipping disabled object " + disabledObjectID + " due to class mismatch. Incoming: " + objClass + ", Disabled: " + disabledObjectState.objClass);
                            continue;
                        }
                        // Proximity check
                        if (inCentPose == null)  // If not already calculated
                        {
                            // Debug.Log("Calculating incoming centroid pose for proximity check.");
                            Pose incomingRosPose = obj.kinematics.pose_with_covariance.pose;
                            incomingPose.position = StateManagerUtils.ConvertRos2UnityPosition(new Vector3(
                                incomingRosPose.position.x,
                                incomingRosPose.position.y,
                                incomingRosPose.position.z
                            ));
                            incomingPose.orientation = StateManagerUtils.ConvertRos2UnityRotation(new Quaternion(
                                incomingRosPose.orientation.x,
                                incomingRosPose.orientation.y,
                                incomingRosPose.orientation.z,
                                incomingRosPose.orientation.w
                            ));
                            incomingPose.position = mapOrigin.TransformPoint(incomingPose.position);
                            incomingPose = disabledObjectState.poseDrivenEntity.ConvertCentroidPoseToRoot(incomingPose);
                            incomingPose.position = StateManagerUtils.FollowGround(incomingPose.position);
                            inCentPose = disabledObjectState.poseDrivenEntity.ConvertRootPoseToCentroid(incomingPose);
                        }
                                               
                        // Debug.Log("checking proximity against disabled object " + disabledObjectID);
                        UnityPose canCentPose = new UnityPose();
                        canCentPose = disabledObjectState.poseDrivenEntity.ConvertRootPoseToCentroid(disabledObjectState.pose);
                        // Debug.Log("Incoming centroid pose: " + inCentPose.position + ", Candidate centroid pose: " + canCentPose.position);
                        distanceFromCandidate = Vector3.Distance(inCentPose.position, canCentPose.position);
                        Bounds candidateBounds = disabledObjectState.poseDrivenEntity.GetLocalBounds();
                        float distanceTolerance = Mathf.Max(candidateBounds.extents.x, candidateBounds.extents.z) * 0.8f;
                        // Debug.Log("Distance from candidate disabled object " + disabledObjectID + " is " + distanceFromCandidate + " with tolerance " + distanceTolerance);
                        if (distanceFromCandidate <= distanceTolerance)  // Match found in disabledObjectStates
                        {
                            matchedState = disabledObjectState;
                            matchedOldId = disabledObjectID;
                            Debug.Log("Matched new tracked object " + objectID + " to existing object " + matchedOldId + " based on proximity.");
                            break;
                        }
                    }
                    if (matchedState != null)  // If match was found,
                    {
                        // Update ID in matched object and Enable it
                        // Debug.Log("Matched state is not null, reassigning ID.");
                        EnableTrackedObject(matchedOldId);
                        matchedState.poseDrivenEntity.gameObject.name = objectID; // Update GameObject name
                        matchedState.objectID = objectID;
                        matchedState.pose = incomingPose;
                        trackedObjectStates.Remove(matchedOldId);
                        trackedObjectStates.Add(objectID, matchedState);
                        matchedState = null;
                        matchedOldId = null;
                        // Debug.Log("Reassigned matched object from old ID: " + matchedOldId + " to new ID: " + objectID);
                    }
                    else
                    {
                        Debug.Log("No match found for tracked object " + objectID + ", spawning new object.");
                        spawnRequests.Add(obj);
                        continue;
                    }
                }
                else
                {
                    Debug.Log("Object ID found in disabled objects, enabling: " + objectID);
                    EnableTrackedObject(objectID);
                    continue;
                }
                
            }    
        }
        
        public List<TrackedObject> RetrieveMessageObjects(DittoMessage msg)
        {
            List<TrackedObject> messageObjectList = new List<TrackedObject>();
            msg.value = StateManagerUtils.DecodeBase64ToJToken(msg.value);                                      // decode the  value field of live message which is base64 encoded JSON
            string[] topicParts = msg.topic?.Split('/');      
            // Debug.Log("Processing tracked object message for Agent: " + this.gameObject.name);   

            if (msg.value["objects"] == null || msg.value["objects"].Type != JTokenType.Array)
            {
                throw new System.Exception("Invalid or missing 'objects' field in tracked object message");
            }

            JArray objectsArray = msg.value["objects"] as JArray;                                               // Extract the list of tracked objects from the recieved tracked message
            foreach (JObject objToken in objectsArray)
            {
                TrackedObject trackedObject = objToken.ToObject<TrackedObject>();
                trackedObject.parent = topicParts[1];
                messageObjectList.Add(trackedObject);
            }
            return messageObjectList;
        }
        public void SpawnTrackedObject(TrackedObject trackedObject)
        {
            TrackedObjectInternalState newObject = new TrackedObjectInternalState();
            // string objectID = trackedObject.parent + ":" + ByteArrayToIdString(trackedObject.object_id);
            string objectID = trackedObject.parent + ":" + new Guid(trackedObject.object_id).ToString("B");
            Pose rosPose = trackedObject.kinematics.pose_with_covariance.pose;
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
            string objClass = StateManagerUtils.GetObjectClass(trackedObject.classification);
            GameObject[] prefabs = prefabMap.TryGetValue(objClass, out GameObject[] mappedPrefab) ? mappedPrefab : unknownPrefabs;
            GameObject prefab = prefabs[UnityEngine.Random.Range(0, prefabs.Length)];
            PoseDrivenEntity prefabPDE = prefab.GetComponent<PoseDrivenEntity>();
            pose = prefabPDE.ConvertCentroidPoseToRoot(pose);
            pose.position = StateManagerUtils.FollowGround(pose.position);

            var obj = UnityEngine.Object.Instantiate(prefab, pose.position, pose.orientation);           // Instantiate the visual GameObject in the world           
            obj.name = objectID;                                                                    // Rename the new object in the Hierarchy window for clarity
            obj.transform.parent = this.transform.parent;
            PoseDrivenEntity detObj;
            switch (objClass)
            {
                case "car":
                case "truck":
                    detObj = obj.GetComponent<PoseDrivenVehicle>();   // Switch to PoseDrivenVehicle
                    detObj.Register(this);                            // Register the DetectedObject with this TrackedObjectManager
                    break;
                case "pedestrian":
                    detObj = obj.GetComponent<PoseDrivenPedestrian>();   // Switch to PoseDrivenPedestrian
                    detObj.Register(this);                            // Register the DetectedObject with this TrackedObjectManager
                    break;
                default:
                    detObj = obj.GetComponent<PoseDrivenEntity>();   // Switch to generic PoseDrivenEntity
                    detObj.Register(this);                            // Register the DetectedObject with this TrackedObjectManager
                    break;
            }
            // PoseDrivenEntity detObj = obj.GetComponent<PoseDrivenEntity>();   // Switch to PoseDrivenVehicle                                          // CRITICAL STEP: Get the specific script instance attached to the new GameObject
            // detObj.Register(this);                            // Register the DetectedObject with this TrackedObjectManager
            newObject.pose = pose;
            newObject.objectID = objectID;
            newObject.poseDrivenEntity = detObj;
            newObject.trackedObjectManager = this;                                                                                    // Register the DetectedObject with this TrackedObjectManager
            newObject.objClass = objClass;
            trackedObjectStates.Add(objectID, newObject);
            Debug.Log("Spawned tracked object with ID: " + objectID + " of class: " + objClass);
            Debug.Log("Number of tracked objects being managed: " + trackedObjectStates.Count);
        }

        public void ModifyTrackedObject(TrackedObject trackedObject)
        {
            string objectID = trackedObject.parent + ":" + new Guid(trackedObject.object_id).ToString("B");
            Pose rosPose = trackedObject.kinematics.pose_with_covariance.pose;
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
            TrackedObjectInternalState targetObject = trackedObjectStates[objectID];
            pose = targetObject.poseDrivenEntity.ConvertCentroidPoseToRoot(pose);
            targetObject.pose = pose;
            targetObject.poseDrivenEntity.ApplyPose(pose);

            // Debug.Log("Modifying tracked object state for object ID: " + objectID);
            // Update the relevant tracked object state variables here
        }

        public void DisableTrackedObject(string objectID)
        {
            TrackedObjectInternalState targetObject = trackedObjectStates[objectID];
            trackedObjectStates.Remove(objectID);
            targetObject.poseDrivenEntity.gameObject.SetActive(false);
            disabledObjectStates.Add(objectID, (targetObject, Time.time));
            Debug.Log("Disabled tracked object with ID: " + objectID);
        }

        public void EnableTrackedObject(string objectID)
        {
            TrackedObjectInternalState targetObject = disabledObjectStates[objectID].objstate;
            disabledObjectStates.Remove(objectID);
            targetObject.poseDrivenEntity.gameObject.SetActive(true);
            trackedObjectStates.Add(objectID,targetObject);
            Debug.Log("Enabled tracked object with ID: " + objectID);
        }

        public void RemoveTrackedObject(string objectID)
        {
            if (trackedObjectStates.ContainsKey(objectID))
            {
                TrackedObjectInternalState targetObject = trackedObjectStates[objectID];
                UnityEngine.Object.Destroy(targetObject.poseDrivenEntity.gameObject);
                trackedObjectStates.Remove(objectID);
                Debug.Log("Removed tracked object with ID: " + objectID);
            }
            else
            {
                Debug.LogWarning("Attempted to remove non-existent tracked object with ID: " + objectID);
            }
        }

    }
}