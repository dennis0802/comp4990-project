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

        [Tooltip("Text to display current score display")]
        [SerializeField]
        private TextMeshProUGUI modeText;

        [Tooltip("Clear scoreboard button")]
        [SerializeField]
        private Button clearScoreButton;

        /// <summary>
        /// Current mode of display (0 = all, 1 = standard, 2 = deadlier, 3 = standard custom, 4 = deadlier custom)
        /// </summary>
        private int displayMode = 0;

        /// <summary>
        /// Strings of difficulties to display
        /// </summary>
        private List<string> displayDiffs = new List<string>(){"All", "Standard", "Deadlier", "Standard (C)", "Deadlier (C)"};

        void OnEnable(){
            UpdateScreen();
        }

        /// <summary>
        /// Clear the scores in the database.
        /// </summary>
        public void ClearScore(){
            IDbConnection dbConnection = GameDatabase.OpenDatabase();
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
            IDbConnection dbConnection = GameDatabase.OpenDatabase();
            IDbCommand dbCommandReadValues = dbConnection.CreateCommand();
            dbCommandReadValues.CommandText = "SELECT COUNT(*) FROM LocalHighscoreTable";
            int count = Convert.ToInt32(dbCommandReadValues.ExecuteScalar());
            string commandText = "SELECT leaderName, difficulty, distance, score FROM LocalHighscoreTable";
            commandText += displayMode != 0 ? " WHERE difficulty = @Param1 ORDER BY score DESC LIMIT 8" : " ORDER BY score DESC LIMIT 8";

            // Display the top 8 scores of the current display.
            dbCommandReadValues.CommandText = commandText;
            QueryParameter<int> queryParameter = new QueryParameter<int>("@Param1", displayMode);
            queryParameter.SetParameter(dbCommandReadValues);

            IDataReader dataReader = dbCommandReadValues.ExecuteReader();
            string scoreDisplay1 = "", scoreDisplay2 = "";
            int rowNum = 1;

            while(dataReader.Read()){
                int difficulty = dataReader.GetInt32(1);
                string difficultyText = difficulty == 1 ? "Standard" : difficulty == 2 ? "Deadlier" : difficulty == 3 ? "Standard(C)" : "Deadlier(C)";

                scoreDisplay1 += "\t" + rowNum++ + "\t\t" + dataReader.GetString(0) + "\n";
                scoreDisplay2 += dataReader.GetInt32(2) + "\t\t\t" + difficultyText + "\t\t" + dataReader.GetInt32(3) + "\n";
            }
            scoreText1.text = scoreDisplay1;
            scoreText2.text = scoreDisplay2;
            modeText.text = "Displaying scores for mode: " + displayDiffs[displayMode];

            dbConnection.Close();

            // Only clear scores when there is a score in the database.
            clearScoreButton.interactable = count != 0;
        }

        /// <summary>
        /// Change the display mode of the scores
        /// </summary>
        /// <param name="mode">The mode to change to</param>
        public void ChangeDisplayMode(int mode){
            displayMode = displayMode == 4 ? 0 : displayMode + 1;
            UpdateScreen();
        }
    }
}