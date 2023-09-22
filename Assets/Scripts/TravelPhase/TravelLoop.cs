using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System;
using UnityEngine;
using Random = UnityEngine.Random;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Mono.Data.Sqlite;
using UI;
using TMPro;
using Database;
using RestPhase;

namespace TravelPhase{
    [DisallowMultipleComponent]
    public class TravelLoop : MonoBehaviour
    {
        [Header("Screen Components")]
        [Tooltip("List of player health bars")]
        [SerializeField]
        private Slider[] playerHealthBars;

        [Tooltip("Car health bar")]
        [SerializeField]
        private Slider carHealthBar;

        [Tooltip("Text object storing player info")]
        [SerializeField]
        private TextMeshProUGUI playerText;

        [Tooltip("Text object storing supply info")]
        [SerializeField]
        private TextMeshProUGUI supplyText;

        [Tooltip("Second option button for destination")]
        [SerializeField]
        private Button destinationButton2;

        [Tooltip("Destination button texts")]
        [SerializeField]
        private TextMeshProUGUI[] destinationTexts;

        [Tooltip("Popup text")]
        [SerializeField]
        private TextMeshProUGUI popupText;

        [Tooltip("Popup object")]
        [SerializeField]
        private GameObject popup;

        [Tooltip("Destination popup window object")]
        [SerializeField]
        private GameObject destinationPopup;

        [Tooltip("Destination popup text")]
        [SerializeField]
        private TextMeshProUGUI destinationPopupText;

        [Tooltip("Travel view object")]
        [SerializeField]
        private GameObject travelViewObject;

        [Tooltip("Background panel object")]
        [SerializeField]
        private GameObject backgroundPanel;

        [Tooltip("Rest menu screens - element 0 will be kept active in the background")]
        [SerializeField]
        private GameObject[] restScreens;

        // To track if a popup is active, will restrict when driving loop occurs.
        public static bool PopupActive = false;
        // To track the new town number and the distance away
        private int newTown, targetTownDistance = 0;
        // To track if the log of destinations has been initialized
        private bool logInitialized = false;
        // To track generated towns
        private List<Town> towns = new List<Town>();
        // To manage destinations
        private Dictionary<int, List<int>> distanceLog = new Dictionary<int, List<int>>();
        private Dictionary<int, List<string>> nextDestinationLog = new Dictionary<int, List<string>>();
        // To time the driving loop
        private float timer = 0.0f;

        void OnEnable(){
            if(!logInitialized){
                InitializeLogs();
            }
            GenerateTowns();
            RefreshScreen();
        }

        void Update(){
            // NOTE: Previous solution used a coroutine, running into problems when the game was paused.
            if(!PopupActive){
                timer += Time.deltaTime;
                if(timer >= 3.0f){
                    if(timer >= 8.0f){
                        Debug.Log("Timestep done");
                        if(Drive()){
                            int eventChance = Random.Range(1,101);

                            // 44/100 chance of generating an event
                            if(eventChance <= 44){
                                GenerateEvent(eventChance);
                            }
                        }
                        timer = 0.0f;
                    }
                }
            }
        }

        /// <summary>
        /// Generate towns' resources for the player to pick when picking a destination
        /// </summary>
        public void GenerateTowns(){
            towns.Add(new Town());
            towns.Add(new Town());
            UpdateButtons();
        }

        /// <summary>
        /// Confirm a destination
        /// </summary>
        /// <param name="id">Id of the button that was clicked.</param>
        public void ConfirmDestination(int id){
            // Update save file
            IDbConnection dbConnection = GameDatabase.CreateSavesAndOpenDatabase();
            IDbCommand dbCommandUpdateValue = dbConnection.CreateCommand();
            dbCommandUpdateValue.CommandText = "UPDATE SaveFilesTable SET inPhase = 1, location = 'The Road' WHERE id = " + GameLoop.FileId;
            dbCommandUpdateValue.ExecuteNonQuery();

            IDbCommand dbCommandReadValue = dbConnection.CreateCommand();
            dbCommandReadValue.CommandText = "SELECT distance FROM SaveFilesTable WHERE id = " + GameLoop.FileId;
            IDataReader dataReader = dbCommandReadValue.ExecuteReader();
            dataReader.Read();

            int curDistance = dataReader.GetInt32(0);
            dbConnection.Close();
            
            // Update town database with new town rolls.
            dbConnection = GameDatabase.CreateTownAndOpenDatabase();
            dbCommandReadValue = dbConnection.CreateCommand();
            dbCommandReadValue.CommandText = "SELECT curTown FROM TownTable WHERE id = " + GameLoop.FileId;
            dataReader = dbCommandReadValue.ExecuteReader();
            dataReader.Read();

            int oldTownNum = dataReader.GetInt32(0);
            targetTownDistance = curDistance + distanceLog[oldTownNum][id-1];
            string destinationTown = nextDestinationLog[oldTownNum][id-1];

            newTown = id == 1 ? oldTownNum + id : oldTownNum + 10;

            dbCommandUpdateValue = dbConnection.CreateCommand();
            dbCommandUpdateValue.CommandText = "UPDATE TownTable SET curTown = " + newTown + ", foodPrice = " + towns[id-1].GetFoodPrice() + ", gasPrice = " +  towns[id-1].GetGasPrice() +
                                                ", scrapPrice = " + towns[id-1].GetScrapPrice()  + ", medkitPrice = " +  towns[id-1].GetMedkitPrice()  + ", tirePrice = " +  towns[id-1].GetTirePrice()  +
                                                ", batteryPrice = " +  towns[id-1].GetBatteryPrice()  + ", ammoPrice = " +  towns[id-1].GetAmmoPrice()  + ", foodStock = " +  towns[id-1].GetFoodStock()  +
                                                ", gasStock = " +  towns[id-1].GetGasStock() + ", scrapStock = " +  towns[id-1].GetScrapStock() + ", medkitStock = " + towns[id-1].GetMedkitStock() +
                                                ", tireStock = " +  towns[id-1].GetTireStock() + ", batteryStock = " +  towns[id-1].GetBatteryStock() + ", ammoStock = " +  towns[id-1].GetAmmoStock() +
                                                ", side1Reward = " + towns[id-1].GetMissions()[0].GetMissionReward() + ", side1Qty = " + towns[id-1].GetMissions()[0].GetMissionQty() + 
                                                ", side1Diff = " + towns[id-1].GetMissions()[0].GetMissionDifficulty() + ", side1Type = " + towns[id-1].GetMissions()[0].GetMissionType() + 
                                                ", side2Reward = " + towns[id-1].GetMissions()[1].GetMissionReward() + ", side2Qty = " + towns[id-1].GetMissions()[1].GetMissionQty() + 
                                                ", side2Diff = " + towns[id-1].GetMissions()[1].GetMissionDifficulty() + ", side2Type = " + towns[id-1].GetMissions()[1].GetMissionType() + 
                                                ", side3Reward = " + towns[id-1].GetMissions()[2].GetMissionReward() + ", side3Qty = " + towns[id-1].GetMissions()[2].GetMissionQty() + 
                                                ", side3Diff = " + towns[id-1].GetMissions()[2].GetMissionDifficulty() + ", side3Type = " + towns[id-1].GetMissions()[2].GetMissionType() + 
                                                ", nextDistanceAway = " + targetTownDistance + ", nextTownName = '" + destinationTown + "' " + 
                                                " WHERE id = " + GameLoop.FileId;
            dbCommandUpdateValue.ExecuteNonQuery();
            dbConnection.Close();

            towns.Clear();
            int distanceLeft = targetTownDistance - curDistance;
            popupText.text = distanceLeft.ToString() + " km to " + destinationTown;
            LaunchPopup();

            RefreshScreen();
        }

