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
        [Tooltip("Leader text")]
        [SerializeField]
        private TextMeshProUGUI leaderText;

        [Tooltip("Leader health")]
        [SerializeField]
        private Slider leaderHealth;

        [Tooltip("Heal leader button")]
        [SerializeField]
        private Button healLeaderButton;

        [Tooltip("Friend text")]
        [SerializeField]
        private TextMeshProUGUI friend1Text;

        [Tooltip("Friend health")]
        [SerializeField]
        private Slider friend1Health;

        [Tooltip("Friend text")]
        [SerializeField]
        private TextMeshProUGUI friend2Text;

        [Tooltip("Friend health")]
        [SerializeField]
        private Slider friend2Health;

        [Tooltip("Friend text")]
        [SerializeField]
        private TextMeshProUGUI friend3Text;

        [Tooltip("Friend health")]
        [SerializeField]
        private Slider friend3Health;

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

            suppliesText1.text = "Food: " + dataReader.GetInt32(7) + "kg\n\nGas: " + dataReader.GetInt32(8) + "L\n\nScrap: " + dataReader.GetInt32(9) + "\n\nMoney: $" +
                                 dataReader.GetInt32(10) + "\n\nMedkit: " + dataReader.GetInt32(11);
            suppliesText2.text = "Tires: " + dataReader.GetInt32(12) + "\n\nBatteries: " + dataReader.GetInt32(13) + "\n\nAmmo: " + dataReader.GetInt32(14);
            locationText.text = dataReader.GetString(5);

            string morale = dataReader.GetInt32(25) >= 20 ? dataReader.GetInt32(25) >= 40 ? dataReader.GetInt32(25) >= 60 ? dataReader.GetInt32(25) >= 80 
                            ? "Hopeful" : "placeholder" : "Elated" : "Glum" : "Despairing";
            leaderText.text = dataReader.GetString(18) + "\n" + GameLoop.Perks[dataReader.GetInt32(19)] + "\n" + GameLoop.Traits[dataReader.GetInt32(20)];
            leaderHealth.value = dataReader.GetInt32(26);

            healLeaderButton.interactable = leaderHealth.value != 100;

            SetTime();
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

            rationsText.text = GameLoop.RationsMode == 1 ? "Current Rations: Low" : GameLoop.RationsMode == 2 ?  "Current Rations: Medium" : "Current Rations: High";
        }

        /// <summary>
        /// Toggle current travel pace
        /// </summary>
        public void TogglePace(){
            GameLoop.Pace++;
            if(GameLoop.Pace > 3){
                GameLoop.Pace = 1;
            }

            paceText.text = GameLoop.Pace== 1 ? "Slow\n40km/h" : GameLoop.Pace == 2 ?  "Average\n50km/h" : "Fast\n60km/h";
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
            coroutine = StartCoroutine(Delay(2));
        }

        /// <summary>
        /// Change the ingame time
        /// </summary>
        private void ChangeTime(){
            GameLoop.Hour++;

            if(GameLoop.Hour == 25){
                GameLoop.Hour = 1;
            }

            SetTime();

            IDbConnection dbConnection = GameDatabase.CreateSavesAndOpenDatabase();
            IDbCommand dbCommandReadValues = dbConnection.CreateCommand();
            dbCommandReadValues.CommandText = "SELECT * FROM SaveFilesTable WHERE id = " + GameLoop.FileId;
            IDataReader dataReader = dbCommandReadValues.ExecuteReader();
            dataReader.Read();
            int overallTime = dataReader.GetInt32(16);
            
            //dbConnection.Close();
            //dbConnection = GameDatabase.CreateSavesAndOpenDatabase();
            IDbCommand dbCommandUpdateValue = dbConnection.CreateCommand();
            dbCommandUpdateValue.CommandText = "UPDATE SaveFilesTable SET time = " + GameLoop.Hour + ", overallTime = " + (overallTime + 1) + " WHERE id = " + GameLoop.FileId;
            dbCommandUpdateValue.ExecuteNonQuery();
            dbConnection.Close();
        }

        private void SetTime(){
            int time = GameLoop.Hour > 12 && GameLoop.Hour <= 24 ? GameLoop.Hour - 12 : GameLoop.Hour;
            string timing = GameLoop.Hour >= 12 && GameLoop.Hour < 24 ? " pm" : " am", activity = GameLoop.Activity == 1 ? "Low" : GameLoop.Activity == 2 ? "Medium" : GameLoop.Activity == 3 ? "High" : "Ravenous";

            timeActivityText.text = "Current Time: " + time + timing + "; Activity: " + activity;
        }

        /// <summary>
        /// Create and open a connection to the database to access active players
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

        public void CancelRest(){
            StopCoroutine(coroutine);
            restCancelButton.interactable = false;
            restReturnButton.interactable = true;
            restStartButton.interactable = true;
            restHoursSlider.interactable = true;
            restDescText.text = "How long would you like to rest for? Supplies will be consumed per hour.";
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
                    ChangeTime();
                }

                if(restHoursSlider.value == 1){
                    restDescText.text = "Resting.";
                    yield return new WaitForSeconds(1.0f);
                    restDescText.text = "Resting..";
                    yield return new WaitForSeconds(1.0f);
                    restDescText.text = "Resting...";
                    yield return new WaitForSeconds(1.0f);
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