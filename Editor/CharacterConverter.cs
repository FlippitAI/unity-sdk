using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEngine.AI;
using Cinemachine;
using System.Linq;
using static Flippit.EnumLists;
using System.Text.RegularExpressions;
using System;
using Amazon.Polly;
using UnityEngine.Rendering;
using Newtonsoft.Json;
using System.Runtime.InteropServices;
using UnityEngine.EventSystems;

namespace Flippit.Editor
{
    [Serializable]
    public class PayloadData
    {
        public string character_id;
        public string owner_id;
        public string name;
        public string backstory;
        public string personality_id;
        public string voice_id;
        public string role;
        public string age_id;
        public string hobbies;
        public string mood_id;
        public string catch_phrases;
        public string primary_goal;
        public string urls;
        public string asset_file_path;
    }

    public class CharacterConverter : EditorWindow
    {
        #region UIElements
        Button convertToIa;
        Button convertToPlayer;
        Button documentation;
        Button discord;
        Button webSite;
        TextField characName;
        TextField characStory;
        TextField primaryGoal;
        TextField role;
        TextField catchPhrases;
        TextField hobbies;
        DropdownField age;
        DropdownField characPersonality;
        DropdownField characVoice;
        #endregion
        #region public
        public Personality Personalities;
        public Voices Voices;
        public Age Age;
        public string[] VoicesID;
        public string[] personalitiesID;
        public string[] AgeID;
        #endregion
        #region private const
        private const string prefabPath = "Assets/Flippit/Resources/Prefabs";
        private const string personalityPath = "Assets/Flippit/Resources/Personalities";
        private const string controllerPath = "Packages/com.flippit.flippitstudio/Runtime/Resources/Controllers/NPC_Anim_Controller.controller";
        private const string playerctrlrPath = "Packages/com.flippit.flippitstudio/Runtime/Resources/Controllers/PlayerController.controller";
        private const string DialogueUI = "Packages/com.flippit.flippitstudio/Runtime/Resources/Prefabs/DialoguePanel.prefab";
        private const string CMVCamPath = "Packages/com.flippit.flippitstudio/Runtime/Resources/Prefabs/CMvcam1.prefab";
        private const string siteUrl = "https://www.flippit.ai/";
        private const string DiscordSupport = "https://discord.gg/MPMxDgKVrm";
        private const string Documentation = "https://flippit.notion.site/Unity-SDK-732f20e3837245cfbdb5b85d0636fa3b";
        #endregion
        #region private
        private GameObject canvas;
        private GameObject UIpanel;
        private GameObject PlayerObject;
        private GameObject CMVCam;
        private GameObject prefab;
        #endregion

        [MenuItem("Flippit/Convert to Player", false, 1)]
        public static void ShowConverterWindow()
        {
            CharacterConverter wnd = GetWindow<CharacterConverter>();
            wnd.titleContent = new GUIContent("Convert to Player");
        }

