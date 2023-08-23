using UnityEngine;
using UnityEngine.AI;


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
        private void Start()
        {
            SphereCollider collider = Detector.GetComponent<SphereCollider>();
            collider.radius = personality.DetectionSize;
            animator= GetComponentInChildren<Animator>();
            agent = GetComponentInChildren<NavMeshAgent>();
            originalRotation = transform.rotation;
        }

        private void OnTriggerEnter(Collider other)
        {
            var plyComp = other.GetComponent<Player>();
            if (plyComp != null)
            {
                if (plyComp.tagT == "Player")
                {
                    playerGO = other.gameObject;
                    iSeeYou = true;
                    playerScript = playerGO.GetComponent<Player>();
                    playerScript.DialogueWithIa = gameObject;
                    playerScript.OpenDialogueBox();
                }
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
        }
    }
}
