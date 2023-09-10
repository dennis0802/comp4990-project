using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;
using RestPhase;
using Database;
using UI;
using AI;
using TMPro;
using Mono.Data.Sqlite;
using AI.States;

namespace CombatPhase{
    [DisallowMultipleComponent]
    public class CombatManager : MonoBehaviour
    {
        [Header("Intro")]
        [Tooltip("Buttons to select weapons a player can equip.")]
        [SerializeField]
        private Button[] weaponButtons;

        [Tooltip("Buttons to start the combat.")]
        [SerializeField]
        private Button startButton;

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

        [Header("Agents")]
        [Tooltip("The mind or global state agents are in.")]
        [SerializeField]
        private BaseState mind;

        [Tooltip("The maimum number of agents that can be updated in a single frame.")]
        [Min(0)]
        [SerializeField]
        private int maxAgentsPerUpdate;

        // All agents in the scene
        public List<BaseAgent> Agents {get; private set;} = new();

        // All registered states
        private static readonly Dictionary<Type, BaseState> RegisteredStates = new();
        // To track spawn points for the party, enemies, and pickups
        private GameObject[] playerSpawnPoints, enemySpawnPoints, pickupSpawnPoints;
        // To track the player
        private GameObject player, restMenu;
        private int diff;
        private int _currentAgentIndex;
        // For scavenging, to allow scavenging up to x seconds.
        private float scavengeTimeLimit = 0.0f, itemTimer = 0.0f, spawnItemTime = 0.0f;
        private float spawnEnemyTime = 0.0f, enemyTimer = 0.0f;
        // To determine weapons selected
        public static int GunSelected = -1, PhysSelected = -1;
        // List of weapons
        private List<string> weaponList = new List<string>(){"Pistol", "Rifle", "Shotgun", "Knife", "Bat", "Shovel"};
        protected static CombatManager Singleton;
        public static bool InCombat = false;
        public static GameObject Camera, CombatEnvironment, RestMenuRef;
        public static BaseState Mind => Singleton.mind;

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
                    itemTimer += Time.deltaTime;
                    enemyTimer += Time.deltaTime;

                    if(scavengeTimeLimit <= 0.0f){
                        InCombat = false;
                        RestMenu.IsScavenging = false;
                        RestMenu.Panel.SetActive(true);
                        combatCamera.SetActive(false);
                        combatText.gameObject.SetActive(false);
                        CombatEnvironment.SetActive(false);
                        endCombatScreen.SetActive(true);
                        Cursor.lockState = CursorLockMode.None;
                        
                        int foodFound = player.GetComponent<Player>().suppliesGathered[0] * 20, gasFound = player.GetComponent<Player>().suppliesGathered[1],
                            scrapFound = player.GetComponent<Player>().suppliesGathered[2] * 10, moneyFound = player.GetComponent<Player>().suppliesGathered[3] * 15,
                            medkitFound = player.GetComponent<Player>().suppliesGathered[4], ammoFound = player.GetComponent<Player>().suppliesGathered[5] * 10;

                        // Update the database
                        // Ammo will be counted as total avaialble plus loaded since collected ammo can be used during combat
                        IDbConnection dbConnection = GameDatabase.CreateActiveCharactersAndOpenDatabase();
                        IDbCommand dbCommandReadValue = dbConnection.CreateCommand();
                        dbCommandReadValue.CommandText = "SELECT leaderName, friend1Name, friend2Name, friend3Name FROM ActiveCharactersTable WHERE id = " + GameLoop.FileId;
                        IDataReader dataReader = dbCommandReadValue.ExecuteReader();
                        dataReader.Read();

                        int livingMembers = 0;
                        for(int i = 0; i < 4; i++){
                            livingMembers += !dataReader.IsDBNull(i) ? 1 : 0;
                        }

                        dbConnection.Close();
                        
                        dbConnection = GameDatabase.CreateSavesAndOpenDatabase();
                        dbCommandReadValue = dbConnection.CreateCommand();
                        dbCommandReadValue.CommandText = "SELECT food, time, rations FROM SaveFilesTable WHERE id = " + GameLoop.FileId;
                        dataReader = dbCommandReadValue.ExecuteReader();
                        dataReader.Read();

                        int time = dataReader.GetInt32(1), totalFood = dataReader.GetInt32(0), rations = dataReader.GetInt32(2);
                        totalFood = totalFood + foodFound - rations * livingMembers > 0 ? totalFood + foodFound - rations * livingMembers : 0;
                        time = time + 1 == 25 ? 1 : time + 1;

                        IDbCommand dbCommandUpdateValue = dbConnection.CreateCommand();
                        dbCommandUpdateValue.CommandText = "UPDATE SaveFilesTable SET food = food + " + totalFood + ", gas = gas + " + (float)(gasFound) + 
                                                            ", scrap = scrap + " + scrapFound + ", money = money + " + moneyFound + ", medkit = medkit + " + medkitFound +
                                                            ", ammo = " + (Player.AmmoLoaded + Player.TotalAvailableAmmo) + ", time = " + time + ", overallTime = overallTime + 1 " +
                                                            " WHERE id = " + GameLoop.FileId;
                        dbCommandUpdateValue.ExecuteNonQuery();
                        dbConnection.Close();

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
                    if(itemTimer >= spawnItemTime){
                        itemTimer = 0.0f;
                        int itemSpawnSelected, itemSelected;

                        do{
                            itemSpawnSelected = Random.Range(0, pickupSpawnPoints.Length);
                        }while(pickupSpawnPoints[itemSpawnSelected].GetComponent<SpawnPoint>().inUse);

                        pickupSpawnPoints[itemSpawnSelected].GetComponent<SpawnPoint>().inUse = true;
                        itemSelected = Random.Range(0, pickupPrefabs.Length);

                        GameObject itemSpawn = Instantiate(pickupPrefabs[itemSelected], pickupSpawnPoints[itemSpawnSelected].transform.position, pickupSpawnPoints[itemSpawnSelected].transform.rotation);
                        itemSpawn.transform.SetParent(CombatEnvironment.transform);
                    }
                }
                // Spawn enemy after some time, depending on difficulty.
                if(enemyTimer >= spawnEnemyTime){
                    enemyTimer = 0.0f;
                    Debug.Log("Enemy spawned.");
                    int enemySpawnSelected = Random.Range(0, enemySpawnPoints.Length);
                    GameObject enemySpawn = Instantiate(enemyPrefab, enemySpawnPoints[enemySpawnSelected].transform.position, enemySpawnPoints[enemySpawnSelected].transform.rotation);
                    enemySpawn.transform.SetParent(CombatEnvironment.transform);
                }

