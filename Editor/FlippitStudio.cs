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
using UnityEngine.SocialPlatforms;
using UnityEngine.TextCore.Text;

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
        public string Api_key
        {
            get { return Api_key; }
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
        private const string personalityPath = "Assets/Flippit/Resources/Personalities";
        private const string prefabPath = "Assets/Flippit/Resources/Prefabs";
        private const string thumbnailsPath = "Thumbnails/";
        private const string prefabsPath = "Assets/Flippit/Resources/Prefabs/";
        #endregion
        #region private
        private string userlogin;
        private string userPass;
        private bool initialized = false;
        private readonly bool verified;
        private bool isLoadingLibrary;
        private CharacterConverter converter;
        private ApiKeyManager apiKeys;
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
            if (loginInput.value != null && passwordInput.value!=null)
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
            else
            {
                Debug.LogWarning("Enter your Identifier and Password");
            }
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
                Debug.LogWarning("Verify your login and password");
            }
        }
        private void SetAPIKeys()
        {
            string FlippitApiKeyResponse = ApiManager.GetRequest("api/v1/integrations/get_integration_token", EditorPrefs.GetString("AccessToken"), EditorPrefs.GetString("RefreshToken"));
            string FlippitApiKey = Regex.Match(FlippitApiKeyResponse, @"""access_key"":""([^""]+)""").Groups[1].Value;

            string SystemApiKeysResponse = ApiManager.GetRequest("api/v1/unity/creds", EditorPrefs.GetString("AccessToken"), EditorPrefs.GetString("RefreshToken"));
            string OpenAiApiKey = Regex.Match(SystemApiKeysResponse, @"""open_ai"":""([^""]+)""").Groups[1].Value;
            string AWSApiKey = Regex.Match(SystemApiKeysResponse, @"""aws_access_key"":""([^""]+)""").Groups[1].Value;
            string AWSSecret = Regex.Match(SystemApiKeysResponse, @"""aws_secret"":""([^""]+)""").Groups[1].Value; 

            if(File.Exists("Assets/Flippit/Resources/ApiKeys.asset"))
            {
                apiKeys = Resources.Load<ApiKeyManager>("ApiKeys");
            }
            else
            {
                if (!AssetDatabase.IsValidFolder("Assets/Flippit"))
                {
                    AssetDatabase.CreateFolder("Assets", "Flippit");
                    AssetDatabase.Refresh();
                }
                if (!AssetDatabase.IsValidFolder("Assets/Flippit/Resources"))
                {
                    AssetDatabase.CreateFolder("Assets/Flippit", "Resources");
                    AssetDatabase.Refresh();
                }
                    apiKeys = CreateInstance<ApiKeyManager>();
                    AssetDatabase.CreateAsset(apiKeys, "Assets/Flippit/Resources/ApiKeys.asset");
            }

            if(FlippitApiKey != null)apiKeys.Flippit = FlippitApiKey; // Set the Flippit API key
            if(OpenAiApiKey != null)apiKeys.OpenAI = OpenAiApiKey; // Set the OpenAI API key
            if(AWSApiKey != null)apiKeys.AWSKey = AWSApiKey; // Set the AWS API key
            if(AWSSecret != null)apiKeys.AWSSecret = AWSSecret; // Set the AWS secret

            UnityEditor.EditorUtility.SetDirty(apiKeys); // Mark the asset as dirty
            UnityEditor.AssetDatabase.SaveAssets(); // Save the changes

        }
        private void OnDisable()
        {
            if (userlogin != null && userPass != null)
                {
                    EditorPrefs.SetString("login", userlogin);
                    EditorPrefs.SetString("Password", userPass);
                    EditorPrefs.SetBool("initialized", verified);
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
        private void LoadLibraryContent(string All)
        {
            if (!isLoadingLibrary)
            {
                isLoadingLibrary = true;
                connexionPanel.style.display = DisplayStyle.None;
                libraryPanel.style.display = DisplayStyle.Flex;
                LoadStudioLibrary(All);
            }
        }
       
        private void LoadStudioLibrary(string All)
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
                Texture2D[] thumbnails = Resources.LoadAll<Texture2D>(thumbnailsPath);

                float totalWidth = position.width - 4;
                int maxElementsPerRow = Mathf.FloorToInt(totalWidth / 150);
                VisualElement rowContainer = null;
                foreach (CharacterWrapper characterWrapper in characterWrappers)
                {
                    Character character = characterWrapper.Character;
                    string characterName = character.Name;
                    string glbUrl = character.Asset_file_path;
                    bool isCharacterInThumbnails = thumbnails.Any(t => t.name == characterName);

                    if (rowContainer == null || rowContainer.childCount >= maxElementsPerRow)
                    {
                        rowContainer = new VisualElement();
                        rowContainer.style.flexDirection = FlexDirection.Row;
                        rowContainer.style.alignItems = Align.Center;
                        rowContainer.style.justifyContent = Justify.FlexStart;
                        ScrollViewContainer.Add(rowContainer);
                    }
                    if (!isCharacterInThumbnails)
                    {
                        
                        Texture2D UnknowProfile = Resources.Load<Texture2D>("default");
                        UnknowProfile.name = characterName;
                        CreateNPCPanel(UnknowProfile, rowContainer, glbUrl, character);
                    }
                    else
                    {
                        
                        foreach (Texture2D t2D in thumbnails)
                        {
                            if (rowContainer == null || rowContainer.childCount >= maxElementsPerRow)
                            {
                                rowContainer = new VisualElement();
                                rowContainer.style.flexDirection = FlexDirection.Row;
                                rowContainer.style.alignItems = Align.Center;
                                rowContainer.style.justifyContent = Justify.FlexStart;
                                ScrollViewContainer.Add(rowContainer);
                            }
                            CreateNPCPanel(t2D, rowContainer, "", character);
                        }
                    }
                }
            }
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
                PersonalityWindow window = new(texture, glbUrl, character);
                window.ShowWindow();
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
                Debug.LogWarning(NPCName.text + " has been deleted.");
                RefreshLibrary();
            };
            CharacterOptions.Add(Delete);
        }
        
        public void RefreshLibrary()
        {
            isLoadingLibrary = false;
            ScrollViewContainer.Clear();
            string allCharacters = ApiManager.GetRequest("api/v1/characters", EditorPrefs.GetString("AccessToken"), EditorPrefs.GetString("RefreshToken"));
            LoadLibraryContent(allCharacters);
        }
        
    }
    public class PersonalityWindow : EditorWindow
    {
        #region UIElements
        Button documentation;
        Button discord;
        Button webSite;
        Button CreateCharacter;
        Button AddPersonality;
        #endregion
        #region const
        private const string siteUrl = "https://www.flippit.ai/";
        private const string DiscordSupport = "https://discord.gg/MPMxDgKVrm";
        private const string Documentation = "https://flippit.notion.site/Unity-SDK-732f20e3837245cfbdb5b85d0636fa3b";
        private const string personalityPath = "Assets/Flippit/Resources/Personalities";
        private const string prefabPath = "Assets/Flippit/Resources/Prefabs";
        private const string prefabsPath = "Assets/Flippit/Resources/Prefabs/";
        private const string controllerPath = "Packages/com.flippit.flippitstudio/Runtime/Resources/Controllers/NPC_Anim_Controller.controller";

        #endregion
        #region private
        private Character character;
        private Texture2D texture;
        private string glbUrl;
        private AvatarLoaderSettings avatarLoaderSettings;
        private readonly bool useEyeAnimations;
        private readonly bool useVoiceToAnim;
        private GameObject prefab;
        #endregion

        public PersonalityWindow(Texture2D texture, string glbUrl, Character character)
        {
            this.texture = texture;
            this.glbUrl = glbUrl;
            this.character = character;
        }
        private static PersonalityWindow currentWindow;
        public void ShowWindow()
        {
            if (currentWindow != null)
            {
                currentWindow.Close();
            }
            PersonalityWindow window = GetWindow<PersonalityWindow>();
            window.texture = texture;
            window.glbUrl = glbUrl;
            window.character = character;
            window.titleContent = new GUIContent(character.Name);
            currentWindow = window;
        }
        public void CreateGUI()
        {
            VisualElement root = rootVisualElement;
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Packages/com.flippit.flippitstudio/Editor/Styles/PersonalityImportWindow.uxml");
            VisualElement labelFromUXML = visualTree.Instantiate();
            root.Add(labelFromUXML);
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Packages/com.flippit.flippitstudio/Editor/Styles/FlippitStudio.uss");
            documentation = root.Q("Documentation") as Button;
            discord = root.Q("Discord") as Button;
            webSite = root.Q("Website") as Button;
            CreateCharacter = root.Q("CreateCharacter") as Button;
            AddPersonality = root.Q("AddPersonality") as Button;
            documentation.RegisterCallback<ClickEvent>(OnDocumentation);
            discord.RegisterCallback<ClickEvent>(OnDiscord);
            webSite.RegisterCallback<ClickEvent>(OnWebSite);
            if (string.IsNullOrEmpty(glbUrl))
            {
                CreateCharacter.SetEnabled(false);
            }
            else
            {
                CreateCharacter.SetEnabled(true);
            }
            CreateCharacter.RegisterCallback<ClickEvent>(OnCreateCharacter);
            AddPersonality.RegisterCallback<ClickEvent>(evt=> OnAddPersonality(evt,character));
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
        public void OnCreateCharacter(ClickEvent evt)
        {
            string prefabPath = prefabsPath + texture.name + ".prefab";
            if (File.Exists(prefabPath))
            {
                InstanciateNpc(texture.name);
            }
            else
            {
                if (avatarLoaderSettings == null)
                {
                    avatarLoaderSettings = AvatarLoaderSettings.LoadSettings();
                }
                var avatarLoader = new AvatarObjectLoader
                {
                    SaveInProjectFolder = true
                };
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
        }
        public void OnAddPersonality(ClickEvent evt, Character character)
        {
            if (evt is null)
            {
                throw new ArgumentNullException(nameof(evt));
            }

            if (Selection.activeGameObject != null)
            {
                GameObject selectedObject = Selection.activeGameObject;
                if (selectedObject.name == "Player"||selectedObject.CompareTag("Player")) Debug.LogWarning("You selected the Player Character. Pleaze, Unpack it, Untag it or rename it if you are sure.");
                else
                {
                    if (selectedObject.TryGetComponent<IACharacter>(out var iaCharacter))
                    {
                        if (character == null) Debug.Log("character est null");
                        IaPersonality loadedPersonality = AssetDatabase.LoadAssetAtPath<IaPersonality>("Assets/Flippit/Resources/Personalities/" + character.Name + ".asset");
                        if (loadedPersonality != null)
                        {
                            iaCharacter.personality = loadedPersonality;
                            Debug.Log("Personality Set to Character.");
                        }
                        else
                        {
                            IaPersonality perso = CreateInstance<IaPersonality>();
                            iaCharacter.personality = perso;
                            perso.ownerId = character.Owner_id;
                            perso.characterId = character.Character_id;
                            perso.characterName = character.Name;
                            perso.backstory = character.Backstory;
                            EnumLists list = new();
                            string[] voicesStr = list.VoicesID;
                            perso.voice = (Voices)GetIndex(voicesStr, character.Voice_id);
                            string[] personStr = list.personalitiesID;
                            perso.personality = (Personality)GetIndex(personStr, character.Personality_id);
                            string[] ageStr = list.AgeID;
                            perso.characterAge = (Age)GetIndex(ageStr, character.Age_id);
                            perso.catchPhrases = character.Catch_phrases;
                            perso.hobbies = character.Hobbies;
                            perso.primaryGoal = character.Primary_goal;
                            perso.role = character.Role;
                            perso.assetFilePath = character.Asset_file_path;
                            AssetDatabase.CreateAsset(perso, personalityPath + "/" + character.Name + ".asset");
                            Debug.Log("New Personality Downloaded and Added to Character.");
                        }
                    }
                    else
                    {
                        ConvertToNPC(character);
                    }
                }
            }
            else
            {
                Debug.LogWarning("No Character selected. Please select the character on which you would like to add personality.");
            }
        }
        private void InstanciateNpc(string NpcName)
        {
            GameObject npcPrefab = PrefabUtility.LoadPrefabContents(prefabsPath + NpcName + ".prefab");
            if (npcPrefab != null)
            {
                Instantiate(npcPrefab);
                ConfirmationMessage(npcPrefab.name + " has been created");
            }
            else
            {
                ConfirmationMessage(npcPrefab + " has not been created");
            }

        }
        public void Failed(object sender, FailureEventArgs args)
        {
            ConfirmationMessage($"{args.Type} - {args.Message} - {args.Url}");
        }
        public void ConfirmationMessage(string message)
        {
            Debug.Log(message);
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
        void ConvertToNPC(Character character)
        {
            if (Selection.activeGameObject != null)
            {
                GameObject selectedObject = Selection.activeGameObject;
                GameObject emptyGameObject = new()
                {
                    name = character.Name,
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
                emptyGameObject.AddComponent<AudioSource>();
                IACharacter iaSc = emptyGameObject.AddComponent<IACharacter>();
                #endregion
                iaSc.Avatar = selectedObject;
                iaSc.Detector = Detector;
                #region Set personality
                IaPersonality perso = CreateInstance<IaPersonality>();
                iaSc.personality = perso;
                perso.ownerId = character.Owner_id;
                perso.characterId = character.Character_id;
                perso.characterName = character.Name;
                perso.backstory = character.Backstory;
                EnumLists list = new();
                string[] voicesStr = list.VoicesID;
                perso.voice = (Voices)GetIndex(voicesStr, character.Voice_id);
                string[] personStr = list.personalitiesID;
                perso.personality = (Personality)GetIndex(personStr, character.Personality_id);
                string[] ageStr = list.AgeID;
                perso.characterAge = (Age)GetIndex(ageStr, character.Age_id);
                perso.catchPhrases = character.Catch_phrases;
                perso.hobbies = character.Hobbies;
                perso.primaryGoal = character.Primary_goal;
                perso.role = character.Role;
                perso.assetFilePath = character.Asset_file_path;
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
                AssetDatabase.CreateAsset(perso, personalityPath + "/" + character.Name + ".asset");
                prefab = PrefabUtility.SaveAsPrefabAsset(emptyGameObject, prefabPath + "/" + character.Name + ".prefab");
                AssetDatabase.SaveAssets();
                DestroyImmediate(emptyGameObject);
                GameObject newPrefabInstance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                ThumbnailGenerator.GenerateThumbnail(newPrefabInstance, character.Name, 128, 128);
                AssetDatabase.Refresh();
                #endregion
                Debug.Log(newPrefabInstance.name + " has been Created, Check Your Prefab Folder.");
            }
            else
            {
                Debug.LogWarning("Please, select the Ia Character First.");
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
        
    }
    public class CharacterWrapper
    {
        public Character Character { get; set; }
    }
    public class Character
    {
        public string Character_id { get; set; }
        public string Owner_id { get; set; }
        public string Name { get; set; }
        public string Backstory { get; set; }
        public string Personality_id { get; set; }
        public string Voice_id { get; set; }
        public string Role { get; set; }
        public string Age_id { get; set; }
        public string Hobbies { get; set; }
        public string Mood_id { get; set; }
        public string Catch_phrases { get; set; }
        public string Primary_goal { get; set; }
        public string Urls { get; set; }
        public string Asset_file_path { get; set; }
    }
}
