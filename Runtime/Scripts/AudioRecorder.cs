#if UNITY_WEBGL && !UNITY_EDITOR
#define USE_WEBGL
#endif

using Flippit;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using UnityEngine;
using System.Linq;

namespace Flippit
{
    public class AudioRecorder
    {
        public bool isRecording;
        private static readonly Dictionary<string, ClipData> Clips = new();
        public static string[] devices { get; private set; }
        public static event Action<string[]> OnDevicesLoaded;
        private float[] recordedSamples;

        public void StartRefresh(Action<string[]> onDevicesLoaded)
        {
            Debug.Log("Refresh des Micros");
#if USE_WEBGL
            RefreshDevicesWebGL(onDevicesLoaded);
#else
            RefreshDevices(onDevicesLoaded);
#endif
        }
        
        [AOT.MonoPInvokeCallback(typeof(ClipCallbackDelegate))]
        private static void UpdateClip(string key)
        {
            if (!Clips.ContainsKey(key))
            {
                Debug.Log($"Failed to find key '{key}' to update Clip");
                return;
            }
            var clipData = Clips[key];
            var position = GetPosition(key);

#if USE_WEBGL
            var samples = new float[clipData.clip.samples];
            WebGLMicrophone.MicrophoneWebGL_GetData(key, samples, samples.Length, 0);
            Debug.Log($"Update {key}-->  length : "+clipData.clip.length+ " last : "+ clipData.last+ " samples : "+ clipData.clip.samples);
            clipData.clip.SetData(samples, position);
#endif
            clipData.last = position;
        }


#if USE_WEBGL
        private async void RefreshDevicesWebGL(Action<string[]> onDevicesLoaded)
        {
            devices = await WebGLMicrophone.MicrophoneWebGL_Devices();
            onDevicesLoaded(devices);
            OnDevicesLoaded?.Invoke(devices);
        }
        public static AudioClip Start(string device, bool loop, int lengthSec, int frequency)
        {
            var key = device ?? "";
            var clip = CreateClip(key, loop, lengthSec, frequency, 1);
            WebGLMicrophone.MicrophoneWebGL_Start(key, loop, lengthSec, frequency, 1, UpdateClip, DeleteClip);
            return clip;
        }
#else
        private void RefreshDevices(Action<string[]> onDevicesLoaded)
        {
            devices = Microphone.devices;
            onDevicesLoaded(devices);
            OnDevicesLoaded?.Invoke(devices);
        }
        public Task<AudioClip> StartRecordingAsync(string device, bool loop, int recordingMaxDuration, int frequency)
        {
            if (!isRecording)
            {
                isRecording = true;
                return Task.FromResult(Microphone.Start(device, loop, recordingMaxDuration, frequency));
            }
            else
            {
                Debug.LogWarning("Recording is already in progress.");
                return Task.FromResult<AudioClip>(null);
            }
        }
#endif

       
        public void EndRecording(string device)
        {
            var key = device ?? "";
#if USE_WEBGL
            WebGLMicrophone.MicrophoneWebGL_End(key);
#else
            Microphone.End(device);
#endif
            isRecording = false;
        }

        private async Task<string> TranscribeAudioAsync(byte[] audioBytes)
        {
            await Task.Delay(TimeSpan.FromSeconds(5)); // Simulate transcription delay
            string placeholderTranscription = "This is a placeholder transcription.";

            return placeholderTranscription;
        }
        private static AudioClip CreateClip(string device, bool loop, int lengthSec, int frequency, int channels)
        {
            var clip = AudioClip.Create($"'{device}'_clip", frequency * lengthSec, channels, frequency, loop);
            Clips[device] = new ClipData
            {
                clip = clip,
            };
            Debug.Log($"Create Clip with {device}");
            return clip;
        }

        public static int GetPosition(string device)
        {
            var key = device ?? "";
#if USE_WEBGL
            return WebGLMicrophone.MicrophoneWebGL_GetPosition(key);
#else
            return Microphone.GetPosition(device);
#endif
        }

        public static bool IsRecording(string device)
        {
            var key = device ?? "";
#if USE_WEBGL
            return WebGLMicrophone.MicrophoneWebGL_IsRecording(key);
#else
            return Microphone.IsRecording(device);
#endif
        }

        public static bool HasPermission(string device)
        {
#if UNITY_IOS
        return Application.HasUserAuthorization(UserAuthorization.Microphone);
#elif UNITY_ANDROID
        return Permission.HasUserAuthorizedPermission(Permission.Microphone);
#else
            return true;
#endif
        }

        public static void RequestPermission(string device)
        {
#if UNITY_IOS
        Application.RequestUserAuthorization(UserAuthorization.Microphone);
#elif UNITY_ANDROID
        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            Permission.RequestUserPermission(Permission.Microphone);
        }
#endif
        }


        [AOT.MonoPInvokeCallback(typeof(ClipCallbackDelegate))]
        private static void DeleteClip(string key)
        {
            Debug.Log($"Called Delete Clip '{key}'");
            if (!Clips.ContainsKey(key))
            {
                Debug.Log($"Failed to find key '{key}' for deletion");
                return;
            }
            Clips.Remove(key);
        }



    }
}