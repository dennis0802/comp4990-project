using System.Collections;
using System.Collections.Generic;
using System.Data;
using UnityEngine;
using UnityEngine.UI;
using Mono.Data.Sqlite;
using TMPro;

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

        // To track which file is being marked for deletion/replacement
        private int targetFile = -1;
        // Timer for splash
        private float timer = 0;
        // To track if a file exists
        private bool idFound = false;

        public void Start(){
            DontDestroyOnLoad(this.gameObject);
            IDbConnection dbConnection = CreateSavesAndOpenDatabase();
            dbConnection.Close();
            dbConnection = CreateCustomAndOpenDatabase();
            dbConnection.Close();
            SetFileDesc();
        }

        public void Update(){
            timer += Time.deltaTime;
            if(timer >= 5.0f){
                
            }
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

            IDbConnection dbConnection = CreateSavesAndOpenDatabase();
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
            IDbConnection dbConnection = CreateSavesAndOpenDatabase();

            // Creating a new file
            if(isCreatingNewFile){
                // Check if the file already exists
                IDbCommand dbCommandReadValues = dbConnection.CreateCommand();
                dbCommandReadValues.CommandText = "SELECT * FROM SaveFilesTable";
                IDataReader dataReader = dbCommandReadValues.ExecuteReader();

                // Search for the id (ids go from 0-3)
                while(dataReader.Read()){
                    if(dataReader.GetInt32(0) == id){
                        idFound = true;
                        break;
                    }
                }

                // Confirm to overwrite or cancel
                if(idFound){
                    ConfirmFileReplace(id);
                    dbConnection.Close();
                    return;
                }

                // Create file
                IDbCommand dbCommandInsertValue = dbConnection.CreateCommand();
                dbCommandInsertValue.CommandText = "INSERT INTO SaveFilesTable(id, inProgress) VALUES (" + id + ", 1)";
                dbCommandInsertValue.ExecuteNonQuery();
                fileDescriptors[id].text = "  File " + (id+1) + "\n  name here\n  distance here\t loc here\n  difficulty here";
                deletionButtons[id].interactable = true;

                // Change screens (not in replacingFile function due to single possible action)
                accessScreen.SetActive(false);
                gameModeScreen.SetActive(true);
                targetFile = id;
            }
            // Loading a file
            else{
                IDbCommand dbCommandReadValues = dbConnection.CreateCommand();
                dbCommandReadValues.CommandText = "SELECT * FROM SaveFilesTable";
                IDataReader dataReader = dbCommandReadValues.ExecuteReader();

                // Search for the id (ids go from 0-3)
                while(dataReader.Read()){
                    if(dataReader.GetInt32(0) == id){
                        idFound = true;
                        break;
                    }
                }

                // Open the game
                if(idFound){
                    Debug.Log("Game should be loaded");
                }
            }
            dbConnection.Close();
        }

        /// <summary> 
        /// Set the file descriptor of each save file
        /// </summary>
        private void SetFileDesc(){
            IDbConnection dbConnection = CreateSavesAndOpenDatabase();
            IDbCommand dbCommandReadValues = dbConnection.CreateCommand();
            dbCommandReadValues.CommandText = "SELECT * FROM SaveFilesTable";
            IDataReader dataReader = dbCommandReadValues.ExecuteReader();

            while(dataReader.Read()){
                fileDescriptors[dataReader.GetInt32(0)].text = "  File " + (dataReader.GetInt32(0)+1) + "\n  name here\n  distance here\t loc here\n  difficulty here";
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
            IDbConnection dbConnection = CreateSavesAndOpenDatabase();
            IDbCommand dbCommandInsertValue = dbConnection.CreateCommand();
            dbCommandInsertValue.CommandText = "REPLACE INTO SaveFilesTable(id, inProgress) VALUES (" + targetFile + ", 1)";
            dbCommandInsertValue.ExecuteNonQuery();
            fileDescriptors[targetFile].text = "  File " + (targetFile+1) + "\n  name here\n  distance here\t loc here\n  difficulty here";
            deletionButtons[targetFile].interactable = true;

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
            IDbConnection dbConnection = CreateSavesAndOpenDatabase();
            IDbCommand dbCommandInsertValue = dbConnection.CreateCommand();
            dbCommandInsertValue.CommandText = "DELETE FROM SaveFilesTable WHERE id = " + targetFile + ";";
            dbCommandInsertValue.ExecuteNonQuery();
            fileDescriptors[targetFile].text = "  File " + (targetFile+1) + "\n\n  No save file";
            deletionButtons[targetFile].interactable = false;
            dbConnection.Close();

            if(!isCreatingNewFile){
                fileButtons[targetFile].interactable = false;
            }
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

        /// <summary>
        /// Create and open a connection to the database to access save files
        /// </summary>
        private IDbConnection CreateSavesAndOpenDatabase(){
            // Open connection to database
            string dbUri = "URI=file:GameData.sqlite";
            IDbConnection dbConnection = new SqliteConnection(dbUri);
            dbConnection.Open();

            // Create a table for the save files in the databases if it doesn't exist yet
            IDbCommand dbCommandCreateTable = dbConnection.CreateCommand();
            dbCommandCreateTable.CommandText = "CREATE TABLE IF NOT EXISTS SaveFilesTable(id INTEGER PRIMARY KEY, inProgress INTEGER)";
            dbCommandCreateTable.ExecuteReader();

            return dbConnection;
        }

        /// <summary>
        /// Create and open a connection to the database to access custom characters
        /// </summary>
        private IDbConnection CreateCustomAndOpenDatabase(){
            // Open connection to database
            string dbUri = "URI=file:GameData.sqlite";
            IDbConnection dbConnection = new SqliteConnection(dbUri);
            dbConnection.Open();

            // Create a table for the save files in the databases if it doesn't exist yet
            IDbCommand dbCommandCreateTable = dbConnection.CreateCommand();
            dbCommandCreateTable.CommandText = "CREATE TABLE IF NOT EXISTS CustomCharactersTable(id INTEGER PRIMARY KEY)";
            dbCommandCreateTable.ExecuteReader();

            return dbConnection;
        }
    }
}

