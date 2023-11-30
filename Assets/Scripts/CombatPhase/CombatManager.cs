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

        [Tooltip("Combat UI panel object")]
        [SerializeField]
        private GameObject panel;

        [Tooltip("Player object")]
        [SerializeField]
        private GameObject playerPrefab;

        [Tooltip("Player low hp panel")]
        [SerializeField]
        private GameObject lowHPPanel;

        [Tooltip("AI teammate object")]
        [SerializeField]
        private GameObject allyPrefab;

        [Tooltip("Enemy objects")]
        [SerializeField]
        private GameObject[] enemyPrefabs;

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

        // Map generator
        private static MapGenerator mapGenerator;
        // To track spawn points for the party, enemies, and pickups
        private GameObject[] playerSpawnPoints, enemySpawnPoints, pickupSpawnPoints;
        // To track the player
        private GameObject player, ally, restMenu;
        private AudioSource winSound, loseSound;
        // Difficult and agent index
        private int diff, _currentAgentIndex, jobDiff, rolledMutantSpawn;
        // Flags for generating the combat world
        private bool flag = false, defenceMissionSet = false;
        // List of teammates
        private List<Teammate> teammates = new List<Teammate>();
        // List of weapons
        private List<string> weaponList = new List<string>(){"Pistol", "Rifle", "Shotgun", "Knife", "Bat", "Shovel"};
        // Main player
        private Player playerMain;
        // For scavenging, to allow scavenging up to x seconds.
        private float scavengeTimeLimit = 0.0f, itemTimer = 0.0f, spawnItemTime = 0.0f;
        // Spawn timing
        private float spawnEnemyTime = 0.0f, enemyTimer = 0.0f;
        // To determine weapons selected
        public static int GunSelected = -1, PhysSelected = -1;
        // List of dead members
        public static List<int> DeadMembers = new List<int>();
        // Audio for collecting items (putting them on the item as they're destroyed doesn't work)
        public static AudioSource itemCollected;
        protected static CombatManager Singleton;
        // Flags
        public static bool InCombat = false, SucceededJob = false, TargetItemFound = false;
        public static int EnemiesToKill, JobType;
        // Combat manager objects
        public static GameObject Camera, CombatEnvironment, PrevMenuRef, ZoomReticle, NormalReticle;
        // AI
        public static BaseState Mind => Singleton.mind;
        public static Vector2 RandomPosition => Random.insideUnitCircle * 45;
        // All agents in the scene
        public List<BaseAgent> Agents {get; private set;} = new();
        // All registered states
        private static readonly Dictionary<Type, BaseState> RegisteredStates = new();

        // Start is called before the first frame update
        void Start(){
            Camera = combatCamera[0];
            loseSound = GetComponents<AudioSource>()[1];
            winSound = GetComponents<AudioSource>()[2];
            itemCollected = GetComponents<AudioSource>()[3];
        }

        void OnEnable()
        {
            UpdateIntroScreen();
            mapGenerator = FindObjectOfType<MapGenerator>();
            mapGenerator.noiseData.seed = Random.Range(0,10000);
            PrevMenuRef.SetActive(false);
            ZoomReticle = GameObject.FindWithTag("ZoomReticle");
            NormalReticle = GameObject.FindWithTag("NormalReticle");
            if(SceneManager.GetActiveScene().buildIndex == 1 && CombatEnvironment == null){
                CombatEnvironment = GameObject.FindWithTag("CombatEnvironment");
            }
            else if(SceneManager.GetActiveScene().buildIndex != 1 && CombatEnvironment != null){
                CombatEnvironment = null;
            }
            ZoomReticle.SetActive(false);
            NormalReticle.SetActive(false);
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
                if(RestMenu.JobNum != 0 || TravelLoop.GoingToCombat || TravelLoop.InFinalCombat){
                    // Collection job
                    if(JobType == 2 && TargetItemFound){
                        EndCombat();
                        return;
                    }

                    // Defence job
                    else if(defenceMissionSet){
                        if(EnemiesToKill <= 0){
                            EndCombat();
                            return;
                        }
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
                
                // Regularly spawn enemies if not a defence job
                if(!TravelLoop.InFinalCombat && !TravelLoop.GoingToCombat && JobType != 1 && enemyTimer >= spawnEnemyTime){
                    enemyTimer = 0.0f;
                    InitializeMutant();
                }

                playerHealthBar.value = playerMain.hp;
                playerHealthText.text = "HP: " + playerHealthBar.value + "/100";
                combatText.text = Player.UsingGun ? "Equipped: " + weaponList[GunSelected] + "\nLoaded = " + Player.AmmoLoaded + "\nTotal Ammo: " + Player.TotalAvailableAmmo 
                                    : "Equipped: " + weaponList[PhysSelected];
                combatText.text += RestMenu.IsScavenging ? "\nTime: " + System.Math.Round(scavengeTimeLimit, 2) : "";
                combatText.text += JobType == 1 || TravelLoop.GoingToCombat || TravelLoop.InFinalCombat ? "\nEnemies Remaining: " + EnemiesToKill : "";
                lowHPPanel.SetActive(playerMain.hp <= 25 && playerMain.hp > 0);

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
            playerSpawnPoints = GameObject.FindGameObjectsWithTag("PlayerSpawn");
            enemySpawnPoints = GameObject.FindGameObjectsWithTag("EnemySpawn");
            pickupSpawnPoints = GameObject.FindGameObjectsWithTag("PickupSpawn");
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

            Save save = DataUser.dataManager.GetSaveById(GameLoop.FileId);
            diff = save.Difficulty;

            // If scavenging, difficulty determines the amount of time.
            if(RestMenu.IsScavenging){
                scavengeTimeLimit = diff == 1 || diff == 3 ? 60.0f : 40.0f;
                spawnItemTime = diff == 1 || diff == 3 ? 10.0f : 15.0f;
            }

            // Difficulty and activity determine enemy spawn time.
            spawnEnemyTime = diff == 1 || diff == 3 ? 10.0f : 8.0f;
            spawnEnemyTime += GameLoop.Activity == 1 ? 2.0f : GameLoop.Activity == 2 ? 0.0f : GameLoop.Activity == 3 ? -1.0f : -3.0f;
            player = SpawnEntity(2, true, false);
            playerMain = player.GetComponent<Player>();

            ActiveCharacter leader = DataUser.dataManager.GetLeader(GameLoop.FileId);
            playerMain.hp = leader.Health;
            IEnumerable<ActiveCharacter> friends = DataUser.dataManager.GetActiveCharacters().Where<ActiveCharacter>(c=>c.FileId == GameLoop.FileId && c.IsLeader == 0);

            // Load AI teammates in
            foreach(ActiveCharacter friend in friends){
                ally = SpawnEntity(2, false, false);
                Teammate t = ally.GetComponent<Teammate>();
                t.id = friend.Id;
                t.leader = playerMain;
                t.allyName = friend.CharacterName;
                t.SetDetectionRange(35.0f);
                t.usingGun = true;
                t.isSharpshooter = friend.Perk == 1;
                t.isHotHeaded = friend.Trait == 1;
                teammates.Add(t);
            }

            // Job settings
            if(RestMenu.JobNum != 0){
                TownEntity townEntity = DataUser.dataManager.GetTownById(GameLoop.FileId);
                switch(RestMenu.JobNum){
                    case 1:
                        jobDiff = townEntity.Side1Diff;
                        JobType = townEntity.Side1Type;
                        break;
                    case 2:
                        jobDiff = townEntity.Side2Diff;
                        JobType = townEntity.Side2Type;
                        break;
                    case 3:
                        jobDiff = townEntity.Side3Diff;
                        JobType = townEntity.Side3Type;
                        break;
                }

                // Spawn the target if a collection job
                if(JobType == 2){
                    SpawnEntity(1, false, true);
                }
                // Spawn enemies if a defence job
                else{
                    int enemiesToSpawn = jobDiff <= 20 ? 5 : jobDiff <= 40 ? 7 : 9;
                    InitializeDefenceMission(enemiesToSpawn);
                }
            }

            // Final combat section
            else if(TravelLoop.InFinalCombat){
                InitializeDefenceMission(9);
                GameObject bossObj = SpawnEntity(4, false, false);
                EnemiesToKill++;

                Mutant m = bossObj.GetComponent<Mutant>();
                m.mutantType = 3;
                float upperBoundDetection = diff == 1 || diff == 3 ? 61.0f : 81.0f;

                m.SetDetectionRange(Random.Range(20.0f, upperBoundDetection));
                m.SetDestination(m.gameObject.transform.position);
                m.SetHP(Random.Range(45,60));
                int damage = diff == 1 || diff == 3 ? Random.Range(10,15) : Random.Range(15,20);
                m.SetStrength(damage);
            }

            // If coming from the travel menu, treat as a defence mission
            else if(TravelLoop.GoingToCombat){
                int enemiesToSpawn = GameLoop.Activity == 1 ? 5 : GameLoop.Activity == 2 ? 7 : GameLoop.Activity == 3 ? 9 : 11;
                InitializeDefenceMission(enemiesToSpawn);
            }
        }

        /// <summary>
        /// End combat and save results
        /// </summary>
        public void EndCombat(){
            Teammate[] partyMembers = FindObjectsOfType<Teammate>().Where(t => t.name.Contains("Teammate")).ToArray();
            UnloadCombat();
            winSound.Play();
            endCombatScreen.SetActive(true);

            int foodFound = player.GetComponent<Player>().suppliesGathered[0] * 20, gasFound = player.GetComponent<Player>().suppliesGathered[1],
                scrapFound = player.GetComponent<Player>().suppliesGathered[2] * 10, moneyFound = player.GetComponent<Player>().suppliesGathered[3] * 15,
                medkitFound = player.GetComponent<Player>().suppliesGathered[4], ammoFound = player.GetComponent<Player>().suppliesGathered[5] * 10;

            // Update the database
            // Update player count (check if any teammates perished)
            if(DeadMembers.Count > 0){
                foreach(int id in DeadMembers){
                    ActiveCharacter character = DataUser.dataManager.GetCharacter(GameLoop.FileId, id);
                    if(character != null && character.CustomCharacterId != -1){
                        PerishedCustomCharacter perished = new PerishedCustomCharacter(){FileId = GameLoop.FileId, CustomCharacterId = character.CustomCharacterId};
                        DataUser.dataManager.InsertPerishedCustomCharacter(perished);
                    }
                    DataUser.dataManager.DeleteActiveCharacter(id);
                }
            }

            // Update HP
            foreach(Teammate teammate in teammates){
                if(teammate.hp <= 0){
                    continue;
                }
                ActiveCharacter character = DataUser.dataManager.GetCharacter(GameLoop.FileId, teammate.id);
                character.Health = teammate.hp;
                DataUser.dataManager.UpdateCharacter(character);
            }

            ActiveCharacter leader = DataUser.dataManager.GetLeader(GameLoop.FileId);
            leader.Health = player.GetComponent<Player>().hp;
            DataUser.dataManager.UpdateCharacter(leader);

            // Ammo will be counted as total avaialble plus loaded since collected ammo can be used during combat
            // Sum up ammo remaining with teammates
            int ammoRemaining = 0;
            foreach(Teammate t in partyMembers){
                ammoRemaining += t.ammoTotal + t.ammoLoaded;
            }

            // Manage rations with the hour that passed during scavenging
            Save save = DataUser.dataManager.GetSaveById(GameLoop.FileId);
            int time = save.CurrentTime, totalFood = save.Food, rations = save.RationMode;
            totalFood = totalFood + foodFound - rations * (partyMembers.Count() + 1) > 0 ? totalFood + foodFound - rations * (partyMembers.Count() + 1) : 0;
            time = time + 1 == 25 ? 1 : time + 1;

            save.Food = totalFood;
            save.Gas += gasFound;
            save.Scrap += scrapFound;
            save.Money += moneyFound;
            save.Medkit += medkitFound;
            save.Ammo = (Player.AmmoLoaded + Player.TotalAvailableAmmo + ammoRemaining);
            save.CurrentTime = time;
            save.OverallTime++;
            DataUser.dataManager.UpdateSave(save);

            string temp = "";
            // Display final results
            if(TravelLoop.GoingToCombat){
                temp += "You successfully defended your party.\n";
            }
            else if(TravelLoop.InFinalCombat){
                temp += "You successfully arrive in Vancouver.\n";
            }
            else if(RestMenu.JobNum == 0){
                temp += "You collected:\n";
                temp += foodFound > 0 ? "* " + foodFound + " kg of food\n" : "";
                temp += gasFound > 0 ? gasFound == 1 ? "* " + gasFound + " can of gas\n" : "* " + gasFound + " cans of gas\n" : "";
                temp += scrapFound > 0 ? "* " + scrapFound + " scrap\n" : "";
                temp += moneyFound > 0 ? "* $" + moneyFound + "\n" : "";
                temp += medkitFound > 0 ? medkitFound == 1 ? "* " + medkitFound + " medkit\n" : "* " + medkitFound + " medkits\n" : "";
                temp += ammoFound > 0 ? "* " + ammoFound + " ammo\n" : "";

                temp += Equals(temp, "You collected:\n") ? "Nothing." : "";
            }
            else if(RestMenu.JobNum != 0){
                temp += "You successfully completed the task.\n";
            }
            endCombatText.text = temp;
        }

        /// <summary>
        /// End combat from leader death
        /// </summary>
        public void EndCombatDeath(){
            loseSound.Play();
            lowHPPanel.SetActive(false);
            UnloadCombat();
            GameLoop.GameOverScreen.SetActive(true);
            Agents.Clear();
            Singleton.Agents.Clear();
            Singleton._currentAgentIndex = 0;
        }

        /// <summary>
        /// Return to previously visited menu (rest if scavenge or job, game over if last combat, travel otherwise)
        /// </summary>
        public void ReturnToNonCombat(){
            if(RestMenu.JobNum != 0){
                SucceededJob = true;
                TargetItemFound = false;
                SceneManager.LoadScene(0);
                JobType = 0;
                PrevMenuRef.SetActive(true);
            }
            else if(RestMenu.IsScavenging){
                RestMenu.IsScavenging = false;
                SceneManager.LoadScene(0);
                PrevMenuRef.SetActive(true);
            }
            else if(TravelLoop.InFinalCombat){
                GameLoop.GameOverScreen.SetActive(true);
                GameLoop.MainPanel.SetActive(true);
                this.gameObject.SetActive(false);
            }
            else if(TravelLoop.GoingToCombat){
                TravelLoop.GoingToCombat = false;
                GameLoop.MainPanel.SetActive(false);
                PrevMenuRef.SetActive(true);
                SceneManager.LoadScene(0);
            }
            
        }

        /// <summary>
        /// Unload combat scene elements
        /// </summary>
        private void UnloadCombat(){
            InCombat = false;
            CombatEnvironment.SetActive(false);
            GameLoop.MainPanel.SetActive(true);
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
        /// Initialize defence mission
        /// </summary>
        /// <param name="enemiesToSpawn">The number of enemies to spawn</param>
        private void InitializeDefenceMission(int enemiesToSpawn){
            for(int i = 0; i < enemiesToSpawn; i++){
                InitializeMutant();
                EnemiesToKill++;
            }
            defenceMissionSet = true;
        }

        /// <summary>
        /// Select a spawnpoint to spawn an entity at
        /// </summary>
        /// <param name="type">The type of spawnpoint to select (1 = pickup, 2 = ally, 3 = enemy, 4 = boss)</param>
        /// <param name="isPlayer">If the entity spawned is the player</param>
        /// <param name="isTarget">If the entity spawned is the target collectible for jobs</param>
        /// <returns>The entity spawned</returns>
        private GameObject SpawnEntity(int type, bool isPlayer, bool isTarget){
            int spawnSelected, itemSelected = Random.Range(1, 101), enemySelected = Random.Range(0,3);
            rolledMutantSpawn = enemySelected;

            // 20% for food (0), 10% for gas (1), 20% for scrap (2), 20% for money (3), 10% for medkit (4), 20% for ammo (5)
            itemSelected = itemSelected <= 20 ? 0 : itemSelected <= 30 ? 1 : itemSelected <= 50 ? 2 : itemSelected <= 70 ? 3 : itemSelected <= 80 ? 4 : 5; 
            GameObject[] spawnPointsOfInterest = type == 1 ? pickupSpawnPoints : type == 2 ? playerSpawnPoints: enemySpawnPoints;
            GameObject toSpawn = type == 1 ? pickupPrefabs[itemSelected] : type == 2 ? allyPrefab : enemyPrefabs[enemySelected];
            toSpawn = type == 1 && isTarget ? targetPickup : type == 2 && isPlayer ? playerPrefab : toSpawn;

            // If spawning an enemy during non-defence missions, spawn wherever. Otherwise pick an unused spawnpoint
            if(type == 3 && (JobType == 1 || JobType == 2)){
                spawnSelected = Random.Range(0, spawnPointsOfInterest.Length);
            }
            else if(type == 4 && TravelLoop.InFinalCombat){
                toSpawn = enemyPrefabs[3];
                rolledMutantSpawn = 3;
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
            m.mutantType = rolledMutantSpawn;

            int hpSet = m.mutantType == 0 ? Random.Range(15,20) : m.mutantType == 1 ? Random.Range(30,45) : Random.Range(10,15), 
                damage = diff == 1 || diff == 3 ? Random.Range(6,11) : Random.Range(10,15);
            float upperBoundDetection = diff == 1 || diff == 3 ? 41.0f : 50.0f;
            upperBoundDetection += m.mutantType == 2 ? 3.0f : 0.0f;

            if(RestMenu.JobNum != 0){
                upperBoundDetection += jobDiff <= 20 ? -2 : jobDiff <= 40 ? 0 : 2;
            }
            m.SetDetectionRange(Random.Range(25.0f, upperBoundDetection));
            m.SetDestination(m.gameObject.transform.position);
            m.SetHP(hpSet);
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