using System.Collections;
using System.Collections.Generic;
using System.Data;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Mono.Data.Sqlite;
using TMPro;
using Database;

namespace UI{
    public class GameLoop : MonoBehaviour{
        // To track rations; 1 = low, 2 = medium, 3 = high
        public static int RationsMode = 2;
        // To track the time (24-hour, assume a universal clock and no timezones)
        public static int Hour = 12;
        // To track the mutant activity; 1 = low, 2 = medium, 3 = high, 4 = ravenous
        public static int Activity = 1;
        // To track the pace of the car; 1 = slow, 2 = medium, 3 = fast
        public static int Pace = 2;
        // To track the file data in the database.
        public static int FileId;

        private void Update(){
            if(Hour >= 21 || Hour <= 5){
                Activity = 4;
            }
            else if(Hour >= 18 || Hour <= 8){
                Activity = 3;
            }
            else if(Hour >= 16 || Hour <= 10){
                Activity = 2;
            }
            else{
                Activity = 1;
            }
        }
    }
}