using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using UnityEngine;
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

                            // Random chance of generating an event or rerun the coroutine
                            if(eventChance <= 30){
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
            dbCommandReadValue.CommandText = "SELECT * FROM SaveFilesTable WHERE id = " + GameLoop.FileId;
            IDataReader dataReader = dbCommandReadValue.ExecuteReader();
            dataReader.Read();

            int curDistance = dataReader.GetInt32(3);
            dbConnection.Close();
            
            // Update town database with new town rolls.
            dbConnection = GameDatabase.CreateTownAndOpenDatabase();
            dbCommandReadValue = dbConnection.CreateCommand();
            dbCommandReadValue.CommandText = "SELECT * FROM TownTable WHERE id = " + GameLoop.FileId;
            dataReader = dbCommandReadValue.ExecuteReader();
            dataReader.Read();

            int oldTownNum = dataReader.GetInt32(27);
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
            popup.SetActive(true);
            PopupActive = true;

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
            dbCommandReadValue.CommandText = "SELECT * FROM CarsTable WHERE id = " + GameLoop.FileId;
            IDataReader dataReader = dbCommandReadValue.ExecuteReader();
            dataReader.Read();

            carHealthBar.value = dataReader.GetInt32(1);

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
            dbCommandReadValue.CommandText = "SELECT * FROM TownTable WHERE id = " + GameLoop.FileId;
            dataReader = dbCommandReadValue.ExecuteReader();
            dataReader.Read();

            int townNum = dataReader.GetInt32(27), targetTownDistance = dataReader.IsDBNull(28) ? 0 : dataReader.GetInt32(28);
            string destination = dataReader.IsDBNull(29) ? "" : dataReader.GetString(29);

            dbConnection.Close();

            dbConnection = GameDatabase.CreateSavesAndOpenDatabase();
            dbCommandReadValue = dbConnection.CreateCommand();
            dbCommandReadValue.CommandText = "SELECT * FROM SaveFilesTable WHERE id = " + GameLoop.FileId;
            dataReader = dbCommandReadValue.ExecuteReader();
            dataReader.Read();

            GameLoop.Hour = dataReader.GetInt32(15);

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

            int time = GameLoop.Hour > 12 && GameLoop.Hour <= 24 ? GameLoop.Hour - 12 : GameLoop.Hour, distanceLeft = targetTownDistance - dataReader.GetInt32(3);
            string timing = GameLoop.Hour >= 12 && GameLoop.Hour < 24 ? " pm" : " am", activity = GameLoop.Activity == 1 ? "Low" : GameLoop.Activity == 2 ? "Medium" : GameLoop.Activity == 3 ? "High" : "Ravenous";

            supplyText.text = "Food: " + dataReader.GetInt32(7) + "kg\nGas: " +  dataReader.GetFloat(8) + " cans\nDistance to Destination: " +  distanceLeft
                            + "km\nDistance Travelled: " + dataReader.GetInt32(3) + "km\nTime: " + time + timing + "\nActivity: " + activity;
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
            dbCommandReadValue.CommandText = "SELECT * FROM TownTable WHERE id = " + GameLoop.FileId;
            IDataReader dataReader = dbCommandReadValue.ExecuteReader();
            dataReader.Read();

            string townName = dataReader.GetString(29);

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
            dbCommandReadValue.CommandText = "SELECT * FROM TownTable WHERE id = " + GameLoop.FileId;
            IDataReader dataReader = dbCommandReadValue.ExecuteReader();
            dataReader.Read();

            int townNum = dataReader.GetInt32(27), index = townNum;
            string supplies = "";

            // If the current town number has only one way to go, disable the 2nd option
            destinationButton2.interactable = !CheckTownList(townNum);
            dbConnection.Close();

            dbConnection = GameDatabase.CreateSavesAndOpenDatabase();
            dbCommandReadValue = dbConnection.CreateCommand();
            dbCommandReadValue.CommandText = "SELECT * FROM SaveFilesTable WHERE id = " + GameLoop.FileId;
            dataReader = dbCommandReadValue.ExecuteReader();
            dataReader.Read();

            // Determine distance based on town #
            for(int i = 1; i <= 2; i++){
                if(i == 2 && !destinationButton2.interactable){
                    destinationTexts[i-1].text = "";
                    break;
                }

                supplies = towns[i-1].SumTownResources() <= 330 ? "Light Supplies" : "Decent Supplies";
                destinationTexts[i-1].text = nextDestinationLog[townNum][i-1]+ "\n" + distanceLog[townNum][i-1] + "km\n" + supplies;
            }
            dbConnection.Close();
        }

        /// <summary>
        /// Drive some distance, increasing distance, changing time, and damaging the car and players.
        /// </summary>
        /// <returns>True if drive had no events from updating, false if drive had events from updating</returns>
        private bool Drive(){
            IDbConnection dbConnection = GameDatabase.CreateTownAndOpenDatabase();
            IDbCommand dbCommandReadValues = dbConnection.CreateCommand();
            dbCommandReadValues.CommandText = "SELECT * FROM TownTable WHERE id = " + GameLoop.FileId;
            IDataReader dataReader = dbCommandReadValues.ExecuteReader();
            dataReader.Read();

            string nextTown = dataReader.GetString(29);
            targetTownDistance = dataReader.GetInt32(28);

            dbConnection.Close();

            dbConnection = GameDatabase.CreateCarsAndOpenDatabase();
            dbCommandReadValues = dbConnection.CreateCommand();
            dbCommandReadValues.CommandText = "SELECT * FROM CarsTable WHERE id = " + GameLoop.FileId;
            dataReader = dbCommandReadValues.ExecuteReader();
            dataReader.Read();

            int carHP = dataReader.GetInt32(1), batteryStatus = dataReader.GetInt32(8), tireStatus = dataReader.GetInt32(9);

            dbConnection.Close();

            // If the car is broken, do no driving.
            if(carHP == 0){
                popup.SetActive(true);
                popupText.text = "The car is broken.\nRepair the car with some scrap.";
                PopupActive = true;
                return false;
            }

            dbConnection = GameDatabase.CreateSavesAndOpenDatabase();
            dbCommandReadValues = dbConnection.CreateCommand();
            dbCommandReadValues.CommandText = "SELECT * FROM SaveFilesTable WHERE id = " + GameLoop.FileId;
            dataReader = dbCommandReadValues.ExecuteReader();
            dataReader.Read();

            int overallTime = dataReader.GetInt32(16), speed = dataReader.GetInt32(18), oldDistance  = dataReader.GetInt32(3), rations = dataReader.GetInt32(17);
            int newDistance = speed == 1 ? oldDistance + 40 : speed == 2 ? oldDistance + 50 : oldDistance + 60;
            newDistance = newDistance >= targetTownDistance ? targetTownDistance : newDistance;
            int decay = speed == 1 ? 3 : speed == 2 ? 5 : 7, tire = dataReader.GetInt32(12), battery = dataReader.GetInt32(13); 
            float gas = dataReader.GetFloat(8);

            // If the car is out of gas, has a dead battery, or a flat tire, do no driving. Alternatively, if a battery or tire is available, replace but still don't drive.
            if(gas == 0f){
                popup.SetActive(true);
                popupText.text = "The car is out of gas.\nProcure some by trading or scavenging.";
                PopupActive = true;
                return false;
            }
            else if(battery > 0 && batteryStatus == 1){
                popup.SetActive(true);
                popupText.text = "You spend an hour replacing your dead battery.";
                PopupActive = true;

                GameLoop.Hour++;

                if(GameLoop.Hour == 25){
                    GameLoop.Hour = 1;
                }

                IDbCommand dbCommandUpdateValues = dbConnection.CreateCommand();
                dbCommandUpdateValues.CommandText = "UPDATE SaveFilesTable SET time = " + GameLoop.Hour + ", overallTime = " + (overallTime + 1) + ", battery = " + (battery - 1) + 
                                                " WHERE id = " + GameLoop.FileId;
                dbCommandUpdateValues.ExecuteNonQuery();
                dbConnection.Close();

                dbConnection = GameDatabase.CreateCarsAndOpenDatabase();
                dbCommandUpdateValues = dbConnection.CreateCommand();
                dbCommandUpdateValues.CommandText = "UPDATE CarsTable SET isBatteryDead = 0 WHERE id = " + GameLoop.FileId;
                dbCommandUpdateValues.ExecuteNonQuery();
                dbConnection.Close();

                return false;
            }
            else if(tire > 0 && tireStatus == 1){
                popup.SetActive(true);
                popupText.text = "You spend an hour replacing your flat tire.";
                PopupActive = true;
                GameLoop.Hour++;

                if(GameLoop.Hour == 25){
                    GameLoop.Hour = 1;
                }

                IDbCommand dbCommandUpdateValues = dbConnection.CreateCommand();
                dbCommandUpdateValues.CommandText = "UPDATE SaveFilesTable SET time = " + GameLoop.Hour + ", overallTime = " + (overallTime + 1) + ", tire = " + (tire - 1) + 
                                                " WHERE id = " + GameLoop.FileId;
                dbCommandUpdateValues.ExecuteNonQuery();
                dbConnection.Close();

                dbConnection = GameDatabase.CreateCarsAndOpenDatabase();
                dbCommandUpdateValues = dbConnection.CreateCommand();
                dbCommandUpdateValues.CommandText = "UPDATE CarsTable SET isTireFlat = 0 WHERE id = " + GameLoop.FileId;
                dbCommandUpdateValues.ExecuteNonQuery();
                dbConnection.Close();

                return false;
            }
            else if(batteryStatus == 1){
                popup.SetActive(true);
                popupText.text = "The car has a dead battery.\nTrade for another one.";
                PopupActive = true;
                return false;
            }
            else if(tireStatus == 1){
                popup.SetActive(true);
                popupText.text = "The car has a flat tire.\nTrade for another one.";
                PopupActive = true;
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
            List<int> teamHealth = new List<int>();
            List<int> teamMorale = new List<int>();
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
            dbCommandReadValues.CommandText = "SELECT * FROM SaveFilesTable LEFT JOIN ActiveCharactersTable ON SaveFilesTable.charactersId = ActiveCharactersTable.id " + 
                                              "WHERE SaveFilesTable.id = " + GameLoop.FileId;
            IDataReader dataReader = dbCommandReadValues.ExecuteReader();
            dataReader.Read();

            List<int> teamHealth = new List<int>();
            List<int> teamMorale = new List<int>();
            List<string> names = new List<string>();

            for(int i = 0; i < 4 ; i++){
                int index = 20 + 9 * i, curHp = dataReader.IsDBNull(28 + 9 * i) ? 0 : dataReader.GetInt32(28 + 9 * i), 
                    curMorale = dataReader.IsDBNull(28 + 9 * i) ? 0 : dataReader.GetInt32(27 + 9 * i);

                if(!dataReader.IsDBNull(index)){
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

            for(int i = 0; i < teamHealth.Count; i++){
                int index = 20 + 9 * i;

                // A recently dead player will have their no hp but their name wasn't recorded as _____TEMPNULL
                if(teamHealth[i] == 0 && !Equals(names[i], "_____TEMPNULL")){
                    flag = true;
                    deadCharacters.Add(names[i]);

                    // Leader died = game over
                    if(i == 0){
                        tempDisplayText += names[0] + " has died.";
                        tempCommand += ", leaderName = null";

                        popupText.text = tempDisplayText;
                        PopupActive = true;
                        popup.SetActive(true);
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
                PopupActive = true;
                popup.SetActive(true);

                dbCommandUpdateValue.CommandText = tempCommand + " WHERE id = " + GameLoop.FileId;
                dbCommandUpdateValue.ExecuteNonQuery();
                dbConnection.Close();
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
        /// <param name="eventChance">The probability of the event happening, 30 or less guaranteed to be passed in</param>
        private void GenerateEvent(int eventChance){
            // Get difficulty, some events will play differently depending on it (more loss, more damage, etc.)
            IDbConnection dbConnection = GameDatabase.CreateActiveCharactersAndOpenDatabase();
            IDbCommand dbCommandReadValue = dbConnection.CreateCommand();
            dbCommandReadValue.CommandText = "SELECT * FROM SaveFilesTable WHERE id = " + GameLoop.FileId;
            IDataReader dataReader = dbCommandReadValue.ExecuteReader();
            dataReader.Read();

            int diff = dataReader.GetInt32(4);

            dbConnection.Close();

            // 2/30 possibility for a random player to take extra damage (Ex. Bob breaks a rib/leg)
            if(eventChance <= 2){
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
                rand = Random.Range(0,4);
                string[] temp = {" breaks a rib.", " breaks a leg.", " breaks an arm.", " sits down wrong."};
                int hpLoss = diff % 2 == 0 ? Random.Range(13,20) : Random.Range(5,13), curHealth = dataReader.GetInt32(index+8);
                curHealth = curHealth - hpLoss > 0 ? curHealth - hpLoss : 0;

                string commandText = "UPDATE SaveFilesTable SET ";
                commandText += index == 9 ? "leaderHealth = " + curHealth : index == 18 ? "friend1Health = " + curHealth : index == 27 
                                          ? "friend2Health = " + curHealth : "friend3Health = " + curHealth;
                commandText += " WHERE id = " + GameLoop.FileId;

                IDbCommand dbCommandUpdateValue = dbConnection.CreateCommand();
                dbCommandUpdateValue.CommandText = commandText;
                dbCommandUpdateValue.ExecuteNonQuery();

                popupText.text = name + temp[rand];
                dbConnection.Close();
            }
            // 2/30 possibility for a random resource type decay more (ex. 10 cans of gas goes missing. Everyone blames Bob.)
            else if(eventChance <= 4){
                dbConnection = GameDatabase.CreateSavesAndOpenDatabase();
                dbCommandReadValue = dbConnection.CreateCommand();
                dbCommandReadValue.CommandText = "SELECT * FROM SaveFilesTable WHERE id = " + GameLoop.FileId;
                dataReader = dbCommandReadValue.ExecuteReader();
                dataReader.Read();

                string temp = "", name = "", commandText = "UPDATE SaveFilesTable SET ";
                int type = Random.Range(7,15), lost = diff % 2 == 0 ? Random.Range(15,30) : Random.Range(10,20), curStock = 0, rand = 0, index = 0;
                float curGasStock = 0;
                List<string> tempTexts = new List<string>(){"kg of food", "cans of gas", "scrap", "dollars", "medkits", "tires", "batteries", "ammo"};
                List<string> commandTexts = new List<string>(){"food = ", "gas = ", "scrap = ", "money = ", "medkit = ", "tire = ", "battery = ", "ammo = "};

                if(type >= 11 && type <= 13){
                    lost = diff % 2 == 0 ? Random.Range(3,6) : Random.Range(1,3);
                }

                temp = tempTexts[type-7];
                commandText += commandTexts[type-7];

                if(type != 8){
                    curStock = dataReader.GetInt32(type);
                    curStock = curStock - lost > 0 ? curStock - lost : 0;
                    commandText += curStock.ToString();
                }
                else{
                    curGasStock = dataReader.GetFloat(type);
                    curGasStock = curGasStock - (float)(lost) > 0.0f ? curGasStock - (float)(lost) : 0.0f;
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
                popupText.text = lost.ToString() + " " + temp + " goes missing.\nEveryone blames " + name + "."; 
                dbConnection.Close();
            }
            // 2/30 possibility for the car to take more damage (ex. The car drives over some rough terrain)
            else if(eventChance <= 6){
                dbConnection = GameDatabase.CreateCarsAndOpenDatabase();
                dbCommandReadValue = dbConnection.CreateCommand();
                dbCommandReadValue.CommandText = "SELECT * FROM CarsTable WHERE id = " + GameLoop.FileId;
                dataReader = dbCommandReadValue.ExecuteReader();
                dataReader.Read();

                int hpLoss = diff % 2 == 0 ? Random.Range(20,30) : Random.Range(10,20), curHealth = dataReader.GetInt32(1);
                curHealth = curHealth - hpLoss > 0 ? curHealth - hpLoss : 0;
                string commandText = "UPDATE CarsTable SET carHP = " + curHealth + " WHERE id = " + GameLoop.FileId;
                
                IDbCommand dbCommandUpdateValue = dbConnection.CreateCommand();
                dbCommandUpdateValue.CommandText = commandText;
                dbCommandUpdateValue.ExecuteNonQuery();

                popupText.text = "The car struggles to drive over some terrain.";
                dbConnection.Close();
            }
            // 2/30 possibility for more resources to be found (ex. Bob finds 10 cans of gas in an abandoned car)
            else if(eventChance <= 8){
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
            // 3/30 possibility to find a new party member (ex. The party meets Bob. They have the Perk surgeon and Trait paranoid. Are they allowed to join?)
            else if(eventChance <= 11){
                // Check that a slot is available.
            }
            // 1/30 possibility for an upgrade to be found. (ex. Bob finds durable tires in an abandoned car.)
            else if(eventChance <= 12){
                // Check that a slot is available.
            }
            // 1/30 possibility for party-wide damage. (ex. The party cannot find clean water. Everyone is dehydrated.)
            else if(eventChance <= 13){
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
            // 1/30 possibility for a tire to go flat
            else if(eventChance <= 14){
                dbConnection = GameDatabase.CreateSavesAndOpenDatabase();
                dbCommandReadValue = dbConnection.CreateCommand();
                dbCommandReadValue.CommandText = "SELECT * FROM SaveFilesTable WHERE id = " + GameLoop.FileId;
                dataReader = dbCommandReadValue.ExecuteReader();
                dataReader.Read();

                int tires = dataReader.GetInt32(12);

                dbConnection.Close();

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

                dbConnection.Close();
            }
            // 1/30 possibility for a car battery to die.
            else if(eventChance <= 15){
                dbConnection = GameDatabase.CreateSavesAndOpenDatabase();
                dbCommandReadValue = dbConnection.CreateCommand();
                dbCommandReadValue.CommandText = "SELECT * FROM SaveFilesTable WHERE id = " + GameLoop.FileId;
                dataReader = dbCommandReadValue.ExecuteReader();
                dataReader.Read();

                int batteries = dataReader.GetInt32(13);

                dbConnection.Close();

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

                dbConnection.Close();
            }
            else{
                popupText.text = "Picking " + eventChance + " was a 3/10 chance out of 100 and 1/30 chance out of the 30 available!";
            }
            
            RefreshScreen();
            popup.SetActive(true);
            PopupActive = true;
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
    }
}

