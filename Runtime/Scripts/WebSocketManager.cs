using NativeWebSocket;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
using System;
using UnityEditor;
using System.Text.RegularExpressions;
using System.Collections;
using System.IO;
using System.Net;

namespace Flippit
{
    public class ChatChunkMessage
    {
        public string msg_type;
        public string value;
    }
    [Serializable]
    public class Viseme
    {
        public float start;
        public float end;
        public string type;
        public string value;
    }

    [Serializable]
    public class AudioMessage
    {
        public string msg_type;
        public string audio_bytes;
        public List<Viseme> viseme_bytes;
    }

    public class WebSocketManager : MonoBehaviour
    {
        private WebSocket socket;
        private string apiKey;
        private readonly string urlWebsocket = "wss://31ygdxeij1.execute-api.eu-west-1.amazonaws.com/production?Authorizer=";// "wss://rlsasiw8xc.execute-api.eu-west-1.amazonaws.com/staging/?Authorizer=";
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
                    if (message.Contains("animation_key"))
                    {
                        ChatChunkMessage newChunk = JsonUtility.FromJson<ChatChunkMessage>(message);
                        string[] animationSplit = newChunk.value.Split(':');
                        string animationName = animationSplit[0].Trim();
                       
                        string objectName = null;
                        if (animationSplit.Length > 1)
                        {
                            objectName = animationSplit[1].Trim();
                        }

                        dialSc.PlayAnimation(animationName, objectName);
                    }
                    else if (message.Contains("chat_chunk"))
                    {
                        ChatChunkMessage newChunk = JsonUtility.FromJson<ChatChunkMessage>(message);
                        dialSc.ReceiveMessage(newChunk.value);
                    }
                    else if (message.Contains("audio"))
                    {
                        AudioMessage audioMessage = JsonUtility.FromJson<AudioMessage>(message);
                        byte[] decodedAudioBytes = Convert.FromBase64String(audioMessage.audio_bytes);
                        dialSc.WriteIntoFile(decodedAudioBytes);
                        GetComponent<DialogueWindow>().ReceiveVisemes(audioMessage.viseme_bytes);
                    }
                    else if (message.Contains("terminator"))
                    {
                        ChatChunkMessage newChunk = JsonUtility.FromJson<ChatChunkMessage>(message);
                        dialSc.TerminateResponse(newChunk.value);
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