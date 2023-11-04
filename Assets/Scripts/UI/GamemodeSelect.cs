using System.Collections;
using System.Collections.Generic;
using System.Data;
using UnityEngine;
using UnityEngine.UI;
using Mono.Data.Sqlite;
using TMPro;
using Database;
using System.Linq;

namespace UI{
    [DisallowMultipleComponent]
    public class GamemodeSelect : MonoBehaviour {

        [Header("Gamemode Text")]
        [Tooltip("Gamemode title")]
        [SerializeField]
        private TextMeshProUGUI gamemodeTitle;
        
        [Tooltip("Gamemode title")]
        [SerializeField]
        private TextMeshProUGUI gamemodeDesc;

        [Tooltip("Gamemode selected text")]
        [SerializeField]
        private TextMeshProUGUI modeSelectedText;

        [Header("Character Preview")]
        [Tooltip("Leader model")]
        [SerializeField]
        private GameObject leaderModel;

        [Tooltip("Leader model")]
        [SerializeField]
        private GameObject partnerModel;

        [Header("UI Elements")]
        [Tooltip("Button to toggle partner selection")]
        [SerializeField]        
        private Button togglePartnerButton;

        [Tooltip("Button to select partner")]
        [SerializeField]
        private Button selectPartnerButton;

        [Tooltip("Button to randomize partner")]
        [SerializeField]        
        private Button randomizePartnerButton;

        [Tooltip("Text object describing partner")]
        [SerializeField]
        private TextMeshProUGUI partnerDesc;

        [Tooltip("Text object describing leader")]
        [SerializeField]
        private TextMeshProUGUI leaderDesc;

        [Tooltip("Text object on toggle button")]
        [SerializeField]
        private TextMeshProUGUI togglePartnerButtonText;

        // To track selected difficulties
        private int selectedMode = 1;

        // To track traits for the database
        public static string LeaderName, PartnerName;
        public static int LeaderPerk, LeaderTrait, LeaderColor, LeaderOutfit, LeaderAcc, LeaderHat, Difficulty = 1;
        public static int PartnerPerk, PartnerTrait, PartnerColor, PartnerOutfit, PartnerAcc, PartnerHat;
        public static int[] CustomIDs = new int[]{-1,-1};

        // For randomizing a character
        public static List<string> RandomNames = new List<string>(){
            "Grayson","Iliana","Delaney","Ibrahim","Jaylin","Jazmine","Emilio","Sheldon","Brady","Jeffery","Izaiah","Juliette","Aidyn","Matias","Ryker","Saniya","Karen",
            "Luke","Sonia","Dakota","Catalina","Maci","Aurora","Ronin","Skye","Jasiah","Taylor","Johnathon","Monserrat","Keyon","Desmond","Jaylen","Brandon","Riley",
            "Emmy","Macey","Ramiro","Andreas","Yazmin","Adam","Jovany","Liliana","Leonel","Roselyn","Zain","Paige","Karissa","Dane","Emery","Aidan", "Annie", "Britta",
            "Shirley", "Pierce", "Troy", "Abed"
        };

        // Perk list
        public static List<string> Perks = new List<string>(){"Mechanic", "Sharpshooter", "Health Care", "Surgeon", "Programmer", "Musician"};
        
        // Trait list
        public static List<string> Traits = new List<string>(){"Charming", "Paranoid", "Optimist", "Bandit", "Hot Headed", "Creative"};

        // Mode list
        private List<string> modes = new List<string>(){"Standard", "Deadlier", "Standard Custom", "Deadlier Custom"};

        // Mode descriptions
        private List<string> modeDescs = new List<string>(){
            "Standard enemies, randomized characters, decent amount of supplies", "Deadlier enemies, randomized characters, scarce amount of supplies",
            "Standard enemies, custom characters, decent amount of supplies", "Deadlier enemies, custom characters, scarce amount of supplies"
        };

        // To track if character selection is for assigning or accessing
        public static bool AssigningChar = false, assigningPartner = false;

        /// <summary>
        /// Select a game mode
        /// </summary>
        public void SelectMode(int key){
            if(key <= 0 || key >= 5){
                return;
            }
            gamemodeTitle.text = modes[key-1];
            gamemodeDesc.text = modeDescs[key-1];
            selectedMode = key;
        }

