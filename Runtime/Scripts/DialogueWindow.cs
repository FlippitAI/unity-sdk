#if UNITY_WEBGL && !UNITY_EDITOR
#define USE_WEBGL
#endif

using Amazon.Runtime.Internal.Endpoints.StandardLibrary;
using Cinemachine;
using OpenAI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine.UIElements;
using UnityEngine.XR;
using static Flippit.EnumLists;

namespace Flippit
{
    public class AudioInfo
    {
        public int SampleRate { get; set; }
        public int Channels { get; set; }
    }
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
        //public string audio_format;// a retirer si pas staging
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
        
        [Header("reference Game Objects")]
        public GameObject ChatContainer;
        public GameObject DiscussionContent;
        public GameObject inputField;
        public Scrollbar verticalScrollBar;

        [Header("Inputs Options")]
        public bool UseMicrophone;
        public bool displayInputFieldPanel;
        public KeyCode pushToTalkButton = KeyCode.Tab;
        public KeyCode ExitDiscussion = KeyCode.Escape;
        public int recordingMaxDuration = 10;
        public string[] MicrophoneOptions;
        public int selectedMicrophoneIndex = 0;

        [Header("Output Options")]
        public bool ShowDiscussion;
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
        private TMP_Text inputText;
        private readonly float currentTime = 0;
        private IACharacter iaSc;
        private GameObject playerGO;
        private CinemachineVirtualCamera VCam;
        private float elapsedTime;
        private string speechSentence;
        private bool isRecording;
        private AudioClip clip;
        private float time;
        private OpenAIApi openai;
        private ApiKeyManager apiKeyManager;
        private bool isTalkingToCharacter = false;
        private bool isIncrementing;
        private string device;
        #endregion
        private WebSocketManager manager;
        private AudioRecorder audioRecorder;
        private GameObject dialogueNPC;
        private GameObject dialoguePlayer;
        const string dialogueNPCPath = "Prefabs/DialogueNPC";
        const string dialoguePlayerPath = "Prefabs/DialoguePlayer";
        
        private GameObject dialogueNpcObject;
        [HideInInspector]
        public TextMeshProUGUI playerText;
        private string targetText;
        private readonly float textSpeed = 0.05f; 
        private GameObject dialoguePlayerObject;

        public List<List<Viseme>> visemeSets = new();
        private readonly List<string> sentences = new();
        private readonly List<string> files = new();
        private List <GameObject> conversationObjects;
        private int previousFilePathCount = 0;
        private int previousClipsCount = 0;
        private bool isPlayingAudio = false;
        private int currentSentenceIndex = 0;
        public UnityWebRequest www;
        public List<AudioClip> audioClips = new();
        private string fileName;
        private string filePath;
        // Start is called before the first frame update

        void Start()
        {
            audioRecorder = new AudioRecorder();
            apiKeyManager = Resources.Load<ApiKeyManager>("Apikeys");
            openai = new(apiKeyManager.OpenAI);
            inputText = inputField.GetComponent<TMP_InputField>().textComponent;
            dialogueNPC = Resources.Load<GameObject>(dialogueNPCPath);
            dialoguePlayer = Resources.Load<GameObject>(dialoguePlayerPath);
            if (verticalScrollBar == null)
            {
                verticalScrollBar = GameObject.Find("Scrollbar Vertical").GetComponent<Scrollbar>();
            }
            RefreshMicros();
        }
       
        private void Update()
        {
            DiscussionContent.SetActive(ShowDiscussion);
            if (Application.internetReachability != NetworkReachability.NotReachable && UseMicrophone)
            {
                if (Input.GetKeyDown(pushToTalkButton) && !isRecording)
                {
                    StartRecordingIfPossible();
                }
                else if (Input.GetKeyUp(pushToTalkButton))
                {
                    StopRecordingIfActive();
                }
            }

            if (Input.GetKeyDown(ExitDiscussion) && isTalkingToCharacter)
            {
                FinishDiscussion();
            }

            if (audioRecorder.isRecording)
            {
                UpdateRecordingTime();
            }

            if (useVoices)
            {
#if USE_WEBGL
                if(audioClips.Count > 0 && audioClips.Count != previousClipsCount)
                {
                Debug.Log("clips --> " +audioClips.Count);
                if (!isPlayingAudio)
                    {
                        StartCoroutine(PlayWebGLClips());
                    }
                    previousClipsCount = audioClips.Count;
                }
#else
                if (files.Count > 0 && files.Count != previousFilePathCount)
                {
                    if (!isPlayingAudio)
                    {
                        StartCoroutine(PlayAudioClips());
                    }
                    previousFilePathCount = files.Count;
                }
#endif
            }
        }
        
