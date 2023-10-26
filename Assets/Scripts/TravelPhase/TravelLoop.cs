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
using CombatPhase;

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

        [Tooltip("Destination popup image")]
        [SerializeField]
        private Image destinationImageDisplay;

        [Tooltip("Destination popup text")]
        [SerializeField]
        private TextMeshProUGUI destinationPopupText;

        [Tooltip("Travel view object")]
        [SerializeField]
        private GameObject travelViewObject;

        [Tooltip("Background panel object")]
        [SerializeField]
        private GameObject backgroundPanel;

        [Tooltip("Event generator for the road")]
        [SerializeField]
        private EventGenerator eventGenerator;

        [Tooltip("Rest menu screens - element 0 will be kept active in the background")]
        [SerializeField]
        private GameObject[] restScreens;

        // To track if a popup is active, will restrict when driving loop occurs.
        public static bool PopupActive = false, InFinalCombat = false;
        // To track the new town number and the distance away
        private int newTown, targetTownDistance = 0, currentDistance;
        // To track if the log of destinations has been initialized
        private bool logInitialized = false;
        // To track generated towns
        private List<Town> towns = new List<Town>();
        // To manage destinations
        private Dictionary<int, List<int>> distanceLog = new Dictionary<int, List<int>>();
        private Dictionary<int, List<string>> nextDestinationLog = new Dictionary<int, List<string>>();
        // To time the driving loop
        public static float Timer = 0.0f;
        // Flag for going to combat
        public static bool GoingToCombat = false;

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
                Timer += Time.deltaTime;

                if(!AnimateEnvironment.NearingTown && IsCloseToDestination()){
                    AnimateEnvironment.NearingTown = true;
                }

                if(Timer >= 8.0f){
                    if(Drive()){
                        int eventChance = Random.Range(1,101);
                        Debug.Log("Event rolled: " + eventChance + "completed " + System.DateTime.Now.ToString());

                        // 44/100 chance of generating an event
                        if(eventChance <= 44){
                            string msg = eventGenerator.GenerateEvent(eventChance);
                            if(!msg.Equals("")){
                                LaunchPopup(msg);
                                if(msg.Equals("You suddenly find yourself surrounded by mutants.")){
                                    GoingToCombat = true;
                                }
                            }
                            RefreshScreen();
                        }
                    }
                    Timer = 0.0f;
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
            IDbConnection dbConnection = GameDatabase.OpenDatabase();
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
            dbConnection = GameDatabase.OpenDatabase();
            dbCommandReadValue = dbConnection.CreateCommand();
            dbCommandReadValue.CommandText = "SELECT curTown FROM TownTable WHERE id = " + GameLoop.FileId;
            dataReader = dbCommandReadValue.ExecuteReader();
            dataReader.Read();

            int oldTownNum = dataReader.GetInt32(0);
            targetTownDistance = curDistance + distanceLog[oldTownNum][id-1];
            string destinationTown = nextDestinationLog[oldTownNum][id-1];

            newTown = id == 1 ? oldTownNum + id : oldTownNum + 10;

            // Special cases due to mapping
            newTown = newTown == 4 ? 16 : newTown;

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
                                                ", nextDistanceAway = " + targetTownDistance + ", nextTownName = '" + destinationTown + "', prevTown = " + oldTownNum + 
                                                " WHERE id = " + GameLoop.FileId;
            dbCommandUpdateValue.ExecuteNonQuery();
            dbConnection.Close();

            towns.Clear();
            int distanceLeft = targetTownDistance - curDistance;
            LaunchPopup(distanceLeft.ToString() + " km to " + destinationTown);
            RefreshScreen();
        }

        /// <summary>
        /// Refresh the screen upon loading the travel UI.
        /// </summary>
        public void RefreshScreen(){
            // Read the database for party info
            string tempPlayerText = "";

            IDbConnection dbConnection = GameDatabase.OpenDatabase();
            IDbCommand dbCommandReadValue = dbConnection.CreateCommand();
            dbCommandReadValue.CommandText = "SELECT carHp FROM CarsTable WHERE id = " + GameLoop.FileId;
            IDataReader dataReader = dbCommandReadValue.ExecuteReader();
            dataReader.Read();

            carHealthBar.value = dataReader.GetInt32(0);

            dbConnection.Close();

            dbConnection = GameDatabase.OpenDatabase();
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
            dbConnection = GameDatabase.OpenDatabase();
            dbCommandReadValue = dbConnection.CreateCommand();
            dbCommandReadValue.CommandText = "SELECT curTown, nextDistanceAway, nextTownName FROM TownTable WHERE id = " + GameLoop.FileId;
            dataReader = dbCommandReadValue.ExecuteReader();
            dataReader.Read();

            int townNum = dataReader.GetInt32(0), targetTownDistance = dataReader.IsDBNull(1) ? 0 : dataReader.GetInt32(1);
            string destination = dataReader.IsDBNull(2) ? "" : dataReader.GetString(2);

            dbConnection.Close();

            dbConnection = GameDatabase.OpenDatabase();
            dbCommandReadValue = dbConnection.CreateCommand();
            dbCommandReadValue.CommandText = "SELECT time, distance, food, gas, distance FROM SaveFilesTable WHERE id = " + GameLoop.FileId;
            dataReader = dbCommandReadValue.ExecuteReader();
            dataReader.Read();

            GameLoop.Hour = dataReader.GetInt32(0);
            GameLoop.Activity = GameLoop.Hour >= 21 || GameLoop.Hour <= 5 ? 4 : GameLoop.Hour >= 18 || GameLoop.Hour <= 8 ? 3 : GameLoop.Hour >= 16 || GameLoop.Hour <= 10 ? 2 : 1;

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
            IDbConnection dbConnection = GameDatabase.OpenDatabase();
            IDbCommand dbCommandUpdateValue = dbConnection.CreateCommand();
            dbCommandUpdateValue.CommandText = "UPDATE SaveFilesTable SET inPhase = 2 WHERE id = " + GameLoop.FileId;
            dbCommandUpdateValue.ExecuteNonQuery();
            dbConnection.Close();

            PopupActive = true;
            Timer = 0.0f;
            PrepRestScreen();
            SceneManager.LoadScene(1);
        }

        /// <summary>
        /// Stop the car on the road because of destination arrival
        /// </summary>
        public void Arrive(){
            IDbConnection dbConnection = GameDatabase.OpenDatabase();
            IDbCommand dbCommandReadValue = dbConnection.CreateCommand();
            dbCommandReadValue.CommandText = "SELECT nextTownName FROM TownTable WHERE id = " + GameLoop.FileId;
            IDataReader dataReader = dbCommandReadValue.ExecuteReader();
            dataReader.Read();

            string townName = dataReader.GetString(0);

            dbConnection.Close();

            dbConnection = GameDatabase.OpenDatabase();
            IDbCommand dbCommandUpdateValue = dbConnection.CreateCommand();
            dbCommandUpdateValue.CommandText = "UPDATE SaveFilesTable SET inPhase = 0, location = '" + townName + "' WHERE id = " + GameLoop.FileId;
            dbCommandUpdateValue.ExecuteNonQuery();
            dbConnection.Close();

            PopupActive = true;
            Timer = 0.0f;
            PrepRestScreen();

            // Final combat section
            if(townName.Equals("Vancouver")){
                InFinalCombat = true;
                CombatManager.PrevMenuRef = this.gameObject;
                StartCoroutine(GameLoop.LoadAsynchronously(3));
            }
            else{
                restScreens[0].transform.parent.GetComponent<RestMenu>().RefreshScreen();
                SceneManager.LoadScene(1);
            }
        }

        /// <summary>
        /// Resume travelling from a popup
        /// </summary>
        public void ResumeTravel(){
            PopupActive = false;
            HasCharacterDied();

            if(GoingToCombat){
                CombatManager.PrevMenuRef = this.gameObject;
                StartCoroutine(GameLoop.LoadAsynchronously(3));
            }
        }

        /// <summary>
        /// Return general car status
        /// </summary>
        /// <returns> True if the car has no battery, a flat tire, or has no hp</returns>
        public static bool IsCarBroken(){
            IDbConnection dbConnection = GameDatabase.OpenDatabase();
            IDbCommand dbCommandReadValue = dbConnection.CreateCommand();
            dbCommandReadValue.CommandText = "SELECT carHp, isBatteryDead, isTireFlat FROM CarsTable WHERE id = " + GameLoop.FileId;
            IDataReader dataReader = dbCommandReadValue.ExecuteReader();
            dataReader.Read();

            bool status = dataReader.GetInt32(0) == 0 || dataReader.GetInt32(1) == 1 || dataReader.GetInt32(2) == 1;

            dbConnection.Close();
            return status;
        }

        /// <summary>
        /// Check if party is close to destination
        /// </summary>
        /// <returns>True if within 1hr of travel, false otherwise</returns>
        private bool IsCloseToDestination(){
            IDbConnection dbConnection = GameDatabase.OpenDatabase();
            IDbCommand dbCommandReadValue = dbConnection.CreateCommand();
            dbCommandReadValue.CommandText = "SELECT distance, speed FROM SaveFilesTable WHERE id = " + GameLoop.FileId;
            IDataReader dataReader = dbCommandReadValue.ExecuteReader();
            dataReader.Read();

            int curDistance = dataReader.GetInt32(0), speed = dataReader.GetInt32(1), speedActual = speed == 1 ? 65 : speed == 2 ? 80 : 95;

            dbConnection.Close();

            dbConnection = GameDatabase.OpenDatabase();
            dbCommandReadValue = dbConnection.CreateCommand();
            dbCommandReadValue.CommandText = "SELECT nextDistanceAway FROM TownTable WHERE id = " + GameLoop.FileId;
            dataReader = dbCommandReadValue.ExecuteReader();
            dataReader.Read();

            int target = dataReader.GetInt32(0);

            dbConnection.Close();

            return curDistance < target && curDistance >= target - speedActual;
        }

        /// <summary>
        /// Select a destination
        /// </summary>
        /// <param name="id">Id of the button that was clicked.</param>
        private void UpdateButtons(){
            IDbConnection dbConnection = GameDatabase.OpenDatabase();
            IDbCommand dbCommandReadValue = dbConnection.CreateCommand();
            dbCommandReadValue.CommandText = "SELECT curTown FROM TownTable WHERE id = " + GameLoop.FileId;
            IDataReader dataReader = dbCommandReadValue.ExecuteReader();
            dataReader.Read();

            int townNum = dataReader.GetInt32(0), index = townNum;
            string supplies = "";

            // If on-route to Vancouver, no need to proceed with updating
            if(townNum == 21 || townNum == 39 || townNum == 30){
                return;
            }

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
            IDbConnection dbConnection = GameDatabase.OpenDatabase();
            IDbCommand dbCommandReadValues = dbConnection.CreateCommand();
            dbCommandReadValues.CommandText = "SELECT nextTownName, nextDistanceAway FROM TownTable WHERE id = " + GameLoop.FileId;
            IDataReader dataReader = dbCommandReadValues.ExecuteReader();
            dataReader.Read();

            string nextTown = dataReader.GetString(0), tempStr = "";
            targetTownDistance = dataReader.GetInt32(1);
            dbConnection.Close();

            dbConnection = GameDatabase.OpenDatabase();
            dbCommandReadValues = dbConnection.CreateCommand();
            dbCommandReadValues.CommandText = "SELECT carHp, isBatteryDead, isTireFlat, engineUpgrade, miscUpgrade1 FROM CarsTable WHERE id = " + GameLoop.FileId;
            dataReader = dbCommandReadValues.ExecuteReader();
            dataReader.Read();

            int carHP = dataReader.GetInt32(0), batteryStatus = dataReader.GetInt32(1), tireStatus = dataReader.GetInt32(2), engineUpgrade = dataReader.GetInt32(3),
                gardenUpgrade = dataReader.GetInt32(4);

            dbConnection.Close();

            dbConnection = GameDatabase.OpenDatabase();
            dbCommandReadValues = dbConnection.CreateCommand();
            dbCommandReadValues.CommandText = "SELECT overallTime, speed, distance, rations, tire, battery, gas FROM SaveFilesTable WHERE id = " + GameLoop.FileId;
            dataReader = dbCommandReadValues.ExecuteReader();
            dataReader.Read();

            int overallTime = dataReader.GetInt32(0), speed = dataReader.GetInt32(1), oldDistance = dataReader.GetInt32(2), rations = dataReader.GetInt32(3),
                speedActual = speed == 1 ? 65 : speed == 2 ? 80 : 95;
            int newDistance = oldDistance + speedActual, decay = speed == 1 ? 3 : speed == 2 ? 5 : 7, tire = dataReader.GetInt32(4), battery = dataReader.GetInt32(5);
            float gas = dataReader.GetFloat(6);

            newDistance = engineUpgrade == 1 ? newDistance + 10 : newDistance;
            newDistance = newDistance >= targetTownDistance ? targetTownDistance : newDistance;

            // If the car is out of gas, broke, has a dead battery, or a flat tire, do no driving. 
            // Alternatively, if a battery or tire is available, replace but still don't drive.
            if(gas == 0f || carHP == 0){
                tempStr = gas == 0f ? "The car is out of gas.\nProcure some by trading or scavenging." : "The car is broken.\nRepair the car with some scrap.";
                LaunchPopup(tempStr);
                dbConnection.Close();
                return false;
            }
            else if((battery > 0 && batteryStatus == 1) || (tire > 0 && tireStatus == 1)){
                tempStr = battery > 0 && batteryStatus == 1 ? "You spend an hour replacing your dead battery." : "You spend an hour replacing your flat tire.";
                LaunchPopup(tempStr);
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

                dbConnection = GameDatabase.OpenDatabase();
                dbCommandUpdateValues = dbConnection.CreateCommand();
                dbCommandUpdateValues.CommandText = repairCommand;
                dbCommandUpdateValues.ExecuteNonQuery();
                dbConnection.Close();

                return false;
            }
            else if(batteryStatus == 1 || tireStatus == 1){
                tempStr = batteryStatus == 1 ? "The car has a dead battery.\nTrade for another one." : "The car has a flat tire.\nTrade for another one.";
                LaunchPopup(tempStr);
                dbConnection.Close();
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

            dbConnection = GameDatabase.OpenDatabase();
            dbCommandUpdateValue = dbConnection.CreateCommand();
            dbCommandUpdateValue.CommandText = "UPDATE CarsTable SET carHP = " + carHP + " WHERE id = " + GameLoop.FileId;
            dbCommandUpdateValue.ExecuteNonQuery();

            dbConnection.Close();

            // Characters will always take some damage when travelling, regardless of rations
            dbConnection = GameDatabase.OpenDatabase();
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
            dbConnection = GameDatabase.OpenDatabase();
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
                
                dbConnection = GameDatabase.OpenDatabase();
                dbCommandReadValues = dbConnection.CreateCommand();
                dbCommandReadValues.CommandText = "SELECT prevTown, curTown FROM TownTable WHERE id = " + GameLoop.FileId;
                dataReader = dbCommandReadValues.ExecuteReader();
                dataReader.Read();

                int prevTown = dataReader.GetInt32(0), curTown = dataReader.GetInt32(1);

                dbConnection.Close();

                destinationImageDisplay.sprite = GameLoop.RetrieveMapImage(prevTown, curTown);
                destinationPopup.SetActive(true);
                destinationPopupText.text = nextTown;
                travelViewObject.SetActive(false);
                backgroundPanel.SetActive(true);
                AnimateEnvironment.NearingTown = false;
                return false;
            }

            return true;
        }

        /// <summary>
        /// Check if anyone in the party has died.
        /// </summary>
        /// <returns>True if someone has perished, false otherwise</returns>
        private bool HasCharacterDied(){
            IDbConnection dbConnection = GameDatabase.OpenDatabase();
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

            dbConnection = GameDatabase.OpenDatabase();
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

                        LaunchPopup(tempDisplayText);
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

                LaunchPopup(tempDisplayText);

                dbCommandUpdateValue.CommandText = tempCommand + " WHERE id = " + GameLoop.FileId;
                dbCommandUpdateValue.ExecuteNonQuery();
                dbConnection.Close();

                dbConnection = GameDatabase.OpenDatabase();
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
        /// Utility function to check if a town is a one-way town (has only one other destination connecting to it)
        /// </summary>
        /// <returns>True if the town is in the one way town list, false otherwise</returns>
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
        /// Utility function to launch popups
        /// </summary>
        /// <param name="msg">The message to display on the popup</param>
        private void LaunchPopup(string msg){
            popupText.text = msg;
            popup.SetActive(true);
            PopupActive = true;
        }
    }
}

