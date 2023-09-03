using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Mono.Data.Sqlite;
using TMPro;
using UI;
using Database;
using TravelPhase;

namespace RestPhase{
    [DisallowMultipleComponent]
    public class RestMenu : MonoBehaviour{
        [Header("Descriptions")]
        [Tooltip("Rations mode text")]
        [SerializeField]
        private TextMeshProUGUI rationsText;

        [Tooltip("Time and activity text")]
        [SerializeField]
        private TextMeshProUGUI timeActivityText;

        [Tooltip("Trader text")]
        [SerializeField]
        private TextMeshProUGUI traderText;

        [Tooltip("Trader text offer details")]
        [SerializeField]
        private TextMeshProUGUI traderOfferText;
        
        [Tooltip("Pace text")]
        [SerializeField]
        private TextMeshProUGUI paceText;

        [Tooltip("Rest hours text")]
        [SerializeField]
        private TextMeshProUGUI restHoursText;

        [Tooltip("Rest description text")]
        [SerializeField]
        private TextMeshProUGUI restDescText;

        [Tooltip("Location text")]
        [SerializeField]
        private TextMeshProUGUI locationText;

        [Tooltip("Supplies text")]
        [SerializeField]
        private TextMeshProUGUI suppliesText1;

        [Tooltip("Supplies text")]
        [SerializeField]
        private TextMeshProUGUI suppliesText2;

        [Tooltip("Current food text")]
        [SerializeField]
        private TextMeshProUGUI curFoodText;

        [Tooltip("Popup to confirm messages")]
        [SerializeField]
        private GameObject confirmPopup;

        [Tooltip("Popup text")]
        [SerializeField]
        private TextMeshProUGUI popupText;

        [Tooltip("Popup to decide whether to leave")]
        [SerializeField]
        private GameObject leavePopup;

        [Header("Party Members")]
        [Tooltip("Friend text")]
        [SerializeField]
        private TextMeshProUGUI[] playerText;

        [Tooltip("Friend health")]
        [SerializeField]
        private Slider[] playerHealth;

        [Tooltip("Heal friend button")]
        [SerializeField]
        private Button[] healButton;

        [Tooltip("Friend models")]
        [SerializeField]
        private GameObject[] playerModel;

        [Tooltip("Colors for players")]
        [SerializeField]
        private Material[] playerColors;

        [Header("Buttons")]
        [Tooltip("Accept trade offer button")]
        [SerializeField]
        private Button acceptButton;

        [Tooltip("Decline trade offer button")]
        [SerializeField]
        private Button declineButton;

        [Tooltip("Wait for trader button")]
        [SerializeField]
        private Button waitButton;

        [Tooltip("Return from trading button")]
        [SerializeField]
        private Button tradeReturnButton;

        [Tooltip("Return from resting button")]
        [SerializeField]
        private Button restReturnButton;

        [Tooltip("Initiate rest button")]
        [SerializeField]
        private Button restStartButton;

        [Tooltip("Cancel rest button")]
        [SerializeField]
        private Button restCancelButton;

        [Tooltip("Rest hours slider")]
        [SerializeField]
        private Slider restHoursSlider;

        [Tooltip("Car health slider")]
        [SerializeField]
        private Slider carHPSlider;

        [Header("Town Components")]
        [Tooltip("Town button")]
        [SerializeField]
        private Button townButton;

        [Tooltip("Job buttons")]
        [SerializeField]
        private Button[] jobButtons;
        
        [Tooltip("Job button descriptions")]
        [SerializeField]
        private TextMeshProUGUI[] jobButtonDescs;
        
        [Header("Town Shop Components")]
        [Tooltip("Button text for buying/selling")]
        [SerializeField]
        private TextMeshProUGUI[] shopButtonTexts;

        [Tooltip("Buttons for buying/selling")]
        [SerializeField]
        private Button[] shopButtons;

        [Tooltip("Food row")]
        [SerializeField]
        private TextMeshProUGUI foodRowText;

        [Tooltip("Gas row")]
        [SerializeField]
        private TextMeshProUGUI gasRowText;

        [Tooltip("Scrap row")]
        [SerializeField]
        private TextMeshProUGUI scrapRowText;        

        [Tooltip("Medkit row")]
        [SerializeField]
        private TextMeshProUGUI medRowText;

        [Tooltip("Tire row")]
        [SerializeField]
        private TextMeshProUGUI tireRowText;

        [Tooltip("Battery row")]
        [SerializeField]
        private TextMeshProUGUI batteryRowText;

        [Tooltip("Ammo row")]
        [SerializeField]
        private TextMeshProUGUI ammoRowText;

        [Tooltip("Money text")]
        [SerializeField]
        private TextMeshProUGUI moneyAmtText;

        [Tooltip("Distance text")]
        [SerializeField]
        private TextMeshProUGUI distanceText;

        [Header("Car Repair Components")]
        [Tooltip("Scrap repair text")]
        [SerializeField]
        private TextMeshProUGUI scrapRepairText;

        [Tooltip("Buttons for scrap use")]
        [SerializeField]
        private Button[] scrapButtons;
        
        [Header("Upgrades")]
        [Tooltip("Wheel upgrade text.")]
        [SerializeField]
        private TextMeshProUGUI wheelText;

        [Tooltip("Battery upgrade text.")]
        [SerializeField]
        private TextMeshProUGUI batteryText;

        [Tooltip("Engine upgrade text.")]
        [SerializeField]
        private TextMeshProUGUI engineText;

        [Tooltip("Tool upgrade text.")]
        [SerializeField]
        private TextMeshProUGUI toolText;

        [Tooltip("Misc 1 upgrade text.")]
        [SerializeField]
        private TextMeshProUGUI misc1Text;

        [Tooltip("Misc 2 upgrade text.")]
        [SerializeField]
        private TextMeshProUGUI misc2Text;

