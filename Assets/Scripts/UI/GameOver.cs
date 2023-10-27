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
            leaderName = !Equals(RestMenu.LeaderName, "") ? RestMenu.LeaderName : !Equals(CombatManager.LeaderName, "") ? CombatManager.LeaderName : "NULL";
            CalculateScore();
        }

        /// <summary>
        /// Calculate the overall score
        /// </summary>
        private void CalculateScore(){
            // Check for friends alive
            IDbConnection dbConnection = GameDatabase.OpenDatabase();
            IDbCommand dbCommandReadValues = dbConnection.CreateCommand();
            dbCommandReadValues.CommandText = "SELECT friend1Name, friend2Name, friend3Name, leaderName FROM ActiveCharactersTable WHERE id = " + GameLoop.FileId;
            IDataReader dataReader = dbCommandReadValues.ExecuteReader();
            dataReader.Read();

            for(int i = 0; i < 3; i++){
                friendsAlive += dataReader.IsDBNull(i) ? 0 : 1;
            }

            if(!dataReader.IsDBNull(3)){
                leaderName = dataReader.GetString(3);
            }

            // Check resources
            dbCommandReadValues = dbConnection.CreateCommand();
            dbCommandReadValues.CommandText = "SELECT difficulty, distance, food, gas, scrap, money, medkit, tire, battery, ammo, overallTime FROM SaveFilesTable " + 
                                              "WHERE id = " + GameLoop.FileId + ";";
            dataReader = dbCommandReadValues.ExecuteReader();
            dataReader.Read();

            int distance = dataReader.GetInt32(1), difficulty = dataReader.GetInt32(0), food = dataReader.GetInt32(2), gas = (int)(dataReader.GetFloat(3)),
                scrap = dataReader.GetInt32(4), money = dataReader.GetInt32(5), medkit = dataReader.GetInt32(6), tire = dataReader.GetInt32(7), battery = dataReader.GetInt32(8), 
                ammo = dataReader.GetInt32(9), timeTaken = dataReader.GetInt32(10);

            // Score is base amount of supplies (medkit, tires, and batteries doubled) + a tenth of the distance + a time bonus determined below multiplied by a difficulty bonus
            // + friends alive (500 each).
            int finalScore = food + gas + scrap + money + medkit * 2 + tire * 2 + battery * 2 + ammo + distance/10 + 500 * friendsAlive;

            // A faster time means a higher score
            finalScore += 1000 - timeTaken > 0 ? 1000 - timeTaken : 0;
            finalScore *= difficulty % 2 == 0 ? 2 : 1;

            // Check for the number of high scores - creating a new score means id is one above the highest existing.
            dbCommandReadValues = dbConnection.CreateCommand();
            dbCommandReadValues.CommandText = "SELECT COUNT(*) FROM LocalHighscoreTable";
            int count = Convert.ToInt32(dbCommandReadValues.ExecuteScalar());

            IDbCommand dbCommandInsertValues = dbConnection.CreateCommand();
            dbCommandInsertValues.CommandText = "INSERT INTO LocalHighscoreTable (id, leaderName, difficulty, distance, friends, score) VALUES(" +
                                               (count + 1) + ", '" + leaderName + "', " + difficulty + ", " + distance + ", " + friendsAlive + ", " + 
                                               finalScore + ")";
            dbCommandInsertValues.ExecuteNonQuery();
            dbConnection.Close();

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
            GameLoop.FileId = -1;
            SceneManager.LoadScene(0);
        }
    }
}