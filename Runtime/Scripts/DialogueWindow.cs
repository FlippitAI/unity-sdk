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

    public class DialogueWindow : MonoBehaviour
    {
        #region public var
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
        private readonly OpenAIApi openai = new("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");
        private bool isTalkingToCharacter = false;
        #endregion
        WebSocketManager manager;
        EnumLists lists;

        // Start is called before the first frame update
        void Start()
        {
            InputMessage = DiscussionPanel.GetComponentInChildren<TextMeshProUGUI>();
            inputText = inputField.GetComponent<TextMeshProUGUI>();
            chatAreaText = DiscussionPanel.GetComponentInChildren<TextMeshProUGUI>();
        }
        private void Update()
        {
            if (Application.internetReachability != NetworkReachability.NotReachable && UseMicrophone)
            {
                if (Input.GetKeyDown(pushToTalkButton) && !isRecording)
                {
                    StartRecording();
                }
                else if (Input.GetKeyUp(pushToTalkButton))
                {
                    time = 0;
                    isRecording = false;
                    EndRecording();
                    Debug.Log("Enregistrement fini");
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
                    Debug.Log("Enregistrement stoppé de force (duration)");
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

        private async void ReadSentences()
        {
            if (currentSentenceIndex >= 0 && currentSentenceIndex < sentences.Count)
            {
                string sentenceToRead = sentences[currentSentenceIndex];
                int index = (int)IaPersona.voice;
                lists = new EnumLists();
                string voiceID = lists.voiceNames[index];
                Task speechTask = IaActive.GetComponent<TTS>().SpeechMe(sentenceToRead, voiceID);
                await speechTask;
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

        public async void PlayAnimation(string animName)
        {
            Animator animator = IaActive.GetComponentInChildren<Animator>();

            if (animName == "Talking" || animName == "Walk")
            {
                animator.SetBool("Talking", true);
            }
            else
            {
                animator.SetTrigger(animName);
                animator.SetBool("Talking", true);
            }

            if (animName == "Walk")
            {
                GameObject[] questObj = IaActive.GetComponent<IACharacter>().QuestObjects;

                if (questObj.Length > 0)
                {
                    while (animator.GetBool("Talking"))
                    {
                        // Wait for Talking to be set to true (from external piece of code)
                        await Task.Delay(500);
                    }
                    NavMeshAgent agent = IaActive.GetComponent<NavMeshAgent>();
                    agent.destination = questObj[0].transform.position; // to change to dynamically update the object -- need to be linked to the db
                    agent.stoppingDistance = 2.0f; // Set the stopping distance to 2.0 units

                    FinishDiscussion();
                }
            }
        }
        private void StartRecording()
        {
            isRecording = true;
            clip = Microphone.Start(Microphone.devices[0], false, recordingMaxDuration, 44100);
        }
        private async void EndRecording()
        {
            Microphone.End(Microphone.devices[0]);
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
    }
}