        /// <summary>
        /// Refresh the screen upon loading the travel UI.
        /// </summary>
        public void RefreshScreen(){
            // Read the database for party info
            string tempPlayerText = "";

            IDbConnection dbConnection = GameDatabase.CreateCarsAndOpenDatabase();
            IDbCommand dbCommandReadValue = dbConnection.CreateCommand();
            dbCommandReadValue.CommandText = "SELECT carHp FROM CarsTable WHERE id = " + GameLoop.FileId;
            IDataReader dataReader = dbCommandReadValue.ExecuteReader();
            dataReader.Read();

            carHealthBar.value = dataReader.GetInt32(0);

            dbConnection.Close();

            dbConnection = GameDatabase.CreateActiveCharactersAndOpenDatabase();
            dbCommandReadValue = dbConnection.CreateCommand();
            dbCommandReadValue.CommandText = "SELECT * FROM ActiveCharactersTable WHERE id = " + GameLoop.FileId;
            dataReader = dbCommandReadValue.ExecuteReader();
            dataReader.Read();

            for(int i = 0; i < playerHealthBars.Length; i++){
                int index = 1 + 9 * i;
                if(dataReader.IsDBNull(index) && i == 0){
                    playerHealthBars[i].value = 0;
                    tempPlayerText += "\nCar\n";
                }
                else if(!dataReader.IsDBNull(index)){
                    playerHealthBars[i].value = dataReader.GetInt32(9 + 9 * i);
                    tempPlayerText += i == 0 ? dataReader.GetString(index) + "\nCar\n" : dataReader.GetString(index) + "\n";
                }
                else{
                    playerHealthBars[i].value = 0;
                    tempPlayerText += "\n";
                }
            }
            
            dbConnection.Close();
            playerText.text = tempPlayerText;

            // Read the database for travel (key supplies, distance) info
            dbConnection = GameDatabase.CreateTownAndOpenDatabase();
            dbCommandReadValue = dbConnection.CreateCommand();
            dbCommandReadValue.CommandText = "SELECT curTown, nextDistanceAway, nextTownName FROM TownTable WHERE id = " + GameLoop.FileId;
            dataReader = dbCommandReadValue.ExecuteReader();
            dataReader.Read();

            int townNum = dataReader.GetInt32(0), targetTownDistance = dataReader.IsDBNull(1) ? 0 : dataReader.GetInt32(1);
            string destination = dataReader.IsDBNull(2) ? "" : dataReader.GetString(2);

            dbConnection.Close();

            dbConnection = GameDatabase.CreateSavesAndOpenDatabase();
            dbCommandReadValue = dbConnection.CreateCommand();
            dbCommandReadValue.CommandText = "SELECT time, distance, food, gas, distance FROM SaveFilesTable WHERE id = " + GameLoop.FileId;
            dataReader = dbCommandReadValue.ExecuteReader();
            dataReader.Read();

            GameLoop.Hour = dataReader.GetInt32(0);

            if(GameLoop.Hour >= 21 || GameLoop.Hour <= 5){
                GameLoop.Activity = 4;
            }
            else if(GameLoop.Hour >= 18 || GameLoop.Hour <= 8){
                GameLoop.Activity = 3;
            }
            else if(GameLoop.Hour >= 16 || GameLoop.Hour <= 10){
                GameLoop.Activity = 2;
            }
            else{
                GameLoop.Activity = 1;
            }

            int time = GameLoop.Hour > 12 && GameLoop.Hour <= 24 ? GameLoop.Hour - 12 : GameLoop.Hour, distanceLeft = targetTownDistance - dataReader.GetInt32(1);
            string timing = GameLoop.Hour >= 12 && GameLoop.Hour < 24 ? " pm" : " am", activity = GameLoop.Activity == 1 ? "Low" : GameLoop.Activity == 2 ? "Medium" : GameLoop.Activity == 3 ? "High" : "Ravenous";

            supplyText.text = "Food: " + dataReader.GetInt32(2) + "kg\nGas: " +  dataReader.GetFloat(3) + " cans\nDistance to Destination: " +  distanceLeft
                            + "km\nDistance Travelled: " + dataReader.GetInt32(4) + "km\nTime: " + time + timing + "\nActivity: " + activity;
            dbConnection.Close();
        }

        /// <summary>
        /// Stop the car on the road, switching back to rest menu
        /// </summary>
        public void StopCar(){
            IDbConnection dbConnection = GameDatabase.CreateSavesAndOpenDatabase();
            IDbCommand dbCommandUpdateValue = dbConnection.CreateCommand();
            dbCommandUpdateValue.CommandText = "UPDATE SaveFilesTable SET inPhase = 2 WHERE id = " + GameLoop.FileId;
            dbCommandUpdateValue.ExecuteNonQuery();
            dbConnection.Close();

            PopupActive = true;
            timer = 0.0f;
            PrepRestScreen();
            SceneManager.LoadScene(1);
        }

        /// <summary>
        /// Stop the car on the road because of destination arrival
        /// </summary>
        public void Arrive(){
            IDbConnection dbConnection = GameDatabase.CreateSavesAndOpenDatabase();
            IDbCommand dbCommandReadValue = dbConnection.CreateCommand();
            dbCommandReadValue.CommandText = "SELECT nextTownName FROM TownTable WHERE id = " + GameLoop.FileId;
            IDataReader dataReader = dbCommandReadValue.ExecuteReader();
            dataReader.Read();

            string townName = dataReader.GetString(0);

            dbConnection.Close();

            dbConnection = GameDatabase.CreateSavesAndOpenDatabase();
            IDbCommand dbCommandUpdateValue = dbConnection.CreateCommand();
            dbCommandUpdateValue.CommandText = "UPDATE SaveFilesTable SET inPhase = 0, location = '" + townName + "' WHERE id = " + GameLoop.FileId;
            dbCommandUpdateValue.ExecuteNonQuery();
            dbConnection.Close();

            PopupActive = true;
            timer = 0.0f;
            PrepRestScreen();
            SceneManager.LoadScene(1);
        }

        /// <summary>
        /// Resume travelling from a popup
        /// </summary>
        public void ResumeTravel(){
            PopupActive = false;
            HasCharacterDied();
        }

        /// <summary>
        /// Utility function to initialize dictionaries for tracking destinations and the distance away.
        /// </summary>
        private void InitializeLogs(){
            // The key is the town BEFORE moving to the new town (ex. 0 = Montreal, starting town provides access to Ottawa at 198km away)
            // 0 = Montreal, 1 = Ottawa, 2 = Timmins, 3 = Thunder Bay, 11 = Toronto, 12 = Windsor, 13 = Chicago, 14 = Milwaukee, 15 = Minneapolis,
            // 16 = Winnipeg, 17 = Regina, 18 = Calgary, 19 = Banff, 20/38 = Kelowna, 26 = Saskatoon, 27 = Edmonton, 28 = Hinton, 29 = Kamloops 
            nextDestinationLog.Add(0, MapDestination("Ottawa", ""));
            distanceLog.Add(0, MapDistance(198, 0));
            nextDestinationLog.Add(1, MapDestination("Timmins", "Toronto"));
            distanceLog.Add(1, MapDistance(718, 450));
            nextDestinationLog.Add(2, MapDestination("Thunder Bay", ""));
            distanceLog.Add(2, MapDistance(777, 0));
            nextDestinationLog.Add(3, MapDestination("Winnipeg", ""));
            distanceLog.Add(3, MapDistance(702, 0));
            nextDestinationLog.Add(11, MapDestination("Windsor", ""));
            distanceLog.Add(11, MapDistance(376, 0));
            nextDestinationLog.Add(12, MapDestination("Chicago", ""));
            distanceLog.Add(12, MapDistance(457, 0));
            nextDestinationLog.Add(13, MapDestination("Milwaukee", ""));
            distanceLog.Add(13, MapDistance(148, 0));
            nextDestinationLog.Add(14, MapDestination("Minneapolis", ""));
            distanceLog.Add(14, MapDistance(542, 0));
            nextDestinationLog.Add(15, MapDestination("Winnipeg", ""));
            distanceLog.Add(15, MapDistance(736, 0));
            nextDestinationLog.Add(16, MapDestination("Regina", "Saskatoon"));
            distanceLog.Add(16, MapDistance(573, 786));
            nextDestinationLog.Add(17, MapDestination("Calgary", ""));
            distanceLog.Add(17, MapDistance(758, 0));
            nextDestinationLog.Add(18, MapDestination("Banff", ""));
            distanceLog.Add(18, MapDistance(127, 0));
            nextDestinationLog.Add(19, MapDestination("Kelowna", "Kamloops"));
            distanceLog.Add(19, MapDistance(480, 494));
            nextDestinationLog.Add(20, MapDestination("Vancouver", ""));
            distanceLog.Add(20, MapDistance(390, 0));
            nextDestinationLog.Add(26, MapDestination("Edmonton", ""));
            distanceLog.Add(26, MapDistance(523, 0));
            nextDestinationLog.Add(27, MapDestination("Hinton", ""));
            distanceLog.Add(27, MapDistance(288, 0));
            nextDestinationLog.Add(28, MapDestination("Kamloops", "Kelowna"));
            distanceLog.Add(28, MapDistance(519, 683));
            nextDestinationLog.Add(29, MapDestination("Vancouver", ""));
            distanceLog.Add(29, MapDistance(357, 0));
            nextDestinationLog.Add(38, MapDestination("Vancouver", ""));
            distanceLog.Add(38, MapDistance(390, 0));
            logInitialized = true;
        }

        /// <summary>
        /// Utility function to map one/two destinations as a list of destinations for a town.
        /// </summary>
        private List<string> MapDestination(string arg1, string arg2){
            List <string> destinations = new List<string>();
            destinations.Add(arg1);
            destinations.Add(arg2);
            return destinations;
        }

        /// <summary>
        /// Utility function to map one/two distances as a list of distances away for a town.
        /// </summary>
        private List<int> MapDistance(int arg1, int arg2){
            List <int> distances = new List<int>();
            distances.Add(arg1);
            distances.Add(arg2);
            return distances;
        }

        /// <summary>
        /// Select a destination
        /// </summary>
        /// <param name="id">Id of the button that was clicked.</param>
        private void UpdateButtons(){
            IDbConnection dbConnection = GameDatabase.CreateTownAndOpenDatabase();
            IDbCommand dbCommandReadValue = dbConnection.CreateCommand();
            dbCommandReadValue.CommandText = "SELECT curTown FROM TownTable WHERE id = " + GameLoop.FileId;
            IDataReader dataReader = dbCommandReadValue.ExecuteReader();
            dataReader.Read();

            int townNum = dataReader.GetInt32(0), index = townNum;
            string supplies = "";

            // If the current town number has only one way to go, disable the 2nd option
            destinationButton2.interactable = !CheckTownList(townNum);
            dbConnection.Close();

            // Determine distance based on town #
            for(int i = 1; i <= 2; i++){
                if(i == 2 && !destinationButton2.interactable){
                    destinationTexts[i-1].text = "";
                    break;
                }

                supplies = towns[i-1].SumTownResources() <= 330 ? "Light Supplies" : "Decent Supplies";
                destinationTexts[i-1].text = nextDestinationLog[townNum][i-1]+ "\n" + distanceLog[townNum][i-1] + "km\n" + supplies;
            }
        }