        [Header("Screens")]
        [Tooltip("Game over screen components")]
        [SerializeField]
        private GameObject gameOverScreen;

        [Tooltip("Travel screen components")]
        [SerializeField]
        private GameObject travelScreen;

        [Tooltip("Travel window components")]
        [SerializeField]
        private GameObject travelWindow;

        [Tooltip("Background used throughout the screens")]
        [SerializeField]
        private GameObject backgroundPanel;

        // To track rest hours on the slider
        private float restHours = 1;
        // To track the coroutine running for waiting actions
        private Coroutine coroutine;
        // To track prices in town shops
        private int[] sellingPrices = new int[7], buyingPrices = new int[7], shopStocks = new int[7];
        // For supply trading (not towns)
        private int tradeOffer, tradeDemand, tradeOfferQty, tradeDemandQty;
        // To track game phase (travel, combat, rest)
        private int phaseNum;
        // To track leader name for game over
        public static string LeaderName = "";
        // To track friends alive for game over
        public static int FriendsAlive = 0;

        void OnEnable(){
            RefreshScreen();
        }

        /// <summary>
        /// Refresh the screen upon loading the rest menu.
        /// </summary>
        public void RefreshScreen(){
            // Main menus
            IDbConnection dbConnection = GameDatabase.CreateSavesAndOpenDatabase();
            IDbCommand dbCommandReadValues = dbConnection.CreateCommand();
            dbCommandReadValues.CommandText = "SELECT * FROM SaveFilesTable LEFT JOIN ActiveCharactersTable ON SaveFilesTable.charactersId = ActiveCharactersTable.id " + 
                                              "WHERE SaveFilesTable.id = " + GameLoop.FileId;
            IDataReader dataReader = dbCommandReadValues.ExecuteReader();
            dataReader.Read();

            int money = dataReader.GetInt32(10), food = dataReader.GetInt32(7), scrap = dataReader.GetInt32(9), medkit = dataReader.GetInt32(11), 
                tires = dataReader.GetInt32(12), batteries = dataReader.GetInt32(13), ammo = dataReader.GetInt32(14), curDistance = dataReader.GetInt32(3);
            phaseNum = dataReader.GetInt32(6);
            float gas = dataReader.GetFloat(8);

            suppliesText1.text = "Food: " +  food + "kg\n\nGas: " + gas + " cans\n\nScrap: " + scrap + "\n\nMoney: $" +
                                 money + "\n\nMedkit: " + medkit;
            suppliesText2.text = "Tires: " + tires + "\n\nBatteries: " + batteries + "\n\nAmmo: " + ammo;
            curFoodText.text = "You have " + food + "kg of food";
            locationText.text = phaseNum == 0 ? dataReader.GetString(5) : "The Road";

            townButton.interactable = phaseNum == 0;

            for(int i = 0; i < 4; i++){
                int index = 20 + 9 * i;
                if(!dataReader.IsDBNull(index)){
                    DisplayCharacter(index, i, dataReader);
                }
                else{
                    healButton[i].interactable = false;
                    playerText[i].text = "";
                    playerHealth[i].gameObject.SetActive(false);
                    playerModel[i].SetActive(false);
                }
            }

            GameLoop.RationsMode = dataReader.GetInt32(17);
            GameLoop.Hour = dataReader.GetInt32(15);
            GameLoop.Pace = dataReader.GetInt32(18);

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

            // 22 + 9 * i gets the traits
            List<int> foundTraits = new List<int>();
            for(int i = 0; i < 4; i++){
                if(!dataReader.IsDBNull(20+9*i)){
                    foundTraits.Add(dataReader.GetInt32(22+9*i));
                }
                else{
                    foundTraits.Add(-1);
                }
            }
            
            rationsText.text = GameLoop.RationsMode == 1 ? "Current Rations: Low" : GameLoop.RationsMode == 2 ?  "Current Rations: Medium" : "Current Rations: High";
            paceText.text = GameLoop.Pace== 1 ? "Slow\n40km/h" : GameLoop.Pace == 2 ?  "Average\n50km/h" : "Fast\n60km/h";
            int time = GameLoop.Hour > 12 && GameLoop.Hour <= 24 ? GameLoop.Hour - 12 : GameLoop.Hour;
            string timing = GameLoop.Hour >= 12 && GameLoop.Hour < 24 ? " pm" : " am", activity = GameLoop.Activity == 1 ? "Low" : GameLoop.Activity == 2 ? "Medium" : GameLoop.Activity == 3 ? "High" : "Ravenous";

            timeActivityText.text = "Current Time: " + time + timing + "; Activity: " + activity;

            // Car
            scrapRepairText.text = "You have " + scrap + " scrap.";

            // Enable buttons based on scrap 
            for(int i = 0; i < 3; i++){
                scrapButtons[i].interactable = scrap >= Mathf.Pow(2, i);
            }

            dbConnection.Close();

            // Town shop menus
            dbConnection = GameDatabase.CreateTownAndOpenDatabase();
            dbCommandReadValues = dbConnection.CreateCommand();
            dbCommandReadValues.CommandText = "SELECT * FROM TownTable WHERE id = " + GameLoop.FileId;
            dataReader = dbCommandReadValues.ExecuteReader();
            dataReader.Read();

            foreach (TextMeshProUGUI text in shopButtonTexts)
            {
                text.text = GameLoop.IsSelling ? "Sell" : "Buy";
            }

            // Change rate based on distance later, should get lower
            GameLoop.SellRate = curDistance < 1000 ? 0.4f : curDistance < 1500 ? 0.3f : curDistance < 2000 ? 0.2f : 0.1f;

            int[] teamStocks = {food, (int)gas, scrap, medkit, tires, batteries, ammo};

            for(int i = 0; i < 7; i++){
                buyingPrices[i] = dataReader.GetInt32(i+1);
                // Add a 10% discount if a charming character is present
                buyingPrices[i] = foundTraits.Contains(0) ? buyingPrices[i] - (int)(buyingPrices[i] * 0.1f) : buyingPrices[i];
                sellingPrices[i] = (int)((float)(buyingPrices[i]) * GameLoop.SellRate);
                shopStocks[i] = dataReader.GetInt32(i+8);
                if(i == 0 || i == 6){
                    shopButtonTexts[i].text += " 10";
                }
            }

            foodRowText.text = "Food\t\t\t" + dataReader.GetInt32(8) + "\t\t       $" + buyingPrices[0] + "\t$" + sellingPrices[0] + "\t\t" + food;
            gasRowText.text = "Gas\t\t\t" + dataReader.GetInt32(9) + "\t\t       $" + buyingPrices[1] + "\t$" + sellingPrices[1] + "\t\t" + gas;
            scrapRowText.text = "Scrap\t\t\t" + dataReader.GetInt32(10) + "\t\t       $" + buyingPrices[2] + "\t$" + sellingPrices[2] + "\t\t" + scrap;
            medRowText.text = "Medkit\t\t" + dataReader.GetInt32(11) + "\t\t       $" + buyingPrices[3] + "\t$" + sellingPrices[3] + "\t\t" + medkit;
            tireRowText.text = "Tire\t\t\t" + dataReader.GetInt32(12) + "\t\t       $" + buyingPrices[4] + "\t$" + sellingPrices[4] + "\t\t" + tires;
            batteryRowText.text = "Battery\t\t" + dataReader.GetInt32(13) + "\t\t       $" + buyingPrices[5] + "\t$" + sellingPrices[5] + "\t\t" + batteries;
            ammoRowText.text = "Ammo\t\t" + dataReader.GetInt32(14) + "\t\t       $" + buyingPrices[6] + "\t$" + sellingPrices[6] + "\t\t" + ammo;
            moneyAmtText.text= "You have $" + money;

            // Enable buttons depending on stock and money
            // Disable buying if shop stock is empty or you have insufficient money
            // Disable selling if your stock is empty
            for(int i = 0; i < shopButtons.Length; i++){
                if(GameLoop.IsSelling && teamStocks[i] <= 0){
                    shopButtons[i].interactable = false;
                }
                else if(!GameLoop.IsSelling && money < dataReader.GetInt32(i+1) || shopStocks[i] <= 0){
                    shopButtons[i].interactable = false;
                }
                else{
                    shopButtons[i].interactable = true;
                }
            }

            // Map
            int nextDistance = phaseNum == 2 ? dataReader.GetInt32(28)-curDistance : 0;
            distanceText.text = "Distance Travelled: " + curDistance + " km\nDistance to Next Stop: " + nextDistance + " km";

            // Job listings
            for(int i = 0; i < 3; i++){
                if(dataReader.GetInt32(15+4*i) != 0){
                    jobButtons[i].interactable = true;
                    string type = dataReader.GetInt32(18+4*i) == 1 ? "Defence" : "Collect";
                    string typeDesc = dataReader.GetInt32(18+4*i) == 1 ? "Those creatures are out wandering by my house again. Any travellers willing to defend me will be paid." 
                                                                       : "I dropped something precious to me in no man's land. Any travellers willing to find and return it for me will be paid.";
                    string reward = "";

                    int rewardType = dataReader.GetInt32(15+4*i);
                                // 1-3 = food, 4-6 = gas, 7-9 = scrap, 10-12 = money, 13 = medkit, 14 = tire, 15 = battery, 16-18 = ammo
                    if(rewardType >= 1 && rewardType <= 3){
                        reward = dataReader.GetInt32(16+4*i) + "kg food";
                    }
                    else if(rewardType >= 4 && rewardType <= 6){
                        reward = dataReader.GetInt32(16+4*i) + " cans";
                    }
                    else if(rewardType >= 7 && rewardType <= 9){
                        reward = dataReader.GetInt32(16+4*i) + " scrap";
                    }
                    else if(rewardType >= 10 && rewardType <= 12){
                        reward = "$" + dataReader.GetInt32(16+4*i);
                    }
                    else if(rewardType == 13){
                        reward = dataReader.GetInt32(16+4*i) + " medkits";
                    }
                    else if(rewardType == 14){
                        reward = dataReader.GetInt32(16+4*i) + " tires";
                    }
                    else if(rewardType == 15){
                        reward = dataReader.GetInt32(16+4*i) + " batteries";
                    }
                    else if(rewardType >= 16 && rewardType <= 18){
                        reward = dataReader.GetInt32(16+4*i) + " ammo shells";
                    }

                    string difficulty = dataReader.GetInt32(17+4*i) <= 20 ? "Simple" : dataReader.GetInt32(17+4*i) <= 40 ? "Dangerous" : "Fatal!";
                    string jobDesc = type + "\nReward: " + reward + "\nDifficulty: " + difficulty + "\n\n" + typeDesc;
                    jobButtonDescs[i].text = jobDesc;

                }
                else{
                    jobButtons[i].interactable = false;
                    jobButtonDescs[i].text = "No job available.";
                }
            }

            dbConnection.Close();

            // Upgrades
            dbConnection = GameDatabase.CreateCarsAndOpenDatabase();
            dbCommandReadValues = dbConnection.CreateCommand();
            dbCommandReadValues.CommandText = "SELECT * FROM CarsTable WHERE id = " + GameLoop.FileId;
            dataReader = dbCommandReadValues.ExecuteReader();
            dataReader.Read();

            wheelText.text = dataReader.GetInt32(2) == 1 ? "Durable Tires\nTires that always last regardless of terrain" : "No wheel upgrade available to list.";
            batteryText.text = dataReader.GetInt32(3) == 1 ? "Durable Battery\nBattery that has been tested to never run out" : "No battery upgrade available to list.";
            engineText.text = dataReader.GetInt32(4) == 1 ? "Fuel-Efficent Engine\nEngine that consumes less gas for more distance" : "No engine upgrade available to list.";
            toolText.text = dataReader.GetInt32(5) == 1 ? "Secure Chest\nNo supplies will be forgotten with this chest" : "No tool upgrade available to list.";
            misc1Text.text = dataReader.GetInt32(6) == 1 ? "Travel Garden\nGenerate 1kg of food every hour" : "No misc upgrade available to list.";
            misc2Text.text = dataReader.GetInt32(7) == 1 ? "Cushioned Seating\nParty takes less damage when driving" : "No misc upgrade available to list.";
            int carHP = dataReader.GetInt32(1);
            carHPSlider.value = carHP;

            // Enable buttons depending on car HP and amount of scrap
            for(int i = 0; i < scrapButtons.Length; i++){
                scrapButtons[i].interactable = carHP != 100 && scrap >= (int)(Mathf.Pow(2,i));
            }

            dbConnection.Close();
        }

