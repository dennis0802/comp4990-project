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

        void Start(){
            CalculateScore();
        }

        /// <summary>
        /// Calculate the overall score
        /// </summary>
        private void CalculateScore(){
            IDbConnection dbConnection = GameDatabase.CreateSavesAndOpenDatabase();
            IDbCommand dbCommandReadValues = dbConnection.CreateCommand();
            dbCommandReadValues.CommandText = "SELECT * FROM SaveFilesTable WHERE id = " + GameLoop.FileId + ";";
            IDataReader dataReader = dbCommandReadValues.ExecuteReader();
            dataReader.Read();

            int distance = dataReader.GetInt32(3), difficulty = dataReader.GetInt32(4), food = dataReader.GetInt32(7), gas = (int)(dataReader.GetFloat(8)),
                scrap = dataReader.GetInt32(9), money = dataReader.GetInt32(10), medkit = dataReader.GetInt32(11), tire = dataReader.GetInt32(12), battery = dataReader.GetInt32(13), 
                ammo = dataReader.GetInt32(14), timeTaken = dataReader.GetInt32(16);

            // Score is base amount of supplies (medkit, tires, and batteries doubled) + a tenth of the distance + a time bonus determined below multiplied by a difficulty bonus
            // + friends alive (500 each).
            int finalScore = food + gas + scrap + money + medkit * 2 + tire * 2 + battery * 2 + ammo + distance/10 + 500 * RestMenu.FriendsAlive;

            // A faster time means a higher score
            finalScore += 1000 - timeTaken > 0 ? 1000 - timeTaken : 0;
            finalScore *= difficulty % 2 == 0 ? 2 : 1;
            dbConnection.Close();

            // Check for the number of high scores - creating a new score means id is one above the highest existing.
            dbConnection = GameDatabase.CreateLocalHighScoreAndOpenDatabase();
            dbCommandReadValues = dbConnection.CreateCommand();
            dbCommandReadValues.CommandText = "SELECT COUNT(*) FROM LocalHighscoreTable";
            int count = Convert.ToInt32(dbCommandReadValues.ExecuteScalar());

            IDbCommand dbCommandInsertValues = dbConnection.CreateCommand();
            dbCommandInsertValues.CommandText = "INSERT INTO LocalHighscoreTable (id, leaderName, difficulty, distance, friends, score) VALUES(" +
                                               (count + 1) + ", '" + RestMenu.LeaderName + "', " + difficulty + ", " + distance + ", " + RestMenu.FriendsAlive + ", " + 
                                               finalScore + ")";
            dbCommandInsertValues.ExecuteNonQuery();
            dbConnection.Close();

            scoreDetailText.text = "Food: " + food + "\tGas: " + gas + "\tScrap: " + scrap + "\tMoney: " + money + "\tAmmo: " + ammo +
                                   "\nMedkit: " + medkit + "x2" + "\tBattery: " + battery + "x2" + "\tTire: " + tire + "x2\n" + "Distance: " + distance + " / 10\t" +
                                   "Time Taken: " + (1000 - timeTaken) + "\tFriends Alive: 500 * " + RestMenu.FriendsAlive;
            totalScoreText.text = "Final Score: " + finalScore.ToString();
            mainMenu.DeleteFile();
        }

        /// <summary>
        /// Confirm that score was viewed.
        /// </summary>
        public void ConfirmView(){
            SceneManager.LoadScene(0);
        }

        // Use for score window
        /// <summary>
        /// Display scores in the database
        /// </summary>
        /*private void DisplayScores(){
            IDbConnection dbConnection = GameDatabase.CreateLocalHighScoreAndOpenDatabase();
            IDbCommand dbCommandReadValues = dbConnection.CreateCommand();
            dbCommandReadValues.CommandText = "SELECT * FROM LocalHighscoreTable ORDER BY score LIMIT 10";
            IDataReader dataReader = dbCommandReadValues.ExecuteReader();
            string scoreDisplay = "";
            
            while(dataReader.Read()){
                int difficulty = dataReader.GetInt32(2);
                string difficultyText = difficulty == 1 ? "Standard" : difficulty == 2 ? "Deadlier" : difficulty == 3 ? "Standard Custom" : "Deadlier Custom";

                scoreDisplay += dataReader.GetInt32(0) + "\t" + dataReader.GetString(1) + "\t" + difficultyText + "\t" + dataReader.GetInt32(3) + "\t" + "\t" + 
                                dataReader.GetInt32(4) + "\t" + dataReader.GetInt32(5) + "\n";
            }
            scoreText.text = scoreDisplay;
        }*/
    }
}