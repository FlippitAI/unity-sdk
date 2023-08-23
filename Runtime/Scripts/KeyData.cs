using UnityEngine;

namespace Flippit
{
    [CreateAssetMenu(menuName = "Flippit/Key Data")]
    public class KeyData : ScriptableObject
    {
        public string login;
        public string Password;
        public string key;
    }
}

