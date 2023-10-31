using System.Text.RegularExpressions;
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

        [Tooltip("Player menu icon")]
        [SerializeField]
        private GameObject[] playerIcons;

        [Header("Assigning Character Components")]
        [Tooltip("Assiging player menu icon")]
        [SerializeField]
        private GameObject[] assignIcons;

        [Tooltip("Page text")]
        [SerializeField]
        private TextMeshProUGUI assignPageText;

        [Tooltip("Game objects containing each slot to display a character")]
        [SerializeField]
        private TextMeshProUGUI[] assignCharacterDescText;

        [Tooltip("Game objects containing buttons to assign characters")]
        [SerializeField]
        private Button[] assignButtons;

        // Static list of all colors
        public static Material[] Colors;

        // Static list of all colors
        public static Material[] OutfitPatterns;

        // To track page number
        public static int pageNum = 1;
        // To track character customization features
        private int colorNum = 1, accNum = 1, outfitNum = 1, hatNum = 1;
        // To track character customization features for the icon
        private int iconColorNum = 1, iconAccNum = 1, iconOutfitNum = 1, iconHatNum = 1;
        // Lower and upper bound of records to display per page.
        private int lowerBound = 0, upperBound = 8;
        // To track currently viewed character
        private int viewedCharacter = -1;
        // To track if id was found
        private bool idFound;

        // List of perks (mechanic, sharpshooter, health care, surgeon, programmer, musician)
        private List<string> perkDescs = new List<string>(){
            "Fixing the car will be easier.", "Shots will be more likely to pierce.", 
            "Will come with additional medical supplies.","Healing other members will be easier.", "Will think through situations logcially.", 
            "Increasing the group's morale will be easier."
        };

        // List of traits (charming, paranoid, optimist, bandit, hot headed, creative)
        private List<string> traitDescs = new List<string>(){
            "Smooth-talk traders on the road for better deals.", "More wary of the world and strangers around them.", "Starts with higher morale.",  
            "Will not feel guilty about destructive choices.", "Stronger, but will get into more arguments.", "Will solve problems in a 'creative' manner."  
        };

        public void Start(){
            Colors = playerColors;
            UpdateButtonsText();
        }

        public void UpdateButtonsText(){
            if(!GamemodeSelect.AssigningChar){
                UpdateButtonText(playerIcons, characterDescText);
            }
            else{
                UpdateButtonText(assignIcons, assignCharacterDescText);
            }
        }

        /// <summary>
        /// Access characters in the database for customizing
        /// </summary>
        /// <param name="baseId">Base id number for the button (ie. button 1, button 2...) to determine which character id in the database</param>
        public void AccessCharacter(int baseId){
            // Determine character to access
            int accessId = ReadCharacterId(baseId);
            if(accessId == -1){
                return;
            }
            viewedCharacter = accessId;

            IDbConnection dbConnection = GameDatabase.OpenDatabase();

            // Database commands to search for character id
            idFound = false;
            IDbCommand dbCommandReadValues = dbConnection.CreateCommand();
            dbCommandReadValues.CommandText = "SELECT id FROM CustomCharactersTable";
            IDataReader dataReader = dbCommandReadValues.ExecuteReader();

            // Search for the id (ids go 0-44)
            while(dataReader.Read()){
                if(dataReader.GetInt32(0) == viewedCharacter){
                    idFound = true;
                    break;
                }
            }

            // If id found, access character info
            if(idFound){
                dbCommandReadValues = dbConnection.CreateCommand();
                dbCommandReadValues.CommandText = "SELECT name, perk, trait, accessory, hat, color, outfit FROM CustomCharactersTable WHERE id = @id;";
                QueryParameter<int> queryParameter = new QueryParameter<int>("@id", accessId);
                queryParameter.SetParameter(dbCommandReadValues);
                dataReader = dbCommandReadValues.ExecuteReader();
                dataReader.Read();
                nameField.text = dataReader.GetString(0);
                perkList.value = dataReader.GetInt32(1);
                traitList.value = dataReader.GetInt32(2);
                accNum = dataReader.GetInt32(3);
                hatNum = dataReader.GetInt32(4);
                colorNum = dataReader.GetInt32(5);
                outfitNum = dataReader.GetInt32(6);
            }
            // Otherwise set to defaults
            else{
                nameField.text = "";
                colorNum = 1;
                accNum = 1;
                outfitNum = 1;
                hatNum = 1;
                perkList.value = 0;
                traitList.value = 0;
            }

            ChangeCharacterInfo();
            ChangeSampleDisplay(1);
            ChangeSampleDisplay(2);
            ChangeSampleDisplay(3);
            dbConnection.Close();

            UpdateButtonsText();
        }

        /// <summary>
        /// Get the access id of a character, given the base button id.
        /// </summary>
        /// <param name="baseId">Base id number for the button (ie. button 1, button 2...) to determine which character id in the database</param>
        public static int ReadCharacterId(int baseId){
            int id = (pageNum - 1) * 9 + baseId - 1;
            return id < 0 || id >= 45 ? -1 : id;
        }

        /// <summary>
        /// Save a custom character
        /// </summary>
        private void SaveCharacter(){
            IDbConnection dbConnection = GameDatabase.OpenDatabase();
            IDbCommand dbCommandInsertValue = dbConnection.CreateCommand();
            dbCommandInsertValue.CommandText = "INSERT OR REPLACE INTO CustomCharactersTable(id, name, perk, trait, accessory, hat, color, outfit) VALUES (" + 
                                                "@id, @name, @perk, @trait, @acc, @hat, @color, @outfit);";
            QueryParameter<string> stringParameter = new QueryParameter<string>("@name", nameField.text);
            stringParameter.SetParameter(dbCommandInsertValue);

            List<int> intParameters = new List<int>(){viewedCharacter, perkList.value, traitList.value, accNum, hatNum, colorNum, outfitNum};
            List<string> intParameterNames = new List<string>(){"@id", "@perk", "@trait", "@acc", "@hat", "@color", "@outfit"};
            for(int i = 0; i < intParameters.Count; i++){
                QueryParameter<int> intParameter = new QueryParameter<int>(intParameterNames[i], intParameters[i]);
                intParameter.SetParameter(dbCommandInsertValue);
            }
            dbCommandInsertValue.ExecuteNonQuery();

            int baseId = viewedCharacter - (pageNum - 1) * 9;
            characterDescText[baseId].text = "          Name: " + nameField.text + "\n          Perk: " + perkList.captionText.text 
                                            + "\n          Trait: " + traitList.captionText.text + "\n";  
            viewedCharacter = -1;
            dbConnection.Close();
            UpdateButtonsText();
        }

        /// <summary>
        /// Validate character name before saving
        /// </summary>
        public void ValidateCharacter(){
            // If empty or whitespace, show an error (name must exist)
            if(string.IsNullOrWhiteSpace(nameField.text)){
                errorText.SetActive(true);
                errorText.GetComponent<TextMeshProUGUI>().text = "Name cannot be empty.";
                return;
            }

            // 'Sanitize' the input by restricting entry values (any characters from A-Za-z, 0-9, and spaces 1-10 times)
            string pattern = @"^[0-9A-Za-z ]{1,10}$";
            Match m = Regex.Match(nameField.text, pattern, RegexOptions.IgnoreCase);
            if(!m.Success){
                errorText.SetActive(true);
                errorText.GetComponent<TextMeshProUGUI>().text = "Name can only contain letters, numbers, and spaces.";
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
            IDbConnection dbConnection = GameDatabase.OpenDatabase();
            IDbCommand dbCommandInsertValue = dbConnection.CreateCommand();
            dbCommandInsertValue.CommandText = "DELETE FROM CustomCharactersTable WHERE id = @id;";
            QueryParameter<int> queryParameter = new QueryParameter<int>("@id", viewedCharacter);
            queryParameter.SetParameter(dbCommandInsertValue);

            dbCommandInsertValue.ExecuteNonQuery();
            
            int baseId = viewedCharacter - (pageNum - 1) * 9;
            characterDescText[baseId].text = "          Create new character";
            viewedCharacter = -1;
            dbConnection.Close();
            UpdateButtonsText();
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
            if(GamemodeSelect.AssigningChar){
                assignPageText.text = "Page " + pageNum + "/5";
            }
            else{
                pageText.text = "Page " + pageNum + "/5";
            }
            UpdateButtonsText();
        }

        /// <summary>
        /// Update the text on the character button
        /// </summary>
        private void UpdateButtonText(GameObject[] icons, TextMeshProUGUI[] descText){
            int baseId;
            bool idFound = false;
            for(int i = lowerBound; i <= upperBound; i++){
                baseId = i - (pageNum - 1) * 9;
                IDbConnection dbConnection = GameDatabase.OpenDatabase();
                IDbCommand dbCommandReadValues = dbConnection.CreateCommand();
                dbCommandReadValues.CommandText = "SELECT id, name, perk, trait, accessory, hat, color, outfit FROM CustomCharactersTable";
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
                    icons[baseId].SetActive(true);
                    descText[baseId].gameObject.SetActive(true);
                    assignButtons[baseId].interactable = true;
                    descText[baseId].text = "          Name: " + dataReader.GetString(1) + "\n          Perk: " + perkList.options[dataReader.GetInt32(2)].text
                                                     + "\n          Trait: " + traitList.options[dataReader.GetInt32(3)].text + "\n";
                    iconAccNum = dataReader.GetInt32(4);
                    iconHatNum = dataReader.GetInt32(5);
                    iconColorNum = dataReader.GetInt32(6);
                    iconOutfitNum = dataReader.GetInt32(7);                  
                }
                else if(GamemodeSelect.AssigningChar){
                    icons[baseId].SetActive(false);
                    descText[baseId].gameObject.SetActive(false);
                    assignButtons[baseId].interactable = false;
                }
                // Generic text
                else{
                    icons[baseId].SetActive(true);
                    assignButtons[baseId].interactable = true;
                    iconAccNum = 1;
                    iconHatNum = 1;
                    iconColorNum = 1;
                    iconOutfitNum = 1;   
                    descText[baseId].text = "          Create new character";
                }
                ChangeMenuIcon(baseId); 
                idFound = false;
                dbConnection.Close();
            }
        }

        /// <summary>
        /// Change text of character info (color, outfit, perk, trait, hat, accessory)
        /// </summary>
        public void ChangeCharacterInfo(){
            perkDescText.text = perkDescs[perkList.value];
            traitDescText.text = traitDescs[traitList.value];

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
                component.GetComponent<MeshRenderer>().material = Colors[colorNum-1];
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
                component.GetComponent<MeshRenderer>().material = Colors[colorNum-1];
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
    
        /// <summary>
        /// Change the appearance of the character on the button
        /// </summary>
        /// <param name="baseId">Base id of the button to change the player icon</param>
        private void ChangeMenuIcon(int baseId){
            GameObject iconComp;
            
            iconComp = GamemodeSelect.AssigningChar ? assignIcons[baseId] : playerIcons[baseId];
            // Color
            iconComp.transform.GetChild(0).transform.GetChild(0).GetComponent<MeshRenderer>().material = Colors[iconColorNum-1];
            iconComp.transform.GetChild(0).transform.GetChild(1).GetComponent<MeshRenderer>().material = Colors[iconColorNum-1];

            switch(iconHatNum){
                case 1:
                    iconComp.transform.GetChild(3).transform.GetChild(0).gameObject.SetActive(false);
                    iconComp.transform.GetChild(3).transform.GetChild(1).gameObject.SetActive(false);
                    break;
                case 2:
                    iconComp.transform.GetChild(3).transform.GetChild(0).gameObject.SetActive(true);
                    iconComp.transform.GetChild(3).transform.GetChild(1).gameObject.SetActive(false);
                    break;
                case 3:
                    iconComp.transform.GetChild(3).transform.GetChild(0).gameObject.SetActive(false);
                    iconComp.transform.GetChild(3).transform.GetChild(1).gameObject.SetActive(true);
                    break;
            }

            switch(iconOutfitNum){
                case 1:
                    iconComp.transform.GetChild(1).transform.GetChild(0).gameObject.SetActive(false);
                    iconComp.transform.GetChild(1).transform.GetChild(1).gameObject.SetActive(false);
                    break;
                case 2:
                    iconComp.transform.GetChild(1).transform.GetChild(0).gameObject.SetActive(true);
                    iconComp.transform.GetChild(1).transform.GetChild(1).gameObject.SetActive(false);
                    break;
                case 3:
                    iconComp.transform.GetChild(1).transform.GetChild(0).gameObject.SetActive(false);
                    iconComp.transform.GetChild(1).transform.GetChild(1).gameObject.SetActive(true);
                    break;
            }

            switch(iconAccNum){
                case 1:
                    iconComp.transform.GetChild(2).transform.GetChild(0).gameObject.SetActive(false);
                    iconComp.transform.GetChild(2).transform.GetChild(1).gameObject.SetActive(false);
                    break;
                case 2:
                    iconComp.transform.GetChild(2).transform.GetChild(0).gameObject.SetActive(true);
                    iconComp.transform.GetChild(2).transform.GetChild(1).gameObject.SetActive(false);
                    break;
                case 3:
                    iconComp.transform.GetChild(2).transform.GetChild(0).gameObject.SetActive(false);
                    iconComp.transform.GetChild(2).transform.GetChild(1).gameObject.SetActive(true);
                    break;
            }
        }
    

    }
}