        /// <summary>
        /// Change selected hours based on the slider
        /// </summary>
        public void ChangeSelectedHours(){
            RefreshScreen();
            restHours = restHoursSlider.value;
            restHoursText.text = restHours > 1 ? restHours + " hours" : restHours + " hour";
        }

        /// <summary>
        /// Toggle current rations
        /// </summary>
        public void ToggleRations(){
            GameLoop.RationsMode++;
            if(GameLoop.RationsMode > 3){
                GameLoop.RationsMode = 1;
            }
            
            IDbConnection dbConnection = GameDatabase.CreateSavesAndOpenDatabase();
            IDbCommand dbCommandUpdateValue = dbConnection.CreateCommand();
            dbCommandUpdateValue.CommandText = "UPDATE SaveFilesTable SET rations = " + GameLoop.RationsMode + " WHERE id = " + GameLoop.FileId;
            dbCommandUpdateValue.ExecuteNonQuery();
            dbConnection.Close();

            RefreshScreen();
        }

        /// <summary>
        /// Toggle current travel pace
        /// </summary>
        public void TogglePace(){
            GameLoop.Pace++;
            if(GameLoop.Pace > 3){
                GameLoop.Pace = 1;
            }

            IDbConnection dbConnection = GameDatabase.CreateSavesAndOpenDatabase();
            IDbCommand dbCommandUpdateValue = dbConnection.CreateCommand();
            dbCommandUpdateValue.CommandText = "UPDATE SaveFilesTable SET speed = " + GameLoop.Pace + " WHERE id = " + GameLoop.FileId;
            dbCommandUpdateValue.ExecuteNonQuery();
            dbConnection.Close();

            RefreshScreen();
        }

