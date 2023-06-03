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

        [Tooltip("Game objects containing each slot to display a character")]
        [SerializeField]
        private TextMeshProUGUI perkDescText;

        [Tooltip("Game objects containing each slot to display a character")]
        [SerializeField]
        private TextMeshProUGUI traitDescText;

        [Tooltip("Error for blank name")]
        [SerializeField]
        private GameObject errorText;

        [Tooltip("Character component number text objects")]
        [SerializeField]
        private TextMeshProUGUI colorNumText, accNumText, hatNumText, outfitNumText;

        [Header("Player Components")]
        [Tooltip("Player body visuals")]
        [SerializeField]
        private GameObject[] playerVisuals;

        [Tooltip("Colors for players")]
        [SerializeField]
        private Material[] playerColors;

        [Tooltip("Player hats")]
        [SerializeField]
        private GameObject[] playerHats;

        [Tooltip("Player accessories")]
        [SerializeField]
        private GameObject[] playerAccs;

        [Tooltip("Player outfits")]
        [SerializeField]
        private GameObject[] playerOutfits;

        // To track page number
        private int pageNum = 1;
        // To track character customization features
        private int colorNum = 1, accNum = 1, outfitNum = 1, hatNum = 1;
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
                perkList.value = dataReader.GetInt32(2);
                traitList.value = dataReader.GetInt32(3);
                accNum = dataReader.GetInt32(4);
                hatNum = dataReader.GetInt32(5);
                colorNum = dataReader.GetInt32(6);
                outfitNum = dataReader.GetInt32(7);
            }
            // Otherwise set to defaults
            else{
                nameField.text = "";
                colorNum = 1;
                accNum = 1;
                outfitNum = 1;
                hatNum = 1;
                ChangeSampleDisplay(1);
                ChangeSampleDisplay(2);
                ChangeSampleDisplay(3);
                perkList.value = 0;
                traitList.value = 0;
            }

            ChangeCharacterInfo();
            dbConnection.Close();

            UpdateButtonText();
        }

        /// <summary>
        /// Save a custom character
        /// </summary>
        private void SaveCharacter(){
            IDbConnection dbConnection = GameDatabase.CreateCustomAndOpenDatabase();
            IDbCommand dbCommandInsertValue = dbConnection.CreateCommand();
            dbCommandInsertValue.CommandText = "INSERT OR REPLACE INTO CustomCharactersTable(id, name, perk, trait, accessory, hat, color, outfit) VALUES (" 
                                                + viewedCharacter + ", '" + nameField.text + "', " + perkList.value + ", " + traitList.value + ", "
                                                + accNum + ", " + hatNum + ", " + colorNum + ", " + outfitNum + ")";
            dbCommandInsertValue.ExecuteNonQuery();

            int baseId = viewedCharacter - (pageNum - 1) * 9;
            characterDescText[baseId].text = "          Name: " + nameField.text + "\n          Perk: " + perkList.captionText.text 
                                            + "\n          Trait: " + traitList.captionText.text + "\n";  
            viewedCharacter = -1;
            dbConnection.Close();
        }

        /// <summary>
        /// Validate character name before saving
        /// </summary>
        public void ValidateCharacter(){
            // If empty or whitespace, show an error (name must exist)
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
        /// Change page number of characters page
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
                    characterDescText[baseId].text = "          Name: " + dataReader.GetString(1) + "\n          Perk: " + perkList.options[dataReader.GetInt32(2)].text
                                                     + "\n          Trait: " + traitList.options[dataReader.GetInt32(3)].text + "\n";                    
                }
                // Generic text
                else{
                    characterDescText[baseId].text = "          Create new character";
                }
                idFound = false;
                dbConnection.Close();
            }
        }

        /// <summary>
        /// Change text of character info (color, outfit, perk, trait, hat, accessory)
        /// </summary>
        public void ChangeCharacterInfo(){
            // Perk
            switch(perkList.value){
                case 0:
                    perkDescText.text = "Fixing the car will be easier.";
                    break;
                case 1:
                    perkDescText.text = "Shots will be more likely to pierce.";
                    break;
                case 2:
                    perkDescText.text = "Will come with additional medical supplies.";
                    break;
                case 3:
                    perkDescText.text = "Healing other members will be easier.";
                    break;
                case 4:
                    perkDescText.text = "Will think through situations logcially.";
                    break;
                case 5:
                    perkDescText.text = "Increasing the group's morale will be easier.";
                    break;
            }  

            // Trait
            switch(traitList.value){
                case 0: // Charming
                    traitDescText.text = "Smooth-talk traders on the road for better deals.";
                    break;
                case 1: // Paranoid
                    traitDescText.text = "More wary of the world and strangers around them.";
                    break;
                case 2: // Civilized
                    traitDescText.text = "Will look for peaceful solutions to problems.";  
                    break;
                case 3: // Bandit
                    traitDescText.text = "Will not feel guilty about destructive choices."; 
                    break;
                case 4: // Hot Headed
                    traitDescText.text = "Stronger, but will get into more arguments.";   
                    break;
                case 5: // Creative
                    traitDescText.text = "Will solve problems in a 'creative' manner.";     
                    break;
            }

            // Color, accessory, outfit, and hat number
            colorNumText.text = colorNum.ToString();
            accNumText.text = accNum.ToString();
            outfitNumText.text = outfitNum.ToString();
            hatNumText.text = hatNum.ToString();
            ChangeSampleDisplay(1);
            ChangeSampleDisplay(2);
            ChangeSampleDisplay(3);

            // Visuals
            foreach(GameObject component in playerVisuals){
                component.GetComponent<MeshRenderer>().material = playerColors[colorNum-1];
            }      
        }

        /// <summary>
        /// Change color number of character
        /// </summary>
        /// <param name="forward">If the color number is incrementing or not</param>
        public void ChangeColor(bool forward){
            if(forward){
                colorNum = colorNum == 9 ? 1 : colorNum + 1;
            }
            else{
                colorNum = colorNum == 1 ? 9 : colorNum - 1;
            }

            foreach(GameObject component in playerVisuals){
                component.GetComponent<MeshRenderer>().material = playerColors[colorNum-1];
            }
            colorNumText.text = colorNum.ToString();
        }

        /// <summary>
        /// Change acessory number of character
        /// </summary>
        /// <param name="forward">If the accessory number is incrementing or not</param>
        public void ChangeAccessory(bool forward){
            if(forward){
                accNum = accNum == 3 ? 1 : accNum + 1;
            }
            else{
                accNum = accNum == 1 ? 3 : accNum - 1;
            }

            ChangeSampleDisplay(1);
            accNumText.text = accNum.ToString();
        }

        /// <summary>
        /// Change hat number of character
        /// </summary>
        /// <param name="forward">If the hat number is incrementing or not</param>
        public void ChangeHat(bool forward){
            if(forward){
                hatNum = hatNum == 3 ? 1 : hatNum + 1;
            }
            else{
                hatNum = hatNum == 1 ? 3 : hatNum - 1;
            }

            ChangeSampleDisplay(2);
            hatNumText.text = hatNum.ToString();
        }

        /// <summary>
        /// Change outfit number of character
        /// </summary>
        /// <param name="forward">If the outfit number is incrementing or not</param>
        public void ChangeOutfit(bool forward){
            if(forward){
                outfitNum = outfitNum == 3 ? 1 : outfitNum + 1;
            }
            else{
                outfitNum = outfitNum == 1 ? 3 : outfitNum - 1;
            }

            ChangeSampleDisplay(3);
            outfitNumText.text = outfitNum.ToString();
        }

        /// <summary>
        /// Change what the sample character displays depending on player choice.
        /// </summary>
        /// <param name="mode">1 for accessory, 2 for hat, 3 for outfit</param>
        private void ChangeSampleDisplay(int mode){
            switch(mode){
                case 3:
                    switch(outfitNum){
                        case 1:
                            playerOutfits[0].SetActive(false);
                            playerOutfits[1].SetActive(false);
                            break;
                        case 2:
                            playerOutfits[0].SetActive(true);
                            playerOutfits[1].SetActive(false);
                            break;
                        case 3:
                            playerOutfits[0].SetActive(false);
                            playerOutfits[1].SetActive(true);
                            break;
                    }
                    break;

                case 2:
                    switch(hatNum){
                        case 1:
                            playerHats[0].SetActive(false);
                            playerHats[1].SetActive(false);
                            break;
                        case 2:
                            playerHats[0].SetActive(true);
                            playerHats[1].SetActive(false);
                            break;
                        case 3:
                            playerHats[0].SetActive(false);
                            playerHats[1].SetActive(true);
                            break;
                    }
                    break;

                case 1:
                    switch(accNum){
                        case 1:
                            playerAccs[0].SetActive(false);
                            playerAccs[1].SetActive(false);
                            break;
                        case 2:
                            playerAccs[0].SetActive(true);
                            playerAccs[1].SetActive(false);
                            break;
                        case 3:
                            playerAccs[0].SetActive(false);
                            playerAccs[1].SetActive(true);
                            break;
                    }
                    break;
            }
        }
    }
}
