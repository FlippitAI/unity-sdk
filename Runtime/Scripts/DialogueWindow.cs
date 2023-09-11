#if UNITY_WEBGL && !UNITY_EDITOR
#define USE_WEBGL
#endif

using Cinemachine;
using OpenAI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

using static Flippit.EnumLists;


namespace Flippit
{
    public class CharacterInfos
    {
        public string ownerId;
        public string action;
        public string name;
        public string backstory;
        public string role;
        public string catchPhrases;
        public string primaryGoal;
        public string urls;
        public string assetFilePath;
        public string hobbies;
        public string moodId;
        public string personalityId;
        public string characterId;
        public string voiceId;
        public string ageID;
        public string prompt;
    }

    public class ClipData
    {
        public AudioClip clip;
        public int last;
    }
    public delegate void ClipCallbackDelegate(string device);
    public class DialogueWindow : MonoBehaviour
    {
        #region public var
        public static event System.Action<string[]> OnDevicesLoaded;
        public static string[] devices { get; private set; }
         
        [Header("reference Game Objects")]
        public GameObject ChatContainer;
        public GameObject DiscussionPanel;
        public GameObject inputField;
       
        [Header("Inputs Options")]
        public bool UseMicrophone;
        public bool displayInputFieldPanel;
        public KeyCode pushToTalkButton = KeyCode.Tab;
        public KeyCode ExitDiscussion = KeyCode.Escape;
        public int recordingMaxDuration = 10;

        [Header("Output Options")]
        public bool ShowDiscussion;
        public float windowMaxWidth = 300f, windowMaxHeight = 200f;
        public bool useVoices;

        [Header("Camera Options")]
        public float CameraOffsettargetX = -1;
        public float CameraOffsettransitionDuration = 2;
        [HideInInspector]
        public float FadeDuration = .1f;

        [HideInInspector]
        public string TextInput;
        [HideInInspector]
        public IaPersonality IaPersona;
        [HideInInspector]
        public string discussion;
        [HideInInspector]
        public GameObject IaActive;
        [HideInInspector]
        public bool internet;
        #endregion
        #region private var
        private TextMeshProUGUI inputText;
        private TextMeshProUGUI chatAreaText;
        private float currentTime = 0;
        private IACharacter iaSc;
        private GameObject playerGO;
        private CinemachineVirtualCamera VCam;
        private float elapsedTime;
        private string speechSentence;
        private readonly List<string> sentences = new();
        private int currentSentenceIndex = 0;
        private bool firstSentence;
        private CanvasGroup dialogueCanvasGroup;
        private TextMeshProUGUI InputMessage;
        private bool isRecording;
        private AudioClip clip;
        private float time;
        private readonly string fileName = "recording.wav";
        private OpenAIApi openai;
        private ApiKeyManager apiKeyManager;
        private bool isTalkingToCharacter = false;
        private static readonly Dictionary<string, ClipData> Clips = new ();
        #endregion
        WebSocketManager manager;
        EnumLists lists;

        public List<List<Viseme>> visemeSets = new List<List<Viseme>>();

