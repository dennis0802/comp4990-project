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
            dbConnection = CreateActiveCharactersAndOpenDatabase();
            dbConnection.Close();
            DontDestroyOnLoad(this.gameObject);
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
            dbCommandCreateTable.CommandText = "CREATE TABLE IF NOT EXISTS SaveFilesTable(id INTEGER PRIMARY KEY, charactersId INTEGER, distance INTEGER, difficulty INTEGER, " +
                                               "location TEXT, FOREIGN KEY(charactersId) REFERENCES ActiveCharactersTable (id))";
            dbCommandCreateTable.ExecuteReader();

            return dbConnection;
        }

        /// <summary>
        /// Create and open a connection to the database to access active players
        /// </summary>
        public static IDbConnection CreateActiveCharactersAndOpenDatabase(){
            // Open connection to database
            string dbUri = "URI=file:GameData.sqlite";
            IDbConnection dbConnection = new SqliteConnection(dbUri);
            dbConnection.Open();

            // Create a table for the save files in the databases if it doesn't exist yet
            IDbCommand dbCommandCreateTable = dbConnection.CreateCommand();
            dbCommandCreateTable.CommandText = "CREATE TABLE IF NOT EXISTS ActiveCharactersTable(id INTEGER PRIMARY KEY, " +
                                               "leaderName TEXT(10), leaderPerk INTEGER, leaderTrait INTEGER, leaderAcc INTEGER, leaderOutfit INTEGER, leaderColor INTEGER, leaderHat INTEGER, leaderMorale INTEGER, leaderHealth INTEGER, " +
                                               "friend1Name TEXT(10), friend1Perk INTEGER, friend1Trait INTEGER, friend1Acc INTEGER, friend1Outfit INTEGER, friend1Color INTEGER, friend1Hat INTEGER, friend1Morale INTEGER, friend1Health INTEGER," +
                                               "friend2Name TEXT(10), friend2Perk INTEGER, friend2Trait INTEGER, friend2Acc INTEGER, friend2Outfit INTEGER, friend2Color INTEGER, friend2Hat INTEGER, friend2Morale INTEGER, friend2Health INTEGER," +
                                               "friend3Name TEXT(10), friend3Perk INTEGER, friend3Trait INTEGER, friend3Acc INTEGER, friend3Outfit INTEGER, friend3Color INTEGER, friend3Hat INTEGER, friend3Morale INTEGER, friend3Health INTEGER)";
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