        private void OnEnable()
        {
            ChatContainer.SetActive(displayInputFieldPanel);
            inputField.SetActive(displayInputFieldPanel);
            
            DiscussionPanel(ShowDiscussion);
            conversationObjects ??= new List<GameObject>();
            VCam = FindObjectOfType<CinemachineVirtualCamera>();
            iaSc = IaActive.GetComponent<IACharacter>();
            playerGO = GameObject.FindGameObjectWithTag("Player");
            discussion = iaSc.Discussion;

            PositionVCam(true);

            if (manager == null)
            {
                manager = GetComponent<WebSocketManager>();
            }

            manager.StartWebSocket(IaPersona.characterId);
        }
        void DiscussionPanel(bool ShowDiscussion)
        {
            DiscussionContent.SetActive(ShowDiscussion);
            gameObject.GetComponent<UnityEngine.UI.Image>().enabled= ShowDiscussion;
            Transform DialogueGlobal = gameObject.transform.Find("Dialogue Global");
            DialogueGlobal.GetComponent<UnityEngine.UI.Image>().enabled = ShowDiscussion;
        }
        
        private void StartRecordingIfPossible()
        {
            if (MicrophoneOptions.Length > 0 && selectedMicrophoneIndex <= MicrophoneOptions.Length)
            {
                device = MicrophoneOptions[selectedMicrophoneIndex];
                isRecording = true;
#if USE_WEBGL
                if (AudioRecorder.IsRecording(device))
                {
                    audioRecorder.EndRecording(device);
                }
                else
                {
                    clip = AudioRecorder.Start(device, false, recordingMaxDuration, 44100);
                }
#else
                audioRecorder.StartRecordingAsync(device, false, recordingMaxDuration, 44100)
                    .ContinueWith(task =>
                    {
                        if (task.IsCompleted && !task.IsFaulted)
                        {
                            clip = task.Result;
                            audioRecorder.isRecording = true;
                        }
                        else
                        {
                            Debug.LogWarning("Erreur lors du d�marrage de l'enregistrement audio.");
                        }
                    });
#endif
            }
            else
            {
                Debug.LogWarning("Aucun microphone n'est disponible.");
            }
        }
        public void RefreshMicros()
        {
            audioRecorder.StartRefresh(devices =>
            {
                MicrophoneOptions = devices;
            });
        }
        private async void StopRecordingIfActive()
        {
            time = 0;
            isRecording = false;
            Debug.Log("End of recording");
            audioRecorder.EndRecording(device);
            await ConvertClipToTextAsync(clip);
        }
        
        async Task ConvertClipToTextAsync(AudioClip clip)
        {
            if (clip != null)
            {
                byte[] data = SaveWav.Save("RecordedPrompt", clip);

                var req = new CreateAudioTranscriptionsRequest
                {
                    FileData = new FileData() { Data = data, Name = "audio.wav" },
                    Model = "whisper-1",
                    Language = "en"
                };
                var res = await openai.CreateAudioTranscription(req);

                SpeechSomething(res.Text);
            }
            else
            {
                Debug.Log("Clip has not been recorded, Pleaze check your microphone, clip is null");
            }
        }
        private void UpdateRecordingTime()
        {
            time += Time.deltaTime;
            if (time >= recordingMaxDuration)
            {
                StopRecordingIfActive();
            }
        }
        
