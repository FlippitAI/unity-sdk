using System.Collections;
using UnityEngine;

namespace Flippit
{

    [CreateAssetMenu(menuName = "Flippit/Character Personality")]

    public class IaPersonality : ScriptableObject
    {
        #region public variables
        public string characterName;
        [TextArea(3,10)]
        public string backstory;
        public string primaryGoal;
        public string role = "";
        public string hobbies="";
        public string catchPhrases;
        public EnumLists.Personality personality;
        public EnumLists.Age characterAge;
        public EnumLists.Voices voice;
        public float DetectionSize = 2f;
        public float MoveSpeed = 2f;
        public GameObject[] QuestObjects;
        [HideInInspector]
        public string moodId = null;
        [HideInInspector]
        public string urls= null;
        [HideInInspector]
        public string assetFilePath;
        #endregion
        #region hidden Variables
        [HideInInspector]
        public string action;
        [HideInInspector]
        public EnumLists.Animation animation;
        [HideInInspector]
        public string prompt;
        [HideInInspector]
        public string characterId;
        [HideInInspector]
        public string ownerId;
        [HideInInspector]
        public string personalityId;
        [HideInInspector]
        public string ageId;
        [HideInInspector]
        public string voiceId;
        #endregion
    }
}