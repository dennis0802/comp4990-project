using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Database;

namespace Database{
    public class DataUser : MonoBehaviour
    {
        public static DataManager dataManager;
        // Start is called before the first frame update
        void Start()
        {
            DontDestroyOnLoad(this.gameObject);
            if(dataManager == null){
                dataManager = new DataManager();
            }
            else{
                Destroy(gameObject);
            }
            dataManager.StartUp();
        }
    }
}