        /// <summary>
        /// Confirm selected gamemode.
        /// </summary>
        public void ConfirmMode(){
            Vector3 pos = modeSelectedText.transform.localPosition;
            switch(selectedMode){
                case 1:
                    modeSelectedText.transform.localPosition = new Vector3(pos.x, 40f, pos.z);
                    break;
                case 2:
                    modeSelectedText.transform.localPosition = new Vector3(pos.x, -20f, pos.z);
                    break;
                case 3:
                    modeSelectedText.transform.localPosition = new Vector3(pos.x, -80f, pos.z);
                    break;
                case 4:
                    modeSelectedText.transform.localPosition = new Vector3(pos.x, -140f, pos.z);
                    break;
                default:
                    return;
            }
            Difficulty = selectedMode;
        }

        /// <summary>
        /// Toggle how characters will be selected - assign to a file or to access their database data
        /// </summary>
        public void ToggleCharAssignment(){
            AssigningChar = !AssigningChar;
        }

        /// <summary>
        /// Toggle if partner is being selected
        /// </summary>
        public void SelectingPartner(){
            assigningPartner = !assigningPartner;
        }

        /// <summary>
        /// Reset if partner is being selected
        /// </summary>
        public void ResetPartnerSelect(){
            assigningPartner = false;
        }

        /// <summary>
        /// Toggle if a partner will be used at the start
        /// </summary>
        public void TogglePartner(){
            selectPartnerButton.interactable = !selectPartnerButton.interactable;
            randomizePartnerButton.interactable = selectPartnerButton.interactable;
            partnerModel.gameObject.SetActive(selectPartnerButton.interactable);

            if(selectPartnerButton.interactable){
                RandomizeCharacter(true);
                togglePartnerButtonText.text = "X";
            }
            else{
                partnerDesc.text = "No partner";
                togglePartnerButtonText.text = "O";
            }
        }

        /// <summary>
        /// Reset the screen if player cancels new file creation
        /// </summary>
        public void ResetScreen(){
            selectPartnerButton.interactable = true;
            randomizePartnerButton.interactable = selectPartnerButton.interactable;
            partnerModel.gameObject.SetActive(selectPartnerButton.interactable);
            RandomizeCharacter(true);
            RandomizeCharacter(false);
            togglePartnerButtonText.text = "X";
        }

        /// <summary>
        /// Randomize character to play with
        /// </summary>
        /// <param name="isPartner">If the character randomzied is the partner</param>
        public void RandomizeCharacter(bool isPartner){
            string name = RandomNames[Random.Range(0,49)];
            int perk = Random.Range(0,6), trait = Random.Range(0,6), hatNum = Random.Range(1,4), outfitNum = Random.Range(1,4), accNum = Random.Range(1,4), colorNum = Random.Range(1,10);
            
            if(isPartner){
                PartnerName = name;
                PartnerPerk = perk;
                PartnerTrait = trait;
                PartnerHat = hatNum;
                PartnerOutfit = outfitNum;
                PartnerAcc = accNum;
                PartnerColor = colorNum;
                CustomIDs[1] = -1;
            }
            else{
                LeaderName = name;
                LeaderPerk = perk;
                LeaderTrait = trait;
                LeaderHat = hatNum;
                LeaderOutfit = outfitNum;
                LeaderAcc = accNum;
                LeaderColor = colorNum;
                CustomIDs[0] = -1;
            }
            
            UpdateVisuals(perk, trait, colorNum, hatNum, outfitNum, accNum, name, isPartner);
        }

