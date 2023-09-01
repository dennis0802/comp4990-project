using System.Collections;
using System.Collections.Generic;
using System.Data;
using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Mono.Data.Sqlite;
using Database;
using UI;

namespace UI{
    [DisallowMultipleComponent]
    public class ScoreScreen : MonoBehaviour {
        [Header("Score Display Components")]
        [Tooltip("Score text 1 consisting of position and leader name")]
        [SerializeField]
        private TextMeshProUGUI scoreText1;

        [Tooltip("Score text 2 consisting of difficulty, distance, and score")]
        [SerializeField]
        private TextMeshProUGUI scoreText2;

        [Tooltip("Clear scoreboard button")]
        [SerializeField]
        private Button clearScoreButton;

        void OnEnable(){
            UpdateScreen();
        }

        /// <summary>
        /// Clear the scores in the database.
        /// </summary>
        public void ClearScore(){
            IDbConnection dbConnection = GameDatabase.CreateLocalHighScoreAndOpenDatabase();
            IDbCommand dbCommandDeleteValues = dbConnection.CreateCommand();
            dbCommandDeleteValues.CommandText = "DELETE FROM LocalHighscoreTable";
            dbCommandDeleteValues.ExecuteNonQuery();
            dbConnection.Close();

            UpdateScreen();
        }

        /// <summary>
        /// Update the score screen
        /// </summary>
        private void UpdateScreen(){
            IDbConnection dbConnection = GameDatabase.CreateLocalHighScoreAndOpenDatabase();
            IDbCommand dbCommandReadValues = dbConnection.CreateCommand();
            dbCommandReadValues.CommandText = "SELECT COUNT(*) FROM LocalHighscoreTable";
            int count = Convert.ToInt32(dbCommandReadValues.ExecuteScalar());

            // Display the top 8 scores.
            dbCommandReadValues.CommandText = "SELECT * FROM LocalHighscoreTable ORDER BY score DESC LIMIT 8";
            IDataReader dataReader = dbCommandReadValues.ExecuteReader();
            string scoreDisplay1 = "", scoreDisplay2 = "";
            int rowNum = 1;

            while(dataReader.Read()){
                int difficulty = dataReader.GetInt32(2);
                string difficultyText = difficulty == 1 ? "Standard" : difficulty == 2 ? "Deadlier" : difficulty == 3 ? "Standard(C)" : "Deadlier(C)";

                scoreDisplay1 += "\t" + rowNum++ + "\t\t" + dataReader.GetString(1) + "\n";
                scoreDisplay2 += dataReader.GetInt32(3) + "\t\t\t" + difficultyText + "\t\t" + dataReader.GetInt32(5) + "\n";
            }
            scoreText1.text = scoreDisplay1;
            scoreText2.text = scoreDisplay2;

            dbConnection.Close();

            // Only clear scores when there is a score in the database.
            clearScoreButton.interactable = count != 0;
        }
    }
}