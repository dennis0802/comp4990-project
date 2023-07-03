using System.Collections;
using System.Collections.Generic;
using System.Data;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Mono.Data.Sqlite;
using TMPro;
using Database;

namespace UI
{
    // Save files based on:
    // https://www.mongodb.com/developer/code-examples/csharp/saving-data-in-unity3d-using-sqlite/

    [DisallowMultipleComponent]
    public class MainMenu : MonoBehaviour {
        [Header("Screens")]
        [Tooltip("Game object containing screen for accessing a file")]
        [SerializeField]
        private GameObject accessScreen;

        [Tooltip("Game object containing screen for picking the gamemode")]
        [SerializeField]
        private GameObject gameModeScreen;

        [Header("File Access")]
        [SerializeField]
        private bool isCreatingNewFile;

        [Tooltip("Text objects for description")]
        [SerializeField]
        private TextMeshProUGUI[] fileDescriptors;

        [Tooltip("Buttons for disabling/enabling")]
        [SerializeField]
        private Button[] fileButtons, deletionButtons;

        [Tooltip("Title for accessing files")]
        [SerializeField]
        private TextMeshProUGUI fileAccessTitle;

        [Tooltip("Game object containing file UI")]
        [SerializeField]
        private GameObject fileAccessWindow;

        [Tooltip("Game object containing UI for replacing a save file")]
        [SerializeField]
        private GameObject fileReplaceWindow;

        [Tooltip("Game object containing UI for deleting a save file")]
        [SerializeField]
        private GameObject fileDeleteWindow;

        [Tooltip("Game object containing UI for beginning the game")]
        [SerializeField]
        private GameObject introWindow;

        // Instance of the main menu;
        private static MainMenu menuInstance;

        // To track which file is being marked for deletion/replacement
        private int targetFile = -1;
        // To track if a file exists
        private bool idFound = false;
        // To track file played - this will also be used to grab all player related content in the file
        public static int FileNumberPlaying;
        // To track if in a file
        public static bool FileBeingPlayed = false;

        public void Start(){
            DontDestroyOnLoad(this.gameObject);
            if(menuInstance == null){
                menuInstance = this;
            }
            else{
                Destroy(gameObject);
            }

            SetFileDesc();
        }

        /// <summary>
        /// Access save files
        /// </summary>
        /// <param name="mode">True for new file creation, false for loading
        public void AccessFiles(bool mode){
            isCreatingNewFile = mode ? true : false;
            fileAccessTitle.text = mode ? "Start New File" : "Load File";
            // Temp list to track which ids are used
            List<int> ids = new List<int>(){0,1,2,3};

            IDbConnection dbConnection = GameDatabase.CreateCustomAndOpenDatabase();
            IDbCommand dbCommandReadValues = dbConnection.CreateCommand();
            dbCommandReadValues.CommandText = "SELECT id FROM SaveFilesTable";
            IDataReader dataReader = dbCommandReadValues.ExecuteReader();

            // Disable access to files with no saved data if loading
            if(!isCreatingNewFile){
                // Remove ids with save data
                while(dataReader.Read()){
                    ids.Remove(dataReader.GetInt32(0));
                }

                // Disable access to file access and deletion of files that aren't used
                foreach(int id in ids){
                    fileButtons[id].interactable = false;
                    deletionButtons[id].interactable = false;
                }
            }
            // Enable access to files with no saved data if creating a new file
            else{
                while(dataReader.Read()){
                    ids.Remove(dataReader.GetInt32(0));
                    deletionButtons[dataReader.GetInt32(0)].interactable = true;
                }

                // Disable access to deletion but allow file access for unused files
                foreach(int id in ids){
                    fileButtons[id].interactable = true;
                    deletionButtons[id].interactable = false;
                }
            }
            dbConnection.Close();
        }

