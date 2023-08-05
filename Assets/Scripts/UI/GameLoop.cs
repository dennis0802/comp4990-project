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

        // List of perks (mechanic, sharpshooter, health care, surgeon, programmer, musician)
        public static List<string> Perks = new List<string>(){
            "Mechanic", "Sharpshooter", "Health Care", "Surgeon", "Programmer", "Musician"
        };

        // List of traits (charming, paranoid, civilized, bandit, hot headed, creative)
        public static List<string> Traits = new List<string>(){
            "Charming", "Paranoid", "Civilized", "Bandit", "Hot Headed", "Creative"  
        };

        private void Start(){
            IDbConnection dbConnection = GameDatabase.CreateSavesAndOpenDatabase();
            IDbCommand dbCommandReadValues = dbConnection.CreateCommand();
            dbCommandReadValues.CommandText = "SELECT * FROM SaveFilesTable WHERE id = " + GameLoop.FileId;
            IDataReader dataReader = dbCommandReadValues.ExecuteReader();
            dataReader.Read();
        }

        private void Update(){

        }
    }
}