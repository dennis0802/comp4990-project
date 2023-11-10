using System.Data.Common;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Mono.Data.Sqlite;
using TMPro;
using RestPhase;
using Database;
using TravelPhase;

namespace UI
{
    [DisallowMultipleComponent]
    public class MainMenu : MonoBehaviour {
        [Header("Screens")]
        [Tooltip("Game object containing screen for accessing a file")]
        [SerializeField]
        private GameObject accessScreen;

        [Tooltip("Game object containing screen for picking the gamemode")]
        [SerializeField]
        private GameObject gameModeScreen;

        [Header("File Access")]
        [SerializeField]
        private bool isCreatingNewFile;

        [Tooltip("Text objects for description")]
        [SerializeField]
        private TextMeshProUGUI[] fileDescriptors;

        [Tooltip("Buttons for disabling/enabling")]
        [SerializeField]
        private Button[] fileButtons, deletionButtons;

        [Tooltip("Title for accessing files")]
        [SerializeField]
        private TextMeshProUGUI fileAccessTitle;

        [Tooltip("Game object containing file UI")]
        [SerializeField]
        private GameObject fileAccessWindow;

        [Tooltip("Game object containing UI for replacing a save file")]
        [SerializeField]
        private GameObject fileReplaceWindow;

        [Tooltip("Game object containing UI for deleting a save file")]
        [SerializeField]
        private GameObject fileDeleteWindow;

        [Tooltip("Game object containing UI for beginning the game")]
        [SerializeField]
        private GameObject introWindow;

        [Tooltip("Game object containing UI for the main game components.")]
        [SerializeField]
        private GameObject mainGameUI;

        [Tooltip("Game object containing the main menu screen.")]
        [SerializeField]
        private GameObject mainMenuScreen;

        [Tooltip("Game object containing UI for active components.")]
        [SerializeField]
        private GameObject activeUI;

        [Tooltip("Game object containing UI for the rest menu components.")]
        [SerializeField]
        private GameObject restMenuUI;

        [Tooltip("Game objects containing for the travel screen.")]
        [SerializeField]
        private GameObject[] travelMenuUI;

        [Tooltip("Rest menu script")]
        [SerializeField]
        private RestMenu restMenu;

        // Instance of the main menu;
        private static MainMenu menuInstance;

        // To track which file is being marked for deletion/replacement
        private int targetFile = -1;
        // To track if a file exists
        private bool idFound = false;

        public void Start(){
            DontDestroyOnLoad(this.gameObject);
            if(menuInstance == null){
                menuInstance = this;
            }
            else{
                Destroy(gameObject);
            }

            SetFileDesc();
        }

        /// <summary>
        /// Access save files
        /// </summary>
        /// <param name="mode">True for new file creation, false for loading
        public void AccessFiles(bool mode){
            isCreatingNewFile = mode ? true : false;
            fileAccessTitle.text = mode ? "Start New File" : "Load File";
            // Temp list to track which ids are used
            List<int> ids = new List<int>(){0,1,2,3};
            IEnumerable saves = DataUser.dataManager.GetSaves();

            // Disable access to files with no saved data if loading
            if(!isCreatingNewFile){
                foreach(Save save in saves){
                    ids.Remove(save.Id);
                }

                // Disable access to file access and deletion of files that aren't used
                foreach(int id in ids){
                    fileButtons[id].interactable = false;
                    deletionButtons[id].interactable = false;
                }
            }
            // Enable access to files with no saved data if creating a new file
            else{
                foreach(Save save in saves){
                    ids.Remove(save.Id);
                    deletionButtons[save.Id].interactable = true;
                }

                // Disable access to deletion but allow file access for unused files
                foreach(int id in ids){
                    fileButtons[id].interactable = true;
                    deletionButtons[id].interactable = false;
                }
            }
        }

        /// <summary>
        /// Access a saved game
        /// </summary>
        /// <param name="id">The id of the save file specified in the editor</param>
        public void AccessGame(int id){
            // Attempt to find the file with the id
            idFound = false;
            IEnumerable saves = DataUser.dataManager.GetSaves();
            foreach(Save save in saves){
                if(save.Id == id){
                    idFound = true;
                    break;
                }
            }

            // Creating a new file
            if(isCreatingNewFile){
                // Confirm to overwrite or cancel
                if(idFound){
                    ConfirmFileReplace(id);
                    return;
                }
                deletionButtons[id].interactable = true;

                // Change screens (not in replacingFile function due to single possible action)
                accessScreen.SetActive(false);
                gameModeScreen.SetActive(true);
                targetFile = id;

                gameObject.GetComponent<GamemodeSelect>().RandomizeCharacter(true);
                gameObject.GetComponent<GamemodeSelect>().RandomizeCharacter(false);
            }
            // Loading a file
            else{
                // Open the game
                if(idFound){
                    TransitionMenu(id);
                    int phase = DataUser.dataManager.GetSaveById(GameLoop.FileId).PhaseNum;
                    
                    if(phase == 0 || phase == 2){
                        restMenuUI.SetActive(true);
                        travelMenuUI[0].SetActive(false);
                        travelMenuUI[1].SetActive(false);
                        travelMenuUI[2].SetActive(true);
                    }
                    else if(phase == 1){
                        restMenuUI.SetActive(false);
                        travelMenuUI[0].SetActive(true);
                        travelMenuUI[1].SetActive(true);
                        travelMenuUI[2].SetActive(false);
                    }
                    activeUI.SetActive(true);
                }
            }
        }

