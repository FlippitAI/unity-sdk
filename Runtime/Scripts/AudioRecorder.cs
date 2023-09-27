#if UNITY_WEBGL && !UNITY_EDITOR
#define USE_WEBGL
#endif

using Flippit;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using UnityEngine;

namespace Flippit
{
    public class AudioRecorder
    {
        public bool isRecording;
        private static readonly Dictionary<string, ClipData> Clips = new();
        public static string[] Devices { get; private set; }
        public static event Action<string[]> OnDevicesLoaded;

        public void StartRefresh(Action<string[]> onDevicesLoaded)
        {
#if USE_WEBGL
        RefreshDevicesWebGL(onDevicesLoaded);
#else
            RefreshDevices(onDevicesLoaded);
#endif
        }

        [AOT.MonoPInvokeCallback(typeof(ClipCallbackDelegate))]
        private static void UpdateClip(string key)
        {
            //Debug.Log($"Received data for {key}");
            if (!Clips.ContainsKey(key))
            {
                Debug.Log($"Failed to find key '{key}'");
                return;
            }
            var clipData = Clips[key];
            var position = GetPosition(key);
            var samples = new float[clipData.clip.samples];
#if USE_WEBGL
            WebGLMicrophone.MicrophoneWebGL_GetData(key, samples, samples.Length, 0);
#endif
            clipData.clip.SetData(samples, position);
            clipData.last = position;
        }

        private void RefreshDevices(Action<string[]> onDevicesLoaded)
        {
#if USE_WEBGL
            RefreshDevicesWebGL(onDevicesLoaded);
#else
            Devices = Microphone.devices;
            onDevicesLoaded(Devices);
            OnDevicesLoaded?.Invoke(Devices);
#endif
        }
        private async void RefreshDevicesWebGL(Action<string[]> onDevicesLoaded)
        {
            Devices = await WebGLMicrophone.MicrophoneWebGL_Devices();
            onDevicesLoaded(Devices);
            OnDevicesLoaded?.Invoke(Devices);
        }


        public Task<AudioClip> StartRecordingAsync(string device, bool loop, int recordingMaxDuration, int frequency)
        {
            if (isRecording)
            {
                Debug.LogWarning("Recording is already in progress.");
                return Task.FromResult<AudioClip>(null);
            }
            isRecording = true;
            var key = device ?? "";
#if USE_WEBGL
            var recordingClip = CreateClip(key, loop, recordingMaxDuration, frequency, 1);
            WebGLMicrophone.MicrophoneWebGL_Start(key, loop, recordingMaxDuration, frequency, 1, UpdateClip, DeleteClip);
            return Task.FromResult(recordingClip);
#else
            return Task.FromResult(Microphone.Start(device, loop, recordingMaxDuration, frequency));
#endif

        }

        public void EndRecording(string device)
        {
            isRecording = false;
            var key = device ?? "";
#if USE_WEBGL
            WebGLMicrophone.MicrophoneWebGL_End(key);
#else
            Microphone.End(device);
#endif
        }

        private async Task<string> TranscribeAudioAsync(byte[] audioBytes)
        {
            await Task.Delay(TimeSpan.FromSeconds(5)); // Simulate transcription delay
            string placeholderTranscription = "This is a placeholder transcription.";

            return placeholderTranscription;
        }
        private static AudioClip CreateClip(string device, bool loop, int lengthSec, int frequency, int channels)
        {
            var clip = AudioClip.Create($"{device}_clip", frequency * lengthSec, channels, frequency, loop);
            Clips[device] = new ClipData
            {
                clip = clip,
            };
            Debug.Log($"Started with {device}");
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
            Debug.Log($"Called Delete {key}");
            if (!Clips.ContainsKey(key))
            {
                Debug.Log($"Failed to find key '{key}' for deletion");
                return;
            }
            Clips.Remove(key);
        }
    }
}