        /// <summary>
        /// Assign leader/partner as a custom character
        /// </summary>
        /// <param name="baseId">Base id number for the button (ie. button 1, button 2...) to determine which character id in the database</param>
        public void LoadCustomCharacter(int baseId){
            int colorNum, hatNum, outfitNum, accNum, perk, trait, customId = -1;
            string name;

            int accessId = CharacterCreation.ReadCharacterId(baseId);
            if(accessId == -1){
                return;
            }

            // Database commands to search for character id
            bool idFound = false;
            IEnumerable<CustomCharacter> characters = DataUser.dataManager.GetCustomCharacters();
            foreach(CustomCharacter cc in characters){
                if(cc.Id == accessId){
                    idFound = true;
                    break;
                }
            }

            // If id found, access character info
            if(idFound){
                AssigningChar = !AssigningChar;
                CustomCharacter characterOfInterest = characters.Where<CustomCharacter>(c=>c.Id == accessId).First();
                
                name = characterOfInterest.CharacterName;
                perk = characterOfInterest.Perk;
                trait = characterOfInterest.Trait;
                accNum = characterOfInterest.Acessory;
                hatNum = characterOfInterest.Hat;
                colorNum = characterOfInterest.Color;
                outfitNum = characterOfInterest.Outfit;
                customId = characterOfInterest.Id;

                if(assigningPartner){
                    PartnerName = name;
                    PartnerPerk = perk;
                    PartnerTrait = trait;
                    PartnerHat = hatNum;
                    PartnerOutfit = outfitNum;
                    PartnerAcc = accNum;
                    PartnerColor = colorNum;
                    CustomIDs[1] = customId;
                }
                else{
                    LeaderName = name;
                    LeaderPerk = perk;
                    LeaderTrait = trait;
                    LeaderHat = hatNum;
                    LeaderOutfit = outfitNum;
                    LeaderAcc = accNum;
                    LeaderColor = colorNum;
                    CustomIDs[0] = customId;
                }
                UpdateVisuals(perk, trait, colorNum, hatNum, outfitNum, accNum, name, assigningPartner);
            }
            else{
                return;
            }
        }

        /// <summary>
        /// Update player visuals
        /// </summary>
        /// <param name="perk">Player perk number</param>
        /// <param name="trait">Player trait number</param>
        /// <param name="colorNum">Player color number</param>
        /// <param name="hatNum">Player hat number</param>
        /// <param name="outfitNum">Player outfit number</param>
        /// <param name="accNum">Player accessory number</param>
        /// <param name="name">Player name</param>
        /// <param name="isPartner">If the character randomzied is the partner</param>
        private void UpdateVisuals(int perk, int trait, int colorNum, int hatNum, int outfitNum, int accNum, string name, bool isPartner){
            GameObject model = isPartner ? partnerModel : leaderModel;
            TextMeshProUGUI textFocus = isPartner ? partnerDesc: leaderDesc;
            textFocus.text = "Name: " + name + "\nPerk: " + Perks[perk] + "\nTrait: " + Traits[trait];

            // Color
            model.transform.GetChild(0).transform.GetChild(0).GetComponent<MeshRenderer>().material = CharacterCreation.Colors[colorNum-1];
            model.transform.GetChild(0).transform.GetChild(1).GetComponent<MeshRenderer>().material = CharacterCreation.Colors[colorNum-1];

            switch(hatNum){
                case 1:
                    model.transform.GetChild(3).transform.GetChild(0).gameObject.SetActive(false);
                    model.transform.GetChild(3).transform.GetChild(1).gameObject.SetActive(false);
                    break;
                case 2:
                    model.transform.GetChild(3).transform.GetChild(0).gameObject.SetActive(true);
                    model.transform.GetChild(3).transform.GetChild(1).gameObject.SetActive(false);
                    break;
                case 3:
                    model.transform.GetChild(3).transform.GetChild(0).gameObject.SetActive(false);
                    model.transform.GetChild(3).transform.GetChild(1).gameObject.SetActive(true);
                    break;
            }

            switch(outfitNum){
                case 1:
                    model.transform.GetChild(1).transform.GetChild(0).gameObject.SetActive(false);
                    model.transform.GetChild(1).transform.GetChild(1).gameObject.SetActive(false);
                    break;
                case 2:
                    model.transform.GetChild(1).transform.GetChild(0).gameObject.SetActive(true);
                    model.transform.GetChild(1).transform.GetChild(1).gameObject.SetActive(false);
                    break;
                case 3:
                    model.transform.GetChild(1).transform.GetChild(0).gameObject.SetActive(false);
                    model.transform.GetChild(1).transform.GetChild(1).gameObject.SetActive(true);
                    break;
            }

            switch(accNum){
                case 1:
                    model.transform.GetChild(2).transform.GetChild(0).gameObject.SetActive(false);
                    model.transform.GetChild(2).transform.GetChild(1).gameObject.SetActive(false);
                    break;
                case 2:
                    model.transform.GetChild(2).transform.GetChild(0).gameObject.SetActive(true);
                    model.transform.GetChild(2).transform.GetChild(1).gameObject.SetActive(false);
                    break;
                case 3:
                    model.transform.GetChild(2).transform.GetChild(0).gameObject.SetActive(false);
                    model.transform.GetChild(2).transform.GetChild(1).gameObject.SetActive(true);
                    break;
            }
        }
    }
}