        /// <summary> 
        /// Transition main menu to main game
        /// </summary>
        /// <param name="id">The id of the file played.</param>
        private void TransitionMenu(int id){
            accessScreen.SetActive(false);
            mainGameUI.SetActive(true);
            mainMenuScreen.SetActive(false);
            GameLoop.FileId = id;
        }

        /// <summary> 
        /// Set the file descriptor of each save file
        /// </summary>
        public void SetFileDesc(){
            IEnumerable<Save> saves = DataUser.dataManager.GetSaves();
            IEnumerable<ActiveCharacter> nullLeaders = DataUser.dataManager.GetNullSaves();

            // Delete saves who have no leader
            foreach(ActiveCharacter nullLeader in nullLeaders){
                targetFile = nullLeader.FileId;
                DeleteFile();
            }

            foreach(Save save in saves){
                ActiveCharacter leader = DataUser.dataManager.GetLeader(save.Id);
                if(leader == null){
                    targetFile = save.Id;
                    DeleteFile();
                    continue;
                }
                string diff = save.Difficulty == 1 ? "Standard" : save.Difficulty == 2 ? "Deadlier" : save.Difficulty == 3 ? "Standard Custom" : "Deadlier Custom";
                fileDescriptors[save.Id].text = " File " + (save.Id + 1) + "\n  " + leader.CharacterName + "\n  " + save.Distance + "km\t" + save.CurrentLocation + "\n  " + diff;
            }
        }

        /// <summary>
        /// Confirm that the user wants to replace a save file
        /// </summary>
        /// <param name="id">The id of the save file specified in the editor</param>
        public void ConfirmFileReplace(int id){
            fileAccessWindow.SetActive(false);
            fileReplaceWindow.SetActive(true);
            targetFile = id;
        }

        /// <summary>
        /// Replace the file
        /// </summary>
        public void ReplaceFile(){
            DeleteFile();

            gameObject.GetComponent<GamemodeSelect>().RandomizeCharacter(true);
            gameObject.GetComponent<GamemodeSelect>().RandomizeCharacter(false);
        }

        /// <summary>
        /// Confirm that the user wants to delete a save file
        /// </summary>
        /// <param name="id">The id of the save file specified in the editor</param>
        public void ConfirmFileDeletion(int id){
            fileAccessWindow.SetActive(false);
            fileDeleteWindow.SetActive(true);
            targetFile = id;
        }

        /// <summary>
        /// Delete the file
        /// </summary>
        public void DeleteFile(){
            targetFile = GameLoop.FileId == -1 ? targetFile : GameLoop.FileId;
            DataUser.dataManager.DeleteSave(targetFile);

            fileDescriptors[targetFile].text = "  File " + (targetFile+1) + "\n\n  No save file";
            deletionButtons[targetFile].interactable = false;

            if(!isCreatingNewFile){
                fileButtons[targetFile].interactable = false;
            }
        }

