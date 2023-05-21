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

        [Header("Character Components")]
        [Tooltip("Game objects containing each slot to display a character")]
        [SerializeField]
        private TextMeshProUGUI[] characterDescText;

        // To track page number
        private int pageNum = 1;
        // Lower and upper bound of records to display per page.
        private int lowerBound = 0, upperBound = 8;
        // To track currently viewed character
        private int viewedCharacter = -1;
        // To track if id was found
        private bool idFound;

        public void Start(){
            IDbConnection dbConnection = CreateCustomAndOpenDatabase();
            dbConnection.Close();
            UpdateButtonText();
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

        /// <summary>
        /// Access characters in the database for customizing
        /// </summary>
        /// <param name="baseId">Base id number for the button (ie. button 1, button 2...) to determine which character id in the database</param>
        public void AccessCharacter(int baseId){
            // Determine character to access
            int accessId = (pageNum - 1) * 9 + baseId - 1;
            viewedCharacter = accessId;
            if(accessId < 0 || accessId >= 45){
                return;
            }

            IDbConnection dbConnection = CreateCustomAndOpenDatabase();

            // Database commands to search for character id
            idFound = false;
            IDbCommand dbCommandReadValues = dbConnection.CreateCommand();
            dbCommandReadValues.CommandText = "SELECT * FROM CustomCharactersTable";
            IDataReader dataReader = dbCommandReadValues.ExecuteReader();

            // Search for the id (ids go 1-45)
            while(dataReader.Read()){
                if(dataReader.GetInt32(0) == accessId){
                    idFound = true;
                    break;
                }
            }

            // If id wasn't found, create character
            if(idFound){
                dbCommandReadValues = dbConnection.CreateCommand();
                dbCommandReadValues.CommandText = "SELECT * FROM CustomCharactersTable WHERE id = " + accessId + ";";
                dataReader = dbCommandReadValues.ExecuteReader();
            }
            // Otherwise, access character
            else{
                IDbCommand dbCommandInsertValue = dbConnection.CreateCommand();
                dbCommandInsertValue.CommandText = "INSERT INTO CustomCharactersTable(id) VALUES (" + accessId + ")";
                dbCommandInsertValue.ExecuteNonQuery();
            }
            dbConnection.Close();

            UpdateButtonText();
        }

        /// <summary>
        /// Delete a custom character
        /// </summary>
        private void DeleteCharacter(){
            viewedCharacter = -1;
        }

        /// <summary>
        /// Reset which character was being viewed
        /// </summary>
        public void ResetCharacterView(){
            viewedCharacter = -1;
        }

        /// <summary>
        /// Display characters in the character slots
        /// </summary>
        /// <param name="forward">If the page number is incrementing or not</param>
        public void ChangePage(bool forward){
            if(forward){
                lowerBound = pageNum == 5 ? 0: lowerBound + 9;
                upperBound = pageNum == 5 ? 8: upperBound + 9;
                pageNum = pageNum == 5 ? 1 : pageNum + 1;

            }
            else{
                lowerBound = pageNum == 1 ? 35 : lowerBound - 9;
                upperBound = pageNum == 1 ? 44 : upperBound - 9;
                pageNum = pageNum == 1 ? 5 : pageNum - 1;
            }

            // Change text
            pageText.text = "Page " + pageNum + "/5";
            UpdateButtonText();
        }

        /// <summary>
        /// Update the text on the character button
        /// </summary>
        private void UpdateButtonText(){
            int baseId;
            for(int i = lowerBound; i <= upperBound; i++){
                baseId = i - (pageNum - 1) * 9;
                IDbConnection dbConnection = CreateCustomAndOpenDatabase();
                IDbCommand dbCommandReadValues = dbConnection.CreateCommand();
                dbCommandReadValues.CommandText = "SELECT * FROM CustomCharactersTable";
                IDataReader dataReader = dbCommandReadValues.ExecuteReader();

                // Search for the id (ids go 0-44)
                while(dataReader.Read()){
                    if(dataReader.GetInt32(0) == i){
                        idFound = true;
                        break;
                    }
                }
                // Populate with relevant info
                if(idFound){
                    characterDescText[baseId].text = "          Name:\n          Perk:\n          Trait:\n";                    
                }
                // Generic text
                else{
                    characterDescText[baseId].text = "          Create new character";
                }
                idFound = false;
                dbConnection.Close();
            }
        }
    }
}