                combatText.text = Player.UsingGun ? "Equipped: " + weaponList[GunSelected] + "\nLoaded = " + Player.AmmoLoaded + "\nTotal Ammo: " + Player.TotalAvailableAmmo 
                                    : "Equipped: " + weaponList[PhysSelected];
                combatText.text += RestMenu.IsScavenging ? "\nTime: " + System.Math.Round(scavengeTimeLimit, 2) : "";

                // AI actions
                if(Time.timeScale != 0){
                    if(maxAgentsPerUpdate <= 0){
                        for(int i = 0; i < Agents.Count; i++){
                            try{
                                Agents[i].Perform();
                            }
                            catch(Exception e){
                                Debug.LogError(e);
                            }
                        }
                    }
                    else{
                        for(int i = 0; i < maxAgentsPerUpdate; i++){
                            try{
                                Agents[_currentAgentIndex].Perform();
                            }
                            catch(Exception e){
                                Debug.LogError(e);
                            }
                            NextAgent();
                        }
                    }
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

            IDbConnection dbConnection = GameDatabase.CreateSavesAndOpenDatabase();
            IDbCommand dbCommandReadValue = dbConnection.CreateCommand();
            dbCommandReadValue.CommandText = "SELECT * FROM SaveFilesTable WHERE id = " + GameLoop.FileId;
            IDataReader dataReader = dbCommandReadValue.ExecuteReader();
            dataReader.Read();
            diff = dataReader.GetInt32(4);

            // If scavenging, difficulty determines the amount of time.
            if(RestMenu.IsScavenging){
                scavengeTimeLimit = diff == 1 || diff == 3 ? 60.0f : 40.0f;
                spawnItemTime = diff == 1 || diff == 3 ? 10.0f : 15.0f;
            }

            // Difficulty and activity determine enemy spawn time.
            spawnEnemyTime = diff == 1 || diff == 3 ? 10.0f : 8.0f;
            spawnEnemyTime += GameLoop.Activity == 1 ? 2.0f : GameLoop.Activity == 2 ? 0.0f : GameLoop.Activity == 1.0f ? 0.0f : -2.0f;

            dbConnection.Close();

            playerSpawnPoints = GameObject.FindGameObjectsWithTag("PlayerSpawn");
            enemySpawnPoints = GameObject.FindGameObjectsWithTag("EnemySpawn");
            pickupSpawnPoints = GameObject.FindGameObjectsWithTag("PickupSpawn");
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
            RestMenuRef.SetActive(true);
            SceneManager.LoadScene(1);
        }

        public static BaseState GetState<T>() where T : BaseState{
            return RegisteredStates.ContainsKey(typeof(T)) ? RegisteredStates[typeof(T)] : CreateState<T>();
        }

        private static void RegisterState<T>(BaseState stateToAdd) where T : BaseState{
            RegisteredStates[typeof(T)] = stateToAdd;
        }

        private static BaseState CreateState<T>() where T : BaseState{
            RegisterState<T>(ScriptableObject.CreateInstance(typeof(T)) as BaseState);
            return RegisteredStates[typeof(T)];
        }

        public static void AddAgent(BaseAgent agent){
            if(Singleton.Agents.Contains(agent)){
                return;
            }

            Singleton.Agents.Add(agent);
        }

        public static void RemoveAgent(BaseAgent agent){
            if(!Singleton.Agents.Contains(agent)){
                return;
            }

            int index = Singleton.Agents.IndexOf(agent);
            Singleton.Agents.Remove(agent);
            if(Singleton._currentAgentIndex > index){
                Singleton._currentAgentIndex--;
            }
            if(Singleton._currentAgentIndex < 0 || Singleton._currentAgentIndex >= Singleton.Agents.Count){
                Singleton._currentAgentIndex = 0;
            }
        }

        private void NextAgent(){
            _currentAgentIndex++;
            _currentAgentIndex = _currentAgentIndex >= Agents.Count ? 0 : _currentAgentIndex;
        }

        protected virtual void Awake(){
            if(Singleton == this){
                return;
            }

            if(Singleton != null){
                Destroy(gameObject);
                return;
            }
            Singleton = this;
        }
    }
}