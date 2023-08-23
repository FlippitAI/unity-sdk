using NativeWebSocket;
using UnityEngine;
using System.Text;
using System;

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
        //private readonly string cleApi = "PpbKR5SR445RnhhNJvzYyvd44DMvUqbX";
        private readonly string urlA = "wss://lyp2td10ke.execute-api.eu-west-1.amazonaws.com/prod?key=3Ga4B25zs3H8RgJy&characterId=";//Id=[ID du perso test]
        //private readonly string urlB = "&characterId=";
        private string characterId = ""; 
        
        private string lastState = "";
        private string actualState = "";
        [HideInInspector]
        public string characterInfos;
        DialogueWindow dialSc;
        
        public void StartWebSocket(string characterId) 
        {
            this.characterId = characterId;
            Start();
        }

        private void Start()
        {
            dialSc = GetComponent<DialogueWindow>();
            socket = new WebSocket(urlA + characterId);
            Open();
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

        public void CloseWebsocket()
        {
            if (socket.State == WebSocketState.Open && socket != null) socket.Close();
        }

        private void OnApplicationQuit()
        {
            if (socket.State == WebSocketState.Open && socket != null) socket.Close();
        }
    }
}
