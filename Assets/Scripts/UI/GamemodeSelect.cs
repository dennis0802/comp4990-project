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

        [Tooltip("Colors for players")]
        [SerializeField]
        private Material[] playerColors;

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

        // To track selected and confirmed difficulties
        private int selectedMode = 1, confirmedMode = 1;

        // For randomizing a character
        private List<string> randomNames = new List<string>(){
            "Grayson","Iliana","Delaney","Ibrahim","Jaylin","Jazmine","Emilio","Sheldon","Brady","Jeffery","Izaiah","Juliette","Aidyn","Matias","Ryker","Saniya","Karen",
            "Luke","Sonia","Dakota","Catalina","Maci","Aurora","Ronin","Skye","Jasiah","Taylor","Johnathon","Monserrat","Keyon","Desmond","Jaylen","Brandon","Riley",
            "Emmy","Macey","Ramiro","Andreas","Yazmin","Adam","Jovany","Liliana","Leonel","Roselyn","Zain","Paige","Karissa","Dane","Emery","Aidan"
        };

        // To track if character selection is for assigning or accessing
        public static bool assigningChar = false;

        /// <summary>
        /// Select a game mode
        /// </summary>
        public void SelectMode(int key){
            switch(key){
                case 1:
                    gamemodeTitle.text = "Standard";
                    gamemodeDesc.text = "Standard enemies, randomized characters, decent amount of supplies";
                    break;
                case 2:
                    gamemodeTitle.text = "Deadlier";
                    gamemodeDesc.text = "Deadlier enemies, randomized characters, scarce amount of supplies";
                    break;
                case 3:
                    gamemodeTitle.text = "Standard Custom";
                    gamemodeDesc.text = "Standard enemies, custom characters, decent amount of supplies";
                    break;
                case 4:
                    gamemodeTitle.text = "Deadlier Custom";
                    gamemodeDesc.text = "Deadlier enemies, custom characters, scarce amount of supplies";
                    break;
                default:
                    return;
            }
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
            confirmedMode = selectedMode;
        }

        /// <summary>
        /// Toggle how characters will be selected - assign to a file or to access their database data
        /// </summary>
        public void ToggleCharAssignment(){
            assigningChar = !assigningChar;
        }

        /// <summary>
        /// Toggle if a partner will be used at the start
        /// </summary>
        public void TogglePartner(){
            selectPartnerButton.interactable = !selectPartnerButton.interactable;
            randomizePartnerButton.interactable = selectPartnerButton.interactable;
            partnerModel.gameObject.SetActive(selectPartnerButton.interactable);

            if(selectPartnerButton.interactable){
                partnerDesc.text = "Name:\nDesc:\nTrait:";
                togglePartnerButtonText.text = "X";
            }
            else{
                partnerDesc.text = "No partner";
                togglePartnerButtonText.text = "O";
            }

        }

        public void ResetScreen(){
            selectPartnerButton.interactable = true;
            randomizePartnerButton.interactable = selectPartnerButton.interactable;
            partnerDesc.text = "Name:\nDesc:\nTrait:";
            togglePartnerButtonText.text = "X";
        }

        /// <summary>
        /// Randomize character to play with
        /// </summary>
        /// <param name="isPartner">If the character randomzied is the partner</param>
        public void RandomizeCharacter(bool isPartner){
            string name = randomNames[Random.Range(0,49)];
            int perk = Random.Range(0,5), trait = Random.Range(0,5), hatNum = Random.Range(1,3), outfitNum = Random.Range(1,3), accNum = Random.Range(1,3), colorNum = Random.Range(0,8);
            
            TextMeshProUGUI textFocus = isPartner ? partnerDesc: leaderDesc;
            GameObject model = isPartner ? partnerModel : leaderModel;

            textFocus.text = "Name: " + name + "\nDesc:\nTrait:";

            // Color
            model.transform.GetChild(0).transform.GetChild(0).GetComponent<MeshRenderer>().material = playerColors[colorNum];
            model.transform.GetChild(0).transform.GetChild(1).GetComponent<MeshRenderer>().material = playerColors[colorNum];

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

        /// <summary>
        /// Assign leader/partner as a custom character
        public void SelectCustomCharacter(){
            
        }
    }
}