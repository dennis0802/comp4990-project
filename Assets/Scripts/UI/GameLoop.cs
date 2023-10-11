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
    [DisallowMultipleComponent]
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
        public static int FileId = -1;
        // To track if buying/selling
        public static bool IsSelling = false;
        // To track selling rate
        public static float SellRate;

        // List of perks (mechanic, sharpshooter, health care, surgeon, programmer, musician)
        public static List<string> Perks = new List<string>(){
            "Mechanic", "Sharpshooter", "Health Care", "Surgeon", "Programmer", "Musician"
        };

        // List of traits (charming, paranoid, civilized, bandit, hot headed, creative)
        public static List<string> Traits = new List<string>(){
            "Charming", "Paranoid", "Civilized", "Bandit", "Hot Headed", "Creative"  
        };

        [Tooltip("Game over screen when leader is dead")]
        [SerializeField]
        private GameObject gameOverScreen;

        public static GameObject GameOverScreen;

        private void Start(){
            GameOverScreen = gameOverScreen;
        }

        private void Update(){

        }

        /// <summary>
        /// Utility to generate random numbers and round to the nearest ten
        /// </summary>
        /// <param name="lower">The lower bound</param>
        /// <param name="upper">The upper bound</param>
        /// <returns>A random number rounded to the nearest ten.</returns>
        public static int RoundTo10(int lower, int upper){
            float gen = (float)(Random.Range(lower, upper))/10;
            gen = Mathf.Round(gen);
            return (int)(gen)*10;
        }

        public static void LoadAsync(int index){

        }

        public static IEnumerator LoadAsynchronously(int index){
            AsyncOperation op = SceneManager.LoadSceneAsync(index);

            while(!op.isDone){
                Debug.Log(op.progress);
                yield return null;
            }
        }
    }
}