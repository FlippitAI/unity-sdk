#if UNITY_WEBGL && !UNITY_EDITOR
#define USE_WEBGL
#endif

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using UnityEngine;

#if USE_WEBGL
using System.Runtime.InteropServices;
#endif

namespace Flippit
{
    public static class WebGLMicrophone 
    {
#if USE_WEBGL
        [DllImport("__Internal")]
        public static extern void MicrophoneWebGL_Start(string deviceName, bool loop, int lengthSec, int frequency, int channelCount, Action<string> callback, Action<string> deleteClip);

        [DllImport("__Internal")]
        public static extern int MicrophoneWebGL_GetPosition(string deviceName);

        [DllImport("__Internal")]
        public static extern bool MicrophoneWebGL_IsRecording(string deviceName);

        [DllImport("__Internal")]
        public static extern void MicrophoneWebGL_End(string deviceName);

        [DllImport("__Internal")]
        public static extern void MicrophoneWebGL_GetData(string deviceName, float[] samples, int length, int offset);
        
        [DllImport("__Internal")]
        private static extern void MicrophoneWebGL_FetchMicrophoneDevices(Action<string> resolve, Action<string> reject);
        
        [DllImport("__Internal")]
        private static extern void MicrophoneWebGL_GetPermission(string deviceName);
        
        [DllImport("__Internal")]
        private static extern bool MicrophoneWebGL_HasPermission(string deviceName);
#endif
        private delegate void FetchResultDelegate(string result);

        private static TaskCompletionSource<string> _taskCompletionSource;

        private static Task<string> FetchAsync()
        {
            _taskCompletionSource = new TaskCompletionSource<string>();
#if USE_WEBGL
            MicrophoneWebGL_FetchMicrophoneDevices(ResolveFetch, RejectFetch);
#endif
            return _taskCompletionSource.Task;
        }

        public static async Task<string[]> MicrophoneWebGL_Devices()
        {
            var jsonTask = await FetchAsync();
            return jsonTask.Split('|');
        }

        [AOT.MonoPInvokeCallback(typeof(FetchResultDelegate))]
        private static void ResolveFetch(string result)
        {
            _taskCompletionSource.SetResult(result);
        }

        [AOT.MonoPInvokeCallback(typeof(FetchResultDelegate))]
        private static void RejectFetch(string error)
        {
            _taskCompletionSource.SetException(new Exception(error));
        }
    }

}