        public void CreateGUI()
        {
            VisualElement root = rootVisualElement;

            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Packages/com.flippit.flippitstudio/Editor/Styles/CharacterConverter.uxml");
            VisualElement labelFromUXML = visualTree.Instantiate();
            root.Add(labelFromUXML);
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Packages/com.flippit.flippitstudio/Editor/Styles/FlippitStudio.uss");

            //convertToIa = root.Q("ConvertNpc") as Button;
            convertToPlayer = root.Q("ConvertPlayer") as Button;
            documentation = root.Q("Documentation") as Button;
            discord = root.Q("Discord") as Button;
            webSite = root.Q("Website") as Button;
            characName = root.Q("CharacterName") as TextField;
            characStory = root.Q("CharacterBackStory") as TextField;
            characPersonality = root.Q("CharacterPersonality") as DropdownField;
            characVoice = root.Q("CharacterVoice") as DropdownField;
            primaryGoal = root.Q("primaryGoal") as TextField;
            role = root.Q("Role") as TextField;
            catchPhrases = root.Q("CatchPhrases") as TextField;
            age = root.Q("Age") as DropdownField;
            hobbies = root.Q("Hobbies") as TextField;

            //convertToIa.RegisterCallback<ClickEvent>(OnConvertToNPC);
            convertToPlayer.RegisterCallback<ClickEvent>(OnConvertToPlayer);
            documentation.RegisterCallback<ClickEvent>(OnDocumentation);
            discord.RegisterCallback<ClickEvent>(OnDiscord);
            webSite.RegisterCallback<ClickEvent>(OnWebSite);
        }
        public string GetFormatedJsonPayload(IaPersonality personality)
        {
            PayloadData payloadData = new()
            {
                character_id = personality.characterId,
                owner_id = personality.ownerId,
                name = personality.characterName,
                backstory = personality.backstory,
                personality_id = personality.personalityId,
                voice_id = personality.voiceId,
                role = personality.role,
                age_id = personality.ageId,
                hobbies = personality.hobbies,
                mood_id = personality.moodId,
                catch_phrases = personality.catchPhrases,
                primary_goal = personality.primaryGoal,
                urls = personality.urls,
                asset_file_path = personality.assetFilePath
            };

            JsonSerializerSettings jsonSettings = new()
            {
                NullValueHandling = NullValueHandling.Include
            };

            string jsonPayload = JsonConvert.SerializeObject(payloadData, jsonSettings);
            return jsonPayload;
        }
        public void OnConvertToNPC(ClickEvent evt)
        {
            if (EditorPrefs.GetBool("registered"))
            {
                EnumLists lists = new();
                VoicesID = lists.VoicesID;
                personalitiesID = lists.personalitiesID;
                AgeID = lists.AgeID;

                if (Selection.activeGameObject != null && !string.IsNullOrEmpty(characName.value))
                {
                    GameObject selectedObject = Selection.activeGameObject;
                    GameObject emptyGameObject = new()
                    {
                        name = characName.value,
                        tag = "Flippit/NPC"
                    };

                    #region physic 
                    CapsuleCollider ColliderParameters = emptyGameObject.AddComponent<CapsuleCollider>();
                    ColliderParameters.radius = .5f;
                    ColliderParameters.height = 2f;
                    ColliderParameters.center = new(0, 1, 0);
                    Rigidbody rb = emptyGameObject.AddComponent<Rigidbody>();
                    rb.freezeRotation = true;
                    GameObject Detector = new() { name = "Detection Area" };
                    Detector.transform.parent = emptyGameObject.transform;
                    Detector.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                    SphereCollider DetectorCollider = Detector.AddComponent<SphereCollider>();
                    DetectorCollider.radius = 2;
                    DetectorCollider.isTrigger = true;
                    #endregion
                    #region parent
                    Quaternion initialRotation = selectedObject.transform.rotation;
                    selectedObject.transform.parent = emptyGameObject.transform;
                    selectedObject.transform.SetPositionAndRotation(Vector3.zero, initialRotation);
                    #endregion
                    #region animation
                    RuntimeAnimatorController iaCtrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(controllerPath); ;
                    Animator animatorComp = selectedObject.GetComponent<Animator>();
                    animatorComp.runtimeAnimatorController = iaCtrl;
                    animatorComp.applyRootMotion = false;
                    #endregion
                    #region Navmesh
                    emptyGameObject.AddComponent<NavMeshAgent>();
                    #endregion
                    #region set Script parameters
                    IACharacter iaSc = emptyGameObject.AddComponent<IACharacter>();
                    iaSc.Avatar = selectedObject;
                    iaSc.Detector = Detector;
                    #endregion
                    #region Set personality
                    string tokenAccess = EditorPrefs.GetString("AccessToken");
                    string tokenRefresh = EditorPrefs.GetString("RefreshToken");
                    string newCharacter = ApiManager.PostRequest("api/v1/characters/create", data: null, tokenAccess, tokenRefresh);
                    dynamic jsonResponse = JsonConvert.DeserializeObject(newCharacter);
                    string characterId = jsonResponse.character.character_id;
                    string ownerId = jsonResponse.character.owner_id;
                    IaPersonality perso = CreateInstance<IaPersonality>();
                    iaSc.personality = perso;
                    perso.characterName = characName.value;
                    perso.backstory = characStory.value;
                    perso.voice = (Voices)characVoice.index;
                    perso.personality = (Personality)characPersonality.index;
                    perso.catchPhrases = catchPhrases.text;
                    perso.characterAge = (Age)age.index;
                    perso.hobbies = hobbies.text;//
                    perso.primaryGoal = primaryGoal.value;
                    perso.role = role.text;//
                    perso.characterId = characterId;
                    perso.ownerId = ownerId;
                    perso.voiceId = lists.VoicesID[(int)perso.voice];
                    perso.personalityId = lists.personalitiesID[(int)perso.personality];
                    perso.ageId = lists.AgeID[(int)perso.characterAge];

                    string dataCharacter = GetFormatedJsonPayload(perso);
                    Debug.Log(dataCharacter);
                    ApiManager.PostRequest("api/v1/characters/update", dataCharacter, tokenAccess, tokenRefresh);
                    #endregion
                    #region store asset data
                    CreateFolders();
                    AssetDatabase.CreateAsset(perso, personalityPath + "/" + characName.value + ".asset");
                    prefab = PrefabUtility.SaveAsPrefabAsset(emptyGameObject, prefabPath + "/" + characName.value + ".prefab");
                    AssetDatabase.SaveAssets();
                    DestroyImmediate(emptyGameObject);
                    GameObject newPrefabInstance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                    ThumbnailGenerator.GenerateThumbnail(newPrefabInstance, characName.value, 128, 128);
                    AssetDatabase.Refresh();
                    #endregion

                    ConfirmationMessage(characName.value + " has been Created, Check Your Prefab Folder.");

                }
                else
                {
                    ConfirmationMessage("Please, select the Ia Character First, and give it a name.");
                }

                static void ConfirmationMessage(string message)
                {

                    Debug.Log(message);
                }
            }
            else
            {
                Debug.LogWarning("You must be connected to Flippit Account. Open Studio First");
            }
        }
        public void OnConvertToPlayer(ClickEvent evt)
        {

            if (Selection.activeGameObject != null)
            {
                GameObject selectedObject = Selection.activeGameObject;
                GameObject emptyGameObject = new()
                {
                    name = "Player",
                    tag = "Player"
                };
                // Ajouter les composants souhaités à l'objet sélectionné

                # region Physic Elements
                CapsuleCollider ColliderParameters = emptyGameObject.AddComponent<CapsuleCollider>();
                ColliderParameters.radius = .5f;
                ColliderParameters.height = 2f;
                ColliderParameters.center = new(0, 1, 0);
                Rigidbody rb = emptyGameObject.AddComponent<Rigidbody>();
                rb.freezeRotation = true;
                #endregion
                #region Set Script parmeters
                Player PlayerSc = emptyGameObject.AddComponent<Player>();
                PlayerSc.avatar = selectedObject;
                #endregion
                #region Canvas
                Canvas canvaComp = FindObjectOfType<Canvas>();

                if (canvaComp != null)
                {
                    canvas = canvaComp.gameObject;
                    Transform[] childTransforms = canvas.GetComponentsInChildren<Transform>(true);
                    int childCount = childTransforms.Length; // Stocker la longueur du tableau à l'extérieur de la boucle
                    bool dialoguePanelFound = false; // Booléen pour marquer si l'objet DialoguePanel a été trouvé
                    for (int i = 0; i < childCount; i++)
                    {
                        Transform childTransform = childTransforms[i];
                        if (childTransform != canvas.transform)
                        {
                            // Faites quelque chose avec chaque enfant
                            GameObject childObject = childTransform.gameObject;
                            if (!childObject.activeSelf)
                            {
                                childObject.SetActive(true);
                                if (childObject.name == "DialoguePanel")
                                {
                                    UIpanel = childObject;
                                    dialoguePanelFound = true;
                                    break;
                                }
                                childObject.SetActive(false);
                            }
                            else if (childObject.name == "DialoguePanel")
                            {
                                UIpanel = childObject;
                                dialoguePanelFound = true;
                                break;
                            }
                        }
                    }

                    if (!dialoguePanelFound)
                    {
                        CreatePanel();
                    }
                }
                else
                {
                    CreateCanvas();
                    CreatePanel();
                }
                #endregion
                #region EventSystem
                EventSystem eventsystem = FindObjectOfType<EventSystem>();
                if(eventsystem == null)
                {
                    CreateEventSystem();
                }
                #endregion
                #region parenting
                selectedObject.transform.parent = emptyGameObject.transform;
                selectedObject.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                #endregion
                #region Animator
                RuntimeAnimatorController playerCtrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(playerctrlrPath); ;
                Animator animatorComp = selectedObject.GetComponent<Animator>();
                animatorComp.runtimeAnimatorController = playerCtrl;
                animatorComp.applyRootMotion = false;
                #endregion
                #region save prefab
                CreateFolders();
                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(emptyGameObject, prefabPath + "/" + emptyGameObject.name + ".prefab");
                DestroyImmediate(emptyGameObject);
                PlayerObject = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                PlayerObject.GetComponent<Player>().DialogueInterface = UIpanel;
                Selection.activeGameObject = PlayerObject;
                #endregion
                SetupCamera();
                Debug.Log("Your Player Prefab has been Created, Check Your Prefab Folder.");
            }
            else
            {
                Debug.Log("Please, select the GameObject First.");
            }
        }
        public void OnDocumentation(ClickEvent evt)
        {
            Application.OpenURL(Documentation);
        }
        public void OnDiscord(ClickEvent evt)
        {
            Application.OpenURL(DiscordSupport);
        }
        public void OnWebSite(ClickEvent evt)
        {
            Application.OpenURL(siteUrl);
        }