        /// <summary>
        /// Drive some distance, increasing distance, changing time, and damaging the car and players.
        /// </summary>
        /// <returns>True if drive had no events from updating, false if drive had events from updating</returns>
        private bool Drive(){
            IDbConnection dbConnection = GameDatabase.CreateTownAndOpenDatabase();
            IDbCommand dbCommandReadValues = dbConnection.CreateCommand();
            dbCommandReadValues.CommandText = "SELECT nextTownName, nextDistanceAway FROM TownTable WHERE id = " + GameLoop.FileId;
            IDataReader dataReader = dbCommandReadValues.ExecuteReader();
            dataReader.Read();

            string nextTown = dataReader.GetString(0);
            targetTownDistance = dataReader.GetInt32(1);
            dbConnection.Close();

            dbConnection = GameDatabase.CreateCarsAndOpenDatabase();
            dbCommandReadValues = dbConnection.CreateCommand();
            dbCommandReadValues.CommandText = "SELECT carHp, isBatteryDead, isTireFlat, engineUpgrade, miscUpgrade1 FROM CarsTable WHERE id = " + GameLoop.FileId;
            dataReader = dbCommandReadValues.ExecuteReader();
            dataReader.Read();

            int carHP = dataReader.GetInt32(0), batteryStatus = dataReader.GetInt32(1), tireStatus = dataReader.GetInt32(2), engineUpgrade = dataReader.GetInt32(3),
                gardenUpgrade = dataReader.GetInt32(4);

            dbConnection.Close();

            dbConnection = GameDatabase.CreateSavesAndOpenDatabase();
            dbCommandReadValues = dbConnection.CreateCommand();
            dbCommandReadValues.CommandText = "SELECT overallTime, speed, distance, rations, tire, battery, gas FROM SaveFilesTable WHERE id = " + GameLoop.FileId;
            dataReader = dbCommandReadValues.ExecuteReader();
            dataReader.Read();

            int overallTime = dataReader.GetInt32(0), speed = dataReader.GetInt32(1), oldDistance = dataReader.GetInt32(2), rations = dataReader.GetInt32(3);
            int newDistance = speed == 1 ? oldDistance + 40 : speed == 2 ? oldDistance + 50 : oldDistance + 60;
            newDistance = engineUpgrade == 1 ? newDistance + 10 : newDistance;
            newDistance = newDistance >= targetTownDistance ? targetTownDistance : newDistance;
            int decay = speed == 1 ? 3 : speed == 2 ? 5 : 7, tire = dataReader.GetInt32(4), battery = dataReader.GetInt32(5); 
            float gas = dataReader.GetFloat(6);

            // If the car is out of gas, broke, has a dead battery, or a flat tire, do no driving. 
            // Alternatively, if a battery or tire is available, replace but still don't drive.
            if(gas == 0f || carHP == 0){
                LaunchPopup();
                popupText.text = gas == 0f ? "The car is out of gas.\nProcure some by trading or scavenging." : "The car is broken.\nRepair the car with some scrap.";
                return false;
            }
            else if((battery > 0 && batteryStatus == 1) || (tire > 0 && tireStatus == 1)){
                LaunchPopup();
                popupText.text = battery > 0 && batteryStatus == 1 ? "You spend an hour replacing your dead battery." : "You spend an hour replacing your flat tire.";
                GameLoop.Hour++;

                if(GameLoop.Hour == 25){
                    GameLoop.Hour = 1;
                }

                IDbCommand dbCommandUpdateValues = dbConnection.CreateCommand();
                string repairCommand = battery > 0 && batteryStatus == 1 ? "UPDATE SaveFilesTable SET time = " + GameLoop.Hour + ", overallTime = " + (overallTime + 1) + ", battery = " + (battery - 1) + 
                                                " WHERE id = " + GameLoop.FileId : "UPDATE SaveFilesTable SET time = " + GameLoop.Hour + ", overallTime = " + (overallTime + 1) + ", tire = " + (tire - 1) + 
                                                " WHERE id = " + GameLoop.FileId;
                dbCommandUpdateValues.CommandText = repairCommand;
                dbCommandUpdateValues.ExecuteNonQuery();
                dbConnection.Close();

                repairCommand = battery > 0 && batteryStatus == 1 ? "UPDATE CarsTable SET isBatteryDead = 0 WHERE id = " + GameLoop.FileId : 
                                                                    "UPDATE CarsTable SET isTireFlat = 0 WHERE id = " + GameLoop.FileId;

                dbConnection = GameDatabase.CreateCarsAndOpenDatabase();
                dbCommandUpdateValues = dbConnection.CreateCommand();
                dbCommandUpdateValues.CommandText = repairCommand;
                dbCommandUpdateValues.ExecuteNonQuery();
                dbConnection.Close();

                return false;
            }
            else if(batteryStatus == 1 || tireStatus == 1){
                popupText.text = batteryStatus == 1 ? "The car has a dead battery.\nTrade for another one." : "The car has a flat tire.\nTrade for another one.";
                LaunchPopup();
                return false;
            }

            GameLoop.Hour++;

            if(GameLoop.Hour == 25){
                GameLoop.Hour = 1;
            }

            IDbCommand dbCommandUpdateValue = dbConnection.CreateCommand();
            dbCommandUpdateValue.CommandText = "UPDATE SaveFilesTable SET time = " + GameLoop.Hour + ", overallTime = " + (overallTime + 1) + ", distance = " + newDistance + 
                                               " WHERE id = " + GameLoop.FileId;
            dbCommandUpdateValue.ExecuteNonQuery();
            dbConnection.Close();

            carHP = carHP - decay > 0 ? carHP - decay : 0;

            dbConnection = GameDatabase.CreateCarsAndOpenDatabase();
            dbCommandUpdateValue = dbConnection.CreateCommand();
            dbCommandUpdateValue.CommandText = "UPDATE CarsTable SET carHP = " + carHP + " WHERE id = " + GameLoop.FileId;
            dbCommandUpdateValue.ExecuteNonQuery();

            dbConnection.Close();

            // Characters will always take some damage when travelling, regardless of rations
            dbConnection = GameDatabase.CreateSavesAndOpenDatabase();
            dbCommandReadValues = dbConnection.CreateCommand();
            dbCommandReadValues.CommandText = "SELECT * FROM SaveFilesTable LEFT JOIN ActiveCharactersTable ON SaveFilesTable.charactersId = ActiveCharactersTable.id " + 
                                              "WHERE SaveFilesTable.id = " + GameLoop.FileId;
            dataReader = dbCommandReadValues.ExecuteReader();
            dataReader.Read();

            int overallFood = dataReader.GetInt32(7), hpDecay = 0, moraleDecay = 0;
            List<int> teamHealth = new List<int>(), teamMorale = new List<int>();
            List<string> names = new List<string>();

            // Decrement food and health (and morale if applicable)
            if(overallFood > 0){
                hpDecay = rations == 1 ? 5 : rations == 2 ? 4 : 3;
            }
            else{
                hpDecay = 5;
                moraleDecay = 10;
            }

            for(int i = 0; i < 4 ; i++){
                int index = 20 + 9 * i;

                if(!dataReader.IsDBNull(index)){
                    int curHp = dataReader.GetInt32(28 + 9 * i),
                        curMorale = dataReader.GetInt32(27 + 9 * i);
                    overallFood = GameLoop.RationsMode == 1 ? overallFood - 1 : GameLoop.RationsMode == 2 ? overallFood - 2 : overallFood - 3;
                    overallFood = overallFood <= 0 ? 0 : overallFood;

                    curHp = curHp - hpDecay > 0 ? curHp - hpDecay : 0;
                    curMorale = curMorale - moraleDecay > 0 ? curMorale - moraleDecay : 0;

                    teamHealth.Add(curHp);
                    teamMorale.Add(curMorale);
                    names.Add(dataReader.GetString(index));
                }
                else{
                    teamHealth.Add(0);
                    teamMorale.Add(0);
                    names.Add("_____TEMPNULL");
                }
            }

            // If garden upgrade was found, add 1 kg of food back.
            overallFood += gardenUpgrade == 1 ? 1 : 0;
            // Each timestep consumes quarter of a gas resource
            gas -= 0.25f;

            dbCommandUpdateValue = dbConnection.CreateCommand();
            dbCommandUpdateValue.CommandText = "UPDATE SaveFilesTable SET food = " + overallFood + ", gas = " + gas + " WHERE id = " + GameLoop.FileId;
            dbCommandUpdateValue.ExecuteNonQuery();
            dbConnection.Close();

            // Update health changes
            dbConnection = GameDatabase.CreateActiveCharactersAndOpenDatabase();
            dbCommandUpdateValue = dbConnection.CreateCommand();
            dbCommandUpdateValue.CommandText = "UPDATE ActiveCharactersTable SET leaderHealth = " + teamHealth[0] + ", friend1Health = " + teamHealth[1] +
                                                ", friend2Health = " + teamHealth[2] + ", friend3Health = " + teamHealth[3] + ", leaderMorale = " + teamMorale[0] + 
                                                ", friend1Morale = " + teamMorale[1] + ", friend2Morale = " + teamMorale[2] + ", friend3Morale = " + teamMorale[3] +
                                                " WHERE id = " + GameLoop.FileId; 
            dbCommandUpdateValue.ExecuteNonQuery();
            dbConnection.Close();

            // Check if any character has died.
            if(HasCharacterDied()){
                RefreshScreen();
                return false;
            }
            RefreshScreen();

            // Transition back to town rest if distance matches the target
            if(newDistance == targetTownDistance){
                PopupActive = true;
                
                destinationPopup.SetActive(true);
                destinationPopupText.text = nextTown;
                travelViewObject.SetActive(false);
                backgroundPanel.SetActive(true);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Check if anyone in the party has died.
        /// </summary>
        /// <returns>True if someone has perished, false otherwise</returns>
        private bool HasCharacterDied(){
            IDbConnection dbConnection = GameDatabase.CreateSavesAndOpenDatabase();
            IDbCommand dbCommandReadValues = dbConnection.CreateCommand();
            dbCommandReadValues.CommandText = "SELECT leaderName, friend1Name, friend2Name, friend3Name, leaderHealth, friend1Health, friend2Health, friend3Health, leaderMorale, " +
                                              "friend1Morale, friend2Morale, friend3Morale, customIdLeader, customId1, customId2, customId3 FROM ActiveCharactersTable " + 
                                              "WHERE id = " + GameLoop.FileId;
            IDataReader dataReader = dbCommandReadValues.ExecuteReader();
            dataReader.Read();

            List<int> teamHealth = new List<int>();
            List<int> teamMorale = new List<int>();
            List<string> names = new List<string>();
            List<int> customIds = new List<int>();

            for(int i = 0; i < 4 ; i++){
                int curHp = dataReader.IsDBNull(4+i) ? 0 : dataReader.GetInt32(4+i), curMorale = dataReader.IsDBNull(8+i) ? 0 : dataReader.GetInt32(8+i);

                if(!dataReader.IsDBNull(i)){
                    teamHealth.Add(curHp);
                    teamMorale.Add(curMorale);
                    names.Add(dataReader.GetString(i));
                    customIds.Add(dataReader.GetInt32(12+i));
                }
                else{
                    teamHealth.Add(0);
                    teamMorale.Add(0);
                    names.Add("_____TEMPNULL");
                    customIds.Add(-1);
                }
            }
            dbConnection.Close();

            // Check if any character has died.
            string tempDisplayText = "";

            dbConnection = GameDatabase.CreateActiveCharactersAndOpenDatabase();
            IDbCommand dbCommandUpdateValue = dbConnection.CreateCommand();
            string tempCommand = "UPDATE ActiveCharactersTable SET leaderHealth = " + teamHealth[0] + ", friend1Health = " + teamHealth[1] +
                    ", friend2Health = " + teamHealth[2] + ", friend3Health = " + teamHealth[3] + ", leaderMorale = " + teamMorale[0] + 
                    ", friend1Morale = " + teamMorale[1] + ", friend2Morale = " + teamMorale[2] + ", friend3Morale = " + teamMorale[3]; 
            
            bool flag = false;
            List<string> deadCharacters = new List<string>();
            List<int> deadIds = new List<int>();

            for(int i = 0; i < teamHealth.Count; i++){
                // A recently dead player will have their no hp but their name wasn't recorded as _____TEMPNULL
                if(teamHealth[i] == 0 && !Equals(names[i], "_____TEMPNULL")){
                    flag = true;
                    deadCharacters.Add(names[i]);
                    deadIds.Add(customIds[i]);

                    // Leader died = game over
                    if(i == 0){
                        tempDisplayText += names[0] + " has died.";
                        tempCommand += ", leaderName = null";

                        popupText.text = tempDisplayText;
                        LaunchPopup();
                        RestMenu.LeaderName = names[0];
                        RestMenu.FriendsAlive = names.Where(s => !Equals(s, "_____TEMPNULL") && !Equals(s, names[0])).Count();

                        dbCommandUpdateValue.CommandText = tempCommand + " WHERE id = " + GameLoop.FileId;
                        dbCommandUpdateValue.ExecuteNonQuery();
                        dbConnection.Close();
                        return true; 
                    }
                    tempCommand += ", friend" + i + "Name = null";
                }
            }

            // Display characters that have died.
            if(flag){
                for(int i = 0; i < deadCharacters.Count; i++){
                    if(deadCharacters.Count == 1 && !Equals(deadCharacters[i], "_____TEMPNULL")){
                        tempDisplayText += deadCharacters[i];
                    }
                    else if(i == deadCharacters.Count - 1 && !Equals(deadCharacters[i], "_____TEMPNULL")){
                        tempDisplayText += "and " + deadCharacters[i];
                    }
                    else if(!Equals(deadCharacters[i], "_____TEMPNULL")){
                        tempDisplayText += deadCharacters[i] + ", ";
                    }
                }

                tempDisplayText += deadCharacters.Count > 1 ? " have died." : " has died.";

                popupText.text = tempDisplayText;
                LaunchPopup();

                dbCommandUpdateValue.CommandText = tempCommand + " WHERE id = " + GameLoop.FileId;
                dbCommandUpdateValue.ExecuteNonQuery();
                dbConnection.Close();

                dbConnection = GameDatabase.CreatePerishedCustomAndOpenDatabase();
                dbCommandReadValues = dbConnection.CreateCommand();
                dbCommandReadValues.CommandText = "SELECT COUNT(*) FROM PerishedCustomTable WHERE saveFileId = " + GameLoop.FileId;
                int count = Convert.ToInt32(dbCommandReadValues.ExecuteScalar()); 

                foreach(int id in deadIds){
                    if(id == -1){
                        continue;
                    }

                    IDbCommand dbCommandInsertValue = dbConnection.CreateCommand();
                    dbCommandInsertValue.CommandText = "INSERT INTO PerishedCustomTable (id, saveFileId, customCharacterId)" +
                                                        "VALUES (" + (count+1) + ", " + GameLoop.FileId + ", " + id + ")";
                    dbCommandInsertValue.ExecuteNonQuery();
                    count++;
                }
                return true;
            }

            dbCommandUpdateValue.CommandText = tempCommand + " WHERE id = " + GameLoop.FileId;
            dbCommandUpdateValue.ExecuteNonQuery();
            dbConnection.Close();

            return false;
        }

        /// <summary>
        /// Generate a random event while driving
        /// </summary>
        /// <param name="eventChance">The probability of the event happening, 44 or less guaranteed to be passed in</param>
        private void GenerateEvent(int eventChance){
            // Get difficulty, perks, and traits, some events will play differently depending on it (more loss, more damage, etc.)
            IDbConnection dbConnection = GameDatabase.CreateSavesAndOpenDatabase();
            IDbCommand dbCommandReadValue = dbConnection.CreateCommand();
            dbCommandReadValue.CommandText = "SELECT difficulty FROM SaveFilesTable WHERE id = " + GameLoop.FileId;
            IDataReader dataReader = dbCommandReadValue.ExecuteReader();
            dataReader.Read();
            int diff = dataReader.GetInt32(0);
            dbConnection.Close();

            dbConnection = GameDatabase.CreateActiveCharactersAndOpenDatabase();
            dbCommandReadValue = dbConnection.CreateCommand();
            dbCommandReadValue.CommandText = "SELECT * FROM ActiveCharactersTable WHERE id = " + GameLoop.FileId;
            dataReader = dbCommandReadValue.ExecuteReader();
            dataReader.Read();

            List<int> availablePerks = new List<int>();
            List<int> availableTraits = new List<int>();
            for(int i = 2; i <= 29; i+= 9){
                if(dataReader.IsDBNull(i-1)){
                    availablePerks.Add(-1);
                    availableTraits.Add(-1);
                    continue;
                }
                int foundPerk = dataReader.GetInt32(i), foundTrait = dataReader.GetInt32(i+1);
                availablePerks.Add(foundPerk);
                availableTraits.Add(foundTrait);
            } 
            dbConnection.Close(); 

            // 1-30 are base events, 31-40 depend on if someone in the party has a trait.
            // 4/44 possibility for a random player to take extra damage (Ex. Bob breaks a rib/leg)
            if(eventChance <= 4){
                dbConnection = GameDatabase.CreateCarsAndOpenDatabase();
                dbCommandReadValue = dbConnection.CreateCommand();
                dbCommandReadValue.CommandText = "SELECT miscUpgrade2 FROM CarsTable WHERE id = " + GameLoop.FileId;
                dataReader = dbCommandReadValue.ExecuteReader();
                dataReader.Read();

                int cushioned = dataReader.GetInt32(0);

                dbConnection.Close();

                dbConnection = GameDatabase.CreateActiveCharactersAndOpenDatabase();
                dbCommandReadValue = dbConnection.CreateCommand();
                dbCommandReadValue.CommandText = "SELECT * FROM ActiveCharactersTable WHERE id = " + GameLoop.FileId;
                dataReader = dbCommandReadValue.ExecuteReader();
                dataReader.Read();

                int rand = 0, index = 0;
                // Keep randomly picking until not a dead player
                do
                {
                    rand = Random.Range(0,4);
                    index = 1 + 9 * rand;
                } while (dataReader.IsDBNull(index));

                string name = dataReader.GetString(index);
                string[] temp = {" breaks a rib.", " breaks a leg.", " breaks an arm.", " sits down wrong."};
                int hpLoss = diff % 2 == 0 ? Random.Range(13,20) : Random.Range(5,13), curHealth = dataReader.GetInt32(index+8);
                curHealth = curHealth - hpLoss > 0 ? curHealth - hpLoss : 0;

                // Lose less HP if cushion upgrade found
                hpLoss -= cushioned == 1 ? 5 : 0;

                string commandText = "UPDATE ActiveCharactersTable SET ";
                commandText += index == 1 ? "leaderHealth = " + curHealth : "friend" + rand + "Health = " + curHealth;
                commandText += " WHERE id = " + GameLoop.FileId;

                IDbCommand dbCommandUpdateValue = dbConnection.CreateCommand();
                dbCommandUpdateValue.CommandText = commandText;
                dbCommandUpdateValue.ExecuteNonQuery();

                popupText.text = name + temp[rand];
                dbConnection.Close();
            }
            // 3/44 possibility for a random resource type decay more (ex. 10 cans of gas goes missing. Everyone blames Bob.)
            else if(eventChance <= 7){
                dbConnection = GameDatabase.CreateCarsAndOpenDatabase();
                dbCommandReadValue = dbConnection.CreateCommand();
                dbCommandReadValue.CommandText = "SELECT toolUpgrade FROM CarsTable WHERE id = " + GameLoop.FileId;
                dataReader = dbCommandReadValue.ExecuteReader();
                dataReader.Read();

                // If tool upgrade was found, treat as uneventful drive.
                if(dataReader.GetInt32(0) == 1){
                    dbConnection.Close();
                    return;
                }
                else{
                    dbConnection.Close();
                    
                    dbConnection = GameDatabase.CreateSavesAndOpenDatabase();
                    dbCommandReadValue = dbConnection.CreateCommand();
                    dbCommandReadValue.CommandText = "SELECT * FROM SaveFilesTable WHERE id = " + GameLoop.FileId;
                    dataReader = dbCommandReadValue.ExecuteReader();
                    dataReader.Read();

                    string temp = "", name = "", commandText = "UPDATE SaveFilesTable SET ";
                    int type = Random.Range(7,15), lost = diff % 2 == 0 ? Random.Range(15,30) : Random.Range(10,20), curStock = 0, rand = 0, index = 0;
                    bool breakCondition = false;
                    float curGasStock = 0;
                    List<string> tempTexts = new List<string>(){"kg of food", "cans of gas", "scrap", "dollars", "medkits", "tires", "batteries", "ammo"};
                    List<string> commandTexts = new List<string>(){"food = ", "gas = ", "scrap = ", "money = ", "medkit = ", "tire = ", "battery = ", "ammo = "};

                    // Randomize the item until it is an item in stock
                    do
                    {
                        type = Random.Range(7,15);
                        if(type == 8){
                            breakCondition = dataReader.GetFloat(type) > 0.0f;
                        }
                        else{
                            breakCondition = dataReader.GetInt32(type) > 0;
                        }
                    } while (!breakCondition);

                    if(type >= 11 && type <= 13){
                        lost = diff % 2 == 0 ? Random.Range(3,6) : Random.Range(1,3);
                    }

                    temp = tempTexts[type-7];
                    commandText += commandTexts[type-7];

                    // Gas is a float variable, requires a separate branch.
                    if(type != 8){
                        curStock = dataReader.GetInt32(type);
                        curStock = curStock - lost > 0 ? curStock - lost : 0;
                        commandText += curStock.ToString();
                        lost = lost > curStock ? curStock : lost;
                    }
                    else{
                        curGasStock = dataReader.GetFloat(type);
                        curGasStock = curGasStock - (float)(lost) > 0.0f ? curGasStock - (float)(lost) : 0.0f;
                        commandText += curGasStock.ToString();
                        lost = lost > (int)(curGasStock) ? (int)(curGasStock) : lost;
                    }
                    commandText += " WHERE id = " + GameLoop.FileId;

                    IDbCommand dbCommandUpdateValue = dbConnection.CreateCommand();
                    dbCommandUpdateValue.CommandText = commandText;
                    dbCommandUpdateValue.ExecuteNonQuery();

                    dbConnection.Close();
                    dbConnection = GameDatabase.CreateActiveCharactersAndOpenDatabase();
                    dbCommandReadValue = dbConnection.CreateCommand();
                    dbCommandReadValue.CommandText = "SELECT * FROM ActiveCharactersTable WHERE id = " + GameLoop.FileId;
                    dataReader = dbCommandReadValue.ExecuteReader();
                    dataReader.Read();

                    // Change grammar if singular for some items
                    if(lost == 1){
                        if(type == 1){
                            temp = "can of gas";
                        }
                        else if(type >= 3 && type <= 5){
                            temp = temp.Remove(temp.Length-1, 1);
                        }
                        else if(type == 6){
                            temp = "battery";
                        }
                    }

                    // Keep randomly picking until not a dead player
                    do
                    {
                        rand = Random.Range(0,4);
                        index = 1 + 9 * rand;
                    } while (dataReader.IsDBNull(index));

                    name = dataReader.GetString(index);
                    popupText.text = lost.ToString() + " " + temp + " goes missing.\nEveryone blames " + name + ".";
                } 
                dbConnection.Close();
            }
            // 3/44 possibility for the car to take more damage (ex. The car drives over some rough terrain)
            else if(eventChance <= 10){
                dbConnection = GameDatabase.CreateCarsAndOpenDatabase();
                dbCommandReadValue = dbConnection.CreateCommand();
                dbCommandReadValue.CommandText = "SELECT carHp FROM CarsTable WHERE id = " + GameLoop.FileId;
                dataReader = dbCommandReadValue.ExecuteReader();
                dataReader.Read();

                int hpLoss = diff % 2 == 0 ? Random.Range(20,30) : Random.Range(10,20), curHealth = dataReader.GetInt32(0);
                curHealth = curHealth - hpLoss > 0 ? curHealth - hpLoss : 0;
                string commandText = "UPDATE CarsTable SET carHP = " + curHealth + " WHERE id = " + GameLoop.FileId;
                
                IDbCommand dbCommandUpdateValue = dbConnection.CreateCommand();
                dbCommandUpdateValue.CommandText = commandText;
                dbCommandUpdateValue.ExecuteNonQuery();

                popupText.text = "The car struggles to drive over some terrain.";
                dbConnection.Close();
            }
            // 3/44 possibility for more resources to be found (ex. Bob finds 10 cans of gas in an abandoned car)
            else if(eventChance <= 13){
                dbConnection = GameDatabase.CreateSavesAndOpenDatabase();
                dbCommandReadValue = dbConnection.CreateCommand();
                dbCommandReadValue.CommandText = "SELECT * FROM SaveFilesTable WHERE id = " + GameLoop.FileId;
                dataReader = dbCommandReadValue.ExecuteReader();
                dataReader.Read();

                string temp = "", name = "", commandText = "UPDATE SaveFilesTable SET ";
                int type = Random.Range(7,15), gain = diff % 2 == 0 ? Random.Range(15,30) : Random.Range(10,20), curStock = 0, rand = 0, index = 0;
                float curGasStock = 0;
                List<string> tempTexts = new List<string>(){"kg of food", "cans of gas", "scrap", "dollars", "medkits", "tires", "batteries", "ammo"};
                List<string> commandTexts = new List<string>(){"food = ", "gas = ", "scrap = ", "money = ", "medkit = ", "tire = ", "battery = ", "ammo = "};

                if(type >= 11 && type <= 13){
                    gain = diff % 2 == 0 ? Random.Range(3,6) : Random.Range(1,3);
                }

                temp = tempTexts[type-7];
                commandText += commandTexts[type-7];

                if(type != 8){
                    curStock = dataReader.GetInt32(type) + gain;
                    commandText += curStock.ToString();
                }
                else{
                    curGasStock = dataReader.GetFloat(type) + (float)(gain);
                    commandText += curGasStock.ToString();
                }
                commandText += " WHERE id = " + GameLoop.FileId;

                IDbCommand dbCommandUpdateValue = dbConnection.CreateCommand();
                dbCommandUpdateValue.CommandText = commandText;
                dbCommandUpdateValue.ExecuteNonQuery();

                dbConnection.Close();
                dbConnection = GameDatabase.CreateActiveCharactersAndOpenDatabase();
                dbCommandReadValue = dbConnection.CreateCommand();
                dbCommandReadValue.CommandText = "SELECT * FROM ActiveCharactersTable WHERE id = " + GameLoop.FileId;
                dataReader = dbCommandReadValue.ExecuteReader();
                dataReader.Read();

                // Keep randomly picking until not a dead player
                do
                {
                    rand = Random.Range(0,4);
                    index = 1 + 9 * rand;
                } while (dataReader.IsDBNull(index));

                name = dataReader.GetString(index);
                popupText.text = name + " finds " + gain + " " + temp + " in an abandoned car.";
                dbConnection.Close();
            }
            // 5/44 possibility to find a new party member (ex. The party meets Bob. They have the Perk surgeon and Trait paranoid.)
            else if(eventChance <= 18){
                // Check that a slot is available.
                dbConnection = GameDatabase.CreateActiveCharactersAndOpenDatabase();
                dbCommandReadValue = dbConnection.CreateCommand();
                dbCommandReadValue.CommandText = "SELECT friend1Name, friend2Name, friend3Name, customIdLeader, customId1, customId2, customId3 FROM ActiveCharactersTable WHERE id = " + GameLoop.FileId;
                dataReader = dbCommandReadValue.ExecuteReader();
                dataReader.Read();

                List<string> names = new List<string>();
                List<int> customIds = new List<int>(){dataReader.GetInt32(3)};
                for(int i = 0; i < 3 ; i++){
                    string name = dataReader.IsDBNull(i) ? "_____TEMPNULL" : dataReader.GetString(i);
                    int id = dataReader.IsDBNull(i+4) ? -1 : dataReader.GetInt32(i+4);
                    names.Add(name);
                    customIds.Add(id);
                }

                if(names.Where(n => Equals(n, "_____TEMPNULL")).Count() > 0){
                    int perk = -1, trait = -1, acc = -1, outfit = -1, color = -1, hat = -1, idRead = -1;
                    string name = "", perkRoll = "", traitRoll = "";
                    int index = names.IndexOf(names.Where(n => Equals(n, "_____TEMPNULL")).First());
                    
                    dbConnection.Close();
                    dbConnection = GameDatabase.CreatePerishedCustomAndOpenDatabase();
                    dbCommandReadValue = dbConnection.CreateCommand();
                    dbCommandReadValue.CommandText = "SELECT COUNT(*) FROM PerishedCustomTable WHERE saveFileId = " + GameLoop.FileId;
                    int deadCount = Convert.ToInt32(dbCommandReadValue.ExecuteScalar());

                    dbConnection.Close();

                    dbConnection = GameDatabase.CreateCustomAndOpenDatabase();
                    dbCommandReadValue = dbConnection.CreateCommand();
                    dbCommandReadValue.CommandText = "SELECT COUNT(*) FROM CustomCharactersTable";
                    int customCharacterCount = Convert.ToInt32(dbCommandReadValue.ExecuteScalar());

                    // Generate randomized character - standard or out of unused custom characters because they are all either in the party or dead
                    if(diff == 1 || diff == 3 || customCharacterCount == customIds.Where(c => c != -1).Count() + deadCount){
                        perk = Random.Range(0,GamemodeSelect.Perks.Count()); 
                        trait = Random.Range(0, GamemodeSelect.Traits.Count());
                        acc = Random.Range(1,4); 
                        outfit = Random.Range(1,4); 
                        color = Random.Range(1,10); 
                        hat = Random.Range(1,9);
                        name = GamemodeSelect.RandomNames[Random.Range(0, GamemodeSelect.RandomNames.Count())];
                        perkRoll = GamemodeSelect.Perks[perk];
                        traitRoll = GamemodeSelect.Traits[trait];
                    }
                    // Generate custom character
                    else{
                        int rand = -1;

                        do
                        {
                            rand = Random.Range(0, customCharacterCount);
                        } while (customIds.Contains(rand));

                        dbCommandReadValue = dbConnection.CreateCommand();
                        dbCommandReadValue.CommandText = "SELECT id, name, perk, trait, accessory, hat, color, outfit FROM CustomCharactersTable WHERE id = " + rand;
                        dataReader = dbCommandReadValue.ExecuteReader();
                        dataReader.Read();

                        perk = dataReader.GetInt32(2);
                        trait = dataReader.GetInt32(3);
                        acc = dataReader.GetInt32(4);
                        outfit = dataReader.GetInt32(7);
                        color = dataReader.GetInt32(6);
                        hat = dataReader.GetInt32(5);
                        idRead = dataReader.GetInt32(0);
                        name = dataReader.GetString(1);
                        perkRoll = GamemodeSelect.Perks[perk];
                        traitRoll = GamemodeSelect.Traits[trait];

                        dbConnection.Close();
                        dbConnection = GameDatabase.CreateActiveCharactersAndOpenDatabase();
                    }
                    
                    string commandText = "UPDATE ActiveCharactersTable SET friend" + (index+1) + "Name = '" + name + "', friend" + (index+1) + "Perk = " + perk + 
                                         ", friend" + (index+1) + "Trait = " + trait + ", friend" + (index+1) + "Acc = " + acc + ", friend" + (index+1) + "Color = " + color + 
                                         ", friend" + (index+1) + "Hat = " + hat + ", friend" + (index+1) + "Outfit = " + outfit + ", friend" + (index+1) + "Health = 100" +
                                         ", friend" + (index+1) + "Morale = 75, customId" + (index + 1) + " = " + idRead + " WHERE id = " + GameLoop.FileId;
                    IDbCommand dbCommandUpdateValue = dbConnection.CreateCommand();
                    dbCommandUpdateValue.CommandText = commandText;
                    dbCommandUpdateValue.ExecuteNonQuery();

                    // Add a medkit if healthcare trait.
                    if(perk == 2){
                        dbConnection.Close();
                        dbConnection = GameDatabase.CreateSavesAndOpenDatabase();
                        dbCommandUpdateValue = dbConnection.CreateCommand();
                        dbCommandUpdateValue.CommandText = "UPDATE SaveFilesTable SET medkit = medkit + 1 WHERE id = " + GameLoop.FileId;
                        dbCommandUpdateValue.ExecuteNonQuery();
                    }

                    popupText.text = "The party meets " + name + " and allows them to join.\nThey have the " + perkRoll + " perk and the " + traitRoll + " trait.";
                }
                else{
                    popupText.text = "You drive by someone on the road but your car is full.";
                }
                dbConnection.Close();
            }
            // 1/44 possibility for an upgrade to be found. (ex. The party searches an abandoned car and finds nothing of interest.)
            else if(eventChance <= 19){
                // Check that a slot is available.
                dbConnection = GameDatabase.CreateCarsAndOpenDatabase();
                dbCommandReadValue = dbConnection.CreateCommand();
                dbCommandReadValue.CommandText = "SELECT wheelUpgrade, batteryUpgrade, engineUpgrade, toolUpgrade miscUpgrade1, miscUpgrade2 FROM CarsTable WHERE id = " + 
                                                 GameLoop.FileId;
                dataReader = dbCommandReadValue.ExecuteReader();
                dataReader.Read();

                List<int> curUpgrades = new List<int>(){dataReader.GetInt32(0), dataReader.GetInt32(1), dataReader.GetInt32(2), dataReader.GetInt32(3), dataReader.GetInt32(4),
                                                        dataReader.GetInt32(5)};
                // At least one slot is available.
                if(curUpgrades.Where(c => c == 0).Count() > 0){
                    int selected;
                    string found = "", commandTemp = "";

                    do
                    {
                        selected = Random.Range(0, curUpgrades.Count);
                    } while (curUpgrades[selected] != 0);

                    found = selected == 0 ? "durable tires" : selected == 1 ? "a durable battery" : selected == 2 ? "a fuel-efficient engine" : selected == 3 ? "a secure travel chest" :
                            selected == 4 ? "a travel garden" : "cushioned seating";
                    commandTemp = selected == 0 ? "wheelUpgrade = 1 " : selected == 1 ? "batteryUpgrade = 1" : selected == 2 ? "engineUpgrade = 1" : selected == 3 ? "toolUpgrade == 1" :
                                  selected == 4 ? "miscUpgrade1 = 1" : "miscUpgrade2 = 1";

                    IDbCommand dbCommandUpdateValue = dbConnection.CreateCommand();
                    dbCommandUpdateValue.CommandText = "UPDATE CarsTable SET " + commandTemp + " WHERE id = " + GameLoop.FileId;
                    dbCommandUpdateValue.ExecuteNonQuery();

                    popupText.text = "The party searches an abandoned car and finds " + found + ".";

                    dbConnection.Close();
                }
                // No slot available
                else{
                    popupText.text = "The party searches an abandoned car and finds nothing of interest.";
                }

                dbConnection.Close();
            }
            // 2/44 possibility for party-wide damage. (ex. The party cannot find clean water. Everyone is dehydrated.)
            else if(eventChance <= 21){
                dbConnection = GameDatabase.CreateActiveCharactersAndOpenDatabase();
                dbCommandReadValue = dbConnection.CreateCommand();
                dbCommandReadValue.CommandText = "SELECT * FROM ActiveCharactersTable WHERE id = " + GameLoop.FileId;
                dataReader = dbCommandReadValue.ExecuteReader();
                dataReader.Read();

                int hpLoss = diff % 2 == 0 ? Random.Range(10,15) : Random.Range(5,10);
                List<int> teamHp = new List<int>(){dataReader.GetInt32(9), dataReader.GetInt32(18), dataReader.GetInt32(27), dataReader.GetInt32(36)};
                for(int i = 0; i < teamHp.Count; i++){
                    teamHp[i] = teamHp[i] - hpLoss > 0 ? teamHp[i] - hpLoss : 0;
                }

                string commandText = "UPDATE ActiveCharactersTable SET leaderHealth = " + teamHp[0] + ", friend1Health = " + teamHp[1] + ", friend2Health = " + teamHp[2] +
                                     ", friend3Health = " + teamHp[3] + " WHERE id = " + GameLoop.FileId;
                IDbCommand dbCommandUpdateValue = dbConnection.CreateCommand();
                dbCommandUpdateValue.CommandText = commandText;
                dbCommandUpdateValue.ExecuteNonQuery();

                popupText.text = "The party cannot find clean water. Everyone is dehydrated.";

                dbConnection.Close();
            }
            // 3/44 possibility for a tire to go flat
            else if(eventChance <= 24){
                // If the car has upgraded tires, display the attempt at popping the tire.
                dbConnection = GameDatabase.CreateActiveCharactersAndOpenDatabase();
                dbCommandReadValue = dbConnection.CreateCommand();
                dbCommandReadValue.CommandText = "SELECT tireUpgrade FROM CarsTable WHERE id = " + GameLoop.FileId;
                dataReader = dbCommandReadValue.ExecuteReader();
                dataReader.Read();

                if(dataReader.GetInt32(0) != 0){
                    popupText.text = "The car goes over some rough terrain but the durable tires remain intact.";
                }
                else{
                    dbConnection.Close();

                    dbConnection = GameDatabase.CreateSavesAndOpenDatabase();
                    dbCommandReadValue = dbConnection.CreateCommand();
                    dbCommandReadValue.CommandText = "SELECT tire FROM SaveFilesTable WHERE id = " + GameLoop.FileId;
                    dataReader = dbCommandReadValue.ExecuteReader();
                    dataReader.Read();

                    int tires = dataReader.GetInt32(0);

                    // Determine if the car can still move.
                    if(tires > 0){
                        tires--;
                        IDbCommand dbCommandUpdateValue = dbConnection.CreateCommand();
                        dbCommandUpdateValue.CommandText = "UPDATE SaveFilesTable SET tire = " + tires + " WHERE id = " + GameLoop.FileId;
                        dbCommandUpdateValue.ExecuteNonQuery();
                        popupText.text = "The car goes over some rough terrain and the tire pops.\nYou replace your flat tire.";
                    }
                    else{
                        dbConnection.Close();

                        dbConnection = GameDatabase.CreateCarsAndOpenDatabase();
                        string commandText = "UPDATE CarsTable SET isTireFlat = 1 WHERE id = " + GameLoop.FileId;
                        IDbCommand dbCommandUpdateValue = dbConnection.CreateCommand();
                        dbCommandUpdateValue.CommandText = commandText;
                        dbCommandUpdateValue.ExecuteNonQuery();
                        popupText.text = "The car goes over some rough terrain and the tire pops.\nYou don't have a tire to replace.\nTrade for another one.";
                    }
                }
                dbConnection.Close();
            }
            // 3/44 possibility for a car battery to die.
            else if(eventChance <= 27){
                // If the car has upgraded battery, display the attempt at breaking.
                dbConnection = GameDatabase.CreateActiveCharactersAndOpenDatabase();
                dbCommandReadValue = dbConnection.CreateCommand();
                dbCommandReadValue.CommandText = "SELECT batteryUpgrade FROM CarsTable WHERE id = " + GameLoop.FileId;
                dataReader = dbCommandReadValue.ExecuteReader();
                dataReader.Read();

                if(dataReader.GetInt32(0) != 0){
                    popupText.text = "The car battery starts making noises but go away after some time.";
                }
                else{
                    dbConnection.Close();
                    
                    dbConnection = GameDatabase.CreateSavesAndOpenDatabase();
                    dbCommandReadValue = dbConnection.CreateCommand();
                    dbCommandReadValue.CommandText = "SELECT battery FROM SaveFilesTable WHERE id = " + GameLoop.FileId;
                    dataReader = dbCommandReadValue.ExecuteReader();
                    dataReader.Read();

                    int batteries = dataReader.GetInt32(0);

                    // Determine if the car can still move.
                    if(batteries > 0){
                        batteries--;
                        IDbCommand dbCommandUpdateValue = dbConnection.CreateCommand();
                        dbCommandUpdateValue.CommandText = "UPDATE SaveFilesTable SET battery = " + batteries + " WHERE id = " + GameLoop.FileId;
                        dbCommandUpdateValue.ExecuteNonQuery();
                        popupText.text = "There is smoke coming from the hood - the car battery is dead.\nYou replace your dead battery.";
                    }
                    else{
                        dbConnection.Close();

                        dbConnection = GameDatabase.CreateCarsAndOpenDatabase();
                        string commandText = "UPDATE CarsTable SET isBatteryDead = 1 WHERE id = " + GameLoop.FileId;
                        IDbCommand dbCommandUpdateValue = dbConnection.CreateCommand();
                        dbCommandUpdateValue.CommandText = commandText;
                        dbCommandUpdateValue.ExecuteNonQuery();
                        popupText.text = "There is smoke coming from the hood - the car battery is dead.\nYou don't have a battery to replace.\nTrade for another one.";
                    }
                }
                dbConnection.Close();
            }
            // 3/44 possibility for someone (other than the leader) with low morale to ditch. Cases where morale is high, treat as a typical drive with no evet
            else if(eventChance <= 30){
                dbConnection = GameDatabase.CreateActiveCharactersAndOpenDatabase();
                dbCommandReadValue = dbConnection.CreateCommand();
                dbCommandReadValue.CommandText = "SELECT * FROM ActiveCharactersTable WHERE id = " + GameLoop.FileId;
                dataReader = dbCommandReadValue.ExecuteReader();
                dataReader.Read();

                List<int> morale = new List<int>();
                for(int i = 10; i <= 28 ; i+= 9){
                    if(!dataReader.IsDBNull(i)){
                        int moraleRead = dataReader.IsDBNull(i+7) ? -1 : dataReader.GetInt32(i+7);
                        morale.Add(moraleRead);
                    }
                }

                int lowMorale = morale.Where(m => m >= 0 && m <= 20).Count();
                if(lowMorale > 0){
                    int lowestIndex = morale.IndexOf(morale.Min()), nameIndex = lowestIndex == 0 ? 10 : lowestIndex == 1 ? 19 : 28;
                    string name = dataReader.GetString(nameIndex), commandText = "UPDATE ActiveCharactersTable SET ";
                    commandText += lowestIndex == 0 ? "friend1Name = null " : lowestIndex == 1 ? "friend2Name = null " : "friend3Name = null ";
                    commandText += "WHERE id = " + GameLoop.FileId;

                    IDbCommand dbCommandUpdateValue = dbConnection.CreateCommand();
                    dbCommandUpdateValue.CommandText = commandText;
                    dbCommandUpdateValue.ExecuteNonQuery();
                    dbConnection.Close();

                    popupText.text = "In despair, " + name + " ditches the party, saying their chances are better without the party.";
                }
                else{
                    dbConnection.Close();
                    return;
                }
            }
            
            // 2/44 possibility for musician characters to raise party morale (ex. Bob serenades the party, reminding them of better times. The party is in high spirits.)
            else if(eventChance <= 32 && availablePerks.Where(p => p == 5).Count() > 0){
                // Get the name of the member who has the musician trait
                int nameIndex = availablePerks.IndexOf(5);
                List<int> partyMorale = new List<int>();
                nameIndex = nameIndex == 0 ? 1 : nameIndex == 1 ? 10 : nameIndex == 2 ? 19 : 28;

                dbConnection = GameDatabase.CreateActiveCharactersAndOpenDatabase();
                dbCommandReadValue = dbConnection.CreateCommand();
                dbCommandReadValue.CommandText = "SELECT * FROM ActiveCharactersTable WHERE id = " + GameLoop.FileId;
                dataReader = dbCommandReadValue.ExecuteReader();
                dataReader.Read();

                string name = dataReader.GetString(nameIndex);
                string commandText = "UPDATE ActiveCharactersTable SET ";
                int moraleGain = diff % 2 == 0 ? 5 : 10;

                // Raise only for players who are not dead (ie. name is not null in db)
                for(int i = 0; i < 4; i++){
                    if(!dataReader.IsDBNull(1+9*i)){
                        int moraleFound = dataReader.GetInt32(8+9*i) + moraleGain > 100 ? 100 : dataReader.GetInt32(8+9*i) + moraleGain;
                        commandText += i == 0 ? "leaderMorale = " + moraleFound : ", friend" + i + "Morale = " + moraleFound;
                    }
                }
                commandText += " WHERE id = " + GameLoop.FileId;
                IDbCommand dbCommandUpdateValue = dbConnection.CreateCommand();
                dbCommandUpdateValue.CommandText = commandText;
                dbCommandUpdateValue.ExecuteNonQuery();
                dbConnection.Close();

                popupText.text = name + " serenades the party with a guitar, reminding them of better times.\nThe party is in high spirits.";
            }
            // 2/44 possibility for bandits to lower party morale (ex. Bob attempts to rob a helpless group but is caught and drags the party with him. The party feels guilty.)
            else if(eventChance <= 34 && availableTraits.Where(t => t == 3).Count() > 0){
                // Get the name of the member who has the bandit trait
                int nameIndex = availableTraits.IndexOf(3);
                List<int> partyMorale = new List<int>();
                nameIndex = nameIndex == 0 ? 1 : nameIndex == 1 ? 10 : nameIndex == 2 ? 19 : 28;

                dbConnection = GameDatabase.CreateActiveCharactersAndOpenDatabase();
                dbCommandReadValue = dbConnection.CreateCommand();
                dbCommandReadValue.CommandText = "SELECT * FROM ActiveCharactersTable WHERE id = " + GameLoop.FileId;
                dataReader = dbCommandReadValue.ExecuteReader();
                dataReader.Read();

                string name = dataReader.GetString(nameIndex);
                string commandText = "UPDATE ActiveCharactersTable SET ";
                int moraleLoss = diff % 2 == 0 ? 10 : 5;

                // Raise only for players who are not dead (ie. name is not null in db)
                for(int i = 0; i < 4; i++){
                    if(!dataReader.IsDBNull(1+9*i)){
                        int moraleFound = dataReader.GetInt32(8+9*i) - moraleLoss > 0 ? dataReader.GetInt32(8+9*i) - moraleLoss : 0;
                        commandText += i == 0 ? "leaderMorale = " + moraleFound : ", friend" + i + "Morale = " + moraleFound;
                    }
                }
                commandText += " WHERE id = " + GameLoop.FileId;
                IDbCommand dbCommandUpdateValue = dbConnection.CreateCommand();
                dbCommandUpdateValue.CommandText = commandText;
                dbCommandUpdateValue.ExecuteNonQuery();
                dbConnection.Close();

                popupText.text = name + " attempts to rob a helpless group but is caught and drags the party with them.\nThe party is forced to flee and feels guilty.";
            } 
            // 2/44 possibility for hot headed characters to lower another character's hp. (ex. Bob, annoyed with Ann for a minor issue, lashes out mid-argument.)
            else if(eventChance <= 36 && availableTraits.Where(t => t == 4).Count() > 0){
                // Get the name of the first member who has the hot-headed trait
                int nameIndex = availableTraits.IndexOf(4), hurtMember = 0;
                nameIndex = nameIndex == 0 ? 1 : nameIndex == 1 ? 10 : nameIndex == 2 ? 19 : 28;

                dbConnection = GameDatabase.CreateActiveCharactersAndOpenDatabase();
                dbCommandReadValue = dbConnection.CreateCommand();
                dbCommandReadValue.CommandText = "SELECT * FROM ActiveCharactersTable WHERE id = " + GameLoop.FileId;
                dataReader = dbCommandReadValue.ExecuteReader();
                dataReader.Read();

                // Select a living party member to hurt, not including their self.
                do
                {
                    hurtMember = Random.Range(0,4);
                } while (hurtMember == availableTraits.IndexOf(4) && !dataReader.IsDBNull(1+9*hurtMember));

                string name = dataReader.GetString(nameIndex), hurtName = dataReader.GetString(1+9*hurtMember);
                int hpLoss = diff % 2 == 0 ? 10 : 5, hurtHP = dataReader.GetInt32(9+9*hurtMember) - hpLoss > 0 ? dataReader.GetInt32(9+9*hurtMember) - hpLoss : 0;
                string commandText = "UPDATE ActiveCharactersTable SET ";
                commandText += hurtMember == 0 ? "leaderHealth = " + hurtHP : "friend" + hurtMember + "Health = " + hurtHP;

                commandText += " WHERE id = " + GameLoop.FileId;
                IDbCommand dbCommandUpdateValue = dbConnection.CreateCommand();
                dbCommandUpdateValue.CommandText = commandText;
                dbCommandUpdateValue.ExecuteNonQuery();
                dbConnection.Close();

                popupText.text = name + ", annoyed with " + hurtName + " for a minor issue, lashes out mid-argument.";
            }
            // 2/44 possibility for surgeon characters to fully heal an injured character (ex. Bob's medical skills come in handy for mid-drive surgery on Ann)
            else if(eventChance <= 38 && availablePerks.Where(p => p == 3).Count() > 0){
                // Get the name of the first member who has the surgeon trait
                int nameIndex = availablePerks.IndexOf(3), healMember = 0;
                nameIndex = nameIndex == 0 ? 1 : nameIndex == 1 ? 10 : nameIndex == 2 ? 19 : 28;

                dbConnection = GameDatabase.CreateActiveCharactersAndOpenDatabase();
                dbCommandReadValue = dbConnection.CreateCommand();
                dbCommandReadValue.CommandText = "SELECT * FROM ActiveCharactersTable WHERE id = " + GameLoop.FileId;
                dataReader = dbCommandReadValue.ExecuteReader();
                dataReader.Read();

                // Select a living party member to heal, not including their self.
                do
                {
                    healMember = Random.Range(0,4);
                } while (healMember == availableTraits.IndexOf(4) && dataReader.IsDBNull(1+9*healMember));

                string name = dataReader.GetString(nameIndex), healName = dataReader.GetString(1+9*healMember);
                int hpGain = diff % 2 == 0 ? 5 : 10, healHP = dataReader.GetInt32(9+9*healMember) + hpGain > 100 ? 100 : dataReader.GetInt32(9+9*healMember) + hpGain;
                string commandText = "UPDATE ActiveCharactersTable SET ";
                commandText += healMember == 0 ? "leaderHealth = " + healHP : "friend" + healMember + "Health = " + healHP;

                commandText += " WHERE id = " + GameLoop.FileId;
                IDbCommand dbCommandUpdateValue = dbConnection.CreateCommand();
                dbCommandUpdateValue.CommandText = commandText;
                dbCommandUpdateValue.ExecuteNonQuery();
                dbConnection.Close();

                popupText.text = name + "'s medical skills come in handy using medicinal herbs to treat " + healName + ".";
            } 
            // 2/44 possibility for creative/programmer characters to act (ex. Bob has a creative solution for a car upgrade and succeeds/fails.)
            // Uses an extra roll to determine positive/negative.
            else if(eventChance <= 40 && (availableTraits.Where(t => t == 5).Count() > 0 || availablePerks.Where(p => p == 4).Count() > 0)){
                // Get the name of the first member who has the creative OR programmer trait
                int nameIndex = availableTraits.Where(t => t == 5).Count() > 0 ? availableTraits.IndexOf(5) : availablePerks.IndexOf(4), healMember = 0;
                nameIndex = nameIndex == 0 ? 1 : nameIndex == 1 ? 10 : nameIndex == 2 ? 19 : 28;
                string solType = availableTraits.Where(t => t == 5).Count() > 0 ? "creative" : "systematic and thought-out";

                dbConnection = GameDatabase.CreateActiveCharactersAndOpenDatabase();
                dbCommandReadValue = dbConnection.CreateCommand();
                dbCommandReadValue.CommandText = "SELECT * FROM ActiveCharactersTable WHERE id = " + GameLoop.FileId;
                dataReader = dbCommandReadValue.ExecuteReader();
                dataReader.Read();

                string name = dataReader.GetString(nameIndex), healName = dataReader.GetString(1+9*healMember);
                dbConnection.Close();

                // Check that a slot is available.
                dbConnection = GameDatabase.CreateCarsAndOpenDatabase();
                dbCommandReadValue = dbConnection.CreateCommand();
                dbCommandReadValue.CommandText = "SELECT wheelUpgrade, batteryUpgrade, engineUpgrade, toolUpgrade miscUpgrade1, miscUpgrade2 FROM CarsTable WHERE id = " + 
                                                 GameLoop.FileId;
                dataReader = dbCommandReadValue.ExecuteReader();
                dataReader.Read();

                List<int> curUpgrades = new List<int>(){dataReader.GetInt32(0), dataReader.GetInt32(1), dataReader.GetInt32(2), dataReader.GetInt32(3), dataReader.GetInt32(4),
                                                        dataReader.GetInt32(5),};

                // 1/4 chance for creative, 1/2 for programmer.
                int successRoll = availableTraits.Where(t => t == 5).Count() > 0 ? Random.Range(0,4) : Random.Range(0,2);
                // Check for success, then check if a slot is available. Otherwise an uneventful drive.
                if(successRoll == 0){
                    popupText.text = name + " has a " + solType + " solution for a car upgrade but fails.";
                }
                else if(curUpgrades.Where(c => c == 0).Count() > 0){
                    int selected;
                    string commandTemp = "";

                    do
                    {
                        selected = Random.Range(0, curUpgrades.Count);
                    } while (curUpgrades[selected] != 0);

                    commandTemp = selected == 0 ? "wheelUpgrade = 1 " : selected == 1 ? "batteryUpgrade = 1" : selected == 2 ? "engineUpgrade = 1" : selected == 3 ? "toolUpgrade == 1" :
                                selected == 4 ? "miscUpgrade1 = 1" : "miscUpgrade2 = 1";

                    IDbCommand dbCommandUpdateValue = dbConnection.CreateCommand();
                    dbCommandUpdateValue.CommandText = "UPDATE CarsTable SET " + commandTemp + " WHERE id = " + GameLoop.FileId;
                    dbCommandUpdateValue.ExecuteNonQuery();
                    
                    popupText.text = name + " has a " + solType + " solution for a car upgrade and succeeds.";
                }
                else{
                    return;
                }
                dbConnection.Close();
            }   
            // 2/44 possibility for a combat event to occur if travelling with higher or more activity
            else if(eventChance <= 42 && GameLoop.Activity >= 3){
                popupText.text = "You suddenly find yourself surrounded by mutants.";
                Debug.Log("Trigger combat event here");
            }
            // 2/44 possibility for someone to be pulled out of the car and left for dead if travelling with ravenous activity
            // Morale will determine if member fights them off.
            else if(eventChance <= 44 && GameLoop.Activity == 4){
                dbConnection = GameDatabase.CreateActiveCharactersAndOpenDatabase();
                dbCommandReadValue = dbConnection.CreateCommand();
                dbCommandReadValue.CommandText = "SELECT * FROM ActiveCharactersTable WHERE id = " + GameLoop.FileId;
                dataReader = dbCommandReadValue.ExecuteReader();
                dataReader.Read();

                List<int> morale = new List<int>();
                int selected;

                for(int i = 10; i <= 28 ; i+= 9){
                    if(!dataReader.IsDBNull(i)){
                        int moraleRead = dataReader.IsDBNull(i+7) ? -1 : dataReader.GetInt32(i+7);
                        morale.Add(moraleRead);
                    }
                }

                // Select a living party member to atttack, not including the leader
                do
                {
                    selected = Random.Range(1,4);
                } while (!dataReader.IsDBNull(1+9*selected));

                int nameIndex = selected == 0 ? 10 : selected == 1 ? 19 : 28;
                string name = dataReader.GetString(nameIndex), commandText = "UPDATE ActiveCharactersTable SET ";

                if(morale[nameIndex+7] < 40){
                    commandText += selected == 0 ? "friend1Name = null " : selected == 1 ? "friend2Name = null " : "friend3Name = null ";
                    commandText += "WHERE id = " + GameLoop.FileId;

                    IDbCommand dbCommandUpdateValue = dbConnection.CreateCommand();
                    dbCommandUpdateValue.CommandText = commandText;
                    dbCommandUpdateValue.ExecuteNonQuery();
                    dbConnection.Close();

                    popupText.text = name + " is pulled out of the car and is unable to fight back against the mutants.";
                }
                else{
                    dbConnection.Close();
                    popupText.text = "Mutants attempt to pull " + name + " out of the car, but fail to do so.";
                }
            }

            RefreshScreen();
            LaunchPopup();
        }


        /// <summary>
        /// Utility function to check if a town is a one-way town (has only one other destination connecting to it)
        /// </summary>
        private bool CheckTownList(int townNum){
            List<int> oneWayTowns = new List<int>(){0,2,3,11,12,13,14,15,17,18,20,26,27,29};
            return oneWayTowns.Contains(townNum);
        }

        /// <summary>
        /// Utility function to prep the rest screen to have only screen 0 be visible when re-enabling.
        /// </summary>
        private void PrepRestScreen(){
            for(int i = 0; i < restScreens.Length; i++){
                restScreens[i].SetActive(i == 0);
            }
        }

        /// <summary>
        /// Utility function to launch popups
        /// </summary>
        private void LaunchPopup(){
            popup.SetActive(true);
            PopupActive = true;
        }
    }
}

