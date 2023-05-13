using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Data;
using Mono.Data.Sqlite;
using TMPro;

namespace UI
{
    // Save files based on:
    // https://www.mongodb.com/developer/code-examples/csharp/saving-data-in-unity3d-using-sqlite/

    [DisallowMultipleComponent]
    public class MainMenu : MonoBehaviour {
        [SerializeField]
        private bool isCreatingNewFile;

        [Tooltip("Text objects for description")]
        [SerializeField]
        private TextMeshProUGUI[] fileDescriptors;

        [Tooltip("Title for accessing files")]
        [SerializeField]
        private TextMeshProUGUI fileAccessTitle;

        private int check;
        private bool idFound = false;

        public void Start(){
            // Read values from table
            IDbConnection dbConnection = CreateSavesAndOpenDatabase();
            dbConnection.Close();
            dbConnection = CreateCustomAndOpenDatabase();
            dbConnection.Close();
            SetFileText();
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
                    return;
                }

                IDbCommand dbCommandInsertValue = dbConnection.CreateCommand();
                dbCommandInsertValue.CommandText = "INSERT OR REPLACE INTO SaveFilesTable(id, inProgress) VALUES (" + id + ", 1)";
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
                        break;
                    }
                }

                if(idFound){
                    Debug.Log("Game should be loaded");
                }
                else{
                    Debug.Log("Disable the button during runtime or make an error sound");
                }
            }
            dbConnection.Close();
        }

        /// <summary>
        /// Access save files
        /// </summary>
        /// <param name="mode">True for new file creation, false for loading
        public void AccessFiles(bool mode){
            isCreatingNewFile = mode ? true : false;
            fileAccessTitle.text = mode ? "Start New File" : "Load File";
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
        private void SetFileText(){
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

