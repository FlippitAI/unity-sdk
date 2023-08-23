using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Flippit
{
    public class Player : MonoBehaviour
    {
        #region
        public GameObject DialogueInterface;
        public bool FPS;
        public GameObject avatar;
        [HideInInspector]
        public GameObject DialogueWithIa;
        [HideInInspector]
        public string CharacterID;
        public float walkSpeed = 2f;
        public float runSpeed = 6f;
        [HideInInspector]
        public bool isRunning;
        public float rotateSpeed = 100f;
        public string tagT = "Player";
        #endregion
        #region
        private Rigidbody rb;
        private Animator animatorComp;
        private bool InteractionDialogue = false;
        private Vector3 moveDirection;
        private float movementSpeed;
        #endregion

        // Start is called before the first frame update
        void Start()
        {
            //gameObject.tag = "Player";
            if(FPS)
            {
                avatar.SetActive(false);
            }
            else
            {
                avatar.SetActive(true);
                animatorComp = avatar.GetComponent<Animator>();
            }
            rb = GetComponent<Rigidbody>();
        }

        // Update is called once per frame
        private void Update()
        {
             CharacterControls();
        }

        public void OpenDialogueBox()
        {
            InteractionDialogue = true;
            if (DialogueWithIa != null && !DialogueInterface.activeInHierarchy)
            {
                 DialogueInterface.GetComponent<DialogueWindow>().IaActive = DialogueWithIa;
                 DialogueInterface.GetComponent<DialogueWindow>().IaPersona = DialogueWithIa.GetComponent<IACharacter>().personality;
            }
            DialogueInterface.SetActive(true);
        }

        public void CloseDialogueBox()
        {
            InteractionDialogue = false;
            DialogueWithIa = null;
        }

        public void CharacterControls()
        {
            if (!InteractionDialogue)
            {
                float horizontalInput = Input.GetAxis("Horizontal");
                float verticalInput = Input.GetAxis("Vertical");

                Vector3 cameraForward = Camera.main.transform.forward;
                cameraForward.y = 0f;
                cameraForward.Normalize();

                Vector3 cameraRight = Camera.main.transform.right;
                cameraRight.y = 0f;
                cameraRight.Normalize();

                moveDirection = cameraForward * verticalInput + cameraRight * horizontalInput;
                moveDirection.Normalize();

                isRunning = Input.GetKey(KeyCode.LeftShift);
                movementSpeed = isRunning ? runSpeed : walkSpeed;
                rb.velocity = moveDirection * movementSpeed;

                if (!FPS) animatorComp.SetFloat("MoveSpeed", rb.velocity.magnitude / movementSpeed);

                if (moveDirection != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
                    transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, rotateSpeed * Time.deltaTime);
                }
            }
            else
            {
                movementSpeed = 0f;
                rb.velocity = moveDirection * movementSpeed;
                if (!FPS) animatorComp.SetFloat("MoveSpeed", rb.velocity.magnitude / movementSpeed);
            }    
        }
    }
}
