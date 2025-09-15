
using UnityEngine;
using NativeWebSocket;
using System.Collections.Generic;
using System.Text;
using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class AgentStateRecieverWebSocket : MonoBehaviour
{


    [SerializeField] private GameObject unknownPrefab; // Default prefab for unknown class
    // [SerializeField] private AssetBundle unknownBundle;
    [SerializeField] private GameObject vehiclePrefab; // Assign agent vehicle prefab in Inspector
    [SerializeField] private GameObject trackedCar; // Assign tracked car prefab in Inspector
    // [SerializeField] public AssetBundle carBundle;
    [SerializeField] private GameObject trackedTruck; // Assign tracked truck prefab in Inspector
    // [SerializeField] /public AssetBundle truckBundle;
    [SerializeField] private GameObject trackedPedestrian; // Assign tracked pedesrian prefab in Inspector
    [SerializeField] public AssetBundle pedestrianBundle;
    [SerializeField] private Transform mapOrigin;
    [SerializeField] private Transform mapOriginMod;
    private WebSocket websocket;
    private Dictionary<string, GameObject> agents = new Dictionary<string, GameObject>();
    private Dictionary<string, GameObject> trackedObjects = new Dictionary<string, GameObject>();

    private readonly Dictionary<string, string> classMap = new Dictionary<string, string>
    {
        { "0", "unknown" }, { "1", "car" }, { "2", "truck" },
        { "3", "bus" }, { "4", "trailer" }, { "5", "motorcycle" },
        { "6", "bicycle" }, { "7", "pedestrian" }
    };
    private Dictionary<string, GameObject> prefabMap = new Dictionary<string, GameObject>();
    // private Dictionary<string, AssetBundle> prefabMap = new Dictionary<string, AssetBundle>();
    // private Dictionary<string, string[]> rosClassToPrefabMap = new Dictionary<string, string[]>
    // {
    //     { "car", new string[] { "car", "car" } },
    //     { "carsmooth", new string[] { "smoothbundle", "CarPrefabSmooth" } },
    //     { "pedestrian", new string[] { "pedestrian", "human" } },
    //     { "bike", new string[] { "bikebundle", "C125" } }
    // };

    void Awake()
    {
        prefabMap["car"] = trackedCar;
        prefabMap["truck"] = trackedTruck;

        foreach (var pair in prefabMap)
        {
            if (pair.Value == null)
            {
                Debug.LogWarning($"Prefab for class {pair.Key} is not assigned in Inspector.");
            }
        }
    }

    async void Start()
    {
        string base64Auth = Convert.ToBase64String(Encoding.UTF8.GetBytes("ditto:ditto"));
        var headers = new Dictionary<string, string>
        {
            { "Authorization", "Basic " + base64Auth }
        };
        websocket = new WebSocket("ws://localhost:8080/ws/2", headers);
        websocket.OnOpen += () =>
        {
            Debug.Log("Connected to Ditto WebSocket!");
            SubscribeToEvents();
            // SubscribeToAnnouncements();

            //SubscribeToMessages();

        };


        websocket.OnError += (e) =>
        {
            Debug.LogError("WebSocket error: " + e);
        };


        websocket.OnClose += (e) =>
        {
            Debug.Log("WebSocket closed: " + e);
        };


        websocket.OnMessage += (bytes) =>
        {
            string message = Encoding.UTF8.GetString(bytes);

            JObject json = JObject.Parse(message);


            Debug.Log("Received: " + message);
            DittoMessage msg = JsonConvert.DeserializeObject<DittoMessage>(message);
            string[] parts = msg.topic.Split('/');

            Debug.Log("Is events:" + parts[parts.Length - 2].Equals("events"));


            if (parts[parts.Length - 2].Equals("events"))
            {
                HandleAgentEvent(msg);
            }
            else if (parts[parts.Length - 2].Equals("messages"))
            {
                Debug.Log("Received message: " + message);
                HandleAgentMessage(msg);
            }
        };


        await websocket.Connect();


    }

    void Update()
    {
        #if !UNITY_WEBGL || UNITY_EDITOR
                websocket.DispatchMessageQueue();

        #endif
    }

    async void OnApplicationQuit()
    {
        await websocket.Close();

    }

    void HandleAgentEvent(DittoMessage msg)
    {
        //DittoMessage msg = JsonUtility.FromJson<DittoMessage>(json);
        // DittoMessage msg = JsonConvert.DeserializeObject<DittoMessage>(json);
        string topic = msg.topic;
        Debug.Log("Received event: " + topic);
        
        
        
        // Retrieve the group
        // Retrieve entity name
        
        if (topic.EndsWith("/created"))
        {
            // created Event
            // Thing wrapper object
            //ThingWrapper thing = JsonUtility.FromJson<ThingWrapper>(msg.value);
            ThingWrapper thing = JsonConvert.DeserializeObject<ThingWrapper>(msg.value.ToString());

            // spawn function call
            SpawnAgent(thing);
            // websocket.SendText("STOP-SEND-EVENTS");
            SubscribeToMessages(); // Placed here temporarily as simultaneously not subscribing to events and messages.Find a way to move this to the start() function.  
        }        
        else if (topic.EndsWith("/modified"))
        {
            UpdateAgent(msg);
            //Debug.Log("Received modified event for: ");
            
        }
        else if (topic.EndsWith("/deleted"))
        {
            
            // deleted event   
            string[] parts = topic.Split('/');
            string name = parts[0] + ":" + parts[1];
            Debug.Log("topicDeleted: " + topic.EndsWith("/deleted"));
            // despawn appropriate prefab
            Debug.Log("callling DespawnAgent with ID: " + name);
            DespawnAgent(name);
        }
        else
        {
            Debug.LogError("Unknown event type: " + topic);
        }

        
    }

    void SpawnAgent(ThingWrapper thing)
    {
        // retrieve the pose of the agent
        //Pose pose = JsonUtility.FromJson<Pose>(thing.features);
        Features features = JsonConvert.DeserializeObject<Features>(thing.features["status"].ToString());
        Pose pos = features.properties.kinematics.pose;
        Vector3 rosPos = new Vector3(pos.position.x, pos.position.y, pos.position.z);
        Vector3 worldPosition = mapOrigin.TransformPoint(ConvertRos2UnityPosition(rosPos));
        Quaternion rotation = ConvertRos2UnityRotation(new Quaternion(pos.orientation.x, pos.orientation.y, pos.orientation.z, pos.orientation.w));
        // Quaternion rotation = new Quaternion(pos.orientation.x, pos.orientation.y, pos.orientation.z, pos.orientation.w);
        // spawn the agent prefab
        // GameObject agent = Instantiate(vehiclePrefab, position, rotation);
        // GameObject agent = new GameObject();
        GameObject agent = Instantiate(vehiclePrefab, worldPosition, rotation, this.transform);
        // agent.AddComponent<GizmoData>().color = Color.blue;
        // agent.transform.SetParent(this.transform);
        // agent.transform.position = worldPosition;
        // agent.transform.rotation = rotation;
        // set the agent id
        agent.name = thing.thingID;
        agents.Add(thing.thingID, agent);
        Debug.Log("AgentCreated with ID: " + thing.thingID + " at position: " + worldPosition + " with rotation: " + rotation);
        Debug.Log("Agent URDF" + thing.attributes.ToString());
        // set the agent position and orientation
        //agent.transform.position = new Vector3(thing.features.properties.kinematics.pose.position.x, thing.features.properties.kinematics.pose.position.y, thing.features.properties.kinematics.pose.position.z);
    }

    void UpdateAgent(DittoMessage msg) 
    {
        string[] parts = msg.topic.Split('/');
        string thing_id = parts[0] + ":" + parts[1];
        string targetPath = msg.path;

        if (agents.TryGetValue(thing_id, out GameObject targetObject))
        {
            if (targetPath.EndsWith("kinematics"))
            {
                Pose pos = JsonConvert.DeserializeObject<Pose>(msg.value["pose"].ToString());
                Vector3 position = ConvertRos2UnityPosition(new Vector3(pos.position.x, pos.position.y, pos.position.z));
                Vector3 worldPosition = mapOrigin.TransformPoint(position);
                Quaternion rotation = ConvertRos2UnityRotation(new Quaternion(pos.orientation.x, pos.orientation.y, pos.orientation.z, pos.orientation.w));

                targetObject.transform.position = worldPosition;
                targetObject.transform.rotation = rotation;

                
            }
        }
    }

    void DespawnAgent(string id)
    {
        if (agents.TryGetValue(id, out GameObject agent))
        {
            Destroy(agent);
            agents.Remove(id);
            Debug.Log($"Destroyed Agent : {id}");
        }
        else
        {
            Debug.LogWarning($"No agent found with ID : {id}");
        }
        
    }

    void HandleAgentMessage(DittoMessage msg)
    {
        Debug.Log("HELLO FROM MESSAGE HANDLER");
        // It is presently assumed that only tracked object messages use the live message channel
        // The existance probablity or the classification confidence are currently not used for anything.
        string topic = msg.topic;
        string[] topic_parts = topic.Split('/');
        msg.value = DecodeBase64ToJToken(msg.value);
        Debug.Log("Message Value: " + msg.value.ToString());

        // Identify parent object
        string parentName = topic_parts[0] + ":" + topic_parts[1];
        // Extract object array
        JArray objectsArray = msg.value["objects"] as JArray;
        if (objectsArray == null)
        {
            Debug.LogWarning("No 'objects' array found in value.");
            return;
        }
        

        foreach (JObject obj in objectsArray)
        {
            string objID = new Guid(obj["object_id"].ToObject<byte[]>()).ToString();
            GameObject parent = null;

            if (!agents.TryGetValue(parentName, out parent))
            {
                Debug.LogWarning($"Parent {parentName} not found for tracked object {objID}.");
                // Despawn child object if parent no longer exists?? Is it possible?
                return;
            }

            if (trackedObjects.TryGetValue(objID, out GameObject existingObject))
            {
                Debug.Log($"Tracked Object {objID} already exists under {parentName}");
                UpdateTrackedObject(parent, existingObject, obj);
                return;
            }
            else
            {
                SpawnTrackedObject(parent, obj);
            }

        }       
        

        // Debug.Log("Received message: " + topic);

    }

    void SpawnTrackedObject(GameObject parent, JObject obj)
    {
        string parentName = parent.name;
        string objClass = GetObjectClass(obj);
        // string objID = $"{obj["object_id"]}";
        string objID = new Guid(obj["object_id"].ToObject<byte[]>()).ToString();
        string objName = $"{objClass}_{objID}";
        string objKey = $"{parentName}/{objName}";

        // Check if existing

        // if (!agents.TryGetValue(parentName, out GameObject parent))
        // {
        //     Debug.LogWarning($"Parent {parentName} not found for tracked object {objName}.");
        //     return;
        // }

        // if (trackedObjects.TryGetValue(objKey, out GameObject existingObject))
        // {
        //     Debug.Log($"Tracked Object {objName} already exists under {parentName}");
        //     return;
        // }

        // Spawning new Object
        Pose pos = GetPoseFromMsg(obj);
        if (pos == null || pos.position == null || pos.orientation == null)
        {
            Debug.LogWarning($"Invalid pose for tracked object {objName}. Skipping spawn.");
            return;
        }
        
        Transform parentTransform = parent.transform;
        Vector3 position = ConvertRos2UnityPosition(new Vector3(pos.position.x, pos.position.y, pos.position.z));
        Quaternion orientation = ConvertRos2UnityRotation(new Quaternion(pos.orientation.x, pos.orientation.y, pos.orientation.z, pos.orientation.w));

        Vector3 worldPosition = mapOrigin.TransformPoint(position);
        Quaternion worldRotation = orientation; // Placeholder. Also convert for worldRotation

        // GameObject trackedObject = new GameObject();
        GameObject prefab = prefabMap.TryGetValue(objClass, out GameObject mappedPrefab) ? mappedPrefab : unknownPrefab;
        if (prefab == null)
        {
            Debug.LogWarning($"No prefab for class {objClass}. Using empty GameObject.");
            prefab = new GameObject();
        }

        GameObject trackedObject = Instantiate(prefab, worldPosition, worldRotation, this.transform);
        // trackedObject.AddComponent<GizmoData>().color = Color.yellow;
        // var gizmo = trackedObject.AddComponent<DebugGizmo>();
        // trackedObject.transform.SetParent(this.transform);
        // trackedObject.transform.position = worldPosition;
        // trackedObject.transform.rotation = worldRotation;
        trackedObject.name = objName;
        trackedObjects.Add(objID, trackedObject);

        Debug.Log($"Tracked object {objID} spawned under {parentName} at {position} with rotation {orientation.eulerAngles}, class: {objClass}");

    }

    void UpdateTrackedObject(GameObject parent, GameObject trackedObject, JObject obj)
    {
        string parentName = parent.name;
        string objClass = GetObjectClass(obj);
        string objID = new Guid(obj["object_id"].ToObject<byte[]>()).ToString();

        Pose pos = GetPoseFromMsg(obj);
        if (pos == null || pos.position == null || pos.orientation == null)
        {
            Debug.LogWarning($"Invalid pose for tracked object {objID}. Skipping update.");
            return;
        }

        Transform parentTransform = parent.transform;
        Vector3 position = ConvertRos2UnityPosition(new Vector3(pos.position.x, pos.position.y, pos.position.z));
        Quaternion orientation = ConvertRos2UnityRotation(new Quaternion(pos.orientation.x, pos.orientation.y, pos.orientation.z, pos.orientation.w));

        Vector3 worldPosition = mapOrigin.TransformPoint(position);
        Quaternion worldRotation = orientation; // Placeholder. Also convert for worldRotation

        trackedObject.transform.position = worldPosition;
        // trackedObject.GetComponent<DetectedObject>().updatePosition(worldPosition);
        trackedObject.transform.rotation = worldRotation;

        Debug.Log($"Tracked object: {objID} of parent: {parentName} updated. ");



    }

    void DespawnTrackedObject()
    {
        // Implementation
    } 


    private void SubscribeToEvents()
    {
        Debug.Log("Subscribing to events");
        websocket.SendText("START-SEND-EVENTS");
    }

    private void SubscribeToMessages()
    {
        Debug.Log("Subscribing to messages");
        websocket.SendText("START-SEND-MESSAGES");
    }

    private void SubscribeToAnnouncements()
    {
        Debug.Log("Subscribing to announcements");
        websocket.SendText("START-SEND-ANNOUNCEMENTS");
    }

    
    string getObjectClass1(JObject payload)
    {
        string classID = payload["classification"][0]["label"].ToString();
        // Debug.Log("ClassID" + classID);
        string objClass = "";
        switch (classID)
        {
            case "0":
                objClass = "unknown";
                break;
            case "1":
                objClass = "car";
                break;
            case "2":
                objClass = "truck";
                break;
            case "3":
                objClass = "bus";
                break;
            case "4":
                objClass = "trailer";
                break;
            case "5":
                objClass = "motorcycle";
                break;
            case "6":
                objClass = "bicycle";
                break;
            case "7":
                objClass = "pedestrian";
                break;
            default:
                objClass = "unknown";
                break;
        }
        // Debug.Log("ObjectClass" + objClass);
        return objClass;
    }

    string GetObjectClass(JObject payload)
    {
        string classID = payload["classification"]?[0]?["label"]?.ToString() ?? "0";
        return classMap.TryGetValue(classID, out string objClass) ? objClass : "unknown";
    }


    void spawnTrackedObject(string parentName, JObject obj)
    {
        string obj_class = GetObjectClass(obj);
        string obj_name = obj_class + "_" + obj["object_id"];
        GameObject parent = GameObject.Find(parentName);
        Transform existingChild = parent.transform.Find(obj_name);
        // Debug.Log("child exists " + existingChild != null);



        if (existingChild == null)
        {
            Debug.Log("child will be spawned");
            GameObject trackedObject = new GameObject();
            trackedObject.transform.SetParent(parent.transform);
            trackedObject.name = obj_name;

            // Pose pos = JsonConvert.DeserializeObject<Pose>(obj["kinematics"]["pose_with_covariance"].ToString());
            Pose pos = GetPoseFromMsg(obj);
            Debug.Log("obj[kinamatics][pose_with_covariance]: " + obj["kinematics"]["pose_with_covariance"]);
            Debug.Log("Object pos values: " + JsonUtility.ToJson(pos,true));
            Vector3 position = ConvertRos2UnityPosition(new Vector3(pos.position.x, pos.position.y, pos.position.z));
            Quaternion orientation = ConvertRos2UnityRotation(new Quaternion(pos.orientation.x, pos.orientation.y, pos.orientation.z, pos.orientation.w));
            trackedObject.transform.localPosition = position;
            trackedObject.transform.localRotation = orientation;
            Debug.Log("ObjectCreated with name: " + obj_name + " at position: " + position + " with rotation: " + orientation);

            // trackedObject.transform.position = new Vector3(obj[]);

        }
        else
        {
            Debug.Log("child already exists");
        }
        Debug.Log("TrackedObject name: " + obj_name + "spawned under " + parentName);
    }

    Pose GetPoseFromMsg(JObject obj)
    {
        Pose pos = new Pose();
        pos.position = new Position();
        pos.orientation = new Orientation();

        JToken poseToken = obj?["kinematics"]?["pose_with_covariance"]?["pose"];
        // JToken orientationToken = obj?["kinematics"]?["pose_with_covariance"]?["pose"]?["orientation"];
        if (poseToken != null)
        {
            pos.position.x = poseToken?["position"]["x"]?.Value<float>() ?? 0f;
            pos.position.y = poseToken?["position"]["y"]?.Value<float>() ?? 0f;
            pos.position.z = poseToken?["position"]["z"]?.Value<float>() ?? 0f;
            pos.orientation.x = poseToken?["orientation"]["x"]?.Value<float>() ?? 0f;
            pos.orientation.y = poseToken?["orientation"]["y"]?.Value<float>() ?? 0f;
            pos.orientation.z = poseToken?["orientation"]["z"]?.Value<float>() ?? 0f;
            pos.orientation.w = poseToken?["orientation"]["w"]?.Value<float>() ?? 0f;
        }
        
        // pos.orientation = 
        return pos;
    }

    Vector3 ConvertRos2UnityPosition1(Vector3 rosPos)
    {
        Vector3 RosToUnityPosition = new Vector3(1f, -1f, 1f);
        return Vector3.Scale(rosPos, RosToUnityPosition);
    }
    Quaternion ConvertRos2UnityRotation1(Quaternion rosQuat)
    {
        Quaternion frameAlignment = Quaternion.Euler(90f, 0f, 90f);
        Quaternion alignedQuat = frameAlignment * rosQuat;

        Vector3 euler = alignedQuat.eulerAngles;
        euler.x = -euler.x;
        euler.z = -euler.z;
        
        return Quaternion.Euler(euler);
    }

    Vector3 ConvertRos2UnityPosition(Vector3 rosPos)
    {
        return new Vector3(-rosPos.y,rosPos.z, rosPos.x);
    }
    Quaternion ConvertRos2UnityRotation(Quaternion rosQuat)
    {
        Vector3 rosEuler = RosQuaternionToEuler(rosQuat);

        Vector3 unityEuler = new Vector3(rosEuler.y * Mathf.Rad2Deg, -rosEuler.z * Mathf.Rad2Deg, -rosEuler.x * Mathf.Rad2Deg);
        
        return Quaternion.Euler(unityEuler);
    }

    private static Vector3 RosQuaternionToEuler(Quaternion q)
    {
        // Unity’s Quaternion.eulerAngles gives degrees, but we want radians first
        Vector3 eulerDeg = q.eulerAngles;
        return new Vector3(
            eulerDeg.x * Mathf.Deg2Rad,
            eulerDeg.y * Mathf.Deg2Rad,
            eulerDeg.z * Mathf.Deg2Rad
        );
    }

    
    JToken DecodeBase64ToJToken(JToken token)
    {
        if (token.Type == JTokenType.String)
        {
            string base64 = token.ToString();

            try
            {
                byte[] utf8Bytes = Convert.FromBase64String(base64);
                string jsonString = Encoding.UTF8.GetString(utf8Bytes);

                // Parse the decoded string into a JToken
                return JToken.Parse(jsonString);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Decoding failed: " + ex.Message);
            }
        }

        // Return original token if not a string or decoding fails
        return token;
    }



    // ========================= RVIZ to Unity to Agent obstacle spawning =========================

    public void SendLiveMessage(string thingId, string subject, JToken payload, string path = "/obstacles", bool encodeBase64 = true)
    {
        if (websocket == null || websocket.State != WebSocketState.Open)
        {
            Debug.LogWarning("WebSocket is not connected. Cannot send live message.");
            return;
        }

        // Assuming thingId is in the format "namespace:name"
        string topic = $"{thingId}/messages/{subject}/things/live/messages/{subject}";

        // TODO: maybe also include optional header construction? Needs changed DittoMessage class.
        var headers = new Dictionary<string, string>
        {
            { "Content-Type", "application/json" },
            { "response-required", "false" },
            { "correlation-id", System.Guid.NewGuid().ToString() }
        };

        JToken messagePayload = encodeBase64 ? JToken.FromObject(EncodeJTokenToBase64(payload)) : payload;

        var liveMessage = new SendLiveMessage
        {
            topic = topic,
            path = path,
            headers = headers,
            value = messagePayload
        };

        string messageJson = JsonConvert.SerializeObject(liveMessage);
        websocket.SendText(messageJson);
        // Debug.Log("Sent live message: " + messageJson);

    } // SendLiveMessage()

    public void BroadcastLiveToAgents(JToken payload, string subject, string path = "/obstacles", bool encodeBase64 = true)
    {
        if (agents == null || agents.Count == 0)
        {
            Debug.LogWarning("No agents available to broadcast live message.");
            return;
        }
        Debug.Log($"Broadcasting live message to {agents.Count} agents.");

        foreach (var agent in agents)
        {
            string thingId = agent.Key; //! Assuming the key is the thingId
            SendLiveMessage(thingId, subject, payload, path, encodeBase64);
        }
    } // BroadcastLiveToAgents()



    [Serializable]
    public class SendLiveMessage
    { 
        public string topic;
        public string path;
        public Disctionary<string, string> headers;
        public JToken value; // If value is a complex object, use: public ValueType value;
    }


    private static string EncodeJTokenToBase64(JToken token)
    {
        string jsonString = token.ToString(Formatting.None);
        byte[] utf8Bytes = Encoding.UTF8.GetBytes(jsonString);
        return Convert.ToBase64String(utf8Bytes);
    } // EncodeJTokenToBase64()












    // New: Component to store gizmo color
    public class GizmoData : MonoBehaviour
    {
        public Color color = Color.yellow;
    }
    private void OnDrawGizmos()
    {
        // New: Add debug log to confirm method is called
        // Debug.Log($"OnDrawGizmos called, trackedObjects count: {trackedObjects?.Count ?? 0}");
        if (trackedObjects == null || trackedObjects.Count == 0)
        {
            Debug.LogWarning("No tracked objects to draw gizmos for.");
            return;
        }

        foreach (var pair in trackedObjects)
        {
            GameObject obj = pair.Value;
            GizmoData gizmo = obj.GetComponent<GizmoData>();
            if (gizmo != null)
            {
                Gizmos.color = gizmo.color;
                Gizmos.DrawSphere(obj.transform.position, 1f);
                Gizmos.DrawLine(obj.transform.position, obj.transform.position + obj.transform.forward * 1f);
            }
        }

        foreach (var pair in agents)
        {
            GameObject obj = pair.Value;
            GizmoData gizmo = obj.GetComponent<GizmoData>();
            if (gizmo != null)
            {
                Gizmos.color = gizmo.color;
                Gizmos.DrawSphere(obj.transform.position, 1f);
                Gizmos.DrawLine(obj.transform.position, obj.transform.position + obj.transform.forward * 1f);
            }
        }
        // Existing: Draw mapOrigin gizmo
        if (mapOrigin != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(mapOrigin.position, 0.7f);
        }
    }

    // Data classes for JSON parsing
    [Serializable]
    public class DittoMessage
    {
        public string topic;
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
    public class Features
    {
        public Properties properties;
    }

    [Serializable]
    public class Properties
    {
        public Kinematics kinematics;
        public HazardLightStatus status;
    }

    [Serializable]
    public class HazardLightStatus
    {
        public int report;
    }

    [Serializable]
    public class Kinematics
    {
        public Pose pose;
    }

    [Serializable]
    public class Pose
    {
        public Position position;
        public Orientation orientation;
    }

    [Serializable]
    public class Position
    {
        public float x, y, z;
    }

    [Serializable]
    public class Orientation
    {
        public float x, y, z, w;
    }
}
