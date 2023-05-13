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

        /// <summary>
        /// Start a new game
        /// </summary>
        /// <param name="id">The id of the save file specified in the editor</param>
        public void StartNewGame(int id){
            // Create the file if it doesn't exist (game over by dying or making it to the end will auto delete)
        }

        /// <summary>
        /// Load a saved game
        /// </summary>
        /// <param name="id">The id of the save file specified in the editor</param>
        public void LoadGame(){
            // Ignore the input if the file doesn't exist
        }

        public void TestFunction(){
            hitCount++;
            IDbConnection dbConnection = CreateAndOpenDatabase();
            IDbCommand dbCommandInsertValue = dbConnection.CreateCommand();
            dbCommandInsertValue.CommandText = "INSERT OR REPLACE INTO SaveFilesTable (id, hits) VALUES (0, " + hitCount + ")";
            dbCommandInsertValue.ExecuteNonQuery();

            dbConnection.Close();
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
        /// Create and open a connection to the database
        /// </summary>
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