        /// <summary>
        /// Toggle current food text
        /// </summary>
        public void ToggleFoodText(){
            if(curFoodText.gameObject.activeInHierarchy){
                curFoodText.gameObject.SetActive(false);
            }
            else{
                curFoodText.gameObject.SetActive(true);
            }
        }

        /// <summary>
        /// Go to scavenging mode
        /// </summary>
        public void GoScavenge(){
            Debug.Log("Scavenging to be implemented.");
        }

        /// <summary>
        /// Leave town, going to the travel phase
        /// </summary>
        public void LeaveTown(){
            if(phaseNum == 0){
                SceneManager.LoadScene(2);
                this.gameObject.SetActive(false);
                TravelLoop.PopupActive = true;
                leavePopup.SetActive(true);
            }
            else{
                IDbConnection dbConnection = GameDatabase.CreateSavesAndOpenDatabase();
                IDbCommand dbCommandUpdateValue = dbConnection.CreateCommand();
                dbCommandUpdateValue.CommandText = "UPDATE SaveFilesTable SET inPhase = 1 WHERE id = " + GameLoop.FileId;
                dbCommandUpdateValue.ExecuteNonQuery();

                dbConnection.Close();

                SceneManager.LoadScene(2);
                this.gameObject.SetActive(false);
                travelScreen.SetActive(true);
                travelWindow.SetActive(true);
                backgroundPanel.SetActive(false);
                TravelLoop.PopupActive = false;
            }
        }

        /// <summary>
        /// Start a delay for waiting actions (resting, waiting for a trader)
        /// </summary>
        /// <param name="id">The id of the action; 1 = trading, 2 = resting, 3 = repairing</param>
        public void PerformWaitingAction(int id){
            coroutine = StartCoroutine(Delay(id));
        }

        /// <summary>
        /// Heal a party member using a medkit.
        /// </summary>
        public void UseMedkit(int id){
            IDbConnection dbConnection = GameDatabase.CreateSavesAndOpenDatabase();
            IDbCommand dbCommandReadValues = dbConnection.CreateCommand();
            dbCommandReadValues.CommandText = "SELECT * FROM SaveFilesTable LEFT JOIN ActiveCharactersTable ON SaveFilesTable.charactersId = ActiveCharactersTable.id " + 
                                              "WHERE SaveFilesTable.id = " + GameLoop.FileId;
            IDataReader dataReader = dbCommandReadValues.ExecuteReader();
            dataReader.Read();

            IDbCommand dbCommandUpdateValue = dbConnection.CreateCommand();
            dbCommandUpdateValue.CommandText =  "UPDATE SaveFilesTable SET medkit = medkit - 1 WHERE id = " + GameLoop.FileId;
            dbCommandUpdateValue.ExecuteNonQuery();
            
            int curHealth = dataReader.GetInt32(28 + 9 * id);

            // Change player health value, max at 100.
            string updateCommand = "UPDATE ActiveCharactersTable SET ";
            switch(id){
                case 0:
                    updateCommand += curHealth + 15 > 100 ? "leaderHealth = 100" : "leaderHealth = leaderHealth + 15";
                    break;
                case 1:
                    updateCommand += curHealth + 15 > 100 ? "friend1Health = 100" : "friend1Health = friend1Health + 15";
                    break;
                case 2:
                    updateCommand += curHealth + 15 > 100 ? "friend2Health = 100" : "friend2Health = friend2Health + 15";
                    break;
                case 3:
                    updateCommand += curHealth + 15 > 100 ? "friend3Health = 100" : "friend3Health = friend3Health + 15";
                    break;
            }
            updateCommand += " WHERE id = " + GameLoop.FileId;

            dbCommandUpdateValue.CommandText = updateCommand;
            dbCommandUpdateValue.ExecuteNonQuery();
            dbConnection.Close();

            RefreshScreen();
        }

