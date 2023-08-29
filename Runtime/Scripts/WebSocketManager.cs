using NativeWebSocket;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
using System;
using UnityEditor;

namespace Flippit
{
    public class ChatChunkMessage
    {
        public string msg_type;
        public string value;
    }
    
    public class WebSocketManager : MonoBehaviour
    {
        private WebSocket socket;
        private string apiKey;
        private readonly string urlWebsocket = "wss://ozmcki0ooj.execute-api.eu-west-1.amazonaws.com/production?Authorizer=";
        private readonly string urlCharacterId = "&characterId=";
        private string characterId = ""; 
        
        private string lastState = "";
        private string actualState = "";
        [HideInInspector]
        public string characterInfos;
        DialogueWindow dialSc;
        private ApiKeyManager apiKeyManager;

        public void StartWebSocket(string characterId) 
        {
            this.characterId = characterId;
            Start();
        }

        private void Start()
        {
            apiKeyManager = Resources.Load<ApiKeyManager>("ApiKeys");

            if (apiKeyManager != null && !string.IsNullOrEmpty(apiKeyManager.Flippit))
            {
                apiKey = apiKeyManager.Flippit;
                dialSc = GetComponent<DialogueWindow>();

                Dictionary<string, string> headers = new()
                {
                    { "Origin", "Unity" },
                };

                socket = new WebSocket(url: urlWebsocket + apiKey + urlCharacterId + characterId, headers: headers);
                Open();

            }
            else
            {
                Debug.LogError("API Key is not set. Please try log out from Flippit Studio on the web, log back in and refresh the page without cache");
            }
        }

        public async void Open()
        {
            if (dialSc != null && Application.internetReachability == NetworkReachability.ReachableViaLocalAreaNetwork)
            {
                socket.OnOpen += () =>
                {
                    Debug.Log("Connection open : ");
                };
                socket.OnError += (e) =>
                {
                    Debug.LogError("Error : " + e);
                };
                socket.OnClose += (e) =>
                {
                    Debug.Log("Connection closed :" + e);
                };
                socket.OnMessage += (bytes) =>
                {
                    var message = Encoding.UTF8.GetString(bytes);
                    OnChatChunkReceived?.Invoke(message);
                    //Debug.Log(" message : " + message);
                    if (message.Contains("animation_key"))
                    {
                        ChatChunkMessage newChunk = JsonUtility.FromJson<ChatChunkMessage>(message);

                        string[] animationSplit = newChunk.value.Split(':');
                        string animationName = animationSplit[0].Trim();
                        // Debug.Log(animationName);

                        string objectName = null;
                        if (animationSplit.Length > 1)
                        {
                            objectName = animationSplit[1].Trim();
                            // Debug.Log(objectName);

                        }

                        GetComponent<DialogueWindow>().PlayAnimation(animationName, objectName);
                    }
                    else if (message.Contains("chat_chunk"))
                    {
                        ChatChunkMessage newChunk = JsonUtility.FromJson<ChatChunkMessage>(message);
                        GetComponent<DialogueWindow>().ReceiveMessage(newChunk.value);
                    }
                };
            }
            await socket.Connect();
        }

        void Update()
        {
            #if !UNITY_WEBGL || UNITY_EDITOR
            if (socket.State == WebSocketState.Open && socket != null) socket.DispatchMessageQueue();
            #endif
            if (lastState != actualState)
            {
                actualState = socket.State.ToString();
                lastState = actualState;
                Debug.Log(socket.State);
            }
        }

        public async void SendWebSocketMessage(string message)
        {
            if (socket.State == WebSocketState.Open && message != null)
            {
                OnChatChunkReceived?.Invoke(message);
                await socket.SendText(message);
            }
        }

        public Action<string> OnChatChunkReceived
        {
            get;
            set;
        }

        public void closeWebsocket()
        {
            if (socket.State == WebSocketState.Open && socket != null) socket.Close();
        }

        private void OnApplicationQuit()
        {
            if (socket.State == WebSocketState.Open && socket != null) socket.Close();
        }
    }
}
