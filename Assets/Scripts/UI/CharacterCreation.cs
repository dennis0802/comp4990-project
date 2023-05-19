using System.Collections;
using System.Collections.Generic;
using System.Data;
using UnityEngine;
using UnityEngine.UI;
using Mono.Data.Sqlite;
using TMPro;

namespace UI{
    public class CharacterCreation : MonoBehaviour
    {
        [Tooltip("Page text")]
        [SerializeField]
        private TextMeshProUGUI pageText;

        [Tooltip("Game objects containing each slot to display a character")]
        [SerializeField]
        private GameObject[] characterSlots;

        // To track page number
        private int pageNum = 1;
        // Lower and upper bound of records to display per page.
        private int lowerBound = 1, upperBound = 9;

        
        public void Start(){
            IDbConnection dbConnection = CreateCustomAndOpenDatabase();
            dbConnection.Close();
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
        /// Display characters in the character slots
        /// </summary>
        private void DisplayCharacter(){

        }

        private void CreateCharacter(){

        }

        private void DeleteCharacter(){

        }

        /// <summary>
        /// Display characters in the character slots
        /// </summary>
        /// <param name="forward">If the page number is incrementing or not</param>
        public void ChangePage(bool forward){
            if(forward){
                pageNum = pageNum == 5 ? 1 : pageNum + 1;
                lowerBound = pageNum == 5 ? 1: lowerBound + 9;
                upperBound = pageNum == 5 ? 9: upperBound + 9;
            }
            else{
                pageNum = pageNum == 1 ? 5 : pageNum - 1;
                lowerBound = pageNum == 1 ? 36 : lowerBound - 9;
                upperBound = pageNum == 1 ? 45 : upperBound - 9;
            }
            pageText.text = "Page " + pageNum + "/5";
        }
    }
}