        /// <summary>
        /// Access a saved game
        /// </summary>
        /// <param name="id">The id of the save file specified in the editor</param>
        public void AccessGame(int id){
            idFound = false;
            IDbConnection dbConnection = GameDatabase.CreateCustomAndOpenDatabase();
            IDbCommand dbCommandReadValues = dbConnection.CreateCommand();
            dbCommandReadValues.CommandText = "SELECT * FROM SaveFilesTable";
            idFound = GameDatabase.MatchId(dbCommandReadValues, id);

            // Creating a new file
            if(isCreatingNewFile){
                // Confirm to overwrite or cancel
                if(idFound){
                    ConfirmFileReplace(id);
                    dbConnection.Close();
                    return;
                }
                deletionButtons[id].interactable = true;

                // Change screens (not in replacingFile function due to single possible action)
                accessScreen.SetActive(false);
                gameModeScreen.SetActive(true);
                targetFile = id;

                gameObject.GetComponent<GamemodeSelect>().RandomizeCharacter(true);
                gameObject.GetComponent<GamemodeSelect>().RandomizeCharacter(false);
            }
            // Loading a file
            else{
                // Open the game
                if(idFound){
                    FileNumberPlaying = id;
                    accessScreen.SetActive(false);
                    SceneManager.LoadScene(1);
                }
            }
            dbConnection.Close();
        }

        /// <summary> 
        /// Set the file descriptor of each save file
        /// </summary>
        private void SetFileDesc(){
            string diff = "";
            IDbConnection dbConnection = GameDatabase.CreateCustomAndOpenDatabase();
            IDbCommand dbCommandReadValues = dbConnection.CreateCommand();
            dbCommandReadValues.CommandText = "SELECT * FROM SaveFilesTable LEFT JOIN ActiveCharactersTable ON SaveFilesTable.charactersId = ActiveCharactersTable.id";
            IDataReader dataReader = dbCommandReadValues.ExecuteReader();

            while(dataReader.Read()){
                switch(dataReader.GetInt32(3)){
                    case 1:
                        diff = "Standard";
                        break;
                    case 2:
                        diff = "Deadlier";
                        break;
                    case 3:
                        diff = "Standard Custom";
                        break;
                    case 4:
                        diff = "Deadlier Custom";
                        break;
                }
                fileDescriptors[dataReader.GetInt32(0)].text = "  File " + (dataReader.GetInt32(0)+1) + "\n  " + dataReader.GetString(12) + 
                                                               "\n  " + dataReader.GetInt32(2) + "km\t " + dataReader.GetString(4) + "\n  " + diff;
            }

            dbConnection.Close();
        }

        /// <summary>
        /// Confirm that the user wants to replace a save file
        /// </summary>
        /// <param name="id">The id of the save file specified in the editor</param>
        public void ConfirmFileReplace(int id){
            fileAccessWindow.SetActive(false);
            fileReplaceWindow.SetActive(true);
            targetFile = id;
        }

        /// <summary>
        /// Replace the file
        /// </summary>
        public void ReplaceFile(){
            IDbConnection dbConnection = GameDatabase.CreateCustomAndOpenDatabase();
            IDbCommand dbCommandInsertValue = dbConnection.CreateCommand();
            dbCommandInsertValue.CommandText = "REPLACE INTO SaveFilesTable(id, charactersId) VALUES (" + targetFile + ", " + targetFile + ")";
            dbCommandInsertValue.ExecuteNonQuery();
            fileDescriptors[targetFile].text = "  File " + (targetFile+1) + "\n\n  No save file";
            deletionButtons[targetFile].interactable = true;

            gameObject.GetComponent<GamemodeSelect>().RandomizeCharacter(true);
            gameObject.GetComponent<GamemodeSelect>().RandomizeCharacter(false);

            dbConnection.Close();
        }

        /// <summary>
        /// Confirm that the user wants to delete a save file
        /// </summary>
        /// <param name="id">The id of the save file specified in the editor</param>
        public void ConfirmFileDeletion(int id){
            fileAccessWindow.SetActive(false);
            fileDeleteWindow.SetActive(true);
            targetFile = id;
        }