        /// <summary>
        /// Perform the trade action displayed.
        /// </summary>
        /// <param name="button">The button id pressed - 0 for decline, 1 for accept</param>
        public void TradeAction(int button){
            // Accept trade
            if(button == 1){
                string offered = "", demanded = "";

                IDbConnection dbConnection = GameDatabase.CreateSavesAndOpenDatabase();
                IDbCommand dbCommandReadValues = dbConnection.CreateCommand();
                dbCommandReadValues.CommandText = "SELECT * FROM SaveFilesTable WHERE id = " + GameLoop.FileId;
                IDataReader dataReader = dbCommandReadValues.ExecuteReader();
                dataReader.Read();

                // Adjust stocks accordingly
                int curPartyStock = 6 + tradeDemand == 8 ? (int)(dataReader.GetFloat(8)) - tradeDemandQty : dataReader.GetInt32(6 + tradeDemand) - tradeDemandQty;
                int receivedStock = 6 + tradeOffer == 8 ? (int)(dataReader.GetFloat(8)) + tradeOfferQty : dataReader.GetInt32(6 + tradeOffer) + tradeOfferQty;

                offered = FilterItem(tradeOffer);
                demanded = FilterItem(tradeDemand);

                IDbCommand dbCommandUpdateValues = dbConnection.CreateCommand();
                dbCommandUpdateValues.CommandText = "UPDATE SaveFilesTable SET " + offered + receivedStock + ", " + demanded + curPartyStock + " WHERE id = " + GameLoop.FileId;
                dbCommandUpdateValues.ExecuteNonQuery();
                dbConnection.Close();

                RefreshScreen();
            }
            acceptButton.interactable = false;
            declineButton.interactable = false;
            waitButton.interactable = true;
            tradeReturnButton.interactable = true;
            traderText.text = "No one appeared.";
            traderOfferText.text = "";
            curFoodText.gameObject.SetActive(true);
        }

        /// <summary>
        /// Cancel resting action.
        /// </summary>
        public void CancelRest(){
            StopCoroutine(coroutine);
            restCancelButton.interactable = false;
            restReturnButton.interactable = true;
            restStartButton.interactable = true;
            restHoursSlider.interactable = true;
            restDescText.text = "How long would you like to rest for? Supplies will be consumed per hour.";
        }

        /// <summary>
        /// Cancel resting action.
        /// </summary>
        /// <param name="id">Button id - 1 = buy, 2 = sell, 3 = return</param>
        public void ToggleSelling(int id){
            GameLoop.IsSelling = id == 2;
            RefreshScreen();
        }

        /// <summary>
        /// Complete a buy/sell action in the town shop.
        /// </summary>
        /// <param name="id">The button that was clicked (1 = food, 2 = gas, 3 = scrap, 4 = medkit, 5 = tire, 6 = battery, 7 = ammo)</param>
        public void CompleteTownTransaction(int id){
            int money = 0, onHand = 0, cost = 0, inStock = 0, qty = 1, sellFactor;
            float gasTemp;
            string updateCommandText = "", updateStockText = "";
            
            sellFactor = GameLoop.IsSelling ? -1 : 1;

            IDbConnection dbConnection = GameDatabase.CreateSavesAndOpenDatabase();
            IDbCommand dbCommandReadValues = dbConnection.CreateCommand();
            dbCommandReadValues.CommandText = "SELECT * FROM SaveFilesTable LEFT JOIN TownTable ON SaveFilesTable.id = TownTable.id " + 
                                              "WHERE SaveFilesTable.id = " + GameLoop.FileId;
            IDataReader dataReader = dbCommandReadValues.ExecuteReader();
            dataReader.Read();

            money = dataReader.GetInt32(10);

            switch(id){
                // Food
                case 1:
                    qty = 10;
                    onHand = dataReader.GetInt32(7) + qty * sellFactor;
                    cost = dataReader.GetInt32(20);
                    inStock = dataReader.GetInt32(27) - qty * sellFactor;
                    updateCommandText = "food = ";
                    updateStockText = "foodStock = ";
                    break;
                // Gas
                case 2:
                    gasTemp = dataReader.GetFloat(8) + qty * sellFactor;
                    cost = dataReader.GetInt32(21);
                    inStock = dataReader.GetInt32(28) - qty * sellFactor;
                    updateCommandText = "gas = ";
                    updateStockText = "gasStock = ";
                    break;
                // Scrap
                case 3:
                    onHand = dataReader.GetInt32(9) + qty * sellFactor;
                    cost = dataReader.GetInt32(22);
                    updateCommandText = "scrap = ";
                    updateStockText = "scrapStock = ";
                    inStock = dataReader.GetInt32(29) - qty * sellFactor;
                    break;
                // Medkit
                case 4:
                    onHand = dataReader.GetInt32(11) + qty * sellFactor;
                    cost = dataReader.GetInt32(23);
                    inStock = dataReader.GetInt32(30) - qty * sellFactor;
                    updateCommandText = "medkit = ";
                    updateStockText = "medkitStock = ";
                    break;
                // Tire
                case 5:
                    onHand = dataReader.GetInt32(12) + qty * sellFactor;
                    cost = dataReader.GetInt32(24);
                    inStock = dataReader.GetInt32(31) - qty * sellFactor;
                    updateCommandText = "tire = ";
                    updateStockText = "tireStock = ";
                    break;
                // Battery
                case 6:
                    onHand = dataReader.GetInt32(13) + qty * sellFactor;
                    cost = dataReader.GetInt32(25);
                    inStock = dataReader.GetInt32(32) - qty * sellFactor;
                    updateCommandText = "battery = ";
                    updateStockText = "batteryStock = ";
                    break;
                // Ammo
                case 7:
                    qty = 10;
                    onHand = dataReader.GetInt32(14) + qty * sellFactor;
                    cost = dataReader.GetInt32(26);
                    updateCommandText = "ammo = ";
                    updateStockText = "ammoStock = ";
                    inStock = dataReader.GetInt32(33) - qty * sellFactor;
                    break;
            }

            money += GameLoop.IsSelling ? sellingPrices[id-1] : cost * -sellFactor;
            
            IDbCommand dbCommandUpdateValue = dbConnection.CreateCommand();
            dbCommandUpdateValue.CommandText =  "UPDATE SaveFilesTable SET " + updateCommandText + onHand + ", " + "money = " + money + " WHERE id = " + GameLoop.FileId;
            dbCommandUpdateValue.ExecuteNonQuery();
            dbConnection.Close();

            dbConnection = GameDatabase.CreateTownAndOpenDatabase();
            dbCommandUpdateValue.CommandText =  "UPDATE TownTable SET " + updateStockText + inStock + " WHERE id = " + GameLoop.FileId;
            dbCommandUpdateValue.ExecuteNonQuery();
            dbConnection.Close();

            RefreshScreen();
        }

