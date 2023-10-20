using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using CombatPhase.ProceduralGeneration;
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
using TravelPhase;

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

        [Tooltip("Target pickup for collection jobs")]
        [SerializeField]
        private GameObject targetPickup;

        [Tooltip("Panel object")]
        [SerializeField]
        private GameObject panel;

        [Tooltip("Player object")]
        [SerializeField]
        private GameObject playerPrefab;

        [Tooltip("AI teammate object")]
        [SerializeField]
        private GameObject allyPrefab;

        [Tooltip("Enemy object")]
        [SerializeField]
        private GameObject enemyPrefab;

        [Tooltip("Cameras used during combat phase")]
        [SerializeField]
        private GameObject[] combatCamera;

        [Tooltip("Combat text")]
        [SerializeField]
        private TextMeshProUGUI combatText;

        [Tooltip("Player health bar")]
        [SerializeField]
        private Slider playerHealthBar;

        [Tooltip("Player health text")]
        [SerializeField]
        private TextMeshProUGUI playerHealthText;

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

        [Tooltip("The maximum number of agents that can be updated in a single frame.")]
        [Min(0)]
        [SerializeField]
        private int maxAgentsPerUpdate;
        private static MapGenerator mapGenerator;
        // All agents in the scene
        public List<BaseAgent> Agents {get; private set;} = new();

        // All registered states
        private static readonly Dictionary<Type, BaseState> RegisteredStates = new();
        // To track spawn points for the party, enemies, and pickups
        private GameObject[] playerSpawnPoints, enemySpawnPoints, pickupSpawnPoints;
        // To track the player
        private GameObject player, ally, restMenu;
        // Difficult and agent index
        private int diff, _currentAgentIndex, jobDiff, jobType;
        // Flags for generating the combat world
        private bool flag = false, defenceMissionSet = false;
        private List<Teammate> teammates = new List<Teammate>();
        // For scavenging, to allow scavenging up to x seconds.
        private float scavengeTimeLimit = 0.0f, itemTimer = 0.0f, spawnItemTime = 0.0f;
        // Spawn timing
        private float spawnEnemyTime = 0.0f, enemyTimer = 0.0f;
        // To determine weapons selected
        public static int GunSelected = -1, PhysSelected = -1;
        // List of weapons
        private List<string> weaponList = new List<string>(){"Pistol", "Rifle", "Shotgun", "Knife", "Bat", "Shovel"};
        public static List<int> DeadMembers = new List<int>();
        protected static CombatManager Singleton;
        public static bool InCombat = false, SucceededJob = false, TargetItemFound = false;
        public static GameObject Camera, CombatEnvironment, PrevMenuRef, ZoomReticle, NormalReticle;
        public static BaseState Mind => Singleton.mind;
        public static Vector2 RandomPosition => Random.insideUnitCircle * 45;
        public static string LeaderName;
        public static int EnemiesToKill;

        // Start is called before the first frame update
        void Start(){
            Camera = combatCamera[0];
        }

        void OnEnable()
        {
            UpdateIntroScreen();
            PrevMenuRef.SetActive(false);
            ZoomReticle = GameObject.FindWithTag("ZoomReticle");
            NormalReticle = GameObject.FindWithTag("NormalReticle");
            if(SceneManager.GetActiveScene().buildIndex == 3 && CombatEnvironment == null){
                CombatEnvironment = GameObject.FindWithTag("CombatEnvironment");
            }
            else if(SceneManager.GetActiveScene().buildIndex != 3 && CombatEnvironment != null){
                CombatEnvironment = null;
            }
            ZoomReticle.SetActive(false);
            NormalReticle.SetActive(false);
            mapGenerator = FindObjectOfType<MapGenerator>();
            mapGenerator.noiseData.seed = Random.Range(0,10000);
        }

        // Update is called once per frame
        void Update()
        {   
            // Initially terrain will clip through the canvas, correct it by pushing in z-direction
            if(!flag && CombatEnvironment != null){
                CombatEnvironment.transform.position = new Vector3(CombatEnvironment.transform.position.x, CombatEnvironment.transform.position.y, CombatEnvironment.transform.position.z + 60f);
                flag = true;
            }

            combatCamera[0].SetActive(InCombat);
            combatCamera[1].SetActive(InCombat);
            if(InCombat){
                combatText.gameObject.SetActive(!PauseMenu.IsPaused);
                playerHealthBar.gameObject.SetActive(!PauseMenu.IsPaused);
                playerHealthText.gameObject.SetActive(!PauseMenu.IsPaused);
                enemyTimer += Time.deltaTime;

                // Job combat functions
                if(RestMenu.JobNum != 0){
                    // End combat when the item has been found
                    if(TargetItemFound){
                        EndCombat();
                        return;
                    }
                }

                // Scavenging functions
                else if(RestMenu.IsScavenging){
                    // Players have limited time to find items that spawn periodically
                    scavengeTimeLimit -= Time.deltaTime;
                    itemTimer += Time.deltaTime;

                    if(scavengeTimeLimit <= 0.0f){
                        EndCombat();
                        return;
                    }

                    // Spawn pickup after enough time has passed
                    if(itemTimer >= spawnItemTime){
                        itemTimer = 0.0f;
                        SpawnEntity(1, false, false);
                    }
                }

                // Defence functions
                else if(defenceMissionSet && (RestMenu.JobNum == 1 || TravelLoop.GoingToCombat)){
                    if(EnemiesToKill <= 0){
                        EndCombat();
                        return;
                    }
                }
                
                // Regularly spawn enemies if not a defence job
                if(jobType != 1 && enemyTimer >= spawnEnemyTime){
                    enemyTimer = 0.0f;
                    InitializeMutant();
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

                foreach(BaseAgent agent in Agents){
                    agent.IncreaseDeltaTime();
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
            panel.SetActive(false);
            CombatEnvironment.SetActive(true);
            NormalReticle.SetActive(true);
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
            dbCommandReadValue.CommandText = "SELECT difficulty FROM SaveFilesTable WHERE id = " + GameLoop.FileId;
            IDataReader dataReader = dbCommandReadValue.ExecuteReader();
            dataReader.Read();
            diff = dataReader.GetInt32(0);

            // If scavenging, difficulty determines the amount of time.
            if(RestMenu.IsScavenging){
                scavengeTimeLimit = diff == 1 || diff == 3 ? 60.0f : 40.0f;
                spawnItemTime = diff == 1 || diff == 3 ? 10.0f : 15.0f;
            }

            // Difficulty and activity determine enemy spawn time.
            spawnEnemyTime = diff == 1 || diff == 3 ? 10.0f : 8.0f;
            spawnEnemyTime += GameLoop.Activity == 1 ? 2.0f : GameLoop.Activity == 2 ? 0.0f : GameLoop.Activity == 3 ? -1.0f : -3.0f;

            dbConnection.Close();

            playerSpawnPoints = GameObject.FindGameObjectsWithTag("PlayerSpawn");
            enemySpawnPoints = GameObject.FindGameObjectsWithTag("EnemySpawn");
            pickupSpawnPoints = GameObject.FindGameObjectsWithTag("PickupSpawn");
            player = SpawnEntity(2, true, false);

            dbConnection = GameDatabase.CreateActiveCharactersAndOpenDatabase();
            dbCommandReadValue = dbConnection.CreateCommand();
            dbCommandReadValue.CommandText = "SELECT leaderHealth, friend1Name, friend2Name, friend3Name, leaderName FROM ActiveCharactersTable WHERE id = " + GameLoop.FileId;
            dataReader = dbCommandReadValue.ExecuteReader();
            dataReader.Read();

            playerHealthBar.value = dataReader.GetInt32(0);
            playerHealthText.text = "HP: " + playerHealthBar.value + "/100";
            LeaderName = dataReader.GetString(4);

            // Load AI teammates in
            for(int i = 1; i <= 3; i++){
                if(!dataReader.IsDBNull(i)){
                    ally = SpawnEntity(2, false, false);
                    Teammate t = ally.GetComponent<Teammate>();
                    t.id = i;
                    t.allyName = dataReader.GetString(i);
                    t.leader = player.GetComponent<Player>();
                    t.SetDetectionRange(15.0f);
                    t.usingGun = true;
                    teammates.Add(t);
                }
            }
            dbConnection.Close();

            // Job settings
            if(RestMenu.JobNum != 0){
                dbConnection = GameDatabase.CreateTownAndOpenDatabase();
                dbCommandReadValue = dbConnection.CreateCommand();
                dbCommandReadValue.CommandText = "SELECT side" + RestMenu.JobNum + "Diff, side" + RestMenu.JobNum + "Type FROM TownTable WHERE id = " + GameLoop.FileId;
                dataReader = dbCommandReadValue.ExecuteReader();
                dataReader.Read();

                jobDiff = dataReader.GetInt32(0);
                jobType = dataReader.GetInt32(1);

                dbConnection.Close();

                // Spawn the target if a collection job
                if(jobType == 2){
                    SpawnEntity(1, false, true);
                }
                // Spawn enemies if a defence job
                else{
                    int enemiesToSpawn = jobDiff <= 20 ? 5 : jobDiff <= 40 ? 7 : 9;

                    for(int i = 0; i < enemiesToSpawn; i++){
                        InitializeMutant();
                        EnemiesToKill++;
                    }
                }
            }

            // If coming from the travel menu, treat as a defence mission
            else if(TravelLoop.GoingToCombat){
                int enemiesToSpawn = GameLoop.Activity == 1 ? 5 : GameLoop.Activity == 2 ? 7 : GameLoop.Activity == 3 ? 9 : 11;

                for(int i = 0; i < enemiesToSpawn; i++){
                    InitializeMutant();
                    EnemiesToKill++;
                }
                defenceMissionSet = true;
            }
        }

        /// <summary>
        /// End combat and save results
        /// </summary>
        public void EndCombat(){
            UnloadCombat();
            endCombatScreen.SetActive(true);

            int foodFound = player.GetComponent<Player>().suppliesGathered[0] * 20, gasFound = player.GetComponent<Player>().suppliesGathered[1],
                scrapFound = player.GetComponent<Player>().suppliesGathered[2] * 10, moneyFound = player.GetComponent<Player>().suppliesGathered[3] * 15,
                medkitFound = player.GetComponent<Player>().suppliesGathered[4], ammoFound = player.GetComponent<Player>().suppliesGathered[5] * 10;

            // Update the database
            IDbConnection dbConnection = GameDatabase.CreateActiveCharactersAndOpenDatabase();

            // Update player count (check if any teammates perished)
            if(DeadMembers.Count > 0){
                IDbCommand dbCommandUpdateDeadValue = dbConnection.CreateCommand();
                string tempDead = "UPDATE ActiveCharactersTable SET ";
                
                for(int i = 0; i < DeadMembers.Count; i++){
                    tempDead += DeadMembers[i] == 1 ? "friend1Name = null " : i == 2 ? "friend2Name = null " : "friend3Name = null ";
                }
                tempDead += "WHERE id = " + GameLoop.FileId;
                dbCommandUpdateDeadValue.CommandText = tempDead;
                dbCommandUpdateDeadValue.ExecuteNonQuery();
            }

            // Ammo will be counted as total avaialble plus loaded since collected ammo can be used during combat
            IDbCommand dbCommandReadValue = dbConnection.CreateCommand();
            dbCommandReadValue.CommandText = "SELECT leaderName, friend1Name, friend2Name, friend3Name FROM ActiveCharactersTable WHERE id = " + GameLoop.FileId;
            IDataReader dataReader = dbCommandReadValue.ExecuteReader();
            dataReader.Read();

            // Update HP while getting living members
            string tempHP = "UPDATE ActiveCharactersTable SET leaderHealth = " + player.GetComponent<Player>().hp;

            int livingMembers = 0;
            for(int i = 0; i < 4; i++){
                livingMembers += !dataReader.IsDBNull(i) ? 1 : 0;
                
                if(!dataReader.IsDBNull(i) && i >= 1){
                    tempHP += ", friend" + i + "Health = " + teammates[i-1].hp;
                }
            }
            tempHP += " WHERE id = " + GameLoop.FileId;
            
            IDbCommand dbCommandUpdateValue = dbConnection.CreateCommand();
            dbCommandUpdateValue.CommandText = tempHP;
            dbCommandUpdateValue.ExecuteNonQuery();
            dbConnection.Close();
            
            // Manage rations with the hour that passed during scavenging
            dbConnection = GameDatabase.CreateSavesAndOpenDatabase();
            dbCommandReadValue = dbConnection.CreateCommand();
            dbCommandReadValue.CommandText = "SELECT food, time, rations FROM SaveFilesTable WHERE id = " + GameLoop.FileId;
            dataReader = dbCommandReadValue.ExecuteReader();
            dataReader.Read();

            int time = dataReader.GetInt32(1), totalFood = dataReader.GetInt32(0), rations = dataReader.GetInt32(2);
            totalFood = totalFood + foodFound - rations * livingMembers > 0 ? totalFood + foodFound - rations * livingMembers : 0;
            time = time + 1 == 25 ? 1 : time + 1;

            // Sum up ammo remaining with teammates
            Teammate[] partyMembers = FindObjectsOfType<Teammate>().Where(t => t.name.Contains("Teammate")).ToArray();
            int ammoRemaining = 0;
            foreach(Teammate t in partyMembers){
                ammoRemaining += t.ammoTotal + t.ammoLoaded;
            }

            dbCommandUpdateValue = dbConnection.CreateCommand();
            dbCommandUpdateValue.CommandText = "UPDATE SaveFilesTable SET food = food + " + totalFood + ", gas = gas + " + (float)(gasFound) + 
                                                ", scrap = scrap + " + scrapFound + ", money = money + " + moneyFound + ", medkit = medkit + " + medkitFound +
                                                ", ammo = " + (Player.AmmoLoaded + Player.TotalAvailableAmmo + ammoRemaining) + ", time = " + 
                                                time + ", overallTime = overallTime + 1 WHERE id = " + GameLoop.FileId;
            dbCommandUpdateValue.ExecuteNonQuery();
            dbConnection.Close();

            // Display final results
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

        /// <summary>
        /// End combat from leader death
        /// </summary>
        public void EndCombatDeath(){
            UnloadCombat();
            GameLoop.GameOverScreen.SetActive(true);
            Agents.Clear();
            Singleton.Agents.Clear();
            Singleton._currentAgentIndex = 0;
        }

        /// <summary>
        /// Return to previously visited menu (rest if scavenge or job, travel otherwise)
        /// </summary>
        public void ReturnToNonCombat(){
            if(RestMenu.JobNum != 0){
                SucceededJob = true;
                TargetItemFound = false;
                SceneManager.LoadScene(1);
            }
            else if(RestMenu.IsScavenging){
                RestMenu.IsScavenging = false;
                SceneManager.LoadScene(1);
            }
            else{
                SceneManager.LoadScene(2);
            }
            PrevMenuRef.SetActive(true);
        }

        /// <summary>
        /// Unload combat scene elements
        /// </summary>
        private void UnloadCombat(){
            InCombat = false;
            CombatEnvironment.SetActive(false);
            RestMenu.Panel.SetActive(true);
            TravelLoop.GoingToCombat = false;
            combatCamera[0].SetActive(false);
            combatCamera[1].SetActive(false);
            combatText.gameObject.SetActive(false);
            NormalReticle.SetActive(false);
            ZoomReticle.SetActive(false);
            playerHealthBar.gameObject.SetActive(false);
            playerHealthText.gameObject.SetActive(false);
            Cursor.lockState = CursorLockMode.None;
        }

        /// <summary>
        /// Select a spawnpoint to spawn an entity at
        /// </summary>
        /// <param name="type">The type of spawnpoint to select (1 = pickup, 2 = ally, 3 = enemy)</param>
        /// <param name="isPlayer">If the entity spawned is the player</param>
        /// <param name="isTarget">If the entity spawned is the target collectible for jobs</param>
        /// <returns>The entity spawned</returns>
        private GameObject SpawnEntity(int type, bool isPlayer, bool isTarget){
            int spawnSelected, itemSelected = Random.Range(1, 101);
            
            // 20% for food (0), 10% for gas (1), 20% for scrap (2), 20% for money (3), 10% for medkit (4), 20% for ammo (5)
            itemSelected = itemSelected <= 20 ? 0 : itemSelected <= 30 ? 1 : itemSelected <= 50 ? 2 : itemSelected <= 70 ? 3 : itemSelected <= 80 ? 4 : 5; 
            GameObject[] spawnPointsOfInterest = type == 1 ? pickupSpawnPoints : type == 2 ? playerSpawnPoints: enemySpawnPoints;
            GameObject toSpawn = type == 1 ? pickupPrefabs[itemSelected] : type == 2 ? allyPrefab : enemyPrefab;
            toSpawn = type == 1 && isTarget ? targetPickup : type == 2 && isPlayer ? playerPrefab : toSpawn;

            // If spawning an enemy during non-defence missions, spawn wherever. Otherwise pick an unused spawnpoint
            if(type == 3 && (jobType == 0 || jobType == 2)){
                spawnSelected = Random.Range(0, spawnPointsOfInterest.Length);
            }
            else{
                do{
                    spawnSelected = Random.Range(0, spawnPointsOfInterest.Length);
                }while(spawnPointsOfInterest[spawnSelected].GetComponent<SpawnPoint>().inUse);
                spawnPointsOfInterest[spawnSelected].GetComponent<SpawnPoint>().inUse = true; 
            }
            GameObject spawned = Instantiate(toSpawn, spawnPointsOfInterest[spawnSelected].transform.position, spawnPointsOfInterest[spawnSelected].transform.rotation);
            spawned.transform.SetParent(CombatEnvironment.transform); 
            return spawned;
        }

        /// <summary>
        /// Initialize an enemy
        /// </summary>
        private void InitializeMutant(){
            GameObject enemySpawn = SpawnEntity(3, false, false);
            
            Mutant m = enemySpawn.GetComponent<Mutant>();
            float upperBoundDetection = diff == 1 || diff == 3 ? 8.0f : 14.0f;
            if(RestMenu.JobNum != 0){
                upperBoundDetection += jobDiff <= 20 ? -2 : jobDiff <= 40 ? 0 : 2;
            }
            m.SetDetectionRange(Random.Range(8.0f, upperBoundDetection));
            m.SetDestination(m.gameObject.transform.position);
            m.SetHP(Random.Range(10,15));
            int damage = diff == 1 || diff == 3 ? Random.Range(6,11) : Random.Range(10,15);
            m.SetStrength(damage);
        }
        
        // ------------------------ STATES AND AI --------------------------------------------------------------
        /// <summary>
        /// Lookup a state type from the dictionary.
        /// </summary>
        /// <typeparam name="T">The type of state to register</typeparam>
        /// <returns>The state of the requested type.</returns>
        public static BaseState GetState<T>() where T : BaseState{
            return RegisteredStates.ContainsKey(typeof(T)) ? RegisteredStates[typeof(T)] : CreateState<T>();
        }

        /// <summary>
        /// Register a state type into the dictionary.
        /// </summary>
        /// <typeparam name="T">The type of state to register</typeparam>
        /// <returns>The state of the requested type.</returns>
        private static void RegisterState<T>(BaseState stateToAdd) where T : BaseState{
            RegisteredStates[typeof(T)] = stateToAdd;
        }

        /// <summary>
        /// Create a state type into the dictionary
        /// </summary>
        /// <typeparam name="T">The type of state to register</typeparam>
        /// <returns>The state of the requested type.</returns>
        private static BaseState CreateState<T>() where T : BaseState{
            RegisterState<T>(ScriptableObject.CreateInstance(typeof(T)) as BaseState);
            return RegisteredStates[typeof(T)];
        }

        /// <summary>
        /// Add an agent from the list of agents
        /// </summary>
        /// <param name="agent">The agent to add</param>
        public static void AddAgent(BaseAgent agent){
            if(Singleton.Agents.Contains(agent)){
                return;
            }

            Singleton.Agents.Add(agent);
        }

        /// <summary>
        /// Remove an agent from the list of agents
        /// </summary>
        /// <param name="agent">The agent to remove</param>
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

        /// <summary>
        /// Move to the next agent in context
        /// </summary>
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