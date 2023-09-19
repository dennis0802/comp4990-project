using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
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

        [Tooltip("End of combat screen due to player death")]
        [SerializeField]
        private GameObject gameOverScreen;

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
        private GameObject player, ally, restMenu;
        private int diff;
        private int _currentAgentIndex;
        private List<Teammate> teammates = new List<Teammate>();
        // For scavenging, to allow scavenging up to x seconds.
        private float scavengeTimeLimit = 0.0f, itemTimer = 0.0f, spawnItemTime = 0.0f;
        private float spawnEnemyTime = 0.0f, enemyTimer = 0.0f;
        // To determine weapons selected
        public static int GunSelected = -1, PhysSelected = -1;
        // List of weapons
        private List<string> weaponList = new List<string>(){"Pistol", "Rifle", "Shotgun", "Knife", "Bat", "Shovel"};
        public static List<int> DeadMembers = new List<int>();
        protected static CombatManager Singleton;
        public static bool InCombat = false;
        public static GameObject Camera, CombatEnvironment, RestMenuRef, GameOverScreen, ZoomReticle, NormalReticle;
        public static BaseState Mind => Singleton.mind;
        public static Vector2 RandomPosition => Random.insideUnitCircle * 195;
        public static string LeaderName;

        // Start is called before the first frame update
        void Start(){
            Camera = combatCamera[0];
            GameOverScreen = gameOverScreen;
        }

        void OnEnable()
        {
            UpdateIntroScreen();
            ZoomReticle = GameObject.FindWithTag("ZoomReticle");
            NormalReticle = GameObject.FindWithTag("NormalReticle");
            if(SceneManager.GetActiveScene().buildIndex == 3 && CombatEnvironment == null){
                CombatEnvironment = GameObject.FindWithTag("CombatEnvironment");
            }
            else if(SceneManager.GetActiveScene().buildIndex != 3 && CombatEnvironment != null){
                CombatEnvironment = null;
            }
            CombatEnvironment.SetActive(false);
            ZoomReticle.SetActive(false);
            NormalReticle.SetActive(false);
        }

        // Update is called once per frame
        void Update()
        {   
            combatCamera[0].SetActive(InCombat && !PauseMenu.IsPaused);
            combatCamera[1].SetActive(InCombat && !PauseMenu.IsPaused);
            if(InCombat){
                combatText.gameObject.SetActive(!PauseMenu.IsPaused);
                playerHealthBar.gameObject.SetActive(!PauseMenu.IsPaused);
                playerHealthText.gameObject.SetActive(!PauseMenu.IsPaused);

                // Players have limited time per scavenge session
                if(RestMenu.IsScavenging){
                    scavengeTimeLimit -= Time.deltaTime;
                    itemTimer += Time.deltaTime;
                    enemyTimer += Time.deltaTime;

                    if(scavengeTimeLimit <= 0.0f){
                        EndCombat();
                        return;
                    }

                    // Spawn pickup after enough time has passed
                    if(itemTimer >= spawnItemTime){
                        itemTimer = 0.0f;
                        int itemSpawnSelected, itemSelected;

                        do{
                            itemSpawnSelected = Random.Range(0, pickupSpawnPoints.Length);
                        }while(pickupSpawnPoints[itemSpawnSelected].GetComponent<SpawnPoint>().inUse);

                        pickupSpawnPoints[itemSpawnSelected].GetComponent<SpawnPoint>().inUse = true;
                        itemSelected = Random.Range(1, 101);
                        // 20% for food (0), 10% for gas (1), 20% for scrap (2), 20% for money (3), 10% for medkit (4), 20% for ammo (5)
                        itemSelected = itemSelected <= 20 ? 0 : itemSelected <= 30 ? 1 : itemSelected <= 50 ? 2 : itemSelected <= 70 ? 3 : itemSelected <= 80 ? 4 : 5; 

                        GameObject itemSpawn = Instantiate(pickupPrefabs[itemSelected], pickupSpawnPoints[itemSpawnSelected].transform.position, pickupSpawnPoints[itemSpawnSelected].transform.rotation);
                        itemSpawn.transform.SetParent(CombatEnvironment.transform);
                    }
                }
                // Spawn enemy after some time, depending on difficulty.
                if(enemyTimer >= spawnEnemyTime){
                    enemyTimer = 0.0f;
                    int enemySpawnSelected = Random.Range(0, enemySpawnPoints.Length);
                    GameObject enemySpawn = Instantiate(enemyPrefab, enemySpawnPoints[enemySpawnSelected].transform.position, enemySpawnPoints[enemySpawnSelected].transform.rotation);
                    enemySpawn.transform.SetParent(CombatEnvironment.transform);
                    
                    Mutant m = enemySpawn.GetComponent<Mutant>();
                    float upperBoundDetection = diff == 1 || diff == 3 ? 8.0f : 14.0f;
                    m.SetDetectionRange(Random.Range(8.0f, upperBoundDetection));
                    m.SetDestination(m.gameObject.transform.position);
                    m.SetHP(Random.Range(10,15));
                    int damage = diff == 1 || diff == 3 ? Random.Range(6,11) : Random.Range(10,15);
                    m.SetStrength(damage);
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
            RestMenu.Panel.SetActive(false);
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

            int selected;
            
            do
            {
                selected = Random.Range(0, playerSpawnPoints.Length);
            } while (playerSpawnPoints[selected].GetComponent<SpawnPoint>().inUse); 
            playerSpawnPoints[selected].GetComponent<SpawnPoint>().inUse = true;

            player = Instantiate(playerPrefab, playerSpawnPoints[selected].transform.position, playerSpawnPoints[selected].transform.rotation);
            player.transform.SetParent(CombatEnvironment.transform);

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
                    do
                    {
                        selected = Random.Range(0, playerSpawnPoints.Length);
                    } while (playerSpawnPoints[selected].GetComponent<SpawnPoint>().inUse); 
                    playerSpawnPoints[selected].GetComponent<SpawnPoint>().inUse = true;

                    ally = Instantiate(allyPrefab, playerSpawnPoints[selected].transform.position, playerSpawnPoints[selected].transform.rotation);
                    ally.transform.SetParent(CombatEnvironment.transform);
                    Teammate t = ally.GetComponent<Teammate>();
                    t.id = i;
                    t.allyName = dataReader.GetString(i);
                    t.leader = player.GetComponent<Player>();
                    t.SetDetectionRange(10.0f);
                    t.usingGun = true;
                    teammates.Add(t);
                }
            }

            dbConnection.Close();
        }

        /// <summary>
        /// End combat and save results
        /// </summary>
        public void EndCombat(){
            InCombat = false;
            RestMenu.IsScavenging = false;
            RestMenu.Panel.SetActive(true);
            combatCamera[0].SetActive(false);
            combatCamera[1].SetActive(false);
            combatText.gameObject.SetActive(false);
            CombatEnvironment.SetActive(false);
            endCombatScreen.SetActive(true);
            NormalReticle.SetActive(false);
            ZoomReticle.SetActive(false);
            playerHealthBar.gameObject.SetActive(false);
            playerHealthText.gameObject.SetActive(false);
            Cursor.lockState = CursorLockMode.None;
            
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

        public void EndCombatDeath(){
            InCombat = false;
            RestMenu.IsScavenging = false;
            RestMenu.Panel.SetActive(true);
            GameLoop.GameOverScreen.SetActive(true);
            combatCamera[0].SetActive(false);
            combatCamera[1].SetActive(false);
            combatText.gameObject.SetActive(false);
            playerHealthBar.gameObject.SetActive(false);
            playerHealthText.gameObject.SetActive(false);
            NormalReticle.SetActive(false);
            ZoomReticle.SetActive(false);
            Agents.Clear();
            Singleton.Agents.Clear();
            Singleton._currentAgentIndex = 0;
            CombatEnvironment.SetActive(false);
            Cursor.lockState = CursorLockMode.None;
        }

        /// <summary>
        /// Return to the rest menu
        /// </summary>
        public void ReturnToRest(){
            RestMenuRef.SetActive(true);
            SceneManager.LoadScene(1);
        }

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