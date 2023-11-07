using UnityEngine;

namespace Database{
    [CreateAssetMenu()]
    public class DatabaseData : ScriptableObject {
        public string pass;
        public string salt;
        public string uri;
    }
}