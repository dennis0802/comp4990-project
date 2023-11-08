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

        [Tooltip("Event generator for the road")]
        [SerializeField]
        private EventGenerator eventGenerator;

        [Tooltip("Rest menu screens - element 0 will be kept active in the background")]
        [SerializeField]
        private GameObject[] restScreens;

        [Tooltip("Rest menu master object")]
        [SerializeField]
        private GameObject restMenuMaster;

        // To track if a popup is active, will restrict when driving loop occurs.
        public static bool PopupActive = false, InFinalCombat = false;
        // To track the new town number and the distance away
        private int newTown, targetTownDistance = 0, currentDistance;
        // To track if the log of destinations has been initialized
        private bool logInitialized = false;
        // Audio source for popups
        private AudioSource popupSound;
        // To track generated towns
        private List<Town> towns = new List<Town>();
        // To manage destinations
        private Dictionary<int, List<int>> distanceLog = new Dictionary<int, List<int>>();
        private Dictionary<int, List<string>> nextDestinationLog = new Dictionary<int, List<string>>();
        // To time the driving loop
        public static float Timer = 0.0f;
        // Flag for going to combat
        public static bool GoingToCombat = false;
        public static List<string> queriesToPerform = new List<string>();
        public static List<List<object>> parametersForQueries = new List<List<object>>();

        void Start(){
            popupSound = GetComponent<AudioSource>();
        }

        void OnEnable(){
            RefreshScreen();
            if(!logInitialized){
                InitializeLogs();
            }
            GenerateTowns();
            InitializeScreen();
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

                        // 44/100 chance of generating an event
                        if(eventChance <= 44){
                            string msg = eventGenerator.GenerateEvent(eventChance);
                            if(!msg.Equals("")){
                                LaunchPopup(msg);
                                if(msg.Equals("You suddenly find yourself surrounded by mutants.")){
                                    GoingToCombat = true;
                                }
                            }
                        }
                    }
                    ChangeGameData();
                    HasCharacterDied();
                    Timer = 0.0f;
                }
            }
        }

        /// <summary>
        /// Initialize the screen with database info
        /// </summary>
        private void InitializeScreen(){
            playerText.text = "";
            IEnumerable<ActiveCharacter> characters = DataUser.dataManager.GetActiveCharacters().Where<ActiveCharacter>(c=>c.FileId == GameLoop.FileId).OrderByDescending(c=>c.IsLeader);
            ActiveCharacter[] tempCharacters = characters.ToArray<ActiveCharacter>();
            int used = 0;

            for(int i = 0; i < tempCharacters.Count(); i++){
                if(i == 0 & tempCharacters[i].IsLeader != 1){
                    playerText.text += "\nCar\n";
                }
                else{
                    playerHealthBars[i].value = tempCharacters[i].Health;
                    playerText.text += i == 0 ? tempCharacters[i].CharacterName + "\nCar\n" : tempCharacters[i].CharacterName + "\n";
                    used = i;
                }
            }

            used++;
            for(;used < 4; used++){
                playerHealthBars[used].value = 0;
            }

            TownEntity townEntity = DataUser.dataManager.GetTownById(GameLoop.FileId);
            targetTownDistance = townEntity.NextDistanceAway;
            Save save = DataUser.dataManager.GetSaveById(GameLoop.FileId);
            GameLoop.Hour = save.CurrentTime;
            GameLoop.Activity = GameLoop.Hour >= 21 || GameLoop.Hour <= 5 ? 4 : GameLoop.Hour >= 18 || GameLoop.Hour <= 8 ? 3 : GameLoop.Hour >= 16 || GameLoop.Hour <= 10 ? 2 : 1;

            int time = GameLoop.Hour > 12 && GameLoop.Hour <= 24 ? GameLoop.Hour - 12 : GameLoop.Hour, distanceLeft = targetTownDistance - save.Distance;
            string timing = GameLoop.Hour >= 12 && GameLoop.Hour < 24 ? " pm" : " am", activity = GameLoop.Activity == 1 ? "Low" : GameLoop.Activity == 2 ? "Medium" : GameLoop.Activity == 3 ? "High" : "Ravenous";

            supplyText.text = "Food: " + save.Food + "kg\nGas: " +  save.Gas + " cans\nDistance to Destination: " +  distanceLeft
                            + "km\nDistance Travelled: " + save.Distance + "km\nTime: " + time + timing + "\nActivity: " + activity;
        }

        /// <summary>
        /// Change game data based on supplied queries, to be done as the user resumes travel, preventing exploit of making progress and leaving on a bad event
        /// </summary>
        private void ChangeGameData(){
            for(int i = 0; i < queriesToPerform.Count(); i++){
                DataUser.dataManager.UpdateTravel(queriesToPerform[i], parametersForQueries[i].ToArray<object>());
                RefreshScreen();
            }
            queriesToPerform.Clear();
            parametersForQueries.Clear();
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
            Save save = DataUser.dataManager.GetSaveById(GameLoop.FileId);
            save.CurrentLocation = "The Road";
            save.PhaseNum = 1;
            DataUser.dataManager.UpdateSave(save);
            
            // Update town database with new town rolls.
            TownEntity townEntity = DataUser.dataManager.GetTownById(GameLoop.FileId);
            targetTownDistance = save.Distance + distanceLog[townEntity.CurTown][id-1];
            string destinationTown = nextDestinationLog[townEntity.CurTown][id-1];
            newTown = id == 1 ? townEntity.CurTown + id : townEntity.CurTown + 10;

            // Special cases due to mapping
            newTown = newTown == 4 ? 16 : newTown;

            // Update current town entity
            townEntity.PrevTown = townEntity.CurTown;
            townEntity.CurTown = newTown;
            townEntity.FoodPrice = towns[id-1].GetFoodPrice();
            townEntity.GasPrice = towns[id-1].GetGasPrice();
            townEntity.ScrapPrice = towns[id-1].GetScrapPrice();
            townEntity.MedkitPrice = towns[id-1].GetMedkitPrice();
            townEntity.TirePrice = towns[id-1].GetTirePrice();
            townEntity.BatteryPrice = towns[id-1].GetBatteryPrice();
            townEntity.AmmoPrice= towns[id-1].GetAmmoPrice();
            townEntity.FoodStock = towns[id-1].GetFoodStock();
            townEntity.GasStock = towns[id-1].GetGasStock();
            townEntity.ScrapStock = towns[id-1].GetScrapStock();
            townEntity.MedkitStock = towns[id-1].GetMedkitStock();
            townEntity.TireStock = towns[id-1].GetTireStock();
            townEntity.BatteryStock = towns[id-1].GetBatteryStock();
            townEntity.AmmoStock = towns[id-1].GetAmmoStock();
            townEntity.Side1Reward = towns[id-1].GetMissions()[0].GetMissionReward();
            townEntity.Side1Qty = towns[id-1].GetMissions()[0].GetMissionQty();
            townEntity.Side1Diff = towns[id-1].GetMissions()[0].GetMissionDifficulty();
            townEntity.Side1Type = towns[id-1].GetMissions()[0].GetMissionType();
            townEntity.Side2Reward = towns[id-1].GetMissions()[1].GetMissionReward();
            townEntity.Side2Qty = towns[id-1].GetMissions()[1].GetMissionQty();
            townEntity.Side2Diff = towns[id-1].GetMissions()[1].GetMissionDifficulty();
            townEntity.Side2Type = towns[id-1].GetMissions()[1].GetMissionType();
            townEntity.Side3Reward = towns[id-1].GetMissions()[2].GetMissionReward();
            townEntity.Side3Qty = towns[id-1].GetMissions()[2].GetMissionQty();
            townEntity.Side3Diff = towns[id-1].GetMissions()[2].GetMissionDifficulty();
            townEntity.Side3Type = towns[id-1].GetMissions()[2].GetMissionType();
            townEntity.NextTownName = destinationTown;
            townEntity.NextDistanceAway = targetTownDistance;
            DataUser.dataManager.UpdateTown(townEntity);

            towns.Clear();
            int distanceLeft = targetTownDistance - save.Distance;
            LaunchPopup(distanceLeft.ToString() + " km to " + destinationTown);
            InitializeScreen();
        }

        /// <summary>
        /// Refresh the screen (for post-event generation)
        /// </summary>
        public void RefreshScreen(){
            // Read the database for party info
            string tempPlayerText = "";
            Car car = DataUser.dataManager.GetCarById(GameLoop.FileId);
            carHealthBar.value = car.CarHP;

            IEnumerable<ActiveCharacter> characters = DataUser.dataManager.GetActiveCharacters().Where(c=>c.FileId == GameLoop.FileId).OrderByDescending(c=>c.IsLeader);
            ActiveCharacter[] tempCharacters = characters.ToArray<ActiveCharacter>();

            int used = 0;
            for(int i = 0; i < characters.Count(); i++){
                if(i == 0 && tempCharacters[i].CharacterName == null){
                    playerHealthBars[i].value = 0;
                    tempPlayerText += "\nCar\n";
                }
                else if(tempCharacters[i].CharacterName != null){
                    playerHealthBars[i].value = tempCharacters[i].Health;
                    tempPlayerText += i == 0 ? tempCharacters[i].CharacterName + "\nCar\n" : tempCharacters[i].CharacterName + "\n";
                }
                else{
                    playerHealthBars[i].value = 0;
                    tempPlayerText += "\n";
                }
                used++;
            }

            for(;used < 4; used++){
                playerHealthBars[used].value = 0;
            }           

            playerText.text = tempPlayerText;

            // Read the database for travel (key supplies, distance) info
            TownEntity townEntity = DataUser.dataManager.GetTownById(GameLoop.FileId);
            Save save = DataUser.dataManager.GetSaveById(GameLoop.FileId);

            int targetTownDistance = townEntity.NextDistanceAway;
            GameLoop.Hour = save.CurrentTime;
            GameLoop.Activity = GameLoop.Hour >= 21 || GameLoop.Hour <= 5 ? 4 : GameLoop.Hour >= 18 || GameLoop.Hour <= 8 ? 3 : GameLoop.Hour >= 16 || GameLoop.Hour <= 10 ? 2 : 1;

            int time = GameLoop.Hour > 12 && GameLoop.Hour <= 24 ? GameLoop.Hour - 12 : GameLoop.Hour, distanceLeft = targetTownDistance - save.Distance;
            string timing = GameLoop.Hour >= 12 && GameLoop.Hour < 24 ? " pm" : " am", activity = GameLoop.Activity == 1 ? "Low" : GameLoop.Activity == 2 ? "Medium" : GameLoop.Activity == 3 ? "High" : "Ravenous";

            supplyText.text = "Food: " + save.Food + "kg\nGas: " +  save.Gas + " cans\nDistance to Destination: " +  distanceLeft
                            + "km\nDistance Travelled: " + save.Distance + "km\nTime: " + time + timing + "\nActivity: " + activity;
        }

        /// <summary>
        /// Stop the car on the road, switching back to rest menu
        /// </summary>
        public void StopCar(){
            Save save = DataUser.dataManager.GetSaveById(GameLoop.FileId);
            save.PhaseNum = 2;
            DataUser.dataManager.UpdateSave(save);
            PopupActive = true;
            Timer = 0.0f;
            PrepRestScreen();
            restMenuMaster.SetActive(true);
            SceneManager.LoadScene(1);
        }

        /// <summary>
        /// Stop the car on the road because of destination arrival
        /// </summary>
        public void Arrive(){
            ChangeGameData();

            TownEntity townEntity = DataUser.dataManager.GetTownById(GameLoop.FileId);
            Save save = DataUser.dataManager.GetSaveById(GameLoop.FileId);
            save.PhaseNum = 0;
            save.CurrentLocation = townEntity.NextTownName;
            DataUser.dataManager.UpdateSave(save);
            PopupActive = true;
            Timer = 0.0f;
            PrepRestScreen();

            // Final combat section
            if(townEntity.NextTownName.Equals("Vancouver")){
                InFinalCombat = true;
                CombatManager.PrevMenuRef = this.gameObject;
                GameLoop.MainPanel.SetActive(false);
                StartCoroutine(GameLoop.LoadAsynchronously(3));
            }
            else{
                restMenuMaster.SetActive(true);
                restScreens[0].transform.parent.GetComponent<RestMenu>().RefreshScreen();
                SceneManager.LoadScene(1);
            }
        }

        /// <summary>
        /// Resume travelling from a popup
        /// </summary>
        public void ResumeTravel(){
            PopupActive = false;
            ChangeGameData();
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
            Car car = DataUser.dataManager.GetCarById(GameLoop.FileId);
            return car.CarHP == 0 || car.IsBatteryDead == 1 || car.IsTireFlat == 1;
        }

        /// <summary>
        /// Check if party is close to destination
        /// </summary>
        /// <returns>True if within 1hr of travel, false otherwise</returns>
        private bool IsCloseToDestination(){
            Save save = DataUser.dataManager.GetSaveById(GameLoop.FileId);
            TownEntity townEntity = DataUser.dataManager.GetTownById(GameLoop.FileId);
            int speed = save.PaceMode, actualSpeed = speed == 1 ? 65 : speed == 2 ? 80 : 95;
            return save.Distance < townEntity.NextDistanceAway && save.Distance >= townEntity.NextDistanceAway - actualSpeed;
        }

        /// <summary>
        /// Select a destination
        /// </summary>
        /// <param name="id">Id of the button that was clicked.</param>
        private void UpdateButtons(){
            TownEntity townEntity = DataUser.dataManager.GetTownById(GameLoop.FileId);
            int townNum = townEntity.CurTown;
            string supplies = "";

            // If on-route to Vancouver, no need to proceed with updating
            if(townNum == 21 || townNum == 39 || townNum == 30){
                return;
            }

            // If the current town number has only one way to go, disable the 2nd option
            destinationButton2.interactable = !CheckTownList(townNum);

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
        /// Drive some distance, increasing distance, changing time and resources, mini-refresh, and damaging the car and players.
        /// </summary>
        /// <returns>True if drive had no events from updating, false if drive had events from updating</returns>
        private bool Drive(){
            TownEntity townEntity = DataUser.dataManager.GetTownById(GameLoop.FileId);
            string nextTown = townEntity.NextTownName, tempStr = "";
            targetTownDistance = townEntity.NextDistanceAway;

            Car car = DataUser.dataManager.GetCarById(GameLoop.FileId);
            int carHP = car.CarHP, batteryStatus = car.IsBatteryDead, tireStatus = car.IsTireFlat, engineUpgrade = car.EngineUpgrade, gardenUpgrade = car.MiscUpgrade1;

            Save save = DataUser.dataManager.GetSaveById(GameLoop.FileId);
            int overallTime = save.OverallTime, speed = save.PaceMode, oldDistance = save.Distance, rations = save.RationMode, speedActual = speed == 1 ? 65 : speed == 2 ? 80 : 95;
            int newDistance = oldDistance + speedActual, decay = speed == 1 ? 3 : speed == 2 ? 5 : 7, tire = save.Tire, battery = save.Battery;
            float gas = save.Gas;

            newDistance = engineUpgrade == 1 ? newDistance + 10 : newDistance;
            newDistance = newDistance >= targetTownDistance ? targetTownDistance : newDistance;
            GameLoop.Hour = save.CurrentTime;

            List<object> parameters;

            // If the car is out of gas, broke, has a dead battery, or a flat tire, do no driving. 
            // Alternatively, if a battery or tire is available, replace but still don't drive.
            if(gas == 0f || carHP == 0){
                tempStr = gas == 0f ? "The car is out of gas.\nProcure some by trading or scavenging." : "The car is broken.\nRepair the car with some scrap.";
                LaunchPopup(tempStr);
                return false;
            }
            else if((battery > 0 && batteryStatus == 1) || (tire > 0 && tireStatus == 1)){
                if(battery > 0 && batteryStatus == 1){
                    tempStr = "You spend an hour replacing your dead battery.";
                    car.IsBatteryDead = 0;
                    save.Battery--;
                }
                else{
                    tempStr = "You spend an hour replacing your flat tire.";
                    car.IsTireFlat = 0;
                    save.Tire--;
                }
                LaunchPopup(tempStr);

                GameLoop.Hour++;
                if(GameLoop.Hour == 25){
                    GameLoop.Hour = 1;
                }
                save.OverallTime++;

                string repairCommand = "UPDATE Save SET CurrentTime = ?, overallTime = ?, battery = ?, tire = ? WHERE Id = ?";
                parameters = new List<object>(){GameLoop.Hour, save.OverallTime, save.Battery, save.Tire, GameLoop.FileId};
                queriesToPerform.Add(repairCommand);
                parametersForQueries.Add(parameters);

                repairCommand = "UPDATE Car SET IsBatteryDead = ? IsTireFlat = ? WHERE Id = ?";
                parameters = new List<object>(){car.IsBatteryDead, car.IsTireFlat, GameLoop.FileId};
                queriesToPerform.Add(repairCommand);
                parametersForQueries.Add(parameters);
                return false;
            }
            else if(batteryStatus == 1 || tireStatus == 1){
                tempStr = batteryStatus == 1 ? "The car has a dead battery.\nTrade for another one." : "The car has a flat tire.\nTrade for another one.";
                LaunchPopup(tempStr);
                return false;
            }

            GameLoop.Hour++;
            if(GameLoop.Hour == 25){
                GameLoop.Hour = 1;
            }
            save.OverallTime++;

            string temp = "UPDATE Save SET CurrentTime = ?, overallTime = ?, distance = ? WHERE Id = ?";
            parameters = new List<object>(){GameLoop.Hour, save.OverallTime, newDistance, GameLoop.FileId};
            queriesToPerform.Add(temp);
            parametersForQueries.Add(parameters);

            carHP = carHP - decay > 0 ? carHP - decay : 0;
            carHealthBar.value = carHP;

            temp = "UPDATE Car SET carHP = ? WHERE Id = ?";
            parameters = new List<object>(){carHP, GameLoop.FileId};
            queriesToPerform.Add(temp);
            parametersForQueries.Add(parameters);

            // Characters will always take some damage when travelling, regardless of rations
            IEnumerable<ActiveCharacter> characters = DataUser.dataManager.GetActiveCharacters().Where<ActiveCharacter>(c=>c.FileId == GameLoop.FileId).OrderByDescending(c=>c.IsLeader);
            int overallFood = save.Food, hpDecay = 0, moraleDecay = 1;
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

            temp = "UPDATE ActiveCharacter SET Health = ?, Morale = ? WHERE Id = ?";
            foreach(ActiveCharacter character in characters){
                int curHp = character.Health, curMorale = character.Morale;
                hpDecay = overallFood <= 0 ? 5 : hpDecay;

                overallFood -= GameLoop.RationsMode;
                overallFood = overallFood <= 0 ? 0 : overallFood;
                curHp = curHp - hpDecay > 0 ? curHp - hpDecay : 0;
                curMorale = curMorale - moraleDecay > 0 ? curMorale - moraleDecay : 0;
                queriesToPerform.Add(temp);
                parameters = new List<object>(){curHp, curMorale, character.Id};
                parametersForQueries.Add(parameters);
            }

            ActiveCharacter[] tempCharacters = characters.ToArray<ActiveCharacter>();
            string playerStr = "";
            for(int i = 0; i < tempCharacters.Count(); i++){
                if(tempCharacters[i].CharacterName == null && i == 0){
                    playerStr += "\nCar\n";
                }
                else if(tempCharacters[i].CharacterName == null){
                    playerStr += "\n";
                }
                else{
                    playerHealthBars[i].value = tempCharacters[i].Health;
                    playerStr += i == 0 ? tempCharacters[i].CharacterName + "\nCar\n" : tempCharacters[i].CharacterName + "\n";
                }
            }
            playerText.text = playerStr;

            // If garden upgrade was found, add 1 kg of food back.
            overallFood += gardenUpgrade == 1 ? 1 : 0;
            // Each timestep consumes quarter of a gas resource
            gas -= 0.25f;

            temp = "UPDATE Save SET food = ?, gas = ? WHERE Id = ?";
            parameters = new List<object>(){overallFood, gas, GameLoop.FileId};
            queriesToPerform.Add(temp);
            parametersForQueries.Add(parameters);

            GameLoop.Activity = GameLoop.Hour >= 21 || GameLoop.Hour <= 5 ? 4 : GameLoop.Hour >= 18 || GameLoop.Hour <= 8 ? 3 : GameLoop.Hour >= 16 || GameLoop.Hour <= 10 ? 2 : 1;
            int time = GameLoop.Hour > 12 && GameLoop.Hour <= 24 ? GameLoop.Hour - 12 : GameLoop.Hour, distanceLeft = targetTownDistance - newDistance;
            string timing = GameLoop.Hour >= 12 && GameLoop.Hour < 24 ? " pm" : " am", activity = GameLoop.Activity == 1 ? "Low" : GameLoop.Activity == 2 ? "Medium" : GameLoop.Activity == 3 ? "High" : "Ravenous";

            supplyText.text = "Food: " + overallFood + "kg\nGas: " +  gas + " cans\nDistance to Destination: " +  distanceLeft
                            + "km\nDistance Travelled: " + newDistance + "km\nTime: " + time + timing + "\nActivity: " + activity;           

            // Check if any character has died.
            if(HasCharacterDied()){
                return false;
            }

            // Transition back to town rest if distance matches the target
            if(newDistance == targetTownDistance){
                PopupActive = true;
                int prevTown = townEntity.PrevTown, curTown = townEntity.CurTown;
                destinationImageDisplay.sprite = GameLoop.RetrieveMapImage(prevTown, curTown);
                destinationPopup.SetActive(true);
                destinationPopupText.text = nextTown;
                travelViewObject.SetActive(false);
                GameLoop.MainPanel.SetActive(true);
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
            IEnumerable<ActiveCharacter> characters = DataUser.dataManager.GetActiveCharacters().Where<ActiveCharacter>(c=>c.FileId == GameLoop.FileId).OrderByDescending(c=>c.IsLeader);
            List<string> deadCharacters = new List<string>();
            bool flag = false;
            string tempDisplayText = "";

            foreach(ActiveCharacter character in characters){
                if(character.Health <= 0){
                    // Leader died - game over
                    if(character.IsLeader == 1){
                        DataUser.dataManager.DeleteActiveCharacter(character.Id);
                        tempDisplayText += character.CharacterName + " has died.";
                        LaunchPopup(tempDisplayText);
                        RefreshScreen();
                        return true;
                    }
                    DataUser.dataManager.DeleteActiveCharacter(character.Id);
                    deadCharacters.Add(character.CharacterName);
                    flag = true;
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
                        tempDisplayText += deadCharacters[i] + " has ";
                    }
                    else if(i == deadCharacters.Count - 1){
                        tempDisplayText += "and " + deadCharacters[i] + "have ";
                    }
                    else{
                        tempDisplayText += deadCharacters[i] + ", ";
                    }
                }

                LaunchPopup(tempDisplayText + "died");
                RefreshScreen();
                return true;
            }
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
            popupSound.Play();
            popupText.text = msg;
            popup.SetActive(true);
            PopupActive = true;
        }
    }
}

