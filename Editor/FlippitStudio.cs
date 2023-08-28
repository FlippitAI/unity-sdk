using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System;
using System.IO;
using System.Text;
using System.Security.Cryptography;
using UnityEngine.Networking;
using UnityEditor.VersionControl;
using System.Net;
using System.Collections;
using UnityEditor.MemoryProfiler;
using System.Linq;
using UnityEngine.SocialPlatforms.GameCenter;
using UnityEditor.Experimental.GraphView;
using System.Net.Http;
using System.Threading.Tasks;
using Amazon.Auth.AccessControlPolicy.ActionIdentifiers;
using Amazon.Polly.Model;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine.Assertions.Must;
using ReadyPlayerMe.Core;
using ReadyPlayerMe.Core.Editor;
using static Flippit.EnumLists;
using UnityEngine.AI;
using System.Dynamic;

namespace Flippit.Editor
{
    public class LoginData
    {
        public string username;
        public string password;
    }
    public class LoginResponse
    {
        public string access_token;
        public string api_key
        {
            get { return api_key; }
        }
    }
    public class FlippitStudio : EditorWindow
    {
        #region UIElements
        Button documentation;
        Button discord;
        Button webSite;
        Button connect;
        VisualElement libraryPanel;
        VisualElement connexionPanel;
        VisualElement ScrollViewContainer;
        TextField loginInput;
        TextField passwordInput;
        #endregion
        #region const
        private const string siteUrl = "https://www.flippit.ai/";
        private const string DiscordSupport = "https://discord.gg/MPMxDgKVrm";
        private const string Documentation = "https://flippit.notion.site/Unity-SDK-732f20e3837245cfbdb5b85d0636fa3b";
        private const string mainUrl = "https://studio-api.flippit.ai";
        private const string personalityPath = "Assets/Flippit/Resources/Personalities";
        private const string prefabPath = "Assets/Flippit/Resources/Prefabs";
        private const string thumbnailsPath = "Thumbnails/";
        private const string prefabsPath = "Assets/Flippit/Resources/Prefabs/";
        private const string controllerPath = "Packages/com.flippit.flippitstudio/Runtime/Resources/Controllers/NPC_Anim_Controlle-clean 1.controller";
        #endregion
        #region private
        private string userlogin;
        private string userPass;
        private bool initialized = false;
        private bool verified;
        private bool isLoadingLibrary;
        private float maxWidth;
        private GameObject prefab;
        private CharacterConverter converter;
        private AvatarLoaderSettings avatarLoaderSettings;
        private bool useEyeAnimations;
        private bool useVoiceToAnim;
        private Character character;
        #endregion
        [MenuItem("Flippit/Studio", false, 0)]
        public static void ShowWindow()
        {
            FlippitStudio wnd = GetWindow<FlippitStudio>();
            wnd.titleContent = new GUIContent("Flippit Studio");
        }
        public void CreateGUI()
        {
            VisualElement root = rootVisualElement;

            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Packages/com.flippit.flippitstudio/Editor/Styles/FlippitStudio.uxml");
            VisualElement labelFromUXML = visualTree.Instantiate();
            root.Add(labelFromUXML);
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Packages/com.flippit.flippitstudio/Editor/Styles/FlippitStudio.uss");

            documentation = root.Q("Documentation") as Button;
            discord = root.Q("Discord") as Button;
            webSite = root.Q("Website") as Button;
            connect = root.Q("Connect") as Button;
            connexionPanel = root.Q("Connexion");
            libraryPanel = root.Q("Library");
            ScrollViewContainer = root.Q("unity-content-container");

            loginInput = root.Q("login") as TextField;
            passwordInput = root.Q("Password") as TextField;

            documentation.RegisterCallback<ClickEvent>(OnDocumentation);
            discord.RegisterCallback<ClickEvent>(OnDiscord);
            webSite.RegisterCallback<ClickEvent>(OnWebSite);
            connect.RegisterCallback<ClickEvent>(OnConnect);

        }
        private void OnGUI()
        {
            if (!initialized) Initialize();
        }
        private void Initialize()
        {
            connexionPanel.style.display = DisplayStyle.Flex;
            libraryPanel.style.display = DisplayStyle.None;
            if (EditorPrefs.GetString("login") != null) userlogin = EditorPrefs.GetString("login");
            if (EditorPrefs.GetString("Password") != null) userPass = EditorPrefs.GetString("Password");
            if (userlogin != null && userPass != null)
            {
                LoginData logData = new()
                {
                    username = userlogin,
                    password = userPass
                };

                TreatData(logData);
                initialized = true;
            }
            else initialized = false;

        }
        public void OnConnect(ClickEvent evt)
        {
            userlogin = loginInput.value;
            userPass = passwordInput.value;
            LoginData logData = new()
            {
                username = userlogin,
                password = userPass
            };
            TreatData(logData);
        }
        private void TreatData(LoginData logData)
        {
            string JSstr = JsonUtility.ToJson(logData);
            string credentialsJson = ApiManager.PostRequest("api/v1/auth/login", JSstr);
            
            if (credentialsJson != null)
            {
                string accessToken = Regex.Match(credentialsJson, @"""access_token"":""([^""]+)""").Groups[1].Value;
                string refreshToken = Regex.Match(credentialsJson, @"""refresh_token"":""([^""]+)""").Groups[1].Value;
                string allCharacters = ApiManager.GetRequest("api/v1/characters", accessToken: accessToken, refreshToken: refreshToken);

                EditorPrefs.SetString("login", logData.username);
                EditorPrefs.SetString("Password", logData.password);
                EditorPrefs.SetString("AccessToken", accessToken);
                EditorPrefs.SetString("RefreshToken", refreshToken);
                SetAPIKeys();
                EditorPrefs.SetBool("registered", true);
                LoadLibraryContent(allCharacters);

            }
            else
            {
                // Handle request failure
                ConfirmationMessage("Verify your login and password");
            }
        }
        private void SetAPIKeys()
        {
            string FlippitApiKeyResponse = ApiManager.GetRequest("api/v1/integrations/get_integration_token", EditorPrefs.GetString("AccessToken"), EditorPrefs.GetString("RefreshToken"));
            string FlippitApiKey = Regex.Match(FlippitApiKeyResponse, @"""access_key"":""([^""]+)""").Groups[1].Value;
            // string SystemApiKeysResponse = ApiManager.GetRequest("unity/creds", EditorPrefs.GetString("AccessToken"), EditorPrefs.GetString("RefreshToken"));
            // string OpenAiApiKey = Regex.Match(SystemApiKeysResponse, @"""open_ai"":""([^""]+)""").Groups[1].Value;
            // string AWSApiKey = Regex.Match(SystemApiKeysResponse, @"""aws_access_key"":""([^""]+)""").Groups[1].Value;
            // string AWSSecret = Regex.Match(SystemApiKeysResponse, @"""aws_secret"":""([^""]+)""").Groups[1].Value; 

            ApiKeyManager apiKeys = Resources.Load<ApiKeyManager>("ApiKeys");

            if (apiKeys == null)
            {
                apiKeys = CreateInstance<ApiKeyManager>();
                AssetDatabase.CreateAsset(apiKeys, "Assets/Flippit/Resources/ApiKeys.asset");
            }
            // Update the fields
            // apiKeys.Flippit = FlippitApiKey; // Set the Flippit API key
            // apiKeys.OpenAI = OpenAiApiKey; // Set the OpenAI API key
            // apiKeys.AWSKey = AWSApiKey; // Set the AWS API key
            // apiKeys.AWSSecret = AWSSecret
            apiKeys.Flippit = FlippitApiKey;
            apiKeys.OpenAI = "12345"; 
            apiKeys.AWSKey = "12345"; 
            apiKeys.AWSSecret = "12345";

            EditorUtility.SetDirty(apiKeys); 
            AssetDatabase.SaveAssets(); 

        }
        private void OnDisable()
        {
            EditorPrefs.SetString("login", userlogin);
            EditorPrefs.SetString("Password", userPass);
            EditorPrefs.SetBool("initialized", verified);
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
        private void LoadLibraryContent(string All)
        {
            if (!isLoadingLibrary)
            {
                isLoadingLibrary = true;
                connexionPanel.style.display = DisplayStyle.None;
                libraryPanel.style.display = DisplayStyle.Flex;
                DrawLocalLibrary(All);
            }
        }
        public void DrawLocalLibrary(string All)
        {

            Texture2D[] thumbnails = Resources.LoadAll<Texture2D>(thumbnailsPath);
            if (thumbnails != null && thumbnails.Length > 0)
            {
                VisualElement rowContainer = null;
                foreach (Texture2D t2D in thumbnails)
                {
                    if (rowContainer == null || rowContainer.childCount >= GetMaxElementsPerRow())
                    {
                        rowContainer = new VisualElement();
                        rowContainer.style.flexDirection = FlexDirection.Row;
                        rowContainer.style.alignItems = Align.Center;
                        rowContainer.style.justifyContent = Justify.FlexStart;
                        ScrollViewContainer.Add(rowContainer);
                    }
                    CreateNPCPanel(t2D, rowContainer, "", null);
                }
                LoadStudioLibrary(thumbnails, rowContainer, All);
            }
            else
            {
                VisualElement rowContainer = null;
                LoadStudioLibrary(thumbnails, rowContainer, All);
            }
            Repaint();
        }
        private void LoadStudioLibrary(Texture2D[] thumbnails, VisualElement rowContainer, string All)
        {
            List<CharacterWrapper> characterWrappers = JsonConvert.DeserializeObject<List<CharacterWrapper>>(All);
            if (characterWrappers.Count == 0)
            {
                VisualElement textpanel = new()
                {
                    name = "messagePanel",

                    style =
                    {
                        flexDirection=FlexDirection.Row
                    }
                };
                ScrollViewContainer.Add(textpanel);
                Label contextmessage = new()
                {
                    name = "contextmessage",
                    text = "No character created yet",
                    style =
                    {
                        fontSize=20,
                    }
                };
                textpanel.Add(contextmessage);
            }
            else
            {
                foreach (CharacterWrapper characterWrapper in characterWrappers)
                {
                    Character character = characterWrapper.character;
                    string characterName = character.name;
                    string glbUrl = character.asset_file_path;
                    bool isCharacterInThumbnails = thumbnails.Any(t => t.name == characterName);

                    if (!isCharacterInThumbnails)
                    {
                        if (rowContainer == null || rowContainer.childCount >= GetMaxElementsPerRow())
                        {
                            rowContainer = new VisualElement();
                            rowContainer.style.flexDirection = FlexDirection.Row;
                            rowContainer.style.alignItems = Align.Center;
                            rowContainer.style.justifyContent = Justify.FlexStart;
                            ScrollViewContainer.Add(rowContainer);
                        }
                        Texture2D UnknowProfile = Resources.Load<Texture2D>("default");
                        UnknowProfile.name = characterName;
                        CreateNPCPanel(UnknowProfile, rowContainer, glbUrl, character);
                    }
                }
            }
        }
        private int GetMaxElementsPerRow()
        {
            maxWidth = position.width - 4;

            int maxElements = Mathf.FloorToInt(maxWidth / 150);
            return maxElements;
        }
        void CreateNPCPanel(Texture2D texture, VisualElement rowContainer, string glbUrl, Character character)
        {
            VisualElement elementBG = new()
            {
                name = "NPCVisualContainer",
                style =
                {
                    width = 140,
                    height = 180,
                    justifyContent = Justify.Center,
                    display = DisplayStyle.Flex,
                    alignItems = Align.Center,
                    alignContent = Align.FlexStart,
                    flexDirection = FlexDirection.Column,
                    backgroundColor = new Color(0.1215686f, 0.145098f, 0.172549f),
                    borderTopLeftRadius = 70,
                    borderBottomLeftRadius = 30,
                    borderTopRightRadius = 20,
                    borderBottomRightRadius = 30,
                    marginLeft = 2,
                    marginRight = 2,
                    marginTop = 2,
                    marginBottom = 2
                }
            };
            rowContainer.Add(elementBG);
            VisualElement CharacterOptions = new()
            {
                name = "Characteroptions",
                style =
                {
                    width = StyleKeyword.Auto,
                    height = 20,
                    minHeight = 20,
                    flexDirection = FlexDirection.Row,
                    flexGrow = 0,
                    flexShrink= 0,
                    alignItems = Align.Stretch,
                    justifyContent = Justify.SpaceAround,
                    paddingBottom = 2,
                    paddingLeft = 2,
                    paddingRight = 2,
                    paddingTop = 0
                }
            };
            elementBG.Add(CharacterOptions);
            Button imageElement = new()
            {
                name = "NPCImg" + texture.name,
                style =
                {
                    backgroundImage= texture,
                    width = 128,
                    height = 128,
                    borderTopLeftRadius = 64,
                    borderBottomLeftRadius = 20,
                    borderTopRightRadius = 20,
                    borderBottomRightRadius = 64,
                    borderLeftWidth = 5,
                    borderRightWidth = 5,
                    borderTopWidth = 5,
                    borderBottomWidth = 5,
                    marginTop = 2,
                    marginBottom = 2,
                    unityBackgroundScaleMode = ScaleMode.ScaleAndCrop,
                    borderLeftColor = new Color(0.9960785f, 0.4156863f, 0.09411766f),
                    borderRightColor = new Color(0.9960785f, 0.4156863f, 0.09411766f),
                    borderTopColor = new Color(0.9960785f, 0.4156863f, 0.09411766f),
                    borderBottomColor = new Color(0.9960785f, 0.4156863f, 0.09411766f),
                    display = DisplayStyle.Flex
                }
            };
            imageElement.clickable.clicked += () =>
            {
                string prefabPath = prefabsPath + texture.name + ".prefab";
                if (File.Exists(prefabPath))
                {
                    InstanciateNpc(texture.name);
                }
                else
                {
                    this.character = character;
                    if (avatarLoaderSettings == null)
                    {
                        avatarLoaderSettings = AvatarLoaderSettings.LoadSettings();
                    }
                    var avatarLoader = new AvatarObjectLoader();
                    avatarLoader.SaveInProjectFolder = true;
                    avatarLoader.OnFailed += Failed;
                    avatarLoader.OnCompleted += Completed;
                    avatarLoader.OperationCompleted += OnOperationCompleted;
                    avatarLoader.AvatarConfig = avatarLoaderSettings.AvatarConfig;
                    if (avatarLoaderSettings != null)
                    {
                        avatarLoader.AvatarConfig = avatarLoaderSettings.AvatarConfig;
                        if (avatarLoaderSettings.GLTFDeferAgent != null)
                        {
                            avatarLoader.GLTFDeferAgent = avatarLoaderSettings.GLTFDeferAgent;
                        }
                    }
                    avatarLoader.LoadAvatar(glbUrl);
                }
            };
            elementBG.Add(imageElement);
            Label NPCName = new()
            {
                name = "CharacterName",
                text = texture.name,
                style =
                {
                    display = DisplayStyle.Flex,
                    color = new Color(255, 255, 255),
                    unityFontStyleAndWeight = FontStyle.Bold,
                    unityTextAlign = TextAnchor.MiddleCenter,
                    flexDirection = FlexDirection.Column,
                    flexWrap = Wrap.Wrap,
                    flexGrow = 0,
                    paddingLeft = 4,
                    paddingRight = 4,
                    paddingTop = 2,
                    paddingBottom = 2,
                    whiteSpace = WhiteSpace.Normal,
                    unityBackgroundScaleMode = ScaleMode.StretchToFill,
                    unityTextOutlineWidth = 0,
                    fontSize = 16
                }
            };
            elementBG.Add(NPCName);
            Texture2D UpdateImg = Resources.Load<Texture2D>("Upload");
            Button Update = new()
            {
                name = "ButtonUpdate",
                style =
                {
                    width = 20,
                    minHeight= 20,
                    minWidth= 20,
                    marginBottom=0,
                    marginLeft=0,
                    marginRight=0,
                    marginTop=0,
                    paddingBottom=0,
                    paddingLeft=0,
                    paddingRight=0,
                    paddingTop=0,
                    backgroundColor = new Color(0.2948113f,0.4942101f,0.5f),
                    backgroundImage= UpdateImg,
                    borderRightWidth=0,
                    borderBottomWidth=0,
                    borderLeftWidth=0,
                    borderTopWidth=0,
                    borderBottomLeftRadius=0,
                    borderBottomRightRadius=0,
                    borderTopLeftRadius=0,
                    borderTopRightRadius=0
                }
            };
            Update.clickable.clicked += () =>
            {
                converter = new();
                string oldName = NPCName.text;
                IaPersonality UpdatePersonality = (IaPersonality)Resources.Load("Personalities/" + NPCName.text);
                Debug.Log("  - ---- -- " + UpdatePersonality.characterName);
                if (oldName != UpdatePersonality.characterName)
                {
                    AssetDatabase.RenameAsset(personalityPath + "/" + oldName + ".asset", UpdatePersonality.characterName);
                    Texture2D thumbnail = (Texture2D)Resources.Load("Thumbnails/" + oldName);
                    AssetDatabase.RenameAsset("Assets/Flippit/Resources/Thumbnails/" + oldName + ".png", UpdatePersonality.characterName);
                    GameObject prefab = Resources.Load("Prefabs/" + oldName) as GameObject;
                    AssetDatabase.RenameAsset(prefabPath + "/" + oldName + ".prefab", UpdatePersonality.characterName);
                }
                EnumLists lists = new();
                Age[] allAges = (Age[])Enum.GetValues(typeof(Age));
                int ageIndex = Array.IndexOf(allAges, UpdatePersonality.characterAge);
                UpdatePersonality.ageId = lists.AgeID[ageIndex];

                Voices[] allVoices = (Voices[])Enum.GetValues(typeof(Voices));
                int voiceIndex = Array.IndexOf(allVoices, UpdatePersonality.voice);
                UpdatePersonality.voiceId = lists.VoicesID[voiceIndex];

                Personality[] allPersonalities = (Personality[])Enum.GetValues(typeof(Personality));
                int personalityIndex = Array.IndexOf(allPersonalities, UpdatePersonality.personality);
                UpdatePersonality.personalityId = lists.personalitiesID[personalityIndex];
                UpdatePersonality.moodId = null;

                string dataCharacter = converter.GetFormatedJsonPayload(UpdatePersonality);
                Debug.Log("api/v1/characters/update: " + dataCharacter);
                ApiManager.PostRequest("api/v1/characters/update", dataCharacter, EditorPrefs.GetString("AccessToken"), EditorPrefs.GetString("RefreshToken"));
                RefreshLibrary();
            };
            CharacterOptions.Add(Update);
            Texture2D DeleteImg = Resources.Load<Texture2D>("Del");
            Button Delete = new()
            {
                name = "ButtonDelete",
                style =
                {
                    width = 20,
                    minHeight= 20,
                    minWidth= 20,
                    marginBottom=0,
                    marginLeft=0,
                    marginRight=0,
                    marginTop=0,
                    paddingBottom=0,
                    paddingLeft=0,
                    paddingRight=0,
                    paddingTop=0,
                    backgroundColor = new Color(1f,0.2f,0.1f),
                    backgroundImage= DeleteImg,
                    borderRightWidth=0,
                    borderBottomWidth=0,
                    borderLeftWidth=0,
                    borderTopWidth=0,
                    borderBottomLeftRadius=0,
                    borderBottomRightRadius=0,
                    borderTopLeftRadius=0,
                    borderTopRightRadius=0
                }
            };
            Delete.clickable.clicked += () =>
            {
                GameObject[] instances = FindObjectsOfType<GameObject>();
                foreach (GameObject instance in instances)
                {
                    if (instance != null && instance.name.StartsWith(NPCName.text))
                    {
                        DestroyImmediate(instance);
                    }
                }
                AssetDatabase.DeleteAsset(prefabPath + "/" + NPCName.text + ".prefab");
                AssetDatabase.DeleteAsset(personalityPath + "/" + NPCName.text + ".asset");
                AssetDatabase.DeleteAsset("Assets/Flippit/Resources/Thumbnails/" + NPCName.text + ".png");
                AssetDatabase.Refresh();
                ConfirmationMessage(NPCName.text + " has been deleted.");
                RefreshLibrary();
            };
            CharacterOptions.Add(Delete);
        }
        private void InstanciateNpc(string NpcName)
        {
            GameObject npcPrefab = PrefabUtility.LoadPrefabContents(prefabsPath + NpcName + ".prefab");
            if (npcPrefab != null)
            {
                Instantiate(npcPrefab);
                //GameObject instanciedPrefab = PrefabUtility.InstantiatePrefab(npcPrefab) as GameObject;

                ConfirmationMessage(npcPrefab.name + " has been created");
            }
            else
            {
                ConfirmationMessage(npcPrefab + " has not been created");
            }

        }
        private void Failed(object sender, FailureEventArgs args)
        {
            ConfirmationMessage($"{args.Type} - {args.Message} - {args.Url}");
        }
        private void OnOperationCompleted(object sender, IOperation<AvatarContext> e)
        {
            //rien
        }
        private void Completed(object sender, CompletionEventArgs args)
        {
            if (avatarLoaderSettings == null)
            {
                avatarLoaderSettings = AvatarLoaderSettings.LoadSettings();
            }
            var paramHash = AvatarCache.GetAvatarConfigurationHash(avatarLoaderSettings.AvatarConfig);
            var path = $"{DirectoryUtility.GetRelativeProjectPath(args.Avatar.name, paramHash)}/{args.Avatar.name}";
            GameObject avatar = EditorUtilities.CreateAvatarPrefab(args.Metadata, path);
            if (useEyeAnimations) avatar.AddComponent<EyeAnimationHandler>();
            if (useVoiceToAnim) avatar.AddComponent<VoiceHandler>();
            DestroyImmediate(args.Avatar, true);
            Selection.activeGameObject = avatar;
            ConvertToNPC(character);
        }

        class CharacterWrapper
        {
            public Character character { get; set; }
        }
        class Character
        {
            public string character_id { get; set; }
            public string owner_id { get; set; }
            public string name { get; set; }
            public string backstory { get; set; }
            public string personality_id { get; set; }
            public string voice_id { get; set; }
            public string role { get; set; }
            public string age_id { get; set; }
            public string hobbies { get; set; }
            public string mood_id { get; set; }
            public string catch_phrases { get; set; }
            public string primary_goal { get; set; }
            public string urls { get; set; }
            public string asset_file_path { get; set; }
        }
        void ConvertToNPC(Character character)
        {
            if (Selection.activeGameObject != null)
            {
                GameObject selectedObject = Selection.activeGameObject;
                GameObject emptyGameObject = new()
                {
                    name = character.name,
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
                #endregion

                iaSc.Avatar = selectedObject;
                Debug.Log(iaSc.Avatar);
                iaSc.Detector = Detector;

                #region Set personality
                IaPersonality perso = CreateInstance<IaPersonality>();
                iaSc.personality = perso;
                perso.ownerId = character.owner_id;
                perso.characterId = character.character_id;
                perso.characterName = character.name;
                perso.backstory = character.backstory;
                EnumLists list = new();
                string[] voicesStr = list.VoicesID;
                perso.voice = (Voices)GetIndex(voicesStr, character.voice_id);
                string[] personStr = list.personalitiesID;
                perso.personality = (Personality)GetIndex(personStr, character.personality_id);
                string[] ageStr = list.AgeID;
                perso.characterAge = (Age)GetIndex(ageStr, character.age_id);
                perso.catchPhrases = character.catch_phrases;
                perso.hobbies = character.hobbies;
                perso.primaryGoal = character.primary_goal;
                perso.role = character.role;
                perso.assetFilePath = character.asset_file_path;
                #endregion

                #region TextToSpeech
                var TTS = emptyGameObject.AddComponent<TTS>();
                TTS.audioSource = emptyGameObject.AddComponent<AudioSource>();
                #endregion
                #region store asset data

                if (!AssetDatabase.IsValidFolder("assets/Flippit"))
                {
                    AssetDatabase.CreateFolder("Assets", "Flippit");
                }
                if (!AssetDatabase.IsValidFolder("Assets/Flippit/Resources"))
                {
                    AssetDatabase.CreateFolder("Flippit", "Ressources");
                }
                if (!AssetDatabase.IsValidFolder(personalityPath))
                {
                    AssetDatabase.CreateFolder("Assets/Flippit/Resources", "Personalities");
                }
                if (!AssetDatabase.IsValidFolder(prefabPath))
                {
                    AssetDatabase.CreateFolder("Assets/Flippit/Resources", "Prefabs");
                }
                AssetDatabase.CreateAsset(perso, personalityPath + "/" + character.name + ".asset");
                prefab = PrefabUtility.SaveAsPrefabAsset(emptyGameObject, prefabPath + "/" + character.name + ".prefab");
                AssetDatabase.SaveAssets();
                DestroyImmediate(emptyGameObject);
                GameObject newPrefabInstance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                ThumbnailGenerator.GenerateThumbnail(newPrefabInstance, character.name, 128, 128);
                AssetDatabase.Refresh();
                #endregion

                ConfirmationMessage(character.name + " has been Created, Check Your Prefab Folder.");
                RefreshLibrary();
            }
            else
            {
                ConfirmationMessage("Please, select the Ia Character First.");
            }
        }
        private int GetIndex(string[] array, string chain)
        {
            for (int i = 0; i < array.Length; i++)
            {
                if (array[i] == chain) return i;
            }
            return -1;
        }
        public void RefreshLibrary()
        {
            isLoadingLibrary = false;
            ScrollViewContainer.Clear();
            string allCharacters = ApiManager.GetRequest("api/v1/characters", EditorPrefs.GetString("AccessToken"), EditorPrefs.GetString("RefreshToken"));
            LoadLibraryContent(allCharacters);
        }
        public void ConfirmationMessage(string message)
        {
            Debug.Log(message);
        }
    }
}
