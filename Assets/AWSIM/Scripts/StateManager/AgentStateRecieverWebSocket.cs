
using UnityEngine;
using NativeWebSocket;
using System.Collections.Generic;
using System.Text;
using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class AgentStateRecieverWebSocket : MonoBehaviour
{
    //public GameObject agentPrefab; // Assign your agent prefab in the Inspector

    private WebSocket websocket;
    private WebSocket websocket2;
    private Dictionary<string, GameObject> agents = new Dictionary<string, GameObject>();

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
            Debug.Log("Received: " + message);
            HandleAgentEvent(message);
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

    void HandleAgentEvent(string json)
    {
        //DittoMessage msg = JsonUtility.FromJson<DittoMessage>(json);
        DittoMessage msg = JsonConvert.DeserializeObject<DittoMessage>(json);
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
            string name = parts[1];
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

    void HandleAgentMessage(string json)
    {
        Debug.Log("Received message: " + json);
        
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

    void SpawnAgent(ThingWrapper thing)
    {
        // retrieve the pose of the agent
        //Pose pose = JsonUtility.FromJson<Pose>(thing.features);
        Features features = JsonConvert.DeserializeObject<Features>(thing.features["status"].ToString());
        Pose pos = features.properties.kinematics.pose;
        Vector3 position = new Vector3(pos.position.x, pos.position.y, pos.position.z);
        Quaternion rotation = new Quaternion(pos.orientation.x, pos.orientation.y, pos.orientation.z, pos.orientation.w);
        // spawn the agent prefab
        //GameObject agent = Instantiate(agentPrefab, pointssition, rotation);
        GameObject agent = new GameObject();
        agent.transform.SetParent(GameObject.Find("StateManager")?.transform);
        agent.transform.position = position;
        agent.transform.rotation = rotation;
        // set the agent id
        agent.name = thing.thingID;
        Debug.Log("AgentCreated with ID: " + thing.thingID + " at position: " + position + " with rotation: " + rotation);
        // set the agent position and orientation
        //agent.transform.position = new Vector3(thing.features.properties.kinematics.pose.position.x, thing.features.properties.kinematics.pose.position.y, thing.features.properties.kinematics.pose.position.z);
    }

    void UpdateAgent(DittoMessage msg) 
    {
        string[] parts = msg.topic.Split('/');
        string thing_id = parts[0] + ":" + parts[1];
        string targetPath = msg.path;

        GameObject targetObject = GameObject.Find(thing_id);

        // If the modified event is for kinematics
        if (targetPath.EndsWith("kinematics"))
        {
            Pose pos = JsonConvert.DeserializeObject<Pose>(msg.value["pose"].ToString());
            Debug.Log("Recieved Agent Pose: " + JsonUtility.ToJson(pos,true));
            // Vector3 position = new Vector3(-pos.position.y, -pos.position.z, pos.position.x);
            // Quaternion rotation = new Quaternion(pos.orientation.y, -pos.orientation.z, -pos.orientation.x, pos.orientation.w);
            Vector3 position = new Vector3(pos.position.x, -pos.position.y, pos.position.z);
            Quaternion rotation = new Quaternion(pos.orientation.x, pos.orientation.y, -pos.orientation.z, pos.orientation.w);
            
            targetObject.transform.localRotation = rotation;
            targetObject.transform.localPosition = position;
            // Debug.Log("AgentUpdated KINEMATICS with ID: " + thing_id);
        }
        
        // You can update more properties here (rotation, state, etc.)
    }

    void DespawnAgent(string id)
    {
        Debug.Log("DespawnAgent called with ID: " + id);
        // destroy the agent game object
         bool found = false;
        // Find all root GameObjects in the scene
        foreach (GameObject obj in UnityEngine.Object.FindObjectsOfType<GameObject>())
        {
            if (obj.name.Contains(id))
            {
                Destroy(obj);
                Debug.Log("Destroyed GameObject: " + obj.name);
                found = true;
            }
        }
        if (!found)
        {
            Debug.Log("No GameObject found with name: " + id);
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
    // public class AgentEvent
    // {
    //     public string @event; // "spawn", "update", "despawn"
    //     public SimpleCarWrapper car;
    // }
    // [Serializable]
    public class ThingWrapper
    {
        public string thingID;
        public string policyID;
        public JToken attributes;
        public JToken features;
    }
    // [Serializable]
    // public class SimpleCarWrapper
    // {
    //     public Features features;
    // }

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