        /// <summary>
        /// Filter the trade item.
        /// </summary>
        /// <param name="id">Item id</param>
        /// <returns>The string of the item to use to update the database</returns>
        private string FilterItem(int id){
            string updateCommandText = id == 1 ? "food = " : id == 2 ? "gas = " : id == 3 ? "scrap = " : id == 4 ? "money = " : id == 5 ? "medkit = " : id == 6 ? "tire = " :
                                       id == 7 ? "battery = " : "ammo = ";
            return updateCommandText;
        }

        /// <summary>
        /// Change the ingame time
        /// </summary>
        private void ChangeTime(){
            GameLoop.Hour++;

            if(GameLoop.Hour == 25){
                GameLoop.Hour = 1;
            }

            IDbConnection dbConnection = GameDatabase.CreateSavesAndOpenDatabase();
            IDbCommand dbCommandReadValues = dbConnection.CreateCommand();
            dbCommandReadValues.CommandText = "SELECT * FROM SaveFilesTable WHERE id = " + GameLoop.FileId;
            IDataReader dataReader = dbCommandReadValues.ExecuteReader();
            dataReader.Read();
            int overallTime = dataReader.GetInt32(16);

            IDbCommand dbCommandUpdateValue = dbConnection.CreateCommand();
            dbCommandUpdateValue.CommandText = "UPDATE SaveFilesTable SET time = " + GameLoop.Hour + ", overallTime = " + (overallTime + 1) + " WHERE id = " + GameLoop.FileId;
            dbCommandUpdateValue.ExecuteNonQuery();
            dbConnection.Close();

            RefreshScreen();
        }

