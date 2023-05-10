using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Data;
using Mono.Data.Sqlite;

namespace UI
{
    // Save files based on:
    // https://www.mongodb.com/developer/code-examples/csharp/saving-data-in-unity3d-using-sqlite/

    [DisallowMultipleComponent]
    public class MainMenu : MonoBehaviour {
        [SerializeField]
        [Tooltip("The sound that is played when a button is clicked.")]
        private AudioSource buttonClick;
        [SerializeField]
        private int hitCount = 0;

        public void Start(){
            // Read values from table
            IDbConnection dbConnection = CreateAndOpenDatabase();
            IDbCommand dbCommandReadValues = dbConnection.CreateCommand();
            dbCommandReadValues.CommandText = "SELECT * FROM SaveFilesTable";
            IDataReader dataReader = dbCommandReadValues.ExecuteReader();

            while(dataReader.Read()){
                hitCount = dataReader.GetInt32(1);
            }

            dbConnection.Close();
        }

        public void StartNewGame(){

        }

        public void LoadGame(){

        }

        public void ExitGame(){
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                UnityEngine.Application.Quit();
            #endif
        }

        private IDbConnection CreateAndOpenDatabase(){
            // Open connection to database
            string dbUri = "URI=file:GameData.sqlite";
            IDbConnection dbConnection = new SqliteConnection(dbUri);
            dbConnection.Open();

            // Create a table for the save files in the databases if it doesn't exist yet
            IDbCommand dbCommandCreateTable = dbConnection.CreateCommand();
            dbCommandCreateTable.CommandText = "CREATE TABLE IF NOT EXISTS SaveFilesTable(id INTEGER PRIMARY KEY, hits INTEGER)";
            dbCommandCreateTable.ExecuteReader();

            return dbConnection;
        }
    }
}

