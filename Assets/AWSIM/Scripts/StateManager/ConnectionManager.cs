using UnityEngine;
using NativeWebSocket;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
// using ROS2;
// using AWSIM;
// using uPLibrary.Networking.M2Mqtt;
// using uPLibrary.Networking.M2Mqtt.Messages;
// using M2MqttUnity;

namespace CDT
{
    public class ConnectionManager : MonoBehaviour
    {
        // Inputs and declarations
        [Header("Websocket Settings")]
        [SerializeField] private string host = "localhost";
        [SerializeField] private int port = 8080;

        private WebSocket ws;

        // Agent Manager reference
        public AgentManager agentManager;

        // Tracked Object Manager reference
        // public TrackedObjectManager trackedObjectManager;

        // Events
        // public event Action<JObject> onDittoEvent;
        // public event Action<JObject> onDittoLiveMessage;
               
        // Lifecycle methods
        async void Start()
        {
            string url = $"ws://{host}:{port}/ws/2";
            string base64Auth = Convert.ToBase64String(Encoding.UTF8.GetBytes("ditto:ditto"));
            var headers = new Dictionary<string, string>
            {
                { "Authorization", "Basic " + base64Auth }
            };

            ws = new WebSocket(url, headers);

            ws.OnOpen += () =>
            {
                Debug.Log("Connected to Ditto WebSocket!");
                SubscribeToDittoTopics();
            };

            ws.OnError += (e) =>
            {
                Debug.Log("WebSocket error: " + e);
            };

            ws.OnClose += (e) =>
            {
                Debug.Log("WebSocket connection closed with code: " + e);
            };

            ws.OnMessage += (bytes) =>
            {
                var message = Encoding.UTF8.GetString(bytes);                
                DittoMessage msg = JsonConvert.DeserializeObject<DittoMessage>(message);
                string[] parts = msg.topic?.Split('/');

                // Debug.Log("Received WebSocket message: " + message);
                // JObject jsonMessage = JObject.Parse(message);
                // string msgTopic = jsonMessage["topic"]?.ToString();
                // string[] parts = msgTopic?.Split('/');

                if (parts[parts.Length - 2].Equals("events"))
                {
                    // onDittoEvent?.Invoke(jsonMessage);
                    agentManager?.EnqueueEvent(msg);
                    // trackedObjectManager?.HandleDittoEvent(jsonMessage);
                }
                else if (parts[parts.Length - 2].Equals("messages"))
                {
                    // onDittoLiveMessage?.Invoke(jsonMessage);
                    // agentManager?.HandleDittoLiveMessage(msg);
                    agentManager?.EnqueueMessage(msg);
                }
                else
                {
                    Debug.LogError("Received unrecognized topic: " + msg.topic);
                }
            };

            try
            {
                await ws.Connect();
            }
            catch (Exception ex)
            {
                Debug.LogError("WebSocket connection failed: " + ex.Message);
            }

            
        }

        void Update()
        {
            #if !UNITY_WEBGL || UNITY_EDITOR
                    ws?.DispatchMessageQueue();
            #endif
        }

        void OnApplicationQuit()
        {
            if (ws != null)
                ws.Close();
        }

        // Other methods

        // ----- Subscribe to Ditto topics-----
        private async void SubscribeToDittoTopics()
        {
            await ws.SendText("START-SEND-EVENTS");
            Debug.Log("Subscribed to Ditto events.");
            await ws.SendText("START-SEND-MESSAGES");
            Debug.Log("Subscribed to Ditto live messages.");
        }

        // ----- Send message to Ditto -----
        public async void SendDittoMessage(JObject dittoMsg)
        {
            if (ws.State == WebSocketState.Open)
            {
                await ws.SendText(dittoMsg.ToString());
            }
            else
            {
                Debug.LogError("WebSocket is not open. Cannot send message.");
            }
        }
    }
}