        /// <summary>
        /// Decrement food while performing waiting actions.
        /// </summary>
        private void DecrementFood(){
            IDbConnection dbConnection = GameDatabase.CreateSavesAndOpenDatabase();
            IDbCommand dbCommandReadValues = dbConnection.CreateCommand();
            dbCommandReadValues.CommandText = "SELECT * FROM SaveFilesTable LEFT JOIN ActiveCharactersTable ON SaveFilesTable.charactersId = ActiveCharactersTable.id " + 
                                              "WHERE SaveFilesTable.id = " + GameLoop.FileId;
            IDataReader dataReader = dbCommandReadValues.ExecuteReader();
            dataReader.Read();

            int overallFood = dataReader.GetInt32(7), phase = dataReader.GetInt32(6);
            List<int> teamHealth = new List<int>();
            List<int> teamMorale = new List<int>();

            // Decrement food if available, otherwise health and morale decrease.
            if(overallFood > 0){
                // For each living character on the team, they consume 1, 2, or 3 units of food each hour depending on the ration mode.
                for(int i = 0; i < 4; i++){
                    int index = 20 + 9 * i;

                    if(!dataReader.IsDBNull(index)){
                        int curHp = dataReader.GetInt32(28 + 9 * i),
                            curMorale = dataReader.GetInt32(27 + 9 * i);
                        
                        overallFood = GameLoop.RationsMode == 1 ? overallFood - 1 : GameLoop.RationsMode == 2 ? overallFood - 2 : overallFood - 3;
                        overallFood = overallFood <= 0 ? 0 : overallFood;
                        int hpRestore = GameLoop.RationsMode == 1 ? 3 : GameLoop.RationsMode == 2 ? 5 : 7;
                        // Heal more in town than on the road
                        hpRestore += phase == 0 ? 4 : 1;
                        
                        // If the character is hurt, recover a little health based on ration mode
                        if(curHp > 0 && curHp < 100){
                            curHp = curHp + hpRestore > 100 ? 100 : curHp + hpRestore;
                            curMorale = curMorale + 3 > 100 ? 100 : curMorale + 3;
                        }
                        teamHealth.Add(curHp);
                        teamMorale.Add(curMorale);
                    }
                    // Character is dead.
                    else{
                        teamHealth.Add(0);
                        teamMorale.Add(0);
                    }
                }

                IDbCommand dbCommandUpdateValue = dbConnection.CreateCommand();
                dbCommandUpdateValue.CommandText = "UPDATE SaveFilesTable SET food = " + overallFood + " WHERE id = " + GameLoop.FileId;
                dbCommandUpdateValue.ExecuteNonQuery();
                dbConnection.Close();

                dbConnection = GameDatabase.CreateActiveCharactersAndOpenDatabase();
                dbCommandUpdateValue = dbConnection.CreateCommand();
                dbCommandUpdateValue.CommandText = "UPDATE ActiveCharactersTable SET leaderHealth = " + teamHealth[0] + ", friend1Health = " + teamHealth[1] +
                                                    ", friend2Health = " + teamHealth[2] + ", friend3Health = " + teamHealth[3] + ", leaderMorale = " + teamMorale[0] + 
                                                    ", friend1Morale = " + teamMorale[1] + ", friend2Morale = " + teamMorale[2] + ", friend3Morale = " + teamMorale[3] +
                                                    "WHERE id = " + GameLoop.FileId; 
                
                dbCommandUpdateValue.ExecuteNonQuery();
                dbConnection.Close();
            }
            // For each living character, their morale and health decrease.
            else{
                List<string> names = new List<string>();
                
                for(int i = 0; i < 4; i++){
                    int index = 20 + 9 * i;
                    if(!dataReader.IsDBNull(index)){
                        int curHp = dataReader.GetInt32(28 + 9 * i),
                            curMorale = dataReader.GetInt32(27 + 9 * i);
                        if(curHp > 0){
                            curHp = curHp - 5 < 0 ? 0: curHp - 5;
                            curMorale = curMorale - 5 < 0 ? 0 : curMorale - 5;
                            teamHealth.Add(curHp);
                            teamMorale.Add(curMorale);
                        }
                        names.Add(dataReader.GetString(index));
                    }
                    // Character is dead, they have 0hp.
                    else{
                        teamHealth.Add(0);
                        teamMorale.Add(0);
                        names.Add("_____TEMPNULL");
                    }
                }
                dbConnection.Close();

                dbConnection = GameDatabase.CreateActiveCharactersAndOpenDatabase();
                IDbCommand dbCommandUpdateValue = dbConnection.CreateCommand();
                string tempCommand = "UPDATE ActiveCharactersTable SET leaderHealth = " + teamHealth[0] + ", friend1Health = " + teamHealth[1] +
                        ", friend2Health = " + teamHealth[2] + ", friend3Health = " + teamHealth[3] + ", leaderMorale = " + teamMorale[0] + 
                        ", friend1Morale = " + teamMorale[1] + ", friend2Morale = " + teamMorale[2] + ", friend3Morale = " + teamMorale[3];

                // Check if any character has died.
                string tempDisplayText = "";

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

                            CancelRest();

                            popupText.text = tempDisplayText;
                            confirmPopup.SetActive(true);
                            this.gameObject.SetActive(false);
                            LeaderName = names[0];
                            FriendsAlive = names.Where(s => !Equals(s, "_____TEMPNULL") && !Equals(s, names[0])).Count();

                            dbCommandUpdateValue.CommandText = tempCommand + " WHERE id = " + GameLoop.FileId;
                            dbCommandUpdateValue.ExecuteNonQuery();
                            dbConnection.Close();
                            return; 
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
                    CancelRest();

                    popupText.text = tempDisplayText;
                    confirmPopup.SetActive(true);
                    this.gameObject.SetActive(false);
                }

                dbCommandUpdateValue.CommandText = tempCommand + " WHERE id = " + GameLoop.FileId;
                dbCommandUpdateValue.ExecuteNonQuery();
                dbConnection.Close();                
            }
        }

        /// <summary>
        /// Check leader status.
        /// </summary>
        public void CheckLeaderStatus(){
            IDbConnection dbConnection = GameDatabase.CreateActiveCharactersAndOpenDatabase();
            IDbCommand dbCommandReadValues = dbConnection.CreateCommand();
            dbCommandReadValues.CommandText = "SELECT * FROM ActiveCharactersTable WHERE id = " + GameLoop.FileId;
            IDataReader dataReader = dbCommandReadValues.ExecuteReader();
            dataReader.Read();

            // If leader name is null, they are dead. Bring to game over screen. Otherwise visibilities are toggled by the engine.
            if(dataReader.IsDBNull(1)){
                this.gameObject.SetActive(false);
                travelScreen.SetActive(false);
                gameOverScreen.SetActive(true);
                backgroundPanel.SetActive(true);
            }

            dbConnection.Close();
        }