        // Start is called before the first frame update
        void Start()
        {
            apiKeyManager = Resources.Load<ApiKeyManager>("Apikeys");
            openai= new(apiKeyManager.OpenAI);
            InputMessage = DiscussionPanel.GetComponentInChildren<TextMeshProUGUI>();
            inputText = inputField.GetComponent<TextMeshProUGUI>();
            chatAreaText = DiscussionPanel.GetComponentInChildren<TextMeshProUGUI>();
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
        
        public static void RefreshDevices(Action<string[]> Callback)
        {
#if USE_WEBGL
            RefreshDevicesWebGL(Callback);
#else
            devices = Microphone.devices;
            Callback(devices);
            OnDevicesLoaded?.Invoke(devices);
#endif
        }
        private static async void RefreshDevicesWebGL(Action<string[]> Callback)
        {
            devices = await WebGLMicrophone.MicrophoneWebGL_Devices();
            Callback(devices);
            OnDevicesLoaded?.Invoke(devices);
        }
        
        public static void RefreshDevices()
        {
            RefreshDevices(_ => {});
        }
        
        private void Update()
        {
            if (Application.internetReachability != NetworkReachability.NotReachable && UseMicrophone)
            {
                if (Input.GetKeyDown(pushToTalkButton) && !isRecording)
                {
                    StartRecording(devices[0], false, recordingMaxDuration, 44100);
                }
                else if (Input.GetKeyUp(pushToTalkButton))
                {
                    time = 0;
                    isRecording = false;
                    EndRecording();
                    Debug.Log("End of recording");
                }
            }

            if (Input.GetKeyDown(ExitDiscussion) && isTalkingToCharacter)
            {
                FinishDiscussion();
            }

            if (isRecording)
            {
                time += Time.deltaTime;
                if (time >= recordingMaxDuration)
                {
                    time = 0;
                    isRecording = false;
                    EndRecording();
                    Debug.Log("Recording stopped");
                }
            }
        }
        private void OnEnable()
        {
            ChatContainer.SetActive(displayInputFieldPanel);
            inputField.SetActive(displayInputFieldPanel);
            DiscussionPanel.SetActive(ShowDiscussion);

            VCam = FindObjectOfType<CinemachineVirtualCamera>();
            iaSc = IaActive.GetComponent<IACharacter>();
            playerGO = GameObject.FindGameObjectWithTag("Player");
            discussion = iaSc.Discussion;

            PositionVCam(true);
            dialogueCanvasGroup = DiscussionPanel.GetComponent<CanvasGroup>();

            if (manager == null)
            {
                manager = GetComponent<WebSocketManager>();
            }

            manager.StartWebSocket(IaPersona.characterId);
        }

        public void SaySomething()
        {
            firstSentence = true;
            if (inputText.text.Length > 1 && IaPersona != null)
            {
                IaPersona.prompt = inputText.text;
                CharacterInfos promptMessage = new() { action = "chat", prompt = inputText.text };
                manager.SendWebSocketMessage(JsonUtility.ToJson(promptMessage));
                inputText.text = "";
            }
        }
        public void SpeechSomething(string speech)
        {
            discussion = "";
            firstSentence = true;
            if (speech.Length > 1 && IaPersona != null)
            {
                IaPersona.prompt = speech;
                CharacterInfos promptMessage = new() { action = "chat", prompt = IaPersona.prompt };
                manager.SendWebSocketMessage(JsonUtility.ToJson(promptMessage));
            }
        }

        public void FinishDiscussion()
        {
            // Reset variables and components
            if (iaSc != null) iaSc.Discussion = discussion;
            discussion = string.Empty;
            sentences.Clear();
            currentSentenceIndex = 0;
            firstSentence = true;
            isRecording = false;
            time = 0;
            clip = null;
            InputMessage.text = string.Empty;
            if (IaActive != null)
            {
                Animator animator = IaActive.GetComponentInChildren<Animator>();
                if (animator != null)
                {
                    animator.SetBool("Talking", false);
                }
            }
            IaActive = null;
            IaPersona = null;
            if (playerGO != null) playerGO.GetComponent<Player>().CloseDialogueBox();
            PositionVCam(false);
            manager.closeWebsocket();
            manager = null;

        }

        public void ReceiveMessage(string message)
        {
            discussion += message;
            speechSentence += message;
            if (Regex.IsMatch(speechSentence, @"[!.?]"))
            {
                speechSentence = Regex.Replace(speechSentence, @"\*(.*?)\*", "");
                sentences.Add(speechSentence);

                speechSentence = null;
                if (useVoices)
                {
                    if (firstSentence)
                    {
                        firstSentence = false;
                        ReadSentences();
                    }
                }

                if (ShowDiscussion)
                {
                    dialogueCanvasGroup.alpha = 1;
                    TextMeshProUGUI textComponent = DiscussionPanel.GetComponentInChildren<TextMeshProUGUI>();
                    textComponent.text = discussion;
                    Vector2 preferredSize = textComponent.GetPreferredValues();
                    float maxWidth = windowMaxWidth;
                    float maxHeight = windowMaxHeight;
                    Vector2 newSize = new(Mathf.Min(preferredSize.x, maxWidth), Mathf.Min(preferredSize.y, maxHeight));
                    DiscussionPanel.GetComponent<RectTransform>().sizeDelta = newSize;
                }

                else
                {
                    chatAreaText.text = discussion;
                }
            }
            else
            {
                if (UseMicrophone)
                {
                    TextMeshProUGUI textComponent = DiscussionPanel.GetComponentInChildren<TextMeshProUGUI>();
                    textComponent.text = discussion;
                    Vector2 preferredSize = textComponent.GetPreferredValues();
                    float maxWidth = 300f;
                    float maxHeight = 200f;
                    Vector2 newSize = new(Mathf.Min(preferredSize.x, maxWidth), Mathf.Min(preferredSize.y, maxHeight));
                    DiscussionPanel.GetComponent<RectTransform>().sizeDelta = newSize;
                }
                else
                {
                    chatAreaText.text = discussion;
                }
            }
        }

        private void PositionVCam(bool dialOuPa)
        {

            if (dialOuPa)
            {
                VCam.LookAt = IaActive.transform;
                StartCoroutine(AnimatePositionX());
            }
            else
            {
                VCam.LookAt = playerGO.transform;
                StartCoroutine(AnimatePositionXReverse());
            }
        }
        private IEnumerator AnimatePositionX()
        {
            VCam.GetComponent<CinemachineCameraOffset>().enabled = true;
            elapsedTime = 0;

            while (elapsedTime < CameraOffsettransitionDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = Mathf.Clamp01(elapsedTime / CameraOffsettransitionDuration);
                float currentX = Mathf.SmoothStep(0, CameraOffsettargetX, t);
                CinemachineCameraOffset cameraOffset = VCam.GetComponent<CinemachineCameraOffset>();
                cameraOffset.m_Offset.x = currentX;
                yield return null;
            }
            isTalkingToCharacter = true;
        }

        private IEnumerator AnimatePositionXReverse()
        {
            elapsedTime = 0;
            while (elapsedTime < CameraOffsettransitionDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = Mathf.Clamp01(elapsedTime / CameraOffsettransitionDuration);
                float currentX = Mathf.SmoothStep(CameraOffsettargetX, 0, t);
                float p = Mathf.Clamp01(currentTime / FadeDuration);
                float alpha = Mathf.Lerp(1, 0, p);
                GetComponentInChildren<CanvasGroup>().alpha = alpha;
                CinemachineCameraOffset cameraOffset = VCam.GetComponent<CinemachineCameraOffset>();
                cameraOffset.m_Offset.x = currentX;
                yield return null;
            }
            VCam.GetComponent<CinemachineCameraOffset>().enabled = false;

            gameObject.SetActive(false);
            isTalkingToCharacter = false;
        }

        public void ReceiveVisemes(List<Viseme> visemesSentence)
        {
            visemeSets.Add(visemesSentence);
        }

        private async void ReadSentences()
        {
            if (currentSentenceIndex >= 0 && currentSentenceIndex < sentences.Count)
            {
                string sentenceToRead = sentences[currentSentenceIndex];
                int index = (int)IaPersona.voice;
                if(index ==0)
                {
                    index = 1;
                }
                lists = new EnumLists();
                string voiceID = lists.voiceNames[index];
                Task speechTask = IaActive.GetComponent<TTS>().SpeechMe(sentenceToRead, voiceID,visemeSets[0]);
                await speechTask;
                visemeSets.RemoveAt(0);
                currentSentenceIndex++;
                ReadSentences();
            }
            else
            {
                Animator animator = IaActive.GetComponentInChildren<Animator>();
                animator.SetBool("Talking", false);
                dialogueCanvasGroup.alpha = 0;
            }
        }


        public void PlayAnimation(string animName, string objectName = null)
        {
            Animator animator = IaActive.GetComponentInChildren<Animator>();
            if (IaActive == null)
            {
                Debug.Log("IaActive GameObject not assigned. Make sure it is assigned in the Unity Editor.");
            }

            if (animName == "Talking" || animName == "Walk" || animName == "Jogging" || animName == "Grab")
            {
                animator.SetBool("Talking", true);
            }
            else
            {
                animator.SetTrigger(animName);
                animator.SetBool("Talking", true);
            }

            if (animName == "Walk" || animName == "Jogging" || animName == "Grab")
            {
                StartCoroutine(PerformActionAnimation(animator, animName, objectName));
            }
        }

        private IEnumerator PerformActionAnimation(Animator animator, string animName, string objectName)
        {
            while (animator.GetBool("Talking"))
            {
                // Wait for Talking to be set to false (from external piece of code)
                yield return new WaitForSeconds(0.5f);
            }

            GameObject[] questObjs = IaActive.GetComponent<IACharacter>().QuestObjects;
            GameObject unityObject = null;

            if (questObjs.Length > 0)
            {
                int matchedObjectIndex = -1;

                for (int i = 0; i < questObjs.Length; i++)
                {
                    unityObject = questObjs[i];

                    if (unityObject != null && (unityObject.name.ToLower().Contains(objectName.ToLower()) || objectName.ToLower().Contains(unityObject.name.ToLower())))
                    {
                        // Object name matches, select it or perform any desired action
                        matchedObjectIndex = i;
                        break;
                    }
                }

                if (matchedObjectIndex != -1)
                {
                    // FinishDiscussion(resetIA: false);

                    NavMeshAgent agent = IaActive.GetComponent<NavMeshAgent>();

                    // Go to the object
                    agent.destination = questObjs[matchedObjectIndex].transform.position;
                    agent.stoppingDistance = 3.0f;

                    while (agent.pathPending || agent.remainingDistance > agent.stoppingDistance)
                    {

                        yield return null;
                    }

                    Debug.Log("Agent has arrived at the destination!");

                    if (animName == "Grab")
                    {
                        Vector3 handOffset = new Vector3(0f, 1.0f, 1.0f); // Adjust the offset as needed

                        // Pick the object
                        if (unityObject != null)
                        {
                            // Store the original position of the object
                            Vector3 objectOriginalPosition = unityObject.transform.position;

                            // Calculate the offset from the avatar's hand position
                            Vector3 handPosition = IaActive.transform.position + IaActive.transform.TransformDirection(handOffset);
                            Vector3 objectOffset = objectOriginalPosition - handPosition;

                            unityObject.transform.position = handPosition;
                            unityObject.transform.rotation = IaActive.transform.rotation;

                            yield return new WaitForSeconds(10.0f); // Delay for the physics engine to stabilize the object

                            // Return to the player
                            // agent.destination = playerGO.transform.position;
                            // agent.stoppingDistance = 3.0f;

                            // while (agent.remainingDistance > agent.stoppingDistance)
                            // {
                            //     // Update the position of the grabbed object relative to the avatar's hand
                            //     unityObject.transform.position = IaActive.transform.position + objectOffset;

                            //     yield return null;
                            // }

                            // Release the object on the ground
                            RaycastHit hit;
                            if (Physics.Raycast(unityObject.transform.position, Vector3.down, out hit))
                            {
                                unityObject.transform.position = hit.point;
                            }
                        }
                    }
                }
            }
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
        
        public static AudioClip StartRecording(string device, bool loop, int lengthSec, int frequency)
        {
            DialogueWindow window = new DialogueWindow();
            window.isRecording = true;
            var key = device ?? "";
#if USE_WEBGL
            var clip = CreateClip(key, loop, lengthSec, frequency, 1);
            WebGLMicrophone.MicrophoneWebGL_Start(key, loop, lengthSec, frequency, 1, UpdateClip, DeleteClip);
            return clip;
#else
            return Microphone.Start(device, loop, lengthSec, frequency);
            //clip = Microphone.Start(Microphone.devices[0], false, recordingMaxDuration, 44100);
#endif
        }
       
        private async void EndRecording(string device)
        {
#if USE_WEBGL
            WebGLMicrophone.MicrophoneWebGL_End(key);
#else   
            Microphone.End(device);
#endif
            byte[] data = SaveWav.Save(fileName, clip);

            var req = new CreateAudioTranscriptionsRequest
            {
                FileData = new FileData() { Data = data, Name = "audio.wav" },
                Model = "whisper-1",
                Language = "en"
            };

            var res = await openai.CreateAudioTranscription(req);

            InputMessage.text = res.Text;
            SpeechSomething(res.Text);
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
    }
}
