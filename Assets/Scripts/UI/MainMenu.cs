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
    // Save files based on:
    // https://www.mongodb.com/developer/code-examples/csharp/saving-data-in-unity3d-using-sqlite/

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

        void Update(){
            
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

            IDbConnection dbConnection = GameDatabase.OpenDatabase();
            IDbCommand dbCommandReadValues = dbConnection.CreateCommand();
            dbCommandReadValues.CommandText = "SELECT id FROM SaveFilesTable";
            IDataReader dataReader = dbCommandReadValues.ExecuteReader();

            // Disable access to files with no saved data if loading
            if(!isCreatingNewFile){
                // Remove ids with save data
                while(dataReader.Read()){
                    ids.Remove(dataReader.GetInt32(0));
                }

                // Disable access to file access and deletion of files that aren't used
                foreach(int id in ids){
                    fileButtons[id].interactable = false;
                    deletionButtons[id].interactable = false;
                }
            }
            // Enable access to files with no saved data if creating a new file
            else{
                while(dataReader.Read()){
                    ids.Remove(dataReader.GetInt32(0));
                    deletionButtons[dataReader.GetInt32(0)].interactable = true;
                }

                // Disable access to deletion but allow file access for unused files
                foreach(int id in ids){
                    fileButtons[id].interactable = true;
                    deletionButtons[id].interactable = false;
                }
            }
            dbConnection.Close();
        }

        /// <summary>
        /// Access a saved game
        /// </summary>
        /// <param name="id">The id of the save file specified in the editor</param>
        public void AccessGame(int id){
            idFound = false;
            IDbConnection dbConnection = GameDatabase.OpenDatabase();
            IDbCommand dbCommandReadValues = dbConnection.CreateCommand();
            dbCommandReadValues.CommandText = "SELECT id FROM SaveFilesTable";
            idFound = GameDatabase.MatchId(dbCommandReadValues, id);

            // Creating a new file
            if(isCreatingNewFile){
                // Confirm to overwrite or cancel
                if(idFound){
                    ConfirmFileReplace(id);
                    dbConnection.Close();
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
                    dbCommandReadValues = dbConnection.CreateCommand();
                    dbCommandReadValues.CommandText = "SELECT inPhase FROM SaveFilesTable WHERE id = @Param1";
                    QueryParameter<int> fileParameter = new QueryParameter<int>("@Param1", GameLoop.FileId);
                    fileParameter.SetParameter(dbCommandReadValues);
                    IDataReader dataReader = dbCommandReadValues.ExecuteReader();
                    dataReader.Read();

                    int phase = dataReader.GetInt32(0);
                    
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
            dbConnection.Close();
        }

        /// <summary> 
        /// Transition main menu to main game
        /// </summary>
        /// <param name="id">The id of the file played.</param>
        private void TransitionMenu(int id){
            accessScreen.SetActive(false);
            mainGameUI.SetActive(true);
            mainMenuScreen.SetActive(false);
            SceneManager.LoadScene(1);
            GameLoop.FileId = id;
        }

        /// <summary> 
        /// Set the file descriptor of each save file
        /// </summary>
        public void SetFileDesc(){
            string diff = "";
            List<int> nullFiles = new List<int>();
            IDbConnection dbConnection = GameDatabase.OpenDatabase();
            IDbCommand dbCommandReadValues = dbConnection.CreateCommand();
            dbCommandReadValues.CommandText = "SELECT id FROM ActiveCharactersTable WHERE leaderName is NULL";
            IDataReader dataReader = dbCommandReadValues.ExecuteReader();

            while(dataReader.Read()){
                nullFiles.Add(dataReader.GetInt32(0));
            }

            foreach(int nullFile in nullFiles){
                targetFile = nullFile;
                DeleteFile();
            }
            targetFile = -1;

            dbCommandReadValues = dbConnection.CreateCommand();
            dbCommandReadValues.CommandText = "SELECT SaveFilesTable.id, leaderName, distance, location, difficulty FROM SaveFilesTable LEFT JOIN ActiveCharactersTable" + 
                                              " ON SaveFilesTable.charactersId = ActiveCharactersTable.id";
            dataReader = dbCommandReadValues.ExecuteReader();

            while(dataReader.Read()){
                int diffRead = dataReader.GetInt32(4);
                diff = diffRead == 1 ? "Standard" : diffRead == 2 ? "Deadlier" : diffRead == 3 ? "Standard Custom" : "Deadlier Custom";
                fileDescriptors[dataReader.GetInt32(0)].text = "  File " + (dataReader.GetInt32(0)+1) + "\n  " + dataReader.GetString(1) + 
                                                               "\n  " + dataReader.GetInt32(2) + "km\t " + dataReader.GetString(3) + "\n  " + diff;
            }

            dbConnection.Close();
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

            IDbConnection dbConnection = GameDatabase.OpenDatabase();
            IDbCommand dbCommandDeleteValue = dbConnection.CreateCommand();
            dbCommandDeleteValue.CommandText = "DELETE FROM SaveFilesTable WHERE id = @Param1";
            QueryParameter<int> fileParameter = new QueryParameter<int>("@Param1", targetFile);
            fileParameter.SetParameter(dbCommandDeleteValue);
            dbCommandDeleteValue.ExecuteNonQuery();

            dbCommandDeleteValue = dbConnection.CreateCommand();
            dbCommandDeleteValue.CommandText = "DELETE FROM ActiveCharactersTable WHERE id = @Param1";
            fileParameter.SetParameter(dbCommandDeleteValue);
            dbCommandDeleteValue.ExecuteNonQuery();

            dbCommandDeleteValue = dbConnection.CreateCommand();
            dbCommandDeleteValue.CommandText = "DELETE FROM CarsTable WHERE id = @Param1";
            fileParameter.SetParameter(dbCommandDeleteValue);
            dbCommandDeleteValue.ExecuteNonQuery();

            dbCommandDeleteValue = dbConnection.CreateCommand();
            dbCommandDeleteValue.CommandText = "DELETE FROM TownTable WHERE id = @Param1";
            fileParameter.SetParameter(dbCommandDeleteValue);
            dbCommandDeleteValue.ExecuteNonQuery();

            dbCommandDeleteValue = dbConnection.CreateCommand();
            dbCommandDeleteValue.CommandText = "DELETE FROM PerishedCustomTable WHERE saveFileId = @Param1";
            fileParameter.SetParameter(dbCommandDeleteValue);
            dbCommandDeleteValue.ExecuteNonQuery();
            dbConnection.Close();

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
            List<int> startParameters;
            List<string> startParametersStr, startParamNames, startParamStrNames;

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
            IDbConnection dbConnection = GameDatabase.OpenDatabase();
            IDbCommand dbCommandInsertValue = dbConnection.CreateCommand();
            dbCommandInsertValue.CommandText = "INSERT OR REPLACE INTO ActiveCharactersTable(id, leaderName, leaderPerk, leaderTrait, leaderColor, leaderAcc, leaderHat, leaderOutfit, leaderMorale, leaderHealth, " +
                                               "friend1Name, friend1Perk, friend1Trait, friend1Color, friend1Acc, friend1Hat, friend1Outfit, friend1Morale, friend1Health, customIdLeader, customId1) VALUES (" + 
                                               "@target, @lname, @lperk, @ltrait, @lcolor, @lacc, @lhat, @loutfit, @lmorale, 100, @pname, @pperk, @ptrait, @pcolor, @pacc, @phat, @poutfit, @pmorale, 100, @cid1, @cid2);";
            startParametersStr = new List<string>(){GamemodeSelect.LeaderName, GamemodeSelect.PartnerName};
            startParamStrNames = new List<string>(){"@lname", "@pname"};
            startParameters = new List<int>(){targetFile, GamemodeSelect.LeaderPerk, GamemodeSelect.LeaderTrait, GamemodeSelect.LeaderColor, GamemodeSelect.LeaderAcc, GamemodeSelect.LeaderHat, 
                                              GamemodeSelect.LeaderOutfit, startingLeaderMorale, GamemodeSelect.PartnerPerk, GamemodeSelect.PartnerTrait, GamemodeSelect.PartnerColor,
                                              GamemodeSelect.PartnerAcc, GamemodeSelect.PartnerHat, GamemodeSelect.PartnerOutfit, startingPartnerMorale, GamemodeSelect.CustomIDs[0], GamemodeSelect.CustomIDs[1]
                                            };
            startParamNames = new List<string>(){"@target", "@lperk", "@ltrait", "@lcolor", "@lacc", "@lhat", "@loutfit", "@lmorale", "@pperk", 
                                                 "@ptrait", "@pcolor", "@pacc", "@phat", "@poutfit", "@pmorale", "@cid1", "@cid2"};
            for(int i = 0; i < startParameters.Count; i++){
                QueryParameter<int> saveParameter = new QueryParameter<int>(startParamNames[i], startParameters[i]);
                saveParameter.SetParameter(dbCommandInsertValue);
            }
            for(int i = 0; i < startParametersStr.Count; i++){
                QueryParameter<string> saveParameter = new QueryParameter<string>(startParamStrNames[i], startParametersStr[i]);
                saveParameter.SetParameter(dbCommandInsertValue);
            }
            dbCommandInsertValue.ExecuteNonQuery();

            // Initializing save file data
            dbCommandInsertValue = dbConnection.CreateCommand();
            dbCommandInsertValue.CommandText = "INSERT OR REPLACE INTO SaveFilesTable(id, charactersId, carId, distance, difficulty, location, inPhase, food, gas, scrap, " +
                                                "money, medkit, tire, battery, ammo, time, overallTime, rations, speed) VALUES (@target, @target, @target, 0, @diff, 'Montreal', 0, " +
                                                "@food, @gas, @scrap, @money, @medkit, @tire, @battery, @ammo, 12, 0, 2, 2);";
            startParameters = new List<int>(){targetFile, GamemodeSelect.Difficulty, startingFood, startingGas, startingScrap, startingMoney, startingMedkit, startingBattery, 
                                                       startingTire, startingAmmo, startingLeaderMorale, startingPartnerMorale};
            startParamNames = new List<string>(){"@target", "@diff", "@food", "@gas", "@scrap", "@money", "@medkit", "@battery", "@tire", "@ammo", "@lmorale", "@pmorale"};
            for(int i = 0; i < startParameters.Count; i++){
                QueryParameter<int> saveParameter = new QueryParameter<int>(startParamNames[i], startParameters[i]);
                saveParameter.SetParameter(dbCommandInsertValue);
            }
            dbCommandInsertValue.ExecuteNonQuery();

            // Initializing town data
            Town start = new Town();
            dbCommandInsertValue = dbConnection.CreateCommand();
            dbCommandInsertValue.CommandText = "INSERT OR REPLACE INTO TownTable(id, foodPrice, gasPrice, scrapPrice, medkitPrice, tirePrice, batteryPrice, ammoPrice, " +
                                               "foodStock, gasStock, scrapStock, medkitStock, tireStock, batteryStock, ammoStock, side1Reward, side1Qty, side1Diff, side1Type, " +
                                               "side2Reward, side2Qty, side2Diff, side2Type, side3Reward, side3Qty, side3Diff, side3Type, curTown, prevTown) VALUES" +
                                               "(@target, @foodPrice, @gasPrice, @scrapPrice, @medkitPrice, @tirePrice, @batteryPrice, @ammoPrice, @foodStock, @gasStock, @scrapStock, " + 
                                               "@medkitStock, @tireStock, @batteryStock, @ammoStock, @m0Reward, @m0qty, @m0diff, @m0type, @m1Reward, @m1qty, @m1diff, @m1type, " + 
                                               "@m2Reward, @m2qty, @m2diff, @m2type, 0, -1)";
            startParameters = new List<int>(){targetFile, start.GetFoodPrice(), start.GetGasPrice(), start.GetScrapPrice(), start.GetMedkitPrice(), start.GetTirePrice(), 
                                                 start.GetBatteryPrice(), start.GetAmmoPrice(), start.GetFoodStock(), start.GetGasStock(), start.GetScrapStock(),
                                                 start.GetMedkitStock(), start.GetTireStock(), start.GetBatteryStock(), start.GetAmmoStock(), start.GetMissions()[0].GetMissionReward(),
                                                 start.GetMissions()[0].GetMissionQty(), start.GetMissions()[0].GetMissionDifficulty(), start.GetMissions()[0].GetMissionType(),
                                                 start.GetMissions()[1].GetMissionReward(), start.GetMissions()[1].GetMissionQty(), start.GetMissions()[1].GetMissionDifficulty(), 
                                                 start.GetMissions()[1].GetMissionType(), start.GetMissions()[2].GetMissionReward(), start.GetMissions()[2].GetMissionQty(),
                                                 start.GetMissions()[2].GetMissionDifficulty(), start.GetMissions()[2].GetMissionType()};
            startParamNames = new List<string>(){"@target", "@foodPrice", "@gasPrice", "scrapPrice", "@medkitPrice", "@tirePrice", "@batteryPrice", "@ammoPrice", "@foodStock", "@gasStock", "@scrapStock",
                                                 "@medkitStock", "@tireStock", "@batteryStock", "@ammoStock", "@m0Reward", "@m0qty", "@m0diff", "@m0type", "@m1Reward", "@m1qty", "@m1diff", "@m1type",
                                                 "@m2Reward", "@m2qty", "@m2diff", "@m2type" 
                                                };
            for(int i = 0; i < startParameters.Count; i++){
                QueryParameter<int> saveParameter = new QueryParameter<int>(startParamNames[i], startParameters[i]);
                saveParameter.SetParameter(dbCommandInsertValue);
            }
            dbCommandInsertValue.ExecuteNonQuery();

            // Initializing car data
            dbCommandInsertValue = dbConnection.CreateCommand();
            dbCommandInsertValue.CommandText = "INSERT OR REPLACE INTO CarsTable(id, carHP, wheelUpgrade, batteryUpgrade, engineUpgrade, toolUpgrade, miscUpgrade1, miscUpgrade2, isBatteryDead, isTireFlat) VALUES" +
                                               "(@targetFile, 100, 0, 0, 0, 0, 0, 0, 0, 0)";
            QueryParameter<int> parameterInt = new QueryParameter<int>("@targetFile", targetFile);
            parameterInt.SetParameter(dbCommandInsertValue);
            dbCommandInsertValue.ExecuteNonQuery();
            dbConnection.Close();

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