        /// <summary>
        /// Start a new game
        /// </summary>
        public void StartNewGame(){
            int startingFood = 100, startingGas = 20, startingScrap = 25, startingMoney = 30, startingMedkit = 1, startingBattery = 1, startingTire = 1, startingAmmo = 150,
                startingLeaderMorale = 75, startingPartnerMorale = 75;

            // Change starting data depending on difficulty
            if(GamemodeSelect.Difficulty == 2 || GamemodeSelect.Difficulty == 4){
                startingFood = 50; 
                startingGas = 10;
                startingScrap = 12; 
                startingMoney = 15; 
                startingMedkit = 0;
                startingBattery = 0;
                startingTire = 0;
                startingAmmo = 75;
            }

            // Add medkits if healthcare perk is used
            startingMedkit += GamemodeSelect.LeaderPerk == 2 || GamemodeSelect.PartnerPerk == 2 ? 2 : 0;
            // Increase morale if optimist
            startingLeaderMorale += GamemodeSelect.LeaderTrait == 2 ? 15 : 0;
            startingPartnerMorale += GamemodeSelect.PartnerTrait == 2 ? 15 : 0; 

            // Initializing active character data
            ActiveCharacter leader = new ActiveCharacter(){CharacterName = GamemodeSelect.LeaderName, Perk = GamemodeSelect.LeaderPerk, Trait = GamemodeSelect.LeaderTrait,
                                                           Color = GamemodeSelect.LeaderColor, Acessory = GamemodeSelect.LeaderAcc, Hat = GamemodeSelect.LeaderHat,
                                                           Outfit = GamemodeSelect.LeaderOutfit, Morale = startingLeaderMorale, Health = 100, IsLeader = 1, FileId = targetFile,
                                                           CustomCharacterId = GamemodeSelect.CustomIDs[0]
                                                        };
            ActiveCharacter partner = new ActiveCharacter(){CharacterName = GamemodeSelect.PartnerName, Perk = GamemodeSelect.PartnerPerk, Trait = GamemodeSelect.PartnerTrait,
                                                           Color = GamemodeSelect.PartnerColor, Acessory = GamemodeSelect.PartnerAcc, Hat = GamemodeSelect.PartnerHat,
                                                           Outfit = GamemodeSelect.PartnerOutfit, Morale = startingPartnerMorale, Health = 100, IsLeader = 0, FileId = targetFile,
                                                           CustomCharacterId = GamemodeSelect.CustomIDs[1]
                                                        };
            DataUser.dataManager.InsertCharacter(leader);
            DataUser.dataManager.InsertCharacter(partner);

            // Initializing save file data
            Save newSave = new Save(){Distance = 0, Difficulty = GamemodeSelect.Difficulty, CurrentLocation = "Montreal", PhaseNum = 0, Food = startingFood, Gas = startingGas, 
                                      Scrap = startingScrap, Money = startingMoney, Medkit = startingMedkit, Battery = startingBattery, Tire = startingTire, Ammo = startingAmmo,
                                      CurrentTime = 12, OverallTime = 0, RationMode = 2, PaceMode = 2, Id = targetFile
                                    };
            DataUser.dataManager.InsertSave(newSave);

            // Initializing town data
            Town start = new Town();
            TownEntity town = new TownEntity(){FoodPrice = start.GetFoodPrice(), GasPrice = start.GetGasPrice(), ScrapPrice = start.GetScrapPrice(), 
                                               MedkitPrice = start.GetMedkitPrice(), TirePrice = start.GetTirePrice(), BatteryPrice = start.GetBatteryPrice(),
                                               AmmoPrice = start.GetAmmoPrice(), FoodStock = start.GetFoodStock(), GasStock = start.GetGasStock(), ScrapStock = start.GetScrapStock(),
                                               MedkitStock = start.GetMedkitStock(), TireStock = start.GetTireStock(), BatteryStock = start.GetBatteryStock(), 
                                               AmmoStock = start.GetAmmoStock(), Side1Reward = start.GetMissions()[0].GetMissionReward(), Side1Qty = start.GetMissions()[0].GetMissionQty(),
                                               Side1Type = start.GetMissions()[0].GetMissionType(), Side1Diff = start.GetMissions()[0].GetMissionDifficulty(), Side2Reward = start.GetMissions()[1].GetMissionReward(), 
                                               Side2Qty = start.GetMissions()[1].GetMissionQty(), Side2Type = start.GetMissions()[1].GetMissionType(), Side2Diff = start.GetMissions()[1].GetMissionDifficulty(),
                                               Side3Reward = start.GetMissions()[2].GetMissionReward(), Side3Qty = start.GetMissions()[2].GetMissionQty(), 
                                               Side3Type = start.GetMissions()[2].GetMissionType(), Side3Diff = start.GetMissions()[2].GetMissionDifficulty(), CurTown = 0, PrevTown = -1,
                                               NextDistanceAway = 0, NextTownName = "", Id = targetFile
                                            };
            DataUser.dataManager.InsertTown(town);

            // Initializing car data
            Car car = new Car(){Id = targetFile, CarHP = 100, WheelUpgrade = 0, BatteryUpgrade = 0, EngineUpgrade = 0, ToolUpgrade = 0, MiscUpgrade1 = 0, MiscUpgrade2 = 0, IsTireFlat = 0, IsBatteryDead = 0};
            DataUser.dataManager.InsertCar(car);

            // Prepare next screen
            travelMenuUI[0].SetActive(false);
            travelMenuUI[1].SetActive(false);
            
            TransitionMenu(targetFile);
            introWindow.SetActive(true);
            activeUI.SetActive(true);
        }

        /// <summary>
        /// Exit the game
        /// </summary>
        public void ExitGame(){
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                UnityEngine.Application.Quit();
            #endif
        }
    }
}