        /// <summary>
        /// Display party members in the menu.
        /// </summary>
        /// <param name="index">The index to start at in the left joined table</param>
        /// <param name="charNumber">Character number to distinguish leader / friend #</param>
        /// <param name="dataReader">Data reader actively reading from database</param>
        private void DisplayCharacter(int index, int charNumber, IDataReader dataReader){
            string morale = dataReader.GetInt32(index+7) >= 20 ? dataReader.GetInt32(index+7) >= 40 ? dataReader.GetInt32(index+7) >= 60 ? dataReader.GetInt32(index+7) >= 80 
                ? "Hopeful" : "Elated" : "Indifferent" : "Glum" : "Despairing";

            playerModel[charNumber].SetActive(true);
            playerText[charNumber].text = dataReader.GetString(index) + "\n" + GameLoop.Perks[dataReader.GetInt32(index+1)] + "\n" + GameLoop.Traits[dataReader.GetInt32(index+2)] + "\n" + morale;
            playerHealth[charNumber].gameObject.SetActive(true);
            playerHealth[charNumber].value = dataReader.GetInt32(index+8);
            healButton[charNumber].interactable = playerHealth[charNumber].value != 100 && dataReader.GetInt32(11) != 0;

            GameObject model = playerModel[charNumber];

            // Color
            model.transform.GetChild(0).transform.GetChild(0).GetComponent<MeshRenderer>().material = playerColors[dataReader.GetInt32(index + 5)-1];
            model.transform.GetChild(0).transform.GetChild(1).GetComponent<MeshRenderer>().material = playerColors[dataReader.GetInt32(index + 5)-1];

            // Hat
            switch(dataReader.GetInt32(index + 6)){
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

            // Outfit
            switch(dataReader.GetInt32(index + 4)){
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

            // Accessory
            switch(dataReader.GetInt32(index + 3)){
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
        /// Generate a trade
        /// </summary>
        private void GenerateTrade(){
            string demandItem = "", offerItem = "";
            int curPartyStock = 0;

            // Continue to randomize until offer and demand are different
            do
            {
                tradeDemand = Random.Range(1,9);
                tradeOffer = Random.Range(1,9);
            } while (tradeDemand == tradeOffer);

            tradeDemandQty = tradeDemand >= 5 && tradeDemand <= 7 ? Random.Range(2,4) : Random.Range(1,20);
            tradeOfferQty = tradeOffer >= 5 && tradeOffer <= 7 ? Random.Range(2,4) : Random.Range(1,20);

            switch(tradeOffer){
                case 1:
                    offerItem = "kg of food";
                    break;
                case 2:
                    offerItem = "cans of gas";
                    break;
                case 3:
                    offerItem = "scrap";
                    break;
                case 4:
                    offerItem = "dollars";
                    break;
                case 5:
                    offerItem = "medkits";
                    break;
                case 6:
                    offerItem = "tires";
                    break;
                case 7:
                    offerItem = "batteries";
                    break;
                case 8:
                    offerItem = "ammo";
                    break;
            }

            IDbConnection dbConnection = GameDatabase.CreateSavesAndOpenDatabase();
            IDbCommand dbCommandReadValues = dbConnection.CreateCommand();
            dbCommandReadValues.CommandText = "SELECT * FROM SaveFilesTable WHERE id = " + GameLoop.FileId;
            IDataReader dataReader = dbCommandReadValues.ExecuteReader();
            dataReader.Read();

            switch(tradeDemand){
                case 1:
                    demandItem = "kg of food";
                    curPartyStock = dataReader.GetInt32(7);
                    break;
                case 2:
                    demandItem = "cans of gas";
                    curPartyStock = (int)(dataReader.GetFloat(8));
                    break;
                case 3:
                    demandItem = "scrap";
                    curPartyStock = dataReader.GetInt32(9);
                    break;
                case 4:
                    demandItem = "dollars";
                    curPartyStock = dataReader.GetInt32(10);
                    break;
                case 5:
                    demandItem = "medkits";
                    curPartyStock = dataReader.GetInt32(11);
                    break;
                case 6:
                    demandItem = "tires";
                    curPartyStock = dataReader.GetInt32(12);
                    break;
                case 7:
                    demandItem = "batteries";
                    curPartyStock = dataReader.GetInt32(13);
                    break;
                case 8:
                    demandItem = "ammo";
                    curPartyStock = dataReader.GetInt32(14);
                    break;
            }
            dbConnection.Close();
            traderOfferText.text = "I request your " + tradeDemandQty + " " + demandItem + " in return for " + tradeOfferQty + " " + offerItem;

            // Check current stock and update the accept button accordingly.
            acceptButton.interactable = curPartyStock >= tradeDemandQty;
        }

        /// <summary>
        /// Delay after a button press
        /// </summary>
        /// <param name="mode">The mode/menu to interact with after the delay</param>
        private IEnumerator Delay(int mode){
            // Trading
            if(mode == 1){
                waitButton.interactable = false;
                tradeReturnButton.interactable = false;
                traderText.text = "Waiting for trader.";
                yield return new WaitForSeconds(1.0f);
                traderText.text = "Waiting for trader..";
                yield return new WaitForSeconds(1.0f);
                traderText.text = "Waiting for trader...";
                yield return new WaitForSeconds(1.0f);
                DecrementFood();
                RefreshScreen();
                ChangeTime();

                int traderChange = Random.Range(1,6);
                if(traderChange <= 2){
                    acceptButton.interactable = true;
                    declineButton.interactable = true;
                    waitButton.interactable = false;
                    tradeReturnButton.interactable = false;
                    curFoodText.gameObject.SetActive(false);
                    GenerateTrade();
                }
                else{
                    waitButton.interactable = true;
                    tradeReturnButton.interactable = true;
                }
                traderText.text = traderChange <= 2 ? "A trader appeared making the following offer:" : "No one appeared.";
            }

            // Resting
            else if(mode == 2){
                restCancelButton.interactable = true;
                restReturnButton.interactable = false;
                restStartButton.interactable = false;
                restHoursSlider.interactable = false;

                while(restHoursSlider.value > 1){
                    restDescText.text = "Resting.";
                    yield return new WaitForSeconds(1.0f);
                    restDescText.text = "Resting..";
                    yield return new WaitForSeconds(1.0f);
                    restDescText.text = "Resting...";
                    yield return new WaitForSeconds(1.0f);
                    restHoursSlider.value--;
                    DecrementFood();
                    ChangeTime();
                }

                if(restHoursSlider.value == 1){
                    restDescText.text = "Resting.";
                    yield return new WaitForSeconds(1.0f);
                    restDescText.text = "Resting..";
                    yield return new WaitForSeconds(1.0f);
                    restDescText.text = "Resting...";
                    yield return new WaitForSeconds(1.0f);
                    DecrementFood();
                    RefreshScreen();
                    ChangeTime();
                }

                restCancelButton.interactable = false;
                restReturnButton.interactable = true;
                restStartButton.interactable = true;
                restHoursSlider.interactable = true;
                restDescText.text = "How long would you like to rest for? Supplies will be consumed per hour.";
            }
            // Repairs
            else if(mode == 3){
                DecrementFood();
                RefreshScreen();
                ChangeTime();
            }
        }
    }

}

