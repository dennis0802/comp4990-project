using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mono.Data.Sqlite;
using System.Data;

namespace Database{
    [DisallowMultipleComponent]
    public class GameDatabase : MonoBehaviour
    {

        // Start is called before the first frame update
        void Start()
        {
            IDbConnection dbConnection = CreateSavesAndOpenDatabase();
            dbConnection.Close();
            dbConnection = CreateCustomAndOpenDatabase();
            dbConnection.Close();
        }

        /// <summary>
        /// Create and open a connection to the database to access custom characters
        /// </summary>
        public static IDbConnection CreateCustomAndOpenDatabase(){
            // Open connection to database
            string dbUri = "URI=file:GameData.sqlite";
            IDbConnection dbConnection = new SqliteConnection(dbUri);
            dbConnection.Open();

            // Create a table for the save files in the databases if it doesn't exist yet
            IDbCommand dbCommandCreateTable = dbConnection.CreateCommand();
            dbCommandCreateTable.CommandText = "CREATE TABLE IF NOT EXISTS CustomCharactersTable(id INTEGER PRIMARY KEY, name TEXT(10), perk INTEGER, trait INTEGER, " +
                                               "accessory INTEGER, hat INTEGER, color INTEGER, outfit INTEGER)";
            dbCommandCreateTable.ExecuteReader();

            return dbConnection;
        }

        /// <summary>
        /// Create and open a connection to the database to access save files
        /// </summary>
        public static IDbConnection CreateSavesAndOpenDatabase(){
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
        /// Search the database to match by an id
        /// </summary>
        /// <param name="readCommand">Database command to read from the table</param>
        /// <param name="id">Id to search for</param>
        /// <returns>True if found, false otherwise.</returns>
        public static bool MatchId(IDbCommand readCommand, int id){
            IDataReader dataReader = readCommand.ExecuteReader();

            // Search for the id (ids go 0-44)
            while(dataReader.Read()){
                if(dataReader.GetInt32(0) == id){
                    return true;
                }
            }
            return false;
        }
    }

}
