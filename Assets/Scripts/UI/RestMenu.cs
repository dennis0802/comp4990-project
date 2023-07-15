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

        [Tooltip("Rest hours slider")]
        [SerializeField]
        private Slider restHoursSlider;

        private float restHours = 1;
        private bool coroutineStarted = false;

        /// <summary>
        /// Refresh the screen upon loading the rest menu.
        /// </summary>
        private void RefreshScreen(){
            ToggleRations();
        }

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
            traderText.text = "Waiting for trader...";
            StartCoroutine(Delay(1));
        }

        /// <summary>
        /// Let the party rest.
        /// </summary>
        public void RestParty(){
            Debug.Log("to be implemented.");
        }

        /// <summary>
        /// Change the ingame time
        /// </summary>
        private void ChangeTime(){
            GameLoop.Hour++;

            if(GameLoop.Hour == 25){
                GameLoop.Hour = 1;
            }

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

        /// <summary>
        /// Delay after a button press
        /// </summary>
        /// <param name="mode">The mode/menu to interact with after the delay</param>
        private IEnumerator Delay(int mode){
            waitButton.interactable = false;
            tradeReturnButton.interactable = false;
            yield return new WaitForSeconds(3.0f);
            waitButton.interactable = true;
            tradeReturnButton.interactable = true;
            coroutineStarted = true;

            if(mode == 1){
                int traderChange = Random.Range(1,6);
                if(traderChange <= 2){
                    acceptButton.interactable = true;
                    declineButton.interactable = true;
                    waitButton.interactable = false;
                    tradeReturnButton.interactable = false;
                }
                traderText.text = traderChange <= 2 ? "A trader appeared making the following offer:" : "No one appeared.";
            }
            if(mode == 2){
                // Heal the party
                restHours--;
                if(restHours > 1){
                    restHoursText.text = restHours > 1 ? restHours + " hours" : restHours + " hour";
                }
            }
            ChangeTime();
            coroutineStarted = false;
        }
    }
}