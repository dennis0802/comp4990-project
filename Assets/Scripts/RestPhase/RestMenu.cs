using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System;
using Random = UnityEngine.Random;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Mono.Data.Sqlite;
using TMPro;
using UI;
using Database;
using TravelPhase;
using CombatPhase;

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

        [Tooltip("Job complete button")]
        [SerializeField]
        private Button jobCompleteButton;

        [Tooltip("Job complete text")]
        [SerializeField]
        private TextMeshProUGUI jobCompleteText;

        [Tooltip("Image object to display map")]
        [SerializeField]
        private Image mapImageDisplay;
        
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

        [Tooltip("Background displaying landscape")]
        [SerializeField]
        private GameObject[] backgroundLandscape;

        [Tooltip("Advice text object")]
        [SerializeField]
        private TextMeshProUGUI adviceText;

        // To track rest hours on the slider
        private float restHours = 1;
        // To track the coroutine running for waiting actions
        private Coroutine coroutine;
        private int paranoidPresent = 0;
        // To track prices in town shops
        private List<int> buyingPrices, sellingPrices, shopStocks;
        // For supply trading (not towns)
        private int tradeOffer, tradeDemand, tradeOfferQty, tradeDemandQty;
        // To track game phase (travel, combat, rest)
        private int phaseNum;
        private bool isInDelay;
        private float timer;
        public static int JobNum;
        public static bool IsScavenging;

        void OnEnable(){
            backgroundPanel.SetActive(true);
            RefreshScreen();
        }

        void Update(){
            Save save = DataUser.dataManager.GetSaveById(GameLoop.FileId);
            if(Equals(save.CurrentLocation, "Vancouver") && SceneManager.GetActiveScene().buildIndex == 1){
                TravelLoop.InFinalCombat = true;
                backgroundPanel.SetActive(false);
                StartCoroutine(GameLoop.LoadAsynchronously(3));
                CombatManager.PrevMenuRef = this.gameObject;
                gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Refresh the screen upon loading the rest menu.
        /// </summary>
        public void RefreshScreen(){
            ManageRewards();

            // Main menus
            Save save = DataUser.dataManager.GetSaveById(GameLoop.FileId);
            IEnumerable<ActiveCharacter> characters = DataUser.dataManager.GetActiveCharacters().Where(a=>a.FileId == GameLoop.FileId).OrderByDescending(a=>a.IsLeader);
            ActiveCharacter[] temp = characters.ToArray<ActiveCharacter>();
            int money = save.Money, food = save.Food, scrap = save.Scrap, medkit = save.Medkit, tires = save.Tire, batteries = save.Battery, ammo = save.Ammo, 
                curDistance = save.Distance;
            phaseNum = save.PhaseNum;
            float gas = save.Gas;

            suppliesText1.text = "Food: " +  food + "kg\n\nGas: " + gas + " cans\n\nScrap: " + scrap + "\n\nMoney: $" +
                                 money + "\n\nMedkit: " + medkit;
            suppliesText2.text = "Tires: " + tires + "\n\nBatteries: " + batteries + "\n\nAmmo: " + ammo;
            curFoodText.text = "You have " + food + "kg of food";
            locationText.text = phaseNum == 0 ? save.CurrentLocation : "The Road";

            townButton.interactable = phaseNum == 0;

            for(int i = 0; i < 4; i++){
                if(i < temp.Count()){
                    DisplayCharacter(i, temp[i], save);
                }
                else{
                    healButton[i].interactable = false;
                    playerText[i].text = "";
                    playerHealth[i].gameObject.SetActive(false);
                    playerModel[i].SetActive(false);
                }
            }

            GameLoop.RationsMode = save.RationMode;
            GameLoop.Hour = save.CurrentTime;
            GameLoop.Pace = save.PaceMode;
            GameLoop.Activity = GameLoop.Hour >= 21 || GameLoop.Hour <= 5 ? 4 : GameLoop.Hour >= 18 || GameLoop.Hour <= 8 ? 3 : GameLoop.Hour >= 16 || GameLoop.Hour <= 10 ? 2 : 1;

            List<int> foundTraits = new List<int>();
            foreach(ActiveCharacter ac in characters){
                foundTraits.Add(ac.Trait);
            }
            paranoidPresent = foundTraits.Contains(1) ? 1 : 0;
            
            rationsText.text = GameLoop.RationsMode == 1 ? "Current Rations: Low (1kg/person)" : 
                                GameLoop.RationsMode == 2 ?  "Current Rations: Medium (2kg/person)" : "Current Rations: High (3kg/person)";
            paceText.text = GameLoop.Pace== 1 ? "Slow\n65km/h" : GameLoop.Pace == 2 ?  "Average\n80km/h" : "Fast\n95km/h";
            int time = GameLoop.Hour > 12 && GameLoop.Hour <= 24 ? GameLoop.Hour - 12 : GameLoop.Hour;
            string timing = GameLoop.Hour >= 12 && GameLoop.Hour < 24 ? " pm" : " am", activity = GameLoop.Activity == 1 ? "Low" : GameLoop.Activity == 2 ? "Medium" : GameLoop.Activity == 3 ? "High" : "Ravenous";

            timeActivityText.text = "Current Time: " + time + timing + "; Activity: " + activity;

            // Background
            backgroundLandscape[0].SetActive(phaseNum == 0);
            backgroundLandscape[1].SetActive(phaseNum != 0);

            // Car
            scrapRepairText.text = "You have " + scrap + " scrap.";

            // Enable buttons based on scrap 
            for(int i = 0; i < 3; i++){
                scrapButtons[i].interactable = scrap >= Mathf.Pow(2, i);
            }

            // Town shop menus
            TownEntity townEntity = DataUser.dataManager.GetTownById(GameLoop.FileId);
            foreach (TextMeshProUGUI text in shopButtonTexts)
            {
                text.text = GameLoop.IsSelling ? "Sell" : "Buy";
            }

            // Change rate based on distance later, should get lower
            GameLoop.SellRate = curDistance < 1000 ? 0.4f : curDistance < 1500 ? 0.3f : curDistance < 2000 ? 0.2f : 0.1f;

            int[] teamStocks = {food, (int)gas, scrap, medkit, tires, batteries, ammo};
            buyingPrices = new List<int>(){townEntity.FoodPrice, townEntity.GasPrice, townEntity.ScrapPrice, townEntity.MedkitPrice, townEntity.BatteryPrice, townEntity.TirePrice,
                                           townEntity.AmmoPrice
                                          };
            shopStocks = new List<int>(){townEntity.FoodStock, townEntity.GasStock, townEntity.ScrapStock, townEntity.MedkitStock, townEntity.BatteryStock, townEntity.TireStock,
                                           townEntity.AmmoStock
                                          };
            sellingPrices = new List<int>(){0,0,0,0,0,0,0};
            for(int i = 0; i < 7; i++){
                // Add a 10% discount if a charming character is present
                buyingPrices[i] = foundTraits.Contains(0) ? (int)(buyingPrices[i] - buyingPrices[i] * 0.1f) : buyingPrices[i];
                // Items will get more expensive with more distance
                buyingPrices[i] += (int)(buyingPrices[i] * (0.4f-GameLoop.SellRate));

                sellingPrices[i] = (int)((float)(buyingPrices[i]) * GameLoop.SellRate);
                sellingPrices[i] = sellingPrices[i] == 0 ? 1 : sellingPrices[i];
                if(i == 0 || i == 6){
                    shopButtonTexts[i].text += " 10";
                }
            }

            foodRowText.text = "Food\t\t\t" + townEntity.FoodStock + "\t\t       $" + buyingPrices[0] + "\t$" + sellingPrices[0] + "\t\t" + food;
            gasRowText.text = "Gas\t\t\t" + townEntity.GasStock + "\t\t       $" + buyingPrices[1] + "\t$" + sellingPrices[1] + "\t\t" + gas;
            scrapRowText.text = "Scrap\t\t\t" + townEntity.ScrapStock + "\t\t       $" + buyingPrices[2] + "\t$" + sellingPrices[2] + "\t\t" + scrap;
            medRowText.text = "Medkit\t\t" + townEntity.MedkitStock + "\t\t       $" + buyingPrices[3] + "\t$" + sellingPrices[3] + "\t\t" + medkit;
            tireRowText.text = "Tire\t\t\t" + townEntity.TireStock + "\t\t       $" + buyingPrices[4] + "\t$" + sellingPrices[4] + "\t\t" + tires;
            batteryRowText.text = "Battery\t\t" + townEntity.BatteryStock + "\t\t       $" + buyingPrices[5] + "\t$" + sellingPrices[5] + "\t\t" + batteries;
            ammoRowText.text = "Ammo\t\t" + townEntity.AmmoStock + "\t\t       $" + buyingPrices[6] + "\t$" + sellingPrices[6] + "\t\t" + ammo;
            moneyAmtText.text = foundTraits.Contains(0) ? "You have $" + money + ". A 10% discount has been applied because of your charm.": "You have $" + money;

            // Enable buttons depending on stock and money
            // Disable buying if shop stock is empty or you have insufficient money
            // Disable selling if your stock is empty
            for(int i = 0; i < shopButtons.Length; i++){
                if(GameLoop.IsSelling && teamStocks[i] <= 0){
                    shopButtons[i].interactable = false;
                }
                else if(!GameLoop.IsSelling && (money < buyingPrices[i] || shopStocks[i] <= 0)){
                    shopButtons[i].interactable = false;
                }
                else{
                    shopButtons[i].interactable = true;
                }
            }

            // Map
            int nextDistance = phaseNum == 2 ? townEntity.NextDistanceAway-curDistance : 0, curTown = townEntity.CurTown, prevTown = townEntity.PrevTown;
            distanceText.text = "Distance Travelled: " + curDistance + " km\nDistance to Next Stop: " + nextDistance + " km";
            mapImageDisplay.sprite = GameLoop.RetrieveMapImage(prevTown, curTown);

            // Job listings
            List<int> missionRewards = new List<int>(){townEntity.Side1Reward, townEntity.Side2Reward, townEntity.Side3Reward};
            List<int> missionTypes = new List<int>(){townEntity.Side1Type, townEntity.Side2Type, townEntity.Side3Type};
            List<int> missionDifficulty = new List<int>(){townEntity.Side1Diff, townEntity.Side2Diff, townEntity.Side3Diff};
            List<int> missionQty = new List<int>(){townEntity.Side1Qty, townEntity.Side2Qty, townEntity.Side3Qty};

            for(int i = 0; i < 3; i++){
                if(missionRewards[i] != 0){
                    jobButtons[i].interactable = true;
                    string type = missionTypes[i] == 1 ? "Defence" : "Collect";
                    string typeDesc = missionTypes[i] == 1 ? "Those creatures are out wandering by my house again. Any travellers willing to defend me will be paid." 
                                                                       : "I dropped something precious to me in no man's land. Any travellers willing to find and return it for me will be paid.";
                    string reward = "";

                    int rewardType = missionRewards[i];
                    // 1-3 = food, 4-6 = gas, 7-9 = scrap, 10-12 = money, 13 = medkit, 14 = tire, 15 = battery, 16-18 = ammo
                    if(rewardType >= 1 && rewardType <= 3){
                        reward = missionQty[i] + "kg food";
                    }
                    else if(rewardType >= 4 && rewardType <= 6){
                        reward = missionQty[i] + " cans";
                    }
                    else if(rewardType >= 7 && rewardType <= 9){
                        reward = missionQty[i] + " scrap";
                    }
                    else if(rewardType >= 10 && rewardType <= 12){
                        reward = "$" + missionQty[i];
                    }
                    else if(rewardType == 13){
                        reward = missionQty[i] + " medkits";
                    }
                    else if(rewardType == 14){
                        reward = missionQty[i] + " tires";
                    }
                    else if(rewardType == 15){
                        reward = missionQty[i] + " batteries";
                    }
                    else if(rewardType >= 16 && rewardType <= 18){
                        reward = missionQty[i] + " ammo shells";
                    }

                    string difficulty = missionDifficulty[i] <= 20 ? "Simple" : missionDifficulty[i] <= 40 ? "Dangerous" : "Fatal!";
                    string jobDesc = type + "\nReward: " + reward + "\nDifficulty: " + difficulty + "\n\n" + typeDesc;
                    jobButtonDescs[i].text = jobDesc;
                }
                else{
                    jobButtons[i].interactable = false;
                    jobButtonDescs[i].text = "No job available.";
                }
            }

            // Upgrades
            Car car = DataUser.dataManager.GetCarById(GameLoop.FileId);

            wheelText.text = car.WheelUpgrade == 1 ? "Durable Tires\nTires always last regardless of terrain." : "No wheel upgrade available to list.";
            batteryText.text = car.BatteryUpgrade == 1 ? "Durable Battery\nBattery always has power." : "No battery upgrade available to list.";
            engineText.text = car.EngineUpgrade == 1 ? "Fuel-Efficent Engine\nEngine consumes less gas for more distance." : "No engine upgrade available to list.";
            toolText.text = car.ToolUpgrade == 1 ? "Secure Chest\nNo supplies will be forgotten again." : "No tool upgrade available to list.";
            misc1Text.text = car.MiscUpgrade1 == 1 ? "Travel Garden\nGenerate 1kg of food/hour." : "No misc upgrade available to list.";
            misc2Text.text = car.MiscUpgrade2 == 1 ? "Cushioned Seating\nParty takes less damage when driving." : "No misc upgrade available to list.";
            int carHP = car.CarHP;
            carHPSlider.value = carHP;

            // Enable buttons depending on car HP and amount of scrap
            for(int i = 0; i < scrapButtons.Length; i++){
                scrapButtons[i].interactable = carHP != 100 && scrap >= (int)(Mathf.Pow(2,i));
            }
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

            Save save = DataUser.dataManager.GetSaveById(GameLoop.FileId);
            save.RationMode = GameLoop.RationsMode;
            DataUser.dataManager.UpdateSave(save);
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

            Save save = DataUser.dataManager.GetSaveById(GameLoop.FileId);
            save.PaceMode = GameLoop.Pace;
            DataUser.dataManager.UpdateSave(save);
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
        /// Go to combat modes
        /// </summary>
        /// <param name="id">The id of the button pressed (0 for scavenging, 1-3 for jobs</param>
        public void GoToCombat(int id){
            // NOTE: RestMenuUI must be set active to false or loading will be slow!
            if(id == 0){
                IsScavenging = true;
            }
            else if(id > 0){
                JobNum = id;
            }

            backgroundPanel.SetActive(false);
            StartCoroutine(GameLoop.LoadAsynchronously(3));
            CombatManager.PrevMenuRef = this.gameObject;
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
                Save save = DataUser.dataManager.GetSaveById(GameLoop.FileId);
                save.PhaseNum = 1;
                DataUser.dataManager.UpdateSave(save);

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
        /// <param name="id">The button of the team member to heal</param>
        public void UseMedkit(int id){
            Save save = DataUser.dataManager.GetSaveById(GameLoop.FileId);
            IEnumerable<ActiveCharacter> characters = DataUser.dataManager.GetActiveCharacters().Where(a=>a.FileId == GameLoop.FileId);

            save.Medkit--;
            DataUser.dataManager.UpdateSave(save);
            List<int> teamHealth = new List<int>();

            foreach(ActiveCharacter ac in characters){
                teamHealth.Add(ac.Health);
            }
            
            ActiveCharacter[] temp = characters.ToArray<ActiveCharacter>();
            temp[id].Health = temp[id].Health + 15 > 100 ? 100 : temp[id].Health + 15;
            DataUser.dataManager.UpdateCharacters((IEnumerable<ActiveCharacter>)(temp));
            RefreshScreen();
        }

        /// <summary>
        /// Perform the trade action displayed.
        /// </summary>
        /// <param name="button">The button id pressed - 0 for decline, 1 for accept</param>
        public void TradeAction(int button){
            // Accept trade
            if(button == 1){
                Save save = DataUser.dataManager.GetSaveById(GameLoop.FileId);
                List<int> partyStock = new List<int>(){save.Food, (int)(save.Gas), save.Scrap, save.Money, save.Medkit, save.Tire, save.Battery, save.Ammo};

                // Adjust stocks accordingly
                int curPartyStock = partyStock[tradeDemand-1] - tradeDemandQty;
                int receivedStock = partyStock[tradeOffer-1] + tradeOfferQty;

                switch(tradeDemand){
                    case 1:
                        save.Food = curPartyStock;
                        break;
                    case 2:
                        save.Gas = curPartyStock;
                        break;
                    case 3:
                        save.Scrap = curPartyStock;
                        break;
                    case 4:
                        save.Money = curPartyStock;
                        break;
                    case 5:
                        save.Medkit = curPartyStock;
                        break;
                    case 6:
                        save.Tire = curPartyStock;
                        break;
                    case 7:
                        save.Battery = curPartyStock;
                        break;
                    case 8:
                        save.Ammo = curPartyStock;
                        break;
                }

                switch(tradeOffer){
                    case 1:
                        save.Food = receivedStock;
                        break;
                    case 2:
                        save.Gas = receivedStock;
                        break;
                    case 3:
                        save.Scrap = receivedStock;
                        break;
                    case 4:
                        save.Money = receivedStock;
                        break;
                    case 5:
                        save.Medkit = receivedStock;
                        break;
                    case 6:
                        save.Tire = receivedStock;
                        break;
                    case 7:
                        save.Battery = receivedStock;
                        break;
                    case 8:
                        save.Ammo = receivedStock;
                        break;
                }
                DataUser.dataManager.UpdateSave(save);

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
            int cost = 0, sellFactor;

            sellFactor = GameLoop.IsSelling ? -1 : 1;
            Save save = DataUser.dataManager.GetSaveById(GameLoop.FileId);
            TownEntity townEntity = DataUser.dataManager.GetTownById(GameLoop.FileId);

            List<int> partyStock = new List<int>(){save.Food, (int)(save.Gas), save.Scrap, save.Medkit, save.Tire, save.Battery, save.Ammo};
            List<int> townStock = new List<int>(){townEntity.FoodStock, townEntity.GasStock, townEntity.ScrapStock, townEntity.MedkitStock, townEntity.TireStock,
                                                  townEntity.BatteryStock, townEntity.AmmoStock};

            for(int i = 0; i < 7; i++){
                if(id - 1 == i){
                    int qty = id == 1 || id == 7 ? 10 : 1;
                    partyStock[i] += qty * sellFactor;
                    cost = GameLoop.IsSelling ? sellingPrices[id-1] : buyingPrices[id-1];
                    townStock[i] -= qty * sellFactor;
                    break;
                }
            }

            save.Food = partyStock[0];
            save.Gas = (float)(partyStock[1]);
            save.Scrap = partyStock[2];
            save.Medkit = partyStock[3];
            save.Tire = partyStock[4];
            save.Battery = partyStock[5];
            save.Ammo = partyStock[6];
            townEntity.FoodStock = townStock[0]; 
            townEntity.GasStock = townStock[1]; 
            townEntity.ScrapStock = townStock[2];
            townEntity.MedkitStock = townStock[3];
            townEntity.TireStock = townStock[4];
            townEntity.BatteryStock = townStock[5];
            townEntity.AmmoStock = townStock[6];

            save.Money += GameLoop.IsSelling ? sellingPrices[id-1] : cost * -sellFactor;

            DataUser.dataManager.UpdateSave(save);
            DataUser.dataManager.UpdateTown(townEntity);
            RefreshScreen();
        }

        /// <summary>
        /// Check leader status.
        /// </summary>
        public void CheckLeaderStatus(){
            ActiveCharacter leader = DataUser.dataManager.GetLeader(GameLoop.FileId);
            if(leader == null){
                this.gameObject.SetActive(false);
                travelScreen.SetActive(false);
                gameOverScreen.SetActive(true);
                backgroundPanel.SetActive(true);
            }
        }

        /// <summary>
        /// Generate advice in town
        /// </summary>
        public void GenerateAdvice(){
            // This advice is sometimes helpful, sometimes satirical
            string generatedMessage = "";
            List<string> messages =  new List<string>()
                                    {"\"Be careful of who you pick up on the road. Though we're all trying to help each other to Vancouver, there can be some nasty people.\"",
                                     "\"I'm not even sure how we're breaking our arms in the car. Somehow, all it took was a little cushioning to solve everything.\"",
                                     "\"I'd buy and sell goods now if you can. Things get more expensive the further west you go.\"",
                                     "\"I've heard rumours there's a big mutant waiting for souls trying to reach Vancouver.\"",
                                     "\"I've always wanted to go on a road trip, but this isn't what I had in mind...\"",
                                     "\"It all went down so fast when the mutants arrived... we barely stand a chance alone.\"",
                                     "\"Pay attention to the personalities on your team - that can make or break groups.\"",
                                     "\"If the pioneers survived the Oregon Trail, surely we can survive a drive to the Pacific.\""
                                    };
            int rolled = Random.Range(1,101), adviceChosen = Random.Range(0,messages.Count);

            // 1-50 generate no advice,
            generatedMessage = rolled <= 50 ? "No one had any advice to give." : messages[adviceChosen];
            adviceText.text = generatedMessage;
        }

        /// <summary>
        /// Change the ingame time
        /// </summary>
        private void ChangeTime(){
            GameLoop.Hour++;
            if(GameLoop.Hour == 25){
                GameLoop.Hour = 1;
            }

            Save save = DataUser.dataManager.GetSaveById(GameLoop.FileId);
            save.CurrentTime = GameLoop.Hour;
            save.OverallTime++;
            DataUser.dataManager.UpdateSave(save);
            RefreshScreen();
        }

        /// <summary>
        /// Decrement food while performing waiting actions.
        /// </summary>
        private void DecrementFood(){
            IEnumerable<ActiveCharacter> characters = DataUser.dataManager.GetActiveCharacters().Where<ActiveCharacter>(a=>a.FileId==GameLoop.FileId).OrderByDescending(a=>a.IsLeader);
            Save save = DataUser.dataManager.GetSaveById(GameLoop.FileId);
            List<string> deadCharacters = new List<string>();
            bool flag = false;
            string tempDisplayText = "";

            // Enough food to feed everyone
            if(save.Food > 0){
                foreach(ActiveCharacter character in characters){
                    int hpRestore = GameLoop.RationsMode == 1 ? 3 : GameLoop.RationsMode == 2 ? 5 : 7;
                    int moraleRestore = 3;
                    hpRestore += save.PhaseNum == 0 ? 4 : 1;
                    save.Food -= GameLoop.RationsMode;
                    save.Food = save.Food <= 0 ? 0 : save.Food;

                    if(character.Health > 0 && character.Health < 100){
                        character.Health += hpRestore;
                        character.Health = character.Health > 100 ? 100 : character.Health;
                    }
                    if(character.Morale > 0 && character.Morale < 100){
                        character.Morale += moraleRestore;
                        character.Morale = character.Morale > 100 ? 100 : character.Morale;
                    }
                    DataUser.dataManager.UpdateCharacter(character);
                }
                DataUser.dataManager.UpdateSave(save);
            }
            // For each living character, their morale and health decrease.
            else{
                foreach(ActiveCharacter character in characters){
                    int hpLoss = save.PhaseNum == 0 ? 2 : 4, moraleLoss = 3;

                    if(character.Health > 0){
                        character.Health -= hpLoss;
                        character.Morale -= moraleLoss;
                        character.Morale = character.Morale < 0 ? 0 : character.Morale;
                    }
                    DataUser.dataManager.UpdateCharacter(character);

                    if(character.Health <= 0){
                        if(character.IsLeader == 1){
                            tempDisplayText = character.CharacterName + " has died.";
                            popupText.text = tempDisplayText;
                            confirmPopup.SetActive(true);
                            this.gameObject.SetActive(false); 
                            CancelRest();
                            DataUser.dataManager.DeleteActiveCharacter(character.Id);
                            return;
                        }
                        DataUser.dataManager.DeleteActiveCharacter(character.Id);
                        deadCharacters.Add(character.CharacterName);
                        flag = true;
                    }
                }
            }

            if(flag){
                IEnumerable<ActiveCharacter> deadMembers = characters.Where(a=>a.Health <= 0);
                foreach(ActiveCharacter dead in deadMembers){
                    if(dead.CustomCharacterId != -1){
                        PerishedCustomCharacter perished = new PerishedCustomCharacter(){FileId = GameLoop.FileId, CustomCharacterId = dead.CustomCharacterId};
                        DataUser.dataManager.InsertPerishedCustomCharacter(perished);
                    }
                }
                
                for(int i = 0; i < deadCharacters.Count; i++){
                    if(deadCharacters.Count == 1){
                        tempDisplayText += deadCharacters[i];
                    }
                    else if(i == deadCharacters.Count - 1){
                        tempDisplayText += "and " + deadCharacters[i];
                    }
                    else{
                        tempDisplayText += deadCharacters[i] + ", ";
                    }
                }

                tempDisplayText += deadCharacters.Count > 1 ? " have died." : " has died.";
                CancelRest();

                popupText.text = tempDisplayText;
                confirmPopup.SetActive(true);
                this.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Display party members in the menu.
        /// </summary>
        /// <param name="charNumber">Character number to distinguish leader / friend #</param>
        /// <param name="character">The current character</param>
        /// <param name="save">The current save file</param>
        private void DisplayCharacter(int charNumber, ActiveCharacter character, Save save){
            string morale = character.Morale >= 20 ? character.Morale >= 40 ? character.Morale >= 60 ? character.Morale >= 80 
                ? "Hopeful" : "Elated" : "Indifferent" : "Glum" : "Despairing";

            playerModel[charNumber].SetActive(true);
            playerText[charNumber].text = character.CharacterName + "\n" + GameLoop.Perks[character.Perk] + "\n" + GameLoop.Traits[character.Trait] + "\n" + morale;
            playerHealth[charNumber].gameObject.SetActive(true);
            playerHealth[charNumber].value = character.Health;
            healButton[charNumber].interactable = playerHealth[charNumber].value != 100 && save.Medkit != 0;

            GameObject model = playerModel[charNumber];

            // Color
            model.transform.GetChild(0).transform.GetChild(0).GetComponent<MeshRenderer>().material = CharacterCreation.Colors[character.Color-1];
            model.transform.GetChild(0).transform.GetChild(1).GetComponent<MeshRenderer>().material = CharacterCreation.Colors[character.Color-1];

            // Hat
            switch(character.Hat){
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
            switch(character.Outfit){
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
            switch(character.Acessory){
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
            tradeOfferQty += paranoidPresent == 1 ? 5 : 0;

            offerItem = tradeOffer == 1 ? "kg of food" : tradeOffer == 2 ? "cans of gas" : tradeOffer == 3 ? "scrap" : tradeOffer == 4 ? "dollars" :
                        tradeOffer == 5 ? "medkits" : tradeOffer == 6 ? "tires" : tradeOffer == 7 ? "batteries" : "ammo";

            // Bias the offer to be tires, scrap, or batteries if the car cannot move
            Car car = DataUser.dataManager.GetCarById(GameLoop.FileId);
            offerItem = car.CarHP == 0 ? "scrap" : car.IsBatteryDead == 1 ? "batteries" : car.IsTireFlat == 1 ? "tires" : offerItem;
            tradeOffer = car.CarHP == 0 ? 3 : car.IsBatteryDead == 1 ? 7 : car.IsTireFlat == 1 ? 6 : tradeOffer;
            tradeOfferQty = car.CarHP == 0 ? Random.Range(1,20) : car.IsBatteryDead == 1 || car.IsTireFlat == 1 ? tradeOfferQty = Random.Range(2,4) : tradeOfferQty;

            // Reroll demand if offer is the same as demand
            if(tradeOffer == tradeDemand){
                while(tradeOffer == tradeDemand){
                    tradeDemand = Random.Range(1,9);
                }
                tradeDemandQty = tradeDemand >= 5 && tradeDemand <= 7 ? Random.Range(2,4) : Random.Range(1,20);
            }

            Save save = DataUser.dataManager.GetSaveById(GameLoop.FileId);
            demandItem = tradeDemand == 1 ? "kg of food" : tradeDemand == 2 ? "cans of gas" : tradeDemand == 3 ? "scrap" : tradeDemand == 4 ? "dollars" :
                        tradeDemand == 5 ? "medkits" : tradeDemand == 6 ? "tires" : tradeDemand == 7 ? "batteries" : "ammo";
            curPartyStock = tradeDemand == 1 ? save.Food : tradeDemand == 2 ? (int)(save.Gas) : tradeDemand == 3 ? save.Scrap : tradeDemand == 4 ? save.Money : 
                            tradeDemand == 5 ? save.Medkit : tradeDemand == 6 ? save.Tire : tradeDemand == 7 ? save.Battery : save.Ammo;
            traderOfferText.text = "I request your " + tradeDemandQty + " " + demandItem + " in return for " + tradeOfferQty + " " + offerItem;

            // Check current stock and update the accept button accordingly.
            acceptButton.interactable = curPartyStock >= tradeDemandQty;
        }

        /// <summary>
        /// Manage rewards from jobs
        /// </summary>
        private void ManageRewards(){
            if(CombatManager.SucceededJob){
                TownEntity town = DataUser.dataManager.GetTownById(GameLoop.FileId);
                Save save = DataUser.dataManager.GetSaveById(GameLoop.FileId);
                int qty = RestMenu.JobNum == 1 ? town.Side1Qty : RestMenu.JobNum == 2 ? town.Side2Qty : town.Side3Qty;
                int reward = RestMenu.JobNum == 1 ? town.Side1Reward : RestMenu.JobNum == 2 ? town.Side2Reward : town.Side3Reward;

                switch(RestMenu.JobNum){
                    case 1:
                        town.Side1Type = 0;
                        town.Side1Diff = 0;
                        town.Side1Reward = 0;
                        town.Side1Qty = 0;
                        break;
                    case 2:
                        town.Side2Type = 0;
                        town.Side2Diff = 0;
                        town.Side2Reward = 0;
                        town.Side2Qty = 0;
                        break;
                    case 3:
                        town.Side3Type = 0;
                        town.Side3Diff = 0;
                        town.Side3Reward = 0;
                        town.Side3Qty = 0;
                        break;
                }

                string displayText = "You have received the following for completing the job:\n* " + qty;

                // 1-3 = food, 4-6 = gas, 7-9 = scrap, 10-12 = money, 13 = medkit, 14 = tire, 15 = battery, 16-18 = ammo
                displayText += reward <= 3 ?  " kg of food" : reward <= 6 ? " cans of gas" : reward <= 9 ? " scrap" : reward <= 12 ? " dollars" :
                               reward == 13 ? " medkits" : reward <= 14 ? " tires" : reward == 15 ? " batteries" : " ammo";

                string temp = reward <= 3 ?  "food = food + " : reward <= 6 ? "gas = gas + " : reward <= 9 ? "scrap = scrap + " : reward <= 12 ? "money = money +" :
                               reward == 13 ? "medkit = medkit + " : reward <= 14 ? "tire = tire + " : reward == 15 ? " battery = battery + " : "ammo = ammo + ";

                if(reward <= 3){
                    save.Food += reward;
                }
                else if(reward <= 6){
                    save.Gas += reward;
                }
                else if(reward <= 9){
                    save.Scrap += reward;
                }
                else if(reward <= 12){
                    save.Money += reward;
                }
                else if(reward == 13){
                    save.Medkit += reward;
                }
                else if(reward == 14){
                    save.Tire += reward;
                }
                else if(reward == 15){
                    save.Battery += reward;
                }
                else{
                    save.Ammo += reward;
                }

                // Update the database (change resources and clear the board)
                DataUser.dataManager.UpdateSave(save);
                DataUser.dataManager.UpdateTown(town);

                // Launch popup
                JobNum = 0;
                jobCompleteText.text = displayText;
                jobCompleteButton.gameObject.SetActive(true);
                CombatManager.SucceededJob = false;
            }
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

                int traderChange = Random.Range(1,6) + paranoidPresent;
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
                traderText.text = traderChange <= 2 && paranoidPresent == 1 ? "The group's paranoia has paid off with the generous offer.\n" + traderText.text : traderText.text;
                traderText.text = traderChange == 3 && paranoidPresent == 1 ? "A trader appeared but the group's paranoia drives them off." : traderText.text;
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

