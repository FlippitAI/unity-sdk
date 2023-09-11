using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
    }

}
