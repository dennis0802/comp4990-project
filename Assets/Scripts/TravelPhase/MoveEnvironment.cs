using System.Collections;
using System.Collections.Generic;
using System.Data;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Mono.Data.Sqlite;
using UI;
using TMPro;
using Database;

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
        private bool coroutineRunning = false;
        private List<Town> towns = new List<Town>();
        private List<int> distances = new List<int>();

        void OnEnable(){
            GenerateTowns();
            RefreshScreen();
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

            dbConnection.Close();
            
            // Update town database with new town rolls.
            dbConnection = GameDatabase.CreateTownAndOpenDatabase();
            IDbCommand dbCommandReadValue = dbConnection.CreateCommand();
            dbCommandReadValue.CommandText = "SELECT * FROM TownTable WHERE id = " + GameLoop.FileId;
            IDataReader dataReader = dbCommandReadValue.ExecuteReader();
            dataReader.Read();

            newTown = dataReader.GetInt32(27) + id;
            targetTownDistance = distances[id-1];

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
                                                " WHERE id = " + GameLoop.FileId;
            dbCommandUpdateValue.ExecuteNonQuery();
            dbConnection.Close();

            towns.Clear();
            popup.SetActive(true);
            PopupActive = true;

            RefreshScreen();
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

            int townNum = dataReader.GetInt32(27);
            string destination = "", supplies = "";

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
                    distances.Add(0);
                    break;
                }
                switch(townNum+i){
                    case 1:
                        destination = "Ottawa";
                        distances.Add(198);
                        break;
                }
                supplies = towns[i-1].SumTownResources() <= 330 ? "Light Supplies" : "Decent Supplies";
                destinationTexts[i-1].text = destination + "\n" + distances[i-1] + "km\n" + supplies;
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

            int townNum = dataReader.GetInt32(27);
            string destination = "";

            // If the current town number has only one way to go, disable the 2nd option
            destinationButton2.interactable = !(townNum == 0);
            dbConnection.Close();

            dbConnection = GameDatabase.CreateSavesAndOpenDatabase();
            dbCommandReadValue = dbConnection.CreateCommand();
            dbCommandReadValue.CommandText = "SELECT * FROM SaveFilesTable WHERE id = " + GameLoop.FileId;
            dataReader = dbCommandReadValue.ExecuteReader();
            dataReader.Read();

            int time = GameLoop.Hour > 12 && GameLoop.Hour <= 24 ? GameLoop.Hour - 12 : GameLoop.Hour;
            
            // Determine distance based on town #
            for(int i = 1; i <= 2; i++){
                if(i == 2 && !destinationButton2.interactable){
                    destinationTexts[i-1].text = "";
                    break;
                }

                switch(townNum){
                    case 1:
                        destination = "Ottawa";
                        targetTownDistance = 198;
                        break;
                }
            }

            int distanceLeft = targetTownDistance - dataReader.GetInt32(3);
            string timing = GameLoop.Hour >= 12 && GameLoop.Hour < 24 ? " pm" : " am", activity = GameLoop.Activity == 1 ? "Low" : GameLoop.Activity == 2 ? "Medium" : GameLoop.Activity == 3 ? "High" : "Ravenous";

            supplyText.text = "Food: " + dataReader.GetInt32(7) + "\nGas: " +  dataReader.GetFloat(8) + "\nDistance to Destination: " +  distanceLeft
                            + "km\nDistance Travelled: " + dataReader.GetInt32(3) + "km\nTime: " + time + timing + "\nActivity: " + activity;
            popupText.text = distanceLeft.ToString() + " km to " + destination;
            dbConnection.Close();

            if(!coroutineRunning){
                coroutine = StartCoroutine(Transition());
            }
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
        private void Drive(){
            IDbConnection dbConnection = GameDatabase.CreateCarsAndOpenDatabase();
            IDbCommand dbCommandReadValues = dbConnection.CreateCommand();
            dbCommandReadValues.CommandText = "SELECT * FROM CarsTable WHERE id = " + GameLoop.FileId;
            IDataReader dataReader = dbCommandReadValues.ExecuteReader();
            dataReader.Read();

            int carHP = dataReader.GetInt32(1);

            dbConnection.Close();

            // If the car is broken, do no driving.
            if(carHP == 0){
                popup.SetActive(true);
                popupText.text = "The car is broken.\nRepair the car with some scrap.";
                PopupActive = true;
                StopCoroutine(coroutine);
                return;
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

            int overallTime = dataReader.GetInt32(16), speed = dataReader.GetInt32(18), oldDistance  = dataReader.GetInt32(3);
            int newDistance = speed == 1 ? oldDistance + 40 : speed == 2 ? oldDistance + 50 : oldDistance + 60;
            newDistance = newDistance >= targetTownDistance ? targetTownDistance : newDistance;
            int decay = speed == 1 ? 3 : speed == 2 ? 5 : 7;

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

            Debug.Log("Damage the characters here");
            RefreshScreen();

            // Transition back to town rest if distance matches the target
        }

        /// <summary>
        /// Move 1 timestep in travelling to the next destination.
        /// </summary>
        private IEnumerator Transition(){
            coroutineRunning = true;
            if(PopupActive){
                coroutineRunning = false;
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
                Debug.Log("test.....");

                // Travel, deal a little damage to the car and the party depending on pace and rationing
                Drive();

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
            coroutineRunning = false;
        }
    }
}

