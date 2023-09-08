using System.Collections;
using System.Collections.Generic;
using System.Data;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
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

        [Tooltip("Enemy spawn points when combat starts.")]
        [SerializeField]
        private GameObject[] enemySpawnPoints;

        [Tooltip("Pickup spawn points when combat starts.")]
        [SerializeField]
        private GameObject[] pickupSpawnPoints;

        [Tooltip("Pickups that the player can collect")]
        [SerializeField]
        private GameObject[] pickupPrefabs;

        [Tooltip("Player object")]
        [SerializeField]
        private GameObject playerPrefab;

        [Tooltip("AI teammate object")]
        [SerializeField]
        private GameObject allyPrefab;

        [Tooltip("Enemy object")]
        [SerializeField]
        private GameObject enemyPrefab;

        [Tooltip("Camera used during combat phase")]
        [SerializeField]
        private GameObject combatCamera;

        [Tooltip("Combat text")]
        [SerializeField]
        private TextMeshProUGUI combatText;

        [Tooltip("Player health bar")]
        [SerializeField]
        private Slider playerHealthBar;

        [Header("End of Combat")]
        [Tooltip("End of combat screen to display stats")]
        [SerializeField]
        private GameObject endCombatScreen;

        [Tooltip("Text object displaying the stats")]
        [SerializeField]
        private TextMeshProUGUI endCombatText;

        // To track player spawn points
        private GameObject[] playerSpawnPoints;
        // To track the player
        private GameObject player;
        private int diff;
        // For scavenging, to allow scavenging up to x seconds.
        private float scavengeTimeLimit = 0.0f, timePassed = 0.0f, spawnTime = 0.0f;
        // To determine weapons selected
        public static int GunSelected = -1, PhysSelected = -1;
        // List of weapons
        private List<string> weaponList = new List<string>(){"Pistol", "Rifle", "Shotgun", "Knife", "Bat", "Shovel"};

        public static bool InCombat = false;
        public static GameObject Camera, CombatEnvironment;

        // Start is called before the first frame update
        void Start(){
            Camera = combatCamera;
        }

        void OnEnable()
        {
            UpdateIntroScreen();
            if(SceneManager.GetActiveScene().buildIndex == 3 && CombatEnvironment == null){
                CombatEnvironment = GameObject.FindWithTag("CombatEnvironment");
            }
            else if(SceneManager.GetActiveScene().buildIndex != 3 && CombatEnvironment != null){
                CombatEnvironment = null;
            }
            CombatEnvironment.SetActive(false);
        }

        // Update is called once per frame
        void Update()
        {   
            combatCamera.SetActive(InCombat && !PauseMenu.IsPaused);
            if(InCombat){
                combatText.gameObject.SetActive(!PauseMenu.IsPaused);

                // Players have limited time per scavenge session
                if(RestMenu.IsScavenging){
                    scavengeTimeLimit -= Time.deltaTime;
                    timePassed += Time.deltaTime;

                    if(scavengeTimeLimit <= 0.0f){
                        InCombat = false;
                        RestMenu.IsScavenging = false;
                        RestMenu.Panel.SetActive(true);
                        combatCamera.SetActive(false);
                        combatText.gameObject.SetActive(false);
                        CombatEnvironment.SetActive(false);
                        endCombatScreen.SetActive(true);
                        Cursor.lockState = CursorLockMode.None;

                        // Update the database here
                        int foodFound = player.GetComponent<Player>().suppliesGathered[0] * 20, gasFound = player.GetComponent<Player>().suppliesGathered[1],
                            scrapFound = player.GetComponent<Player>().suppliesGathered[2] * 10, moneyFound = player.GetComponent<Player>().suppliesGathered[3] * 15,
                            medkitFound = player.GetComponent<Player>().suppliesGathered[4], ammoFound = player.GetComponent<Player>().suppliesGathered[5];

                        string temp = "You collected:\n";
                        temp += foodFound > 0 ? "* " + foodFound + " kg of food\n" : "";
                        temp += gasFound > 0 ? gasFound == 1 ? "* " + gasFound + " can of gas\n" : "* " + gasFound + " cans of gas\n" : "";
                        temp += scrapFound > 0 ? "* " + scrapFound + " scrap\n" : "";
                        temp += moneyFound > 0 ? "* $" + moneyFound : "";
                        temp += medkitFound > 0 ? medkitFound == 1 ? "* " + medkitFound + " medkit\n" : "* " + medkitFound + " medkits\n" : "";
                        temp += ammoFound > 0 ? "* " + ammoFound + " ammo\n" : "";

                        temp += Equals(temp, "You collected:\n") ? "Nothing." : "";
                        endCombatText.text = temp;
                    }

                    // Spawn pickup after enough time has passed
                    if(timePassed >= spawnTime){
                        timePassed = 0.0f;
                        int spawnSelected, itemSelected;

                        do{
                            spawnSelected = Random.Range(0, pickupSpawnPoints.Length);
                        }while(pickupSpawnPoints[spawnSelected].GetComponent<SpawnPoint>().inUse);

                        pickupSpawnPoints[spawnSelected].GetComponent<SpawnPoint>().inUse = true;
                        itemSelected = Random.Range(0, pickupPrefabs.Length);

                        GameObject spawn = Instantiate(pickupPrefabs[itemSelected], pickupSpawnPoints[spawnSelected].transform.position, pickupSpawnPoints[spawnSelected].transform.rotation);
                        spawn.transform.SetParent(CombatEnvironment.transform);
                    }
                    
                    
                }
                combatText.text = Player.UsingGun ? "Equipped: " + weaponList[GunSelected] + "\nLoaded = " + Player.AmmoLoaded + "\nTotal Ammo: " + Player.TotalAvailableAmmo 
                                    : "Equipped: " + weaponList[PhysSelected];
                combatText.text += RestMenu.IsScavenging ? "\nTime: " + System.Math.Round(scavengeTimeLimit, 2) : "";
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
            RestMenu.Panel.SetActive(false);
            CombatEnvironment.SetActive(true);
            Cursor.lockState = CursorLockMode.Locked;

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

                diff = dataReader.GetInt32(4);
                scavengeTimeLimit = diff == 1 || diff == 3 ? 20.0f : 40.0f;
                spawnTime = diff == 1 || diff == 3 ? 10.0f : 15.0f;

                dbConnection.Close();
            }

            playerSpawnPoints = GameObject.FindGameObjectsWithTag("PlayerSpawn");
            int selected;
            
            do
            {
                selected = Random.Range(0, playerSpawnPoints.Length);
            } while (playerSpawnPoints[selected].GetComponent<SpawnPoint>().inUse); 

            playerSpawnPoints[selected].GetComponent<SpawnPoint>().inUse = true;
            player = Instantiate(playerPrefab, playerSpawnPoints[selected].transform.position, playerSpawnPoints[selected].transform.rotation);
            player.transform.SetParent(CombatEnvironment.transform);

            // Load AI teammates in
        }

        /// <summary>
        /// End combat, saving results and returning to the rest menu.
        /// </summary>
        public void EndCombat(){
            SceneManager.LoadScene(1);
        }
    }
}