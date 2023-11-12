using System.Collections;
using System.Collections.Generic;
using System.Data;
using UnityEngine;
using UnityEngine.UI;
using Mono.Data.Sqlite;
using TMPro;
using Database;
using UnityEngine.SceneManagement;
using System;
using RestPhase;
using CombatPhase;
using System.Linq;
using TravelPhase;

namespace UI{
    [DisallowMultipleComponent]
    public class GameOver : MonoBehaviour {

        [Header("Screen Components")]
        [Tooltip("Text to display total score")]
        [SerializeField]
        private TextMeshProUGUI totalScoreText;

        [Tooltip("Text to display highscore details")]
        [SerializeField]
        private TextMeshProUGUI scoreDetailText;

        [Tooltip("Main menu script")]
        [SerializeField]
        private MainMenu mainMenu;

        /// <summary>
        /// Name to use for scoreboard
        /// </summary>
        private string leaderName = "";

        /// <summary>
        /// Name to use for scoreboard
        /// </summary>
        private int friendsAlive = 0;

        void OnEnable(){
            mainMenu = GameObject.FindGameObjectWithTag("MainScreen").GetComponent<MainMenu>();
            leaderName = GameLoop.LeaderName;
            CalculateScore();
        }

        /// <summary>
        /// Calculate the overall score
        /// </summary>
        private void CalculateScore(){
            // Check for friends alive
            IEnumerable<ActiveCharacter> characters = DataUser.dataManager.GetActiveCharacters();
            characters = characters.Where<ActiveCharacter>(c=>c.FileId == GameLoop.FileId);
            friendsAlive = characters.Where<ActiveCharacter>(c=>c.IsLeader == 0).Count();

            foreach(ActiveCharacter ac in characters){
                if(ac.IsLeader == 1 && ac.CharacterName != null){
                    leaderName = ac.CharacterName;
                }
            }

            // Check resources
            Save save = DataUser.dataManager.GetSaveById(GameLoop.FileId);
            int distance = save.Distance, difficulty = save.Difficulty, food = save.Food, gas = (int)(save.Gas), scrap = save.Scrap, money = save.Money, 
                medkit = save.Medkit, tire = save.Tire, battery = save.Battery, ammo = save.Ammo, timeTaken = save.OverallTime;

            // Score is base amount of supplies (medkit, tires, and batteries doubled) + a tenth of the distance + a time bonus determined below multiplied by a difficulty bonus
            // + friends alive (500 each).
            int finalScore = food + gas + scrap + money + medkit * 2 + tire * 2 + battery * 2 + ammo + distance/10 + 500 * friendsAlive;

            // A faster time means a higher score
            finalScore += 1000 - timeTaken > 0 ? 1000 - timeTaken : 0;
            finalScore *= difficulty % 2 == 0 ? 2 : 1;

            // Check for the number of high scores - creating a new score means id is one above the highest existing.
            int count = DataUser.dataManager.GetScores().Count();

            LocalHighscore highscore = new LocalHighscore(){Id = count + 1, LeaderName = leaderName, Difficulty = difficulty, Distance = distance, FriendsAlive = friendsAlive, FinalScore = finalScore};
            DataUser.dataManager.InsertScore(highscore);

            scoreDetailText.text = "Food: " + food + "\tGas: " + gas + "\tScrap: " + scrap + "\tMoney: " + money + "\tAmmo: " + ammo +
                                   "\nMedkit: " + medkit + "x2" + "\tBattery: " + battery + "x2" + "\tTire: " + tire + "x2\n" + "Distance: " + distance + " / 10\t" +
                                   "Time Taken: " + (1000 - timeTaken) + "\tFriends Alive: 500 * " + friendsAlive;
            totalScoreText.text = "Final Score: " + finalScore.ToString();
            mainMenu.DeleteFile();
        }

        /// <summary>
        /// Confirm that score was viewed.
        /// </summary>
        public void ConfirmView(){
            SceneManager.LoadScene(0);
            GameLoop.FileId = -1;
        }
    }
}