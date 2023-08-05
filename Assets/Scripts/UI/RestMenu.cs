using System.Collections;
using System.Collections.Generic;
using System.Data;
using UnityEngine;
using UnityEngine.UI;
using Mono.Data.Sqlite;
using TMPro;
using Database;

namespace UI{
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

        private float restHours = 1;
        private Coroutine coroutine;

        private void Start(){
            RefreshScreen();
        }

        /// <summary>
        /// Refresh the screen upon loading the rest menu.
        /// </summary>
        public void RefreshScreen(){
            IDbConnection dbConnection = GameDatabase.CreateSavesAndOpenDatabase();
            IDbCommand dbCommandReadValues = dbConnection.CreateCommand();
            dbCommandReadValues.CommandText = "SELECT * FROM SaveFilesTable LEFT JOIN ActiveCharactersTable ON SaveFilesTable.charactersId = ActiveCharactersTable.id " + 
                                              "WHERE SaveFilesTable.id = " + GameLoop.FileId;
            IDataReader dataReader = dbCommandReadValues.ExecuteReader();
            dataReader.Read();

            suppliesText1.text = "Food: " + dataReader.GetInt32(7) + "kg\n\nGas: " + dataReader.GetFloat(8) + "L\n\nScrap: " + dataReader.GetInt32(9) + "\n\nMoney: $" +
                                 dataReader.GetInt32(10) + "\n\nMedkit: " + dataReader.GetInt32(11);
            suppliesText2.text = "Tires: " + dataReader.GetInt32(12) + "\n\nBatteries: " + dataReader.GetInt32(13) + "\n\nAmmo: " + dataReader.GetInt32(14);
            locationText.text = dataReader.GetString(5);

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
            
            rationsText.text = GameLoop.RationsMode == 1 ? "Current Rations: Low" : GameLoop.RationsMode == 2 ?  "Current Rations: Medium" : "Current Rations: High";
            paceText.text = GameLoop.Pace== 1 ? "Slow\n40km/h" : GameLoop.Pace == 2 ?  "Average\n50km/h" : "Fast\n60km/h";
            int time = GameLoop.Hour > 12 && GameLoop.Hour <= 24 ? GameLoop.Hour - 12 : GameLoop.Hour;
            string timing = GameLoop.Hour >= 12 && GameLoop.Hour < 24 ? " pm" : " am", activity = GameLoop.Activity == 1 ? "Low" : GameLoop.Activity == 2 ? "Medium" : GameLoop.Activity == 3 ? "High" : "Ravenous";

            timeActivityText.text = "Current Time: " + time + timing + "; Activity: " + activity;

            dbConnection.Close();
        }

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
        /// Change selected hours based on the slider
        /// </summary>
        public void ChangeSelectedHours(){
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
        /// Go to scavenging mode
        /// </summary>
        public void GoScavenge(){
            Debug.Log("To be implemented.");
        }

        /// <summary>
        /// Wait for a trader
        /// </summary>
        public void WaitForTrader(){
            StartCoroutine(Delay(1));
        }

        /// <summary>
        /// Let the party rest.
        /// </summary>
        public void RestParty(){
            StartCoroutine(Delay(2));
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
        /// Perform the trade action displayed.
        /// </summary>
        /// <param name="button">The button id pressed - 0 for decline, 1 for accept</param>
        public void TradeAction(int button){
            // Accept trade
            if(button == 1){

            }
            acceptButton.interactable = false;
            declineButton.interactable = false;
            waitButton.interactable = true;
            tradeReturnButton.interactable = true;
            traderText.text = "No one appeared.";
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
        /// Decrement food while performing waiting actions.
        /// </summary>
        private void DecrementFood(){
            IDbConnection dbConnection = GameDatabase.CreateSavesAndOpenDatabase();
            IDbCommand dbCommandReadValues = dbConnection.CreateCommand();
            dbCommandReadValues.CommandText = "SELECT * FROM SaveFilesTable LEFT JOIN ActiveCharactersTable ON SaveFilesTable.charactersId = ActiveCharactersTable.id " + 
                                              "WHERE SaveFilesTable.id = " + GameLoop.FileId;
            IDataReader dataReader = dbCommandReadValues.ExecuteReader();
            dataReader.Read();

            int overallFood = dataReader.GetInt32(7);

            // For each living character on the team, they consume 1, 2, or 3 units of food each hour depending on the ration mode.
            for(int i = 0; i < 4; i++){
                int index = 20 + 9 * i;
                if(!dataReader.IsDBNull(index)){
                    overallFood = GameLoop.RationsMode == 1 ? overallFood - 1 : GameLoop.RationsMode == 2 ? overallFood - 2 : overallFood - 3;
                }
            }

            IDbCommand dbCommandUpdateValue = dbConnection.CreateCommand();
            dbCommandUpdateValue.CommandText = "UPDATE SaveFilesTable SET food = " + overallFood + " WHERE id = " + GameLoop.FileId;
            dbCommandUpdateValue.ExecuteNonQuery();
            dbConnection.Close();
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
                ChangeTime();

                int traderChange = Random.Range(1,6);
                if(traderChange <= 2){
                    acceptButton.interactable = true;
                    declineButton.interactable = true;
                    waitButton.interactable = false;
                    tradeReturnButton.interactable = false;
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
                    ChangeTime();
                }

                restCancelButton.interactable = false;
                restReturnButton.interactable = true;
                restStartButton.interactable = true;
                restHoursSlider.interactable = true;
                restDescText.text = "How long would you like to rest for? Supplies will be consumed per hour.";
            }
        }
    }

}

