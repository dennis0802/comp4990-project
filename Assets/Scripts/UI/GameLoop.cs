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

        // List of traits (charming, paranoid, optimist, bandit, hot headed, creative)
        public static List<string> Traits = new List<string>(){
            "Charming", "Paranoid", "Optimist", "Bandit", "Hot Headed", "Creative"  
        };

        [Tooltip("Sprites for map screen")]
        [SerializeField]
        private Sprite[] maps;

        [Tooltip("Game over screen when leader is dead")]
        [SerializeField]
        private GameObject gameOverScreen;

        public static GameObject GameOverScreen;
        public static Sprite[] Maps;

        void Awake(){
            Maps = maps;
        }

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

        /// <summary>
        /// Utility to load a scene asynchronously
        /// </summary>
        public static IEnumerator LoadAsynchronously(int index){
            AsyncOperation op = SceneManager.LoadSceneAsync(index);

            while(!op.isDone){
                //Debug.Log(op.progress);
                yield return null;
            }
        }

        /// <summary>
        /// Utility to retrieve a map image
        /// </summary>
        /// <param name="prevTown">Index of the previous town</param>
        /// <param name="curTown">Index of the current (incoming) town</param>
        /// <returns>A sprite of the map image</returns>
        public static Sprite RetrieveMapImage(int prevTown, int curTown){
            // 0 = Montreal, 1 = Ottawa, 2 = Timmins, 3 = Thunder Bay, 11 = Toronto, 12 = Windsor, 13 = Chicago, 14 = Milwaukee, 15 = Minneapolis,
            // 16 = Winnipeg, 17 = Regina, 18 = Calgary, 19 = Banff, 20/38 = Kelowna, 26 = Saskatoon, 27 = Edmonton, 28 = Hinton, 29 = Kamloops 
            Sprite sprite = Maps[0];

            if(prevTown == 1 && curTown == 2){
                sprite = Maps[prevTown+1];
            }
            else if(prevTown == 1 && curTown == 11){
                sprite = Maps[5];
            }
            // Winnipeg is a special case
            else if(prevTown == 16 && curTown == 17){
                sprite = Maps[11];
            }
            else if(prevTown == 16 && curTown == 26){
                sprite = Maps[16];
            }
            // Banff is a special case
            else if(prevTown == 19 && curTown == 20){
                sprite = Maps[14];
            }
            else if(prevTown == 19 && curTown == 29){
                sprite = Maps[15];
            }
            // Hinton is a special case
            else if(prevTown == 28 && curTown == 29){
                sprite = Maps[20];                
            }
            else if(prevTown == 28 && curTown == 38){
                sprite = Maps[19];                
            }
            // Cases 20, 38
            else if(prevTown == 20 || prevTown == 38){
                sprite = Maps[21];  
            }
            else if(prevTown == 27 || prevTown == 26){
                sprite = Maps[prevTown - 9];
            }

            // Case 29
            else if(prevTown == 29){
                sprite = Maps[22];                  
            }
            // Cases 11-15, 17-18
            else if((prevTown >= 11 && prevTown <= 15) || (prevTown >= 17 && prevTown <= 18)){
                sprite = Maps[prevTown-5];
            }
            // Cases 0, 2, 3
            else if(prevTown <= 3){
                sprite = Maps[prevTown+1];
            }
            return sprite;
        }
    }
}