        void CreateCanvas()
        {
            canvas = new("Canvas");
            Canvas canvasComponent = canvas.AddComponent<Canvas>();
            canvasComponent.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            canvas.AddComponent<UnityEngine.UI.CanvasScaler>();
        }
        void CreateEventSystem()
        {
            GameObject eventSystemGO = new()
            {
                name = "EventSystem"
            };
            eventSystemGO.AddComponent<EventSystem>();
            eventSystemGO.AddComponent<StandaloneInputModule>();
        }
        void CreatePanel()
        {
            UIpanel = PrefabUtility.LoadPrefabContents(DialogueUI);
            UIpanel.transform.SetParent(canvas.transform);
            UIpanel.GetComponent<RectTransform>().anchoredPosition = new Vector3(10, -10, 0);
            UIpanel.SetActive(false);
        }

        void SetupCamera()
        {
            Camera MainCamera = Camera.main;
            if (MainCamera != null)
            {
                if (!MainCamera.gameObject.TryGetComponent<CinemachineBrain>(out _))
                {
                    MainCamera.gameObject.AddComponent<CinemachineBrain>();
                }
            }

            CinemachineVirtualCamera virtualCamera = FindObjectOfType<CinemachineVirtualCamera>();
            if (virtualCamera != null)
            {
                virtualCamera.Follow = PlayerObject.transform;
                virtualCamera.LookAt = PlayerObject.transform;
                CinemachineTransposer transposer = virtualCamera.GetCinemachineComponent<CinemachineTransposer>();
                if (transposer != null)
                {
                    transposer.m_BindingMode = CinemachineTransposer.BindingMode.SimpleFollowWithWorldUp;
                    transposer.m_FollowOffset = new(0, 1.8f, -2);
                }
                CinemachineComposer composer = virtualCamera.GetCinemachineComponent<CinemachineComposer>();
                if (composer != null) composer.m_TrackedObjectOffset = new(0, 1.3f, 0);
            }
            else
            {
                CreateCinemachineCamera();
            }
        }
        void CreateCinemachineCamera()
        {
            Camera MainCamera = Camera.main;
            if (MainCamera == null)
            {
                GameObject Cam = new()
                {
                    name = "MainCamera",
                    tag = "MainCamera"
                };
                Camera MainCam = Cam.AddComponent<Camera>();
                Cam.AddComponent<AudioListener>();
                MainCam.gameObject.AddComponent<CinemachineBrain>();
            }
            else
            {
                if (!MainCamera.gameObject.TryGetComponent<CinemachineBrain>(out _))
                {
                    MainCamera.gameObject.AddComponent<CinemachineBrain>();
                }
            }
            CMVCam = PrefabUtility.LoadPrefabContents(CMVCamPath);
            GameObject newVCam = Instantiate(CMVCam);
            CinemachineVirtualCamera virtualCam = newVCam.GetComponent<CinemachineVirtualCamera>();
            virtualCam.m_Follow = PlayerObject.transform;
            virtualCam.m_LookAt = PlayerObject.transform;
        }
        void CreateFolders()
        {
            if (!AssetDatabase.IsValidFolder("assets/Flippit"))
            {
                AssetDatabase.CreateFolder("Assets", "Flippit");
            }
            if (!AssetDatabase.IsValidFolder("Assets/Flippit/Resources"))
            {
                AssetDatabase.CreateFolder("Assets/Flippit", "Resources");
            }
            if (!AssetDatabase.IsValidFolder(personalityPath))
            {
                AssetDatabase.CreateFolder("Assets/Flippit/Resources", "Personalities");
            }
            if (!AssetDatabase.IsValidFolder(prefabPath))
            {
                AssetDatabase.CreateFolder("Assets/Flippit/Resources", "Prefabs");
            }
        }
    }
}
