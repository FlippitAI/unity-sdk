using Amazon.Polly.Model;
using Amazon.Polly;
using Amazon.Runtime;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using System.Threading.Tasks;


namespace Flippit
{
    
    public class IACharacter : MonoBehaviour
    {
        #region public
        public IaPersonality personality;
        public GameObject Detector;
        public GameObject Avatar;
        public GameObject[] QuestObjects;
        [HideInInspector]
        public string Discussion;
        #endregion
        #region private
        private Player playerScript;
        private Animator animator;
        private NavMeshAgent agent;
        private Quaternion originalRotation;
        private Vector3 playerPosition;
        private GameObject playerGO;
        private bool iSeeYou;
        #endregion
        #region Visemes
        public bool isTalking = false;
        private int currentVisemeIndex = 0;
        public List<Viseme> visemesList;
        public AudioClip clip;
        private AudioSource sourceAudio;
        private SkinnedMeshRenderer skinnedMeshRenderer;
        #endregion
        private void Start()
        {
            SphereCollider collider = Detector.GetComponent<SphereCollider>();
            sourceAudio = GetComponent<AudioSource>();
            collider.radius = personality.DetectionSize;
            animator= GetComponentInChildren<Animator>();
            agent = GetComponentInChildren<NavMeshAgent>();
            skinnedMeshRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
            originalRotation = transform.rotation;
            visemesList= new();
        }
        private static readonly Dictionary<string, string> PhonemeToVisemeMapping = new()
        {
            {"sil", "viseme_sil"},
            {"p", "viseme_PP"},
            {"t", "viseme_DD"},
            {"S", "viseme_CH"},
            {"T", "viseme_TH"},
            {"f", "viseme_FF"},
            {"k", "viseme_kk"},
            {"r", "viseme_RR"},
            {"s", "viseme_SS"},
            {"@", "viseme_aa"},
            {"a", "viseme_aa"},
            {"e", "viseme_E"},
            {"E", "viseme_E"},
            {"i", "viseme_I"},
            {"o", "viseme_O"},
            {"O", "viseme_O"},
            {"u", "viseme_U"},
            {"J", "viseme_I"}
        };
        public async Task SpeechMe( List<Viseme> visemes)
        {
            visemesList = visemes;
            await Task.Delay((int)(clip.length * 1000));
        }
        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                playerGO= other.gameObject;
                iSeeYou = true;
                playerScript = playerGO.GetComponent<Player>();
                playerScript.DialogueWithIa = gameObject;
                playerScript.OpenDialogueBox();
            }
        }
        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                iSeeYou = false;
                playerScript.CloseDialogueBox();
                transform.rotation = originalRotation;
            }
        }

        public void Update()
        { 
            animator.SetFloat("MoveSpeed", agent.velocity.magnitude);
            if (iSeeYou)
            { 
                playerPosition = playerGO.transform.position; 
                transform.LookAt(playerPosition); 
            }

            if (isTalking && currentVisemeIndex < visemesList.Count)
            {
                float currentTime = sourceAudio.time * 1000 / sourceAudio.pitch;

                if (currentTime >= visemesList[currentVisemeIndex].start &&
                    currentTime <= visemesList[currentVisemeIndex].end)
                {
                    SetViseme(PhonemeToVisemeMapping[visemesList[currentVisemeIndex].value], 1);
                }
                else
                {
                    SetViseme(PhonemeToVisemeMapping[visemesList[currentVisemeIndex].value], 0);
                }

                if (currentTime > visemesList[currentVisemeIndex].end)
                {
                    currentVisemeIndex++;
                }
            }
            
        }
        void SetViseme(string viseme, float value)
        {
            int blendShapeIndex = skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(viseme);
            if (blendShapeIndex >= 0)
            {
                skinnedMeshRenderer.SetBlendShapeWeight(blendShapeIndex, value);
            }
        }
        public void SetVisemes(List<Viseme> visemeData)
        {
            visemesList = visemeData;
        }
        public void ResetVisemes()
        {
            foreach (var viseme in visemesList)
            {
                SetViseme(PhonemeToVisemeMapping[viseme.value], 0);
            }
            currentVisemeIndex = 0;
        }
    }
}
