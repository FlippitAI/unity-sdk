using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace Flippit.Editor
{
    public class ApiManager
    {
        private const string BaseURL = "https://studio-api.flippit.ai";
        private const string ContentTypeHeader = "application/json";
        public static string GetRequest(string endpoint, string accessToken = null, string refreshToken = null)
        {
            string url = $"{BaseURL}/{endpoint}";
            return SendRequest(url, UnityWebRequest.kHttpVerbGET, null, accessToken, refreshToken);
        }

        public static string PostRequest(string endpoint, string data, string accessToken = null, string refreshToken = null)
        {
            string url = $"{BaseURL}/{endpoint}";
            return SendRequest(url, UnityWebRequest.kHttpVerbPOST, data, accessToken, refreshToken);
        }

        private static string SendRequest(string url, string method, string data = null, string accessToken = null, string refreshToken = null)
        {
            using UnityWebRequest webRequest = new(url, method);
            if (!string.IsNullOrEmpty(data))
            {
                byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(data);
                webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
            }

            webRequest.downloadHandler = new DownloadHandlerBuffer();
            webRequest.SetRequestHeader("Content-Type", ContentTypeHeader);

            if (!string.IsNullOrEmpty(accessToken))
            {
                webRequest.SetRequestHeader("X-FLIPPIT-ACCESS-TOKEN", accessToken);
            }

            if (!string.IsNullOrEmpty(refreshToken))
            {
                webRequest.SetRequestHeader("X-FLIPPIT-REFRESH-TOKEN", refreshToken);
            }

            AsyncOperation asyncOperation = webRequest.SendWebRequest();
            while (!asyncOperation.isDone)
            {
                // Wait for the request to complete
            }

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                string response = webRequest.downloadHandler.text;
                return response;
            }
            else
            {
                Debug.LogError("Request failed. Error: " + webRequest.error);
                return null;
            }
        }
    } 
}
