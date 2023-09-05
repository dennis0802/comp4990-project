using System.Collections;
using System.Collections.Generic;
using System.Data;
using UnityEngine;
using UnityEngine.UI;
using RestPhase;
using Database;
using UI;
using TMPro;
using Mono.Data.Sqlite;

namespace CombatPhase{
    [DisallowMultipleComponent]
    public class CombatLoop : MonoBehaviour
    {
        [Header("Intro")]
        [Tooltip("Buttons to select weapons a player can equip.")]
        [SerializeField]
        private Button[] weaponButtons;

        [Tooltip("Buttons to start the combat.")]
        [SerializeField]
        private Button startButton;

        [Tooltip("Background panel")]
        [SerializeField]
        private GameObject backgroundPanel;

        [Header("Mid-Combat")]
        [Tooltip("Player spawn points when combat starts.")]
        [SerializeField]
        private GameObject[] playerSpawnPoints;

        [Tooltip("Enemy spawn points when combat starts.")]
        [SerializeField]
        private GameObject[] enemySpawnPoints;

        [Tooltip("Pickup spawn points when combat starts.")]
        [SerializeField]
        private GameObject[] pickupSpawnPoints;

        [Tooltip("Player object")]
        [SerializeField]
        private GameObject playerPrefab;

        [Tooltip("Combat text")]
        [SerializeField]
        private TextMeshProUGUI combatText;

        [Tooltip("Player health bar")]
        [SerializeField]
        private Slider playerHealthBar;

        // For scavenging, to allow scavenging up to x seconds.
        private float scavengeTimeLimit = 0.0f;
        public static int GunSelected = -1, PhysSelected = -1;
        private List<string> weaponList = new List<string>(){"Pistol", "Rifle", "Shotgun", "Knife", "Bat", "Shovel"};

        public static bool InCombat = false;

        // Start is called before the first frame update
        void Start(){

        }

        void OnEnable()
        {
            UpdateIntroScreen();
        }

        // Update is called once per frame
        void Update()
        {   
            if(InCombat){
                combatText.text = Player.UsingGun ? "Equipped: " + weaponList[GunSelected] + "\nLoaded = " + Player.AmmoLoaded + "\nTotal Ammo: " + Player.TotalAvailableAmmo 
                                                  : "Equipped: " + weaponList[PhysSelected];
            }
            
            if(RestMenu.IsScavenging){
                scavengeTimeLimit -= Time.deltaTime;
                if(scavengeTimeLimit <= 0.0f){
                    InCombat = false;
                    RestMenu.IsScavenging = false;
                    backgroundPanel.SetActive(true);
                }
            }
        }

        /// <summary>
        /// Update the combat intro screen
        /// </summary>
        public void UpdateIntroScreen(){
            int selectedCount = 0;
            foreach (Button button in weaponButtons)
            {
                selectedCount += !button.interactable ? 1 : 0;
            }
            startButton.interactable = selectedCount == 2;
        }

        /// <summary>
        /// Start the combat phase
        /// </summary>
        public void StartCombat(){
            InCombat = true;
            backgroundPanel.SetActive(false);

            for(int i = 0; i < weaponButtons.Length; i++){
                if(!weaponButtons[i].interactable && i <= 2){
                    GunSelected = i;
                }
                else if(!weaponButtons[i].interactable && i > 2){
                    PhysSelected = i;
                }
            }

            // If scavenging, difficulty determines the amount of time.
            if(RestMenu.IsScavenging){
                IDbConnection dbConnection = GameDatabase.CreateActiveCharactersAndOpenDatabase();
                IDbCommand dbCommandReadValue = dbConnection.CreateCommand();
                dbCommandReadValue.CommandText = "SELECT * FROM ActiveCharactersTable WHERE id = " + GameLoop.FileId;
                IDataReader dataReader = dbCommandReadValue.ExecuteReader();
                dataReader.Read();

                int diff = dataReader.GetInt32(4);
                scavengeTimeLimit = diff == 1 || diff == 3 ? 60.0f : 40.0f;

                dbConnection.Close();
            }
        }
    }
}