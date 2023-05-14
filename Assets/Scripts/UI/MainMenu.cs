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
        [Header("File Access")]
        [SerializeField]
        private bool isCreatingNewFile;

        [Tooltip("Text objects for description")]
        [SerializeField]
        private TextMeshProUGUI[] fileDescriptors;

        [Tooltip("Buttons for disabling/enabling")]
        [SerializeField]
        private Button[] fileButtons;

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

        private int check, targetFile = -1;
        private bool idFound = false;

        public void Start(){
            // Read values from table
            IDbConnection dbConnection = CreateSavesAndOpenDatabase();
            dbConnection.Close();
            dbConnection = CreateCustomAndOpenDatabase();
            dbConnection.Close();
            SetFileUI();
        }

        /// <summary>
        /// Load a saved game
        /// </summary>
        /// <param name="id">The id of the save file specified in the editor</param>
        public void LoadGame(int id){
            idFound = false;
            IDbConnection dbConnection = CreateSavesAndOpenDatabase();

            // Creating a new file
            if(isCreatingNewFile){
                // Check if the file already exists
                IDbCommand dbCommandReadValues = dbConnection.CreateCommand();
                dbCommandReadValues.CommandText = "SELECT * FROM SaveFilesTable";
                IDataReader dataReader = dbCommandReadValues.ExecuteReader();

                while(dataReader.Read()){
                    if(dataReader.GetInt32(0) == id){
                        check = dataReader.GetInt32(1);
                        idFound = true;
                        break;
                    }
                }

                if(idFound){
                    Debug.Log("Ask the player if they want to overwrite or cancel.");
                    ConfirmFileReplace(id);
                    dbConnection.Close();
                    return;
                }

                IDbCommand dbCommandInsertValue = dbConnection.CreateCommand();
                dbCommandInsertValue.CommandText = "INSERT INTO SaveFilesTable(id, inProgress) VALUES (" + id + ", 1)";
                dbCommandInsertValue.ExecuteNonQuery();
                fileDescriptors[id].text = "  File " + (id+1) + "\n  name here\n  distance here\t loc here\n  difficulty here";
                Debug.Log("File Created. Game should start");
            }
            else{
                IDbCommand dbCommandReadValues = dbConnection.CreateCommand();
                dbCommandReadValues.CommandText = "SELECT * FROM SaveFilesTable";
                IDataReader dataReader = dbCommandReadValues.ExecuteReader();

                // Search for the id (ids go from 0-3)
                while(dataReader.Read()){
                    if(dataReader.GetInt32(0) == id){
                        check = dataReader.GetInt32(1);
                        idFound = true;
                    }
                }

                if(idFound){
                    Debug.Log("Game should be loaded");
                }
            }
            dbConnection.Close();
        }

        /// <summary>
        /// Confirm that the user wants to replace a save file
        /// </summary>
        /// <param name="id">The id of the save file specified in the editor</param>
        private void ConfirmFileReplace(int id){
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
            Debug.Log("File Created. Game should start");
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
            // SQLite code for deleting a record
        }

        /// <summary>
        /// Access save files
        /// </summary>
        /// <param name="mode">True for new file creation, false for loading
        public void AccessFiles(bool mode){
            isCreatingNewFile = mode ? true : false;
            fileAccessTitle.text = mode ? "Start New File" : "Load File";
            List<int> ids = new List<int>(){0,1,2,3};

            // Disable access to files with no saved data if loading
            if(!isCreatingNewFile){
                IDbConnection dbConnection = CreateSavesAndOpenDatabase();
                IDbCommand dbCommandReadValues = dbConnection.CreateCommand();
                dbCommandReadValues.CommandText = "SELECT id FROM SaveFilesTable";
                IDataReader dataReader = dbCommandReadValues.ExecuteReader();

                while(dataReader.Read()){
                    ids.Remove(dataReader.GetInt32(0));
                }
                dbConnection.Close();

                foreach(int id in ids){
                    fileButtons[id].interactable = false;
                }
            }
            else{
                foreach(int id in ids){
                    fileButtons[id].interactable = true;
                }
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

        /// <summary> 
        /// Set the file descriptor of each save file
        /// </summary>
        private void SetFileUI(){
            IDbConnection dbConnection = CreateSavesAndOpenDatabase();
            IDbCommand dbCommandReadValues = dbConnection.CreateCommand();
            dbCommandReadValues.CommandText = "SELECT * FROM SaveFilesTable";
            IDataReader dataReader = dbCommandReadValues.ExecuteReader();

            while(dataReader.Read()){
                fileDescriptors[dataReader.GetInt32(0)].text = "  File " + (dataReader.GetInt32(0)+1) + "\n  name here\n  distance here\t loc here\n  difficulty here";
            }

            dbConnection.Close();
        }
    }
}