        public void SaySomething()
        {
            if (inputText.text.Length > 1 && IaPersona != null)
            {
                IaPersona.prompt = inputText.text;
                CharacterInfos promptMessage = new() { action = "chat", prompt = inputText.text };

                manager.SendWebSocketMessage(JsonUtility.ToJson(promptMessage));
                if (ShowDiscussion)
                {
                    dialoguePlayerObject = Instantiate(dialoguePlayer);
                    conversationObjects.Add(dialoguePlayerObject);
                    playerText = dialoguePlayerObject.GetComponentInChildren<TextMeshProUGUI>();
                    dialoguePlayerObject.transform.SetParent(DiscussionContent.transform, false);
                    if (playerText != null)
                    {
                        playerText.text = "";
                        StartIncrementing(inputText.text);
                    }
                }
                inputField.GetComponent<TMP_InputField>().text = "";
            }
            verticalScrollBar.value = 0;
        }
        public string StartIncrementing(string newText)
        {
            if(!isIncrementing)
            {
                targetText = newText;
                isIncrementing = true;

                StartCoroutine(IncrementText());
            }
            
            return newText;
        }
        private IEnumerator IncrementText()
        {
            int charIndex = 0;
            
            while (charIndex < targetText.Length)
            {
                playerText.text += targetText[charIndex];
                charIndex++;

                float prefHeight = playerText.preferredHeight;
                if (dialoguePlayerObject.TryGetComponent<LayoutElement>(out var layoutPlayer))
                {
                    layoutPlayer.preferredHeight = prefHeight;
                }
                yield return new WaitForSeconds(textSpeed);
            }

            isIncrementing = false;
        }
        public void StopIncrementing()
        {
            StopCoroutine(IncrementText());
            isIncrementing = false;
        }
        public void SpeechSomething(string speech)
        {
            if (speech.Length > 1 && IaPersona != null)
            {
                IaPersona.prompt = speech;
                CharacterInfos promptMessage = new() { action = "chat", prompt = IaPersona.prompt };
                manager.SendWebSocketMessage(JsonUtility.ToJson(promptMessage));
                if (ShowDiscussion)
                {
                    dialoguePlayerObject = Instantiate(dialoguePlayer);
                    conversationObjects.Add(dialoguePlayerObject);
                    playerText = dialoguePlayerObject.GetComponentInChildren<TextMeshProUGUI>();
                    dialoguePlayerObject.transform.SetParent(DiscussionContent.transform, false);
                    if (playerText != null)
                    {
                        playerText.text = "";
                        StartIncrementing(speech);
                    }
                    inputField.GetComponent<TMP_InputField>().text = "";
                }
            }
            verticalScrollBar.value = 0;
        }

