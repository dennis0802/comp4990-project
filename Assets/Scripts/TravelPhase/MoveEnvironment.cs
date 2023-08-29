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

        private Coroutine coroutine;

        private int newTown;
        private List<Town> towns = new List<Town>();

        void OnEnable(){
            GenerateTowns();
            RefreshScreen();
        }

        public void GenerateTowns(){
            towns.Add(new Town());
            towns.Add(new Town());
        }

        /// <summary>
        /// Select a destination
        /// </summary>
        /// <param name="id">Id of the button that was clicked.</param>
        public void SelectDestination(int id){
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

            dbConnection = GameDatabase.CreateTownAndOpenDatabase();
            dbCommandUpdateValue = dbConnection.CreateCommand();
            dbCommandUpdateValue.CommandText = "UPDATE TownTable SET curTown = " + newTown + ", foodPrice = " + towns[id-1].GetFoodPrice() + ", gasPrice = " +  towns[id-1].GetGasPrice() +
                                                ", scrapPrice = " + towns[id-1].GetScrapPrice()  + ", medkitPrice = " +  towns[id-1].GetMedkitPrice()  + ", tirePrice = " +  towns[id-1].GetTirePrice()  +
                                                ", batteryPrice = " +  towns[id-1].GetBatteryPrice()  + ", ammoPrice = " +  towns[id-1].GetAmmoPrice()  + ", foodStock = " +  towns[id-1].GetFoodStock()  +
                                                ", gasStock = " +  towns[id-1].GetGasStock() + ", scrapStock = " +  towns[id-1].GetScrapStock() + ", medkitStock = " + towns[id-1].GetMedkitStock() +
                                                ", tireStock = " +  towns[id-1].GetTireStock() + ", batteryStock = " +  towns[id-1].GetBatteryStock() + ", ammoStock = " +  towns[id-1].GetAmmoStock() +
                                                ", side1Reward = " + towns[id-1].GetMissionRewards()[0] + ", side1Qty = " + towns[id-1].GetMissionQty()[0] + ", side1Diff = " + towns[id-1].GetMissionDifficulties()[0] + 
                                                ", side1Type = " + towns[id-1].GetMissionTypes()[0] + ", side2Reward = " + towns[id-1].GetMissionRewards()[1] + ", side2Qty = " + towns[id-1].GetMissionQty()[1] + 
                                                ", side2Diff = " + towns[id-1].GetMissionDifficulties()[1] + ", side2Type = " + towns[id-1].GetMissionTypes()[1] + ", side3Reward = " + towns[id-1].GetMissionRewards()[2] + 
                                                ", side3Qty = " + towns[id-1].GetMissionQty()[2] + ", side3Diff = " + towns[id-1].GetMissionDifficulties()[2] + ", side3Type = " + towns[id-1].GetMissionTypes()[2] + 
                                                " WHERE id = " + GameLoop.FileId;
            dbCommandUpdateValue.ExecuteNonQuery();
            dbConnection.Close();

            towns.Clear();

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
            string destination = "", supplies = "";

            // If the current town number has only one way to go, disable the 2nd option
            destinationButton2.interactable = !(townNum == 0);

            dbConnection = GameDatabase.CreateSavesAndOpenDatabase();
            dbCommandReadValue = dbConnection.CreateCommand();
            dbCommandReadValue.CommandText = "SELECT * FROM SaveFilesTable WHERE id = " + GameLoop.FileId;
            dataReader = dbCommandReadValue.ExecuteReader();
            dataReader.Read();

            int time = GameLoop.Hour > 12 && GameLoop.Hour <= 24 ? GameLoop.Hour - 12 : GameLoop.Hour, distanceToGo = 0;
            
            // Determine distance based on town #
            for(int i = 1; i <= 2; i++){
                if(i == 1 && !destinationButton2.interactable){
                    destinationTexts[i-1].text = "";
                    break;
                }
                switch(townNum+i){
                    case 1:
                        destination = "Ottawa";
                        distanceToGo = 198;
                        break;
                }
                Debug.Log(newTown);
                supplies = towns[i-1].SumTownResources() <= 330 ? "Light Supplies" : "Decent Supplies";
                destinationTexts[i-1].text = destination + "\n" + distanceToGo + "km\n" + supplies;
            }

            int distanceLeft = distanceToGo - dataReader.GetInt32(3);
            string timing = GameLoop.Hour >= 12 && GameLoop.Hour < 24 ? " pm" : " am", activity = GameLoop.Activity == 1 ? "Low" : GameLoop.Activity == 2 ? "Medium" : GameLoop.Activity == 3 ? "High" : "Ravenous";

            supplyText.text = "Food: " + dataReader.GetInt32(7) + "\nGas: " +  dataReader.GetFloat(8) + "\nDistance to Destination: " +  distanceLeft
                            + "km\nDistance Travelled: " + dataReader.GetInt32(3) + "km\nTime: " + time + timing + "\nActivity: " + activity;
                    
            popupText.text = distanceLeft.ToString() + " km to " + destination;

            dbConnection.Close();
        }

        public void StopCar(){
            SceneManager.LoadScene(1);
        }

        /// <summary>
        /// Move 1 timestep in travelling to the next destination.
        /// </summary>
        private IEnumerator Transition(){
            yield return new WaitForSeconds(0.5f);

            // Travel, deal a little damage to the car and the party depending on pace and rationing

            // Random chance of generating an event or rerun the coroutine

            // Stop coroutine when distance matches distanceToGo

        }
    }
}

