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
    public class MoveEnvironment : MonoBehaviour
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

        private Coroutine coroutine;
        public static bool PopupActive = false;

        private int newTown, targetTownDistance = 0;
        private bool coroutineRunning = false, logInitialized = false;
        private List<Town> towns = new List<Town>();
        private Dictionary<int, List<int>> distanceLog = new Dictionary<int, List<int>>();
        private Dictionary<int, List<string>> nextDestinationLog = new Dictionary<int, List<string>>();

        void OnEnable(){
            if(!logInitialized){
                InitializeLogs();
            }
            GenerateTowns();
            RefreshScreen();

            if(!coroutineRunning){
                coroutine = StartCoroutine(Transition());
            }
        }

        void Update(){
            // Sept 1 - Pausing breaks the coroutine sequence
            Debug.Log(coroutineRunning);
        }

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
        /// Utility function to initialize dictionaries for tracking destinations and the distance away.
        /// </summary>
        private void InitializeLogs(){
            // The key is the town BEFORE moving to the new town (ex. 0 = Montreal, starting town provides access to Ottawa at 198km away)
            // 0 = Montreal, 1 = Ottawa, 2 = Timmins, 3 = Thunder Bay, 11 = Toronto, 12 = Windsor, 13 = Chicago, 14 = Milwaukee, 15 = Minneapolis,
            // 16 = Winnipeg, 17 = Regina, 18 = Calgary, 19 = Banff, 20 = Kelowna, 26 = Saskatoon, 27 = Edmonton, 28 = Hinton, 29 = Kamloops 
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
            nextDestinationLog.Add(19, MapDestination("Kelowna", ""));
            distanceLog.Add(19, MapDistance(480, 0));
            nextDestinationLog.Add(20, MapDestination("Vancouver", ""));
            distanceLog.Add(20, MapDistance(390, 0));
            nextDestinationLog.Add(26, MapDestination("Edmonton", ""));
            distanceLog.Add(26, MapDistance(523, 0));
            nextDestinationLog.Add(27, MapDestination("Hinton", ""));
            distanceLog.Add(27, MapDistance(288, 0));
            nextDestinationLog.Add(28, MapDestination("Kamloops", ""));
            distanceLog.Add(28, MapDistance(519, 0));
            nextDestinationLog.Add(29, MapDestination("Vancouver", ""));
            distanceLog.Add(29, MapDistance(357, 0));
            // Placeholder for no destination defined.
            nextDestinationLog.Add(30, MapDestination("", ""));
            distanceLog.Add(30, MapDistance(0, 0));
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
            destinationButton2.interactable = !(townNum == 0);
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
                if(!dataReader.IsDBNull(index)){
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

            // If the current town number has only one way to go, disable the 2nd option
            destinationButton2.interactable = !(townNum == 0);
            dbConnection.Close();

            dbConnection = GameDatabase.CreateSavesAndOpenDatabase();
            dbCommandReadValue = dbConnection.CreateCommand();
            dbCommandReadValue.CommandText = "SELECT * FROM SaveFilesTable WHERE id = " + GameLoop.FileId;
            dataReader = dbCommandReadValue.ExecuteReader();
            dataReader.Read();

            int time = GameLoop.Hour > 12 && GameLoop.Hour <= 24 ? GameLoop.Hour - 12 : GameLoop.Hour, distanceLeft = targetTownDistance - dataReader.GetInt32(3);
            string timing = GameLoop.Hour >= 12 && GameLoop.Hour < 24 ? " pm" : " am", activity = GameLoop.Activity == 1 ? "Low" : GameLoop.Activity == 2 ? "Medium" : GameLoop.Activity == 3 ? "High" : "Ravenous";

            supplyText.text = "Food: " + dataReader.GetInt32(7) + "kg\nGas: " +  dataReader.GetFloat(8) + "L\nDistance to Destination: " +  distanceLeft
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

            StopCoroutine(coroutine);
            coroutineRunning = false;
            SceneManager.LoadScene(1);
        }

        /// <summary>
        /// Resume travelling from a popup
        /// </summary>
        public void ResumeTravel(){
            PopupActive = false;
            coroutine = StartCoroutine(Transition());
        }

        /// <summary>
        /// Drive some distance, increasing distance, changing time, and damaging the car and players.
        /// </summary>
        /// <returns>True if drive was successful, false otherwise</returns>
        private bool Drive(){
            IDbConnection dbConnection = GameDatabase.CreateTownAndOpenDatabase();
            IDbCommand dbCommandReadValues = dbConnection.CreateCommand();
            dbCommandReadValues.CommandText = "SELECT * FROM TownTable WHERE id = " + GameLoop.FileId;
            IDataReader dataReader = dbCommandReadValues.ExecuteReader();
            dataReader.Read();

            targetTownDistance = dataReader.GetInt32(28);

            dbConnection.Close();

            dbConnection = GameDatabase.CreateCarsAndOpenDatabase();
            dbCommandReadValues = dbConnection.CreateCommand();
            dbCommandReadValues.CommandText = "SELECT * FROM CarsTable WHERE id = " + GameLoop.FileId;
            dataReader = dbCommandReadValues.ExecuteReader();
            dataReader.Read();

            int carHP = dataReader.GetInt32(1);

            dbConnection.Close();

            // If the car is broken, do no driving.
            if(carHP == 0){
                popup.SetActive(true);
                popupText.text = "The car is broken.\nRepair the car with some scrap.";
                PopupActive = true;
                StopCoroutine(coroutine);
                coroutineRunning = false;
                return false;
            }

            GameLoop.Hour++;

            if(GameLoop.Hour == 25){
                GameLoop.Hour = 1;
            }

            dbConnection = GameDatabase.CreateSavesAndOpenDatabase();
            dbCommandReadValues = dbConnection.CreateCommand();
            dbCommandReadValues.CommandText = "SELECT * FROM SaveFilesTable WHERE id = " + GameLoop.FileId;
            dataReader = dbCommandReadValues.ExecuteReader();
            dataReader.Read();

            int overallTime = dataReader.GetInt32(16), speed = dataReader.GetInt32(18), oldDistance  = dataReader.GetInt32(3), rations = dataReader.GetInt32(17);
            int newDistance = speed == 1 ? oldDistance + 40 : speed == 2 ? oldDistance + 50 : oldDistance + 60;
            newDistance = newDistance >= targetTownDistance ? targetTownDistance : newDistance;
            int decay = speed == 1 ? 3 : speed == 2 ? 5 : 7;
            float gas = dataReader.GetFloat(8);

            // If the car is out of gas, do no driving.
            if(gas == 0f){
                popup.SetActive(true);
                popupText.text = "The car is out of gas.\nProcure some by trading or scavenging.";
                PopupActive = true;
                StopCoroutine(coroutine);
                coroutineRunning = false;
                return false;
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
                                                ", friend1Morale = " + teamMorale[1] + ", friend2Morale = " + teamMorale[2] + ", friend3Morale = " + teamMorale[3]; 
            dbCommandUpdateValue.ExecuteNonQuery();
            dbConnection.Close();

            // Check if any character has died.
            string tempDisplayText = "";

            dbConnection = GameDatabase.CreateActiveCharactersAndOpenDatabase();
            dbCommandUpdateValue = dbConnection.CreateCommand();
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

                        StopCoroutine(coroutine);
                        coroutineRunning = false;

                        popupText.text = tempDisplayText;
                        popup.SetActive(true);
                        RestMenu.LeaderName = names[0];
                        RestMenu.FriendsAlive = names.Where(s => !Equals(s, "_____TEMPNULL") && !Equals(s, names[0])).Count();

                        dbCommandUpdateValue.CommandText = tempCommand + " WHERE id = " + GameLoop.FileId;
                        dbCommandUpdateValue.ExecuteNonQuery();
                        dbConnection.Close();
                        return false; 
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
                StopCoroutine(coroutine);
                coroutineRunning = false;

                popupText.text = tempDisplayText;
                PopupActive = true;
                popup.SetActive(true);
            }

            dbCommandUpdateValue.CommandText = tempCommand + " WHERE id = " + GameLoop.FileId;
            dbCommandUpdateValue.ExecuteNonQuery();
            dbConnection.Close();  

            RefreshScreen();

            // Transition back to town rest if distance matches the target
            if(newDistance == targetTownDistance){
                Debug.Log("Destination reached.");
                StopCoroutine(coroutine);
            }

            return true;
        }

        /// <summary>
        /// Move 1 timestep in travelling to the next destination.
        /// </summary>
        private IEnumerator Transition(){
            coroutineRunning = true;
            if(PopupActive){
                if(coroutine != null){
                    StopCoroutine(coroutine);
                }
            }
            else{
                // Initial delay for the player to have a chance to cancel out
                yield return new WaitForSeconds(5.0f);
                Debug.Log("test");
                yield return new WaitForSeconds(1.0f);
                Debug.Log("test.");
                yield return new WaitForSeconds(1.0f);
                Debug.Log("test..");
                yield return new WaitForSeconds(1.0f);
                Debug.Log("test...");
                yield return new WaitForSeconds(1.0f);
                Debug.Log("test....");
                yield return new WaitForSeconds(1.0f);

                // Travel, deal a little damage to the car and the party depending on pace and rationing
                if(Drive()){
                    // Driving must have happened for an event to occur.
                    int eventChance = Random.Range(1,101);

                    // Random chance of generating an event or rerun the coroutine
                    if(eventChance <= 10){
                        popup.SetActive(true);
                        popupText.text = "this is a test";
                        PopupActive = true;
                        StopCoroutine(coroutine);
                    }
                    else{
                        StartCoroutine(Transition());
                    }
                }
                else{
                    StartCoroutine(Transition());
                }
            }
            coroutineRunning = false;
        }
    }
}

