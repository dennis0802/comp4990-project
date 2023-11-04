using System.Collections;
using System.Collections.Generic;
using System.Data;
using System;
using System.Linq;
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
            DataUser.dataManager.DeleteScores();
            UpdateScreen();
        }

        /// <summary>
        /// Update the score screen
        /// </summary>
        private void UpdateScreen(){
            IEnumerable<LocalHighscore> scores = DataUser.dataManager.GetScores();
            int count = scores.Count();
            scores = scores.Where<LocalHighscore>(s=>s.Difficulty == displayMode);
            scores = scores.OrderByDescending(s => s.FinalScore);

            string scoreDisplay1 = "", scoreDisplay2 = "";
            int rowNum = 1;
            
            // Limit to 8 scores
            foreach(LocalHighscore score in scores){
                if(rowNum == 9){
                    break;
                }

                int diff = score.Difficulty;
                string diffText = diff == 1 ? "Standard" : diff == 2 ? "Deadlier" : diff == 3 ? "Standard(C)" : "Deadlier(C)";
                scoreDisplay1 += "\t" + rowNum++ + "\t\t" + score.LeaderName + "\n";
                scoreDisplay2 += score.Distance + "\t\t\t" + diffText + "\t\t" + score.FinalScore + "\n";
            }
            scoreText1.text = scoreDisplay1;
            scoreText2.text = scoreDisplay2;
            modeText.text = "Displaying scores for mode: " + displayDiffs[displayMode];

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