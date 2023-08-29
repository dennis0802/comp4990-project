using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mono.Data.Sqlite;
using System.Data;

namespace Database{
    [DisallowMultipleComponent]
    public class GameDatabase : MonoBehaviour
    {
        // Instance of the database
        private static GameDatabase databaseInstance;

        // Start is called before the first frame update
        void Start()
        {
            DontDestroyOnLoad(this.gameObject);
            if(databaseInstance == null){
                databaseInstance = this;
            }
            else{
                Destroy(gameObject);
            }

            IDbConnection dbConnection = CreateSavesAndOpenDatabase();
            dbConnection.Close();
            dbConnection = CreateCustomAndOpenDatabase();
            dbConnection.Close();
            dbConnection = CreateActiveCharactersAndOpenDatabase();
            dbConnection.Close();
            dbConnection = CreateLocalHighScoreAndOpenDatabase();
            dbConnection.Close();
            dbConnection = CreateCarsAndOpenDatabase();
            dbConnection.Close();
            dbConnection = CreateTownAndOpenDatabase();
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
            // Fields: id (character id), name, perk, trait, and physical attributes.
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
            // Fields: id (file id), character id (character table for this file), car id (table for this file)distance travelled, difficulty played, current location, 
            // inPhase tracks if resting (0), travelling (1), or in combat (2)
            IDbCommand dbCommandCreateTable = dbConnection.CreateCommand();
            dbCommandCreateTable.CommandText = "CREATE TABLE IF NOT EXISTS SaveFilesTable(id INTEGER PRIMARY KEY, charactersId INTEGER, carId INTEGER, distance INTEGER, difficulty INTEGER, " +
                                               "location TEXT, inPhase INTEGER, food INTEGER, gas REAL, scrap INTEGER, money INTEGER, medkit INTEGER, tire INTEGER, battery INTEGER, " + 
                                               "ammo INTEGER, time INTEGER, overallTime INTEGER, rations INTEGER, speed INTEGER, FOREIGN KEY(charactersId) REFERENCES ActiveCharactersTable (id), " +
                                               "FOREIGN KEY(carId) REFERENCES CarsTable(id))";
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
            // Fields: id (character table for this file), leader's perk, leader's trait, leader's physical physical attributes, morale, and health.
            //         Repeats for friends 1-3.
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
        /// Create and open a connection to the database to access active cars
        /// </summary>
        public static IDbConnection CreateCarsAndOpenDatabase(){
            // Open connection to database
            string dbUri = "URI=file:GameData.sqlite";
            IDbConnection dbConnection = new SqliteConnection(dbUri);
            dbConnection.Open();

            // Create a table for the cars in the database if it doesn't exist yet
            // Fields: id (car table for this file), wheel upgrade, battery upgrade, engine upgrade, tool upgrade, misc 1, misc 2.
            IDbCommand dbCommandCreateTable = dbConnection.CreateCommand();
            dbCommandCreateTable.CommandText = "CREATE TABLE IF NOT EXISTS CarsTable(id INTEGER PRIMARY KEY, carHp INTEGER, wheelUpgrade INTEGER, batteryUpgrade INTEGER, engineUpgrade INTEGER, " +
                                               "toolUpgrade INTEGER, miscUpgrade1 INTEGER, miscUpgrade2 INTEGER)";
            dbCommandCreateTable.ExecuteReader();

            return dbConnection;
        }

        public static IDbConnection CreateTownAndOpenDatabase(){
            // Open connection to database
            string dbUri = "URI=file:GameData.sqlite";
            IDbConnection dbConnection = new SqliteConnection(dbUri);
            dbConnection.Open();

            // Create a table for town data (shop prices [selling to towns will be the ceiling 40% of the buy price])
            IDbCommand dbCommandCreateTable = dbConnection.CreateCommand();
            dbCommandCreateTable.CommandText = "CREATE TABLE IF NOT EXISTS TownTable(id INTEGER PRIMARY KEY, foodPrice INTEGER, gasPrice INTEGER, scrapPrice INTEGER, " + 
                                               "medkitPrice INTEGER, tirePrice INTEGER, batteryPrice INTEGER, ammoPrice INTEGER, foodStock INTEGER, gasStock INTEGER, " +
                                               "scrapStock INTEGER, medkitStock INTEGER, tireStock INTEGER, batteryStock INTEGER, ammoStock INTEGER, side1Reward INTEGER, side1Qty INTEGER, " +
                                               "side1Diff INTEGER, side1Type INTEGER, side2Reward INTEGER, side2Qty INTEGER, side2Diff INTEGER, side2Type INTEGER, side3Reward INTEGER, " +
                                               "side3Qty INTEGER, side3Diff INTEGER, side3Type INTEGER, curTown INTEGER)";
            dbCommandCreateTable.ExecuteReader();

            return dbConnection;
        }
        
        /// <summary>
        /// Create and open a connection to the database to access local highscores
        /// </summary>
        public static IDbConnection CreateLocalHighScoreAndOpenDatabase(){
            // Open connection to database
            string dbUri = "URI=file:GameData.sqlite";
            IDbConnection dbConnection = new SqliteConnection(dbUri);
            dbConnection.Open();

            // Create a table for local high scores
            // Fields: id, the leader's name, the difficulty played, the distance travelled, the number of friends they survived with, and overall score.
            IDbCommand dbCommandCreateTable = dbConnection.CreateCommand();
            dbCommandCreateTable.CommandText = "CREATE TABLE IF NOT EXISTS LocalHighscoreTable(id INTEGER PRIMARY KEY, leaderName TEXT(10), difficulty INTEGER, distance INTEGER, friends INTEGER, score INTEGER)";
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
