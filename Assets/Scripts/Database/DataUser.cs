using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Database;

namespace Database{
    public class DataUser : MonoBehaviour
    {
        [Tooltip("Database credentials object")]
        [SerializeField]
        private DatabaseData credentials;

        public static DataManager dataManager;
        // Start is called before the first frame update
        void Start()
        {
            if(dataManager == null){
                dataManager = new DataManager(credentials.pass, credentials.salt, credentials.uri);
                DontDestroyOnLoad(this.gameObject);
            }
            else{
                Destroy(gameObject);
            }
            dataManager.StartUp();
        }
    }
}