        public void FinishDiscussion()
        {
            if (iaSc != null) { iaSc.Discussion = discussion; }
            discussion = string.Empty;
            sentences.Clear();
            currentSentenceIndex = 0;
            if (files.Count > 0)
            {
                StartCoroutine(CleanUpFolder());
            }
            isRecording = false;
            time = 0;
            clip = null;
            
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
            if (conversationObjects.Count > 0)
            {
                for (int i = conversationObjects.Count - 1; i >= 0; i--)
                {
                    GameObject obj = conversationObjects[i];
                    Destroy(obj);
                }
                conversationObjects.Clear(); 
            }
            
            PositionVCam(false);
            manager.CloseWebsocket();
            manager = null;
        }
        IEnumerator CleanUpFolder()
        {
            int currentIndex = 0;
            while (currentIndex < files.Count)
            {
                using UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(files[currentIndex], AudioType.MPEG);
                var op = www.SendWebRequest();

                while (!op.isDone)
                {
                    yield return null;
                }

                if (www.result == UnityWebRequest.Result.Success)
                {
                    File.Delete(files[currentIndex]);
                    files.RemoveAt(currentIndex);
                    currentIndex++;
                }
            }
        }
        public void ReceiveMessage(string message)
        {
            IaActive.GetComponent<IACharacter>().isTalking = true;
            if (ShowDiscussion)
            {
                if (dialogueNpcObject == null)
                {
                    dialogueNpcObject = Instantiate(dialogueNPC);
                    conversationObjects.Add(dialogueNpcObject);
                    LayoutElement layout = dialogueNpcObject.GetComponent<LayoutElement>();
                    dialogueNpcObject.transform.SetParent(DiscussionContent.transform, false);
                    TextMeshProUGUI dialogueText = dialogueNpcObject.GetComponentInChildren<TextMeshProUGUI>();
                    if (dialogueText != null)
                    {
                        dialogueText.text = discussion;
                        float prefHeight = dialogueText.preferredHeight;
                        layout.preferredHeight = prefHeight;
                    }
                }
                else
                {
                    TextMeshProUGUI dialogueText = dialogueNpcObject.GetComponentInChildren<TextMeshProUGUI>();
                    LayoutElement layout = dialogueNpcObject.GetComponent<LayoutElement>();
                    if (dialogueText != null)
                    {
                        dialogueText.text = discussion;
                        float prefHeight = dialogueText.preferredHeight;
                        layout.preferredHeight = prefHeight;
                        verticalScrollBar.value = 0;
                    }
                } 
            }
            discussion += message;

            speechSentence += message;

            if (Regex.IsMatch(speechSentence, @"[!.?]"))
            {
                sentences.Add(speechSentence);
                speechSentence = "";
            }
        }
        public void TerminateResponse(string message)
        {
            if (message == "DONE")
            {
                dialogueNpcObject = null;
                discussion = null;
                Animator animator = IaActive.GetComponentInChildren<Animator>();
                animator.SetBool("Talking", false);
                sentences.Clear();
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
            IaActive.GetComponent<IACharacter>().visemesList.AddRange(visemesSentence);
        }
        
        async void PlayVisemes()
        {
            if (currentSentenceIndex >= 0 && currentSentenceIndex < sentences.Count)
            {
                if (clip != null)
                {
                    Task speechTask = IaActive.GetComponent<IACharacter>().SpeechMe(visemeSets[0]);
                    await speechTask;
                }
                visemeSets.RemoveAt(0);
                currentSentenceIndex++;
            }
            else
            {
                Animator animator = IaActive.GetComponentInChildren<Animator>();
                animator.SetBool("Talking", false);
                IaActive.GetComponent<IACharacter>().ResetVisemes();
            }
        }
        private IEnumerator PlayWebGLClips()
        {
            int currentIndex = 0;
            Debug.Log("lit le clip " + audioClips[currentIndex]);
            clip = audioClips[currentIndex];
            if (clip != null)
            {
                Debug.Log("Clip pas Null");
                if (IaActive.TryGetComponent<AudioSource>(out var audioSource))
                {
                    isPlayingAudio = true;
                    IaActive.GetComponent<IACharacter>().clip = clip;
                    audioSource.clip = clip;
                    float pitch = audioSource.pitch;
                    float waitDelay = clip.length / pitch;
                    audioSource.Play();
                    Debug.Log("cens� �tre en lecture");
                    PlayVisemes();
                    yield return new WaitForSeconds(waitDelay);
                }
            }
            else
            {
                Debug.LogError("Erreur lors de la conversion en AudioClip.");
            }
            isPlayingAudio = false;
            File.Delete(files[currentIndex]);
            files.RemoveAt(currentIndex);
            currentIndex++;
            if (!isPlayingAudio && currentIndex > files.Count)
            {
                IaActive.GetComponent<IACharacter>().ResetVisemes();
                IaActive.GetComponent<IACharacter>().visemesList.Clear();
            }
        }
        private IEnumerator PlayAudioClips()
        {
            int currentIndex = 0;
            filePath = files[currentIndex];

#if UNITY_STANDALONE_OSX
            Uri fileUri = new(filePath);
            www = UnityWebRequestMultimedia.GetAudioClip(fileUri, AudioType.MPEG);
#else
            www = UnityWebRequestMultimedia.GetAudioClip(filePath, AudioType.MPEG);

            var op = www.SendWebRequest();

            while (!op.isDone)
            {
                yield return null;
            }

            if (www.result == UnityWebRequest.Result.Success)
            {
                clip = DownloadHandlerAudioClip.GetContent(www);

                if (clip != null)
                {
                    if (IaActive.TryGetComponent<AudioSource>(out var audioSource))
                    {
                        isPlayingAudio = true;
                        IaActive.GetComponent<IACharacter>().clip = clip;
                        audioSource.clip = clip;
                        float pitch = audioSource.pitch;
                        float waitDelay = clip.length / pitch;
                        audioSource.Play();
                        PlayVisemes();
                        yield return new WaitForSeconds(waitDelay);
                    }
                }
                else
                {
                    Debug.LogError("Erreur lors de la conversion en AudioClip.");
                }
                   
                isPlayingAudio = false;
                File.Delete(files[currentIndex]);
                files.RemoveAt(currentIndex);
                currentIndex++;
                if (!isPlayingAudio && currentIndex > files.Count)
                {
                    IaActive.GetComponent<IACharacter>().ResetVisemes();
                    IaActive.GetComponent<IACharacter>().visemesList.Clear();
                }
            }
#endif
        }

        public void WriteIntoFile(byte[] audioData)
        {
            Debug.Log("Taile de AudioData -> " + audioData.Length);
            if (audioData != null && audioData.Length > 0)
            {

#if USE_WEBGL
                //CreateAudioClip(audioData);
#else
                fileName = GenerateUniqueFileName();
                string filePath = Path.Combine(Application.persistentDataPath, fileName);

                Debug.Log("Write File at --> " + filePath);
                while (files.Contains(filePath))
                {
                    Debug.Log("File contain (regenerate name) --> " + filePath);
                    fileName = GenerateUniqueFileName();

                    filePath = Path.Combine(Application.persistentDataPath, fileName);
                }

                files.Add(filePath);
                using MemoryStream memoryStream = new(audioData);
                using FileStream filestream = new(filePath, FileMode.Create);
                byte[] buffer = new byte[8 * 1024];
                int bytesRead;
                while ((bytesRead = memoryStream.Read(buffer, offset: 0, count: buffer.Length)) > 0)
                {
                    filestream.Write(buffer, offset: 0, count: bytesRead);
                }
#endif
            }
            else
            {
                Debug.LogError("Mes couilles sur ton nez t'auras l'air d'un dindon! Error decoding audio data.");
            }
        }

        private void CreateAudioClip(byte[] audioData)
        {
            filePath = Path.Combine(Application.persistentDataPath, "audio_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".wav");
            Debug.Log("Fichier --> "+ filePath);
            
            AudioConverter.ConvertToWav(audioData, filePath, 44100, 16, 1);
            if(File.Exists(filePath)) StartCoroutine(LoadAudioClip());
            
        }
        IEnumerator LoadAudioClip()
        {
            using UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip("file://"+filePath, AudioType.WAV);
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.ConnectionError)
            {
                Debug.LogError("Failed to load audio clip: " + www.error);
            }
            else
            {
                Debug.Log(UnityWebRequest.Result.Success+"prochaine etape Get WWW");
                AudioClip audioClip = DownloadHandlerAudioClip.GetContent(www);
                audioClips.Add(audioClip);
            }
        }
        private string GenerateUniqueFileName()
        {
            string baseFileName = "Vocal";
            string fileExtension = ".mp3";
            int fileNumber = 1;

            while (File.Exists(Path.Combine(Application.persistentDataPath, $"{baseFileName}{fileNumber}{fileExtension}")))
            {
                fileNumber++;
            }

            return $"{baseFileName}{fileNumber}{fileExtension}";
        }

        public void PlayAnimation(string animName, string objectName = null)
        {
            animName = animName.Replace("'", "");
            Animator animator = IaActive.GetComponentInChildren<Animator>();
            if (IaActive == null)
            {
                Debug.LogWarning("IaActive GameObject not assigned. Make sure it is assigned in the Unity Editor.");
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
                        matchedObjectIndex = i;
                        break;
                    }
                }

                if (matchedObjectIndex != -1)
                {
                    NavMeshAgent agent = IaActive.GetComponent<NavMeshAgent>();

                    agent.destination = questObjs[matchedObjectIndex].transform.position;
                    agent.stoppingDistance = 3.0f;

                    while (agent.pathPending || agent.remainingDistance > agent.stoppingDistance)
                    {

                        yield return null;
                    }

                    Debug.Log("Agent has arrived at the destination!");

                    if (animName == "Grab")
                    {
                        Vector3 handOffset = new(0f, 1.0f, 1.0f); 

                        if (unityObject != null)
                        {
                            //Vector3 objectOriginalPosition = unityObject.transform.position;

                            Vector3 handPosition = IaActive.transform.position + IaActive.transform.TransformDirection(handOffset);
                            //Vector3 objectOffset = objectOriginalPosition - handPosition;
                            unityObject.transform.SetPositionAndRotation(handPosition, IaActive.transform.rotation);

                            yield return new WaitForSeconds(10.0f);

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
                            if (Physics.Raycast(unityObject.transform.position, Vector3.down, out RaycastHit hit))
                            {
                                unityObject.transform.position = hit.point;
                            }
                        }
                    }
                }
            }
        }
        private void OnApplicationQuit()
        {
            StartCoroutine(CleanUpFolder());
        }
    }
}