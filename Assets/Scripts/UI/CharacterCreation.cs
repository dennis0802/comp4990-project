using System.Collections;
using System.Collections.Generic;
using System.Data;
using UnityEngine;
using UnityEngine.UI;
using Mono.Data.Sqlite;
using TMPro;
using Database;

namespace UI{
    [DisallowMultipleComponent]
    public class CharacterCreation : MonoBehaviour
    {
        [Tooltip("Page text")]
        [SerializeField]
        private TextMeshProUGUI pageText;

        [Header("Character Components")]
        [Tooltip("Game objects containing each slot to display a character")]
        [SerializeField]
        private TextMeshProUGUI[] characterDescText;

        [Header("Screens")]
        [Tooltip("Character creation screen")]
        [SerializeField]
        private GameObject creationScreen;

        [Tooltip("Character creation menu")]
        [SerializeField]
        private GameObject creationMenu;

        [Header("User Input")]
        [Tooltip("Input field for character name")]
        [SerializeField]
        private TMP_InputField nameField;

        [Tooltip("Dropdown field for perks")]
        [SerializeField]
        private TMP_Dropdown perkList;

        [Tooltip("Dropdown field for traits")]
        [SerializeField]
        private TMP_Dropdown traitList;

        [Tooltip("Error for blank name")]
        [SerializeField]
        private GameObject errorText;

        // To track page number
        private int pageNum = 1;
        // Lower and upper bound of records to display per page.
        private int lowerBound = 0, upperBound = 8;
        // To track currently viewed character
        private int viewedCharacter = -1;
        // To track if id was found
        private bool idFound;

        public void Start(){
            UpdateButtonText();
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

            IDbConnection dbConnection = GameDatabase.CreateCustomAndOpenDatabase();

            // Database commands to search for character id
            idFound = false;
            IDbCommand dbCommandReadValues = dbConnection.CreateCommand();
            dbCommandReadValues.CommandText = "SELECT * FROM CustomCharactersTable";
            IDataReader dataReader = dbCommandReadValues.ExecuteReader();

            // Search for the id (ids go 0-44)
            while(dataReader.Read()){
                if(dataReader.GetInt32(0) == accessId){
                    idFound = true;
                    break;
                }
            }

            // If id found, access character info
            if(idFound){
                dbCommandReadValues = dbConnection.CreateCommand();
                dbCommandReadValues.CommandText = "SELECT * FROM CustomCharactersTable WHERE id = " + accessId + ";";
                dataReader = dbCommandReadValues.ExecuteReader();
                dataReader.Read();
                nameField.text = dataReader.GetString(1);
            }
            else{
                nameField.text = "";
            }
            dbConnection.Close();

            UpdateButtonText();
        }

        /// <summary>
        /// Save a custom character
        /// </summary>
        private void SaveCharacter(){
            IDbConnection dbConnection = GameDatabase.CreateCustomAndOpenDatabase();
            IDbCommand dbCommandInsertValue = dbConnection.CreateCommand();
            dbCommandInsertValue.CommandText = "INSERT OR REPLACE INTO CustomCharactersTable(id, name, perk, trait) VALUES (" 
                                                + viewedCharacter + ", '" + nameField.text + "', '" + perkList.captionText.text + "', '" + traitList.captionText.text + "')";
            dbCommandInsertValue.ExecuteNonQuery();

            int baseId = viewedCharacter - (pageNum - 1) * 9;
            characterDescText[baseId].text = "          Name: " + nameField.text + "\n          Perk: " + perkList.captionText.text 
                                            + "\n          Trait: " + traitList.captionText.text + "\n";  
            viewedCharacter = -1;
            dbConnection.Close();
        }

        /// <summary>
        /// Validate character name, perk, and trait before allowing character to be saved
        /// </summary>
        public void ValidateCharacter(){
            if(string.IsNullOrWhiteSpace(nameField.text)){
                errorText.SetActive(true);
                return;
            }

            errorText.SetActive(false);
            creationMenu.SetActive(true);
            creationScreen.SetActive(false);
            SaveCharacter();
        }

        /// <summary>
        /// Delete a custom character
        /// </summary>
        public void DeleteCharacter(){
            IDbConnection dbConnection = GameDatabase.CreateCustomAndOpenDatabase();
            IDbCommand dbCommandInsertValue = dbConnection.CreateCommand();
            dbCommandInsertValue.CommandText = "DELETE FROM CustomCharactersTable WHERE id = " + viewedCharacter + ";";
            dbCommandInsertValue.ExecuteNonQuery();
            
            int baseId = viewedCharacter - (pageNum - 1) * 9;
            characterDescText[baseId].text = "          Create new character";
            viewedCharacter = -1;
            dbConnection.Close();
        }

        /// <summary>
        /// Reset which character was being viewed
        /// </summary>
        public void ResetCharacterView(){
            viewedCharacter = -1;
            errorText.SetActive(false);
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
                lowerBound = pageNum == 1 ? 36 : lowerBound - 9;
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
            bool idFound = false;
            for(int i = lowerBound; i <= upperBound; i++){
                baseId = i - (pageNum - 1) * 9;
                IDbConnection dbConnection = GameDatabase.CreateCustomAndOpenDatabase();
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
                    characterDescText[baseId].text = "          Name: " + dataReader.GetString(1) + "\n          Perk: " + dataReader.GetString(2) 
                                                     + "\n          Trait: " + dataReader.GetString(3) + "\n";                    
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