        /// <summary>
        /// Delete the file
        /// </summary>
        public void DeleteFile(){
            IDbConnection dbConnection = GameDatabase.CreateCustomAndOpenDatabase();
            IDbCommand dbCommandDeleteValue = dbConnection.CreateCommand();
            dbCommandDeleteValue.CommandText = "DELETE FROM SaveFilesTable WHERE id = " + targetFile + ";";
            dbCommandDeleteValue.ExecuteNonQuery();
            dbCommandDeleteValue.CommandText = "DELETE FROM ActiveCharactersTable WHERE id = " + targetFile + ";";
            dbCommandDeleteValue.ExecuteNonQuery();
            fileDescriptors[targetFile].text = "  File " + (targetFile+1) + "\n\n  No save file";
            deletionButtons[targetFile].interactable = false;
            dbConnection.Close();

            if(!isCreatingNewFile){
                fileButtons[targetFile].interactable = false;
            }
        }

        public void StartNewGame(){
            int startingFood = 100, startingGas = 50, startingScrap = 25, startingMoney = 30, startingMedkit = 1;

            if(GamemodeSelect.Difficulty == 2 || GamemodeSelect.Difficulty == 4){
                startingFood = 50; 
                startingGas = 25;
                startingScrap = 12; 
                startingMoney = 15; 
                startingMedkit = 0;
            }

            // Create table of active characters as a separate table but 
            IDbConnection dbConnection = GameDatabase.CreateActiveCharactersAndOpenDatabase();
            IDbCommand dbCommandInsertValue = dbConnection.CreateCommand();
            dbCommandInsertValue.CommandText = "INSERT OR REPLACE INTO ActiveCharactersTable(id, leaderName, leaderPerk, leaderTrait, leaderColor, leaderAcc, leaderHat, leaderOutfit, leaderMorale, leaderHealth, " +
                                               "friend1Name, friend1Perk, friend1Trait, friend1Color, friend1Acc, friend1Hat, friend1Outfit, friend1Morale, friend1Health) VALUES (" + 
                                                targetFile + ", '" + GamemodeSelect.LeaderName + "', " + GamemodeSelect.LeaderPerk + ", " + GamemodeSelect.LeaderTrait + ", " + GamemodeSelect.LeaderColor +
                                                ", " + GamemodeSelect.LeaderAcc + ", " + GamemodeSelect.LeaderHat + ", " + GamemodeSelect.LeaderOutfit + ", " + 75 + ", " + 100 + ", '" +
                                                GamemodeSelect.PartnerName + "', " + GamemodeSelect.PartnerPerk + ", " + GamemodeSelect.PartnerTrait + ", " + GamemodeSelect.PartnerColor + ", " +
                                                GamemodeSelect.PartnerAcc + ", " + GamemodeSelect.PartnerHat + ", " + GamemodeSelect.PartnerOutfit + ", " + 75 + ", " + 100 + ")";
            dbCommandInsertValue.ExecuteNonQuery();
            dbConnection.Close();

            dbConnection = GameDatabase.CreateCustomAndOpenDatabase();
            IDbCommand dbCommandUpdateValue = dbConnection.CreateCommand();
            dbCommandUpdateValue.CommandText = "INSERT OR REPLACE INTO SaveFilesTable(id, charactersId, distance, difficulty, location, inPhase, food, gas, scrap, money, medkit) VALUES (" + targetFile + ", " + targetFile + ", " + 0 +
                                                ", " + GamemodeSelect.Difficulty + ", 'Montreal', 0, " + startingFood + ", " + startingGas + ", " + startingScrap + ", " + 
                                                startingMoney + ", " + startingMedkit + ");";
            FileNumberPlaying = targetFile;
            dbCommandUpdateValue.ExecuteNonQuery();
            dbConnection.Close();

            accessScreen.SetActive(false);
            introWindow.SetActive(true);
            SceneManager.LoadScene(1);
        }

        /// <summary>
        /// Exit the game
        /// </summary>
        public void ExitGame(){
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                UnityEngine.Application.Quit();
            #endif
        }
    }
}

