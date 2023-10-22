using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using CombatPhase;
using Database;
using UI;
using TMPro;
using Mono.Data.Sqlite;
using AI;

namespace CombatPhase{
    [DisallowMultipleComponent]
    public class Player : MonoBehaviour
    {
        [Tooltip("Player input object")]
        [SerializeField]
        private PlayerInput playerInput;

        [Tooltip("Name text")]
        [SerializeField]
        private TextMeshPro nameText;

        [Tooltip("Bullet prefab")]
        [SerializeField]
        private GameObject bulletPrefab;

        /// <summary>
        /// The player's health bar
        /// </summary>
        private Slider playerHealthBar;

        /// <summary>
        /// The text displaying the player's total health
        /// </summary>
        private TextMeshProUGUI playerHealthText;

        /// <summary>
        /// The zoomed in reticle when using the rifle.
        /// </summary>
        private Image zoomReticle;

        /// <summary>
        /// The normal reticle when not zoomed in.
        /// </summary>
        private Image normalReticle;

        /// <summary>
        /// Player input actions
        /// </summary> 
        private InputAction playerMove, playerShoot, playerZoomIn, weaponSwitch, playerReload, startRun, endRun;
        
        /// <summary>
        /// Controller for the player
        /// </summary> 
        private CharacterController controller;

        /// <summary>
        /// Camera transform following the player
        /// </summary> 
        private Transform cameraTransform;

        /// <summary>
        /// Values for player movement physics
        /// </summary>  
        private float gravity = -9.81f, rotationSpeed = 5f;
        
        /// <summary>
        /// Player speed
        /// </summary>  
        public float playerSpeed = 3.0f;

        /// <summary>
        /// Player current velocity
        /// </summary> 
        private Vector3 playerVelocity;

        /// <summary>
        /// Array to store supplies of each type gathered
        /// </summary> 
        public int[] suppliesGathered = new int[]{0,0,0,0,0,0};

        /// <summary>
        /// Ammo available from party stock
        /// </summary> 
        public static int TotalAvailableAmmo = 0;

        /// <summary>
        /// Ammo currently loaded
        /// </summary> 
        public static int AmmoLoaded = 0;

        /// <summary>
        /// Max ammo a gun can gold
        /// </summary> 
        private const int maxAmmoLoaded = 6;

        /// <summary>
        /// Flags for player actions
        /// </summary> 
        private bool isGrounded, busyReloading, isRunning = false, damagedRecently = false;

        /// <summary>
        /// Shooting audio
        /// </summary> 
        private AudioSource shootingAudio;

        /// <summary>
        /// Empty gun audio
        /// </summary>         
        private AudioSource emptyAudio;

        /// <summary>
        /// Reloading audio
        /// </summary> 
        private AudioSource reloadAudio;

        /// <summary>
        /// Physical attack audio, used for if attack missed
        /// </summary> 
        private AudioSource physSound;

        /// <summary>
        /// Text to alert the player
        /// </summary>
        private TextMeshProUGUI alertText;

        /// <summary>
        /// Combat manager on scene
        /// </summary>
        private CombatManager combatManager;

        /// <summary>
        /// Location to spawn bullets regularly
        /// </summary>
        private GameObject shootLocation;

        /// <summary>
        /// Additional locations to spawn bullets via shotgun
        /// </summary>
        private GameObject[] shotgunShootLocations;

        /// <summary>
        /// Player health
        /// </summary> 
        public int hp;

        /// <summary>
        /// Reload timer
        /// </summary> 
        public int reloadTimer;

        /// <summary>
        /// Player's physical damage output
        /// </summary>
        public int physicalDamageOutput;

        /// <summary>
        /// Flag if gun is being used
        /// </summary> 
        public static bool UsingGun = true;
        
        /// <summary>
        /// Flag if zoomed in with rifle
        /// </summary> 
        public static bool ZoomedIn = false;

        /// <summary>
        /// Flag if player can shoot.
        /// </summary> 
        public bool CanShoot = true;

        /// <summary>
        /// List of colliders on the agent
        /// </summary> 
        public Collider[] Colliders {get; private set;}

        // Start is called before the first frame update
        void Start()
        {
            playerShoot = playerInput.actions["LeftClick"];
            playerMove = playerInput.actions["Move"];
            playerZoomIn = playerInput.actions["Zoom"];
            playerReload = playerInput.actions["Reload"];
            startRun = playerInput.actions["RunStart"];
            endRun = playerInput.actions["RunEnd"];
            startRun.performed += x => PressSprint();
            endRun.performed += x => ReleaseSprint();
            weaponSwitch = playerInput.actions["SwitchWeapon"];
            cameraTransform = Camera.main.transform;
            controller = GetComponent<CharacterController>();
            shootingAudio = GetComponents<AudioSource>()[0];
            emptyAudio = GetComponents<AudioSource>()[1];
            reloadAudio = GetComponents<AudioSource>()[2];
            physSound = GetComponents<AudioSource>()[3];

            // Read the database: customize character with values read and get ammo available.
            InitializeCharacter();

            List<Collider> colliders = GetComponents<Collider>().ToList();
            colliders.AddRange(GetComponentsInChildren<Collider>());
            Colliders = colliders.Distinct().ToArray();

            physicalDamageOutput = CombatManager.PhysSelected == 3 ? 1 : CombatManager.PhysSelected == 4 ? 2 : 3;
        }

        // Update is called once per frame
        void Update()
        {
            // Combat actions for the player
            if(CombatManager.InCombat){
                // Stop falling
                isGrounded = controller.isGrounded;
                if(isGrounded && playerVelocity.y < 0){
                    playerVelocity.y = 0.0f;
                }

                // Running - only when key is hit and NOT zoomed in with a rifle
                playerSpeed = isRunning && !ZoomedIn ? 6.0f : 3.0f;

                // Movement
                Vector2 input = playerMove.ReadValue<Vector2>();
                Vector3 move = new Vector3(input.x,0,input.y);
                move = move.x * cameraTransform.right.normalized + move.z * cameraTransform.forward.normalized;
                move.y = 0.0f;
                controller.Move(move * Time.deltaTime * playerSpeed);

                // Falling
                playerVelocity.y += gravity * Time.deltaTime;
                controller.Move(playerVelocity * Time.deltaTime);

                // Rotation
                Quaternion targetRotation = Quaternion.Euler(0, cameraTransform.eulerAngles.y, 0);
                transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

                // Shooting or Physical attack
                if(playerShoot.triggered){
                    if(CanShoot && UsingGun && AmmoLoaded > 0){
                        int gun = CombatManager.GunSelected;
                        AmmoLoaded -= gun == 2 ? 3 : 1;
                        shootingAudio.Play();

                        // Spawn the bullet here
                        GameObject bullet = Instantiate(bulletPrefab, shootLocation.transform.position, shootLocation.transform.rotation);
                        bullet.transform.SetParent(CombatManager.CombatEnvironment.transform);
                        Projectile projectile = bullet.GetComponent<Projectile>();
                        projectile.Shooter = gameObject;
                        projectile.Velocity = gun == 0 || gun == 1 ? 20 : 15;
                        projectile.Damage = gun == 0 ? 2 : gun == 2 ? 4 : 6;

                        // Shoot 2 additional bullets if using a shotgun, 45 degrees left and right of the main one
                        if(gun == 2){
                            foreach(GameObject location in shotgunShootLocations){
                                bullet = Instantiate(bulletPrefab, location.transform.position, location.transform.rotation);
                                bullet.transform.SetParent(CombatManager.CombatEnvironment.transform);
                                bullet.GetComponent<Projectile>().Shooter = gameObject;
                                bullet.GetComponent<Projectile>().Velocity = 15;
                            }
                        }

                    }
                    else if(CanShoot && UsingGun && AmmoLoaded == 0){
                        emptyAudio.Play();
                    }
                    else if(!UsingGun){
                        // Check for the closest enemy and attack if close enough
                        GameObject[] mutants = GameObject.FindGameObjectsWithTag("Mutant");
                        GameObject target = mutants.Where(m => Vector3.Distance(transform.position, m.transform.position) < 1.0f).First();

                        if(target != null){
                            Mutant m = target.GetComponent<Mutant>();
                            m.PhysicalDamage(physicalDamageOutput);
                        }
                        else{
                            physSound.Play();
                        }
                    }
                }

                // Reloading 
                if(playerReload.triggered){
                    if(CanShoot && TotalAvailableAmmo > 0 && AmmoLoaded != maxAmmoLoaded){
                        StartCoroutine(GunDelay(reloadTimer));
                    }
                    else{
                        reloadAudio.Play();
                    }
                }

                // Switching weapon
                if(weaponSwitch.triggered){
                    UsingGun = !UsingGun;
                    CanShoot = UsingGun;
                    UpdateModel();
                }
            }
        }

        /// <summary>
        /// Initialize the player with data and ammo
        /// </summary>
        private void InitializeCharacter(){
            UpdateModel();

            IDbConnection dbConnection = GameDatabase.CreateActiveCharactersAndOpenDatabase();
            IDbCommand dbCommandReadValue = dbConnection.CreateCommand();
            dbCommandReadValue.CommandText = "SELECT leaderAcc, leaderOutfit, leaderColor, leaderHat, leaderName, leaderHealth FROM ActiveCharactersTable WHERE id = " +
                                             GameLoop.FileId;
            IDataReader dataReader = dbCommandReadValue.ExecuteReader();
            dataReader.Read();

            int acc = dataReader.GetInt32(0), outfit = dataReader.GetInt32(1), color = dataReader.GetInt32(2), hat = dataReader.GetInt32(3);
            hp = dataReader.GetInt32(5);
            nameText.text = dataReader.GetString(4);

            dbConnection.Close();

            transform.GetChild(0).transform.GetChild(0).GetComponent<MeshRenderer>().material = CharacterCreation.Colors[color-1];
            transform.GetChild(0).transform.GetChild(1).GetComponent<MeshRenderer>().material = CharacterCreation.Colors[color-1];

            switch(hat){
                case 1:
                    transform.GetChild(3).transform.GetChild(0).gameObject.SetActive(false);
                    transform.GetChild(3).transform.GetChild(1).gameObject.SetActive(false);
                    break;
                case 2:
                    transform.GetChild(3).transform.GetChild(0).gameObject.SetActive(true);
                    transform.GetChild(3).transform.GetChild(1).gameObject.SetActive(false);
                    break;
                case 3:
                    transform.GetChild(3).transform.GetChild(0).gameObject.SetActive(false);
                    transform.GetChild(3).transform.GetChild(1).gameObject.SetActive(true);
                    break;
            }

            switch(outfit){
                case 1:
                    transform.GetChild(1).transform.GetChild(0).gameObject.SetActive(false);
                    transform.GetChild(1).transform.GetChild(1).gameObject.SetActive(false);
                    break;
                case 2:
                    transform.GetChild(1).transform.GetChild(0).gameObject.SetActive(true);
                    transform.GetChild(1).transform.GetChild(1).gameObject.SetActive(false);
                    break;
                case 3:
                    transform.GetChild(1).transform.GetChild(0).gameObject.SetActive(false);
                    transform.GetChild(1).transform.GetChild(1).gameObject.SetActive(true);
                    break;
            }

            switch(acc){
                case 1:
                    transform.GetChild(2).transform.GetChild(0).gameObject.SetActive(false);
                    transform.GetChild(2).transform.GetChild(1).gameObject.SetActive(false);
                    break;
                case 2:
                    transform.GetChild(2).transform.GetChild(0).gameObject.SetActive(true);
                    transform.GetChild(2).transform.GetChild(1).gameObject.SetActive(false);
                    break;
                case 3:
                    transform.GetChild(2).transform.GetChild(0).gameObject.SetActive(false);
                    transform.GetChild(2).transform.GetChild(1).gameObject.SetActive(true);
                    break;
            }

            combatManager = GameObject.FindWithTag("CombatManager").GetComponent<CombatManager>();
            playerHealthBar = GameObject.FindWithTag("PlayerHealthBar").GetComponent<Slider>();
            playerHealthText = GameObject.FindWithTag("PlayerHealthText").GetComponent<TextMeshProUGUI>();
            shootLocation = GameObject.FindWithTag("ShootLocation");
            shotgunShootLocations = GameObject.FindGameObjectsWithTag("ShotgunShootLocation");

            int gun = CombatManager.GunSelected;
            reloadTimer = gun == 0 ? 3 : gun == 1 ? 5 : 7;
            
            playerHealthBar.value = hp;
            playerHealthText.text = "HP: " + hp.ToString() + "/100";

            dbConnection = GameDatabase.CreateSavesAndOpenDatabase();
            dbCommandReadValue = dbConnection.CreateCommand();
            dbCommandReadValue.CommandText = "SELECT ammo FROM SaveFilesTable WHERE id = " + GameLoop.FileId;
            dataReader = dbCommandReadValue.ExecuteReader();
            dataReader.Read();

            TotalAvailableAmmo = dataReader.GetInt32(0);
            AmmoLoaded = TotalAvailableAmmo - 6 > 0 ? 6 : TotalAvailableAmmo;
            TotalAvailableAmmo -= AmmoLoaded;

            alertText = GameObject.FindWithTag("AlertText").GetComponent<TextMeshProUGUI>();

            dbConnection.Close();
        }
    
        /// <summary>
        /// Toggle running when shift key is hit
        /// </summary>
        void PressSprint(){
            isRunning = !AimCamera.IsSwitched;
        }

        /// <summary>
        /// Toggle running when shift key is released
        /// </summary>
        void ReleaseSprint(){
            isRunning = false;
        }

        /// <summary>
        /// Receive damage from a mutant and apply "invincibility frames"
        /// </summary>
        /// <param name="amt">The amount of damaged received</param>
        private IEnumerator ReceiveDamage(int amt){
            damagedRecently = true;
            hp -= amt;
            playerHealthBar.value = hp;
            playerHealthText.text = "HP: " + hp.ToString() + "/100";

            if(hp <= 0){
                combatManager.EndCombatDeath();
            }
            yield return new WaitForSeconds(1.0f);
            damagedRecently = false;
        }

        /// <summary>
        /// Delay gun use from reloading
        /// </summary>
        /// <param name="delay">The delay to reload</param>
        private IEnumerator GunDelay(int delay){
            CanShoot = false;
            alertText.text = "Reloading";
            AlertText.MaxTime = delay;
            for(int i = 0; i < delay; i++){
                alertText.text = alertText.text + ".";
                yield return new WaitForSeconds(1.0f);
            }

            int totalReplaced = maxAmmoLoaded - AmmoLoaded > 0 ? maxAmmoLoaded - AmmoLoaded : 0;
            Player.TotalAvailableAmmo -= Player.TotalAvailableAmmo - totalReplaced > 0 ? totalReplaced : Player.TotalAvailableAmmo;
            AmmoLoaded = totalReplaced > 0 ? maxAmmoLoaded : 0;
            reloadAudio.Play();
            CanShoot = true;
        }

        /// <summary>
        /// Attempt to damage the player
        /// </summary>
        /// <param name="amt">The amount of damaged received</param>
        public void Damage(int amt){
            // If "invinciblity frames" are active, ignore the attempt. Added to avoid frame-by-frame damage, hp would go down quick
            if(!damagedRecently){
                damagedRecently = true;
                StartCoroutine(ReceiveDamage(amt));
            }
        }

        /// <summary>
        /// Update model depending on player weapon choice
        /// </summary>
        private void UpdateModel(){
            if(UsingGun){
                transform.GetChild(5).transform.GetChild(CombatManager.GunSelected).gameObject.SetActive(true);
                transform.GetChild(5).transform.GetChild(CombatManager.PhysSelected).gameObject.SetActive(false);
            }
            else{
                transform.GetChild(5).transform.GetChild(CombatManager.GunSelected).gameObject.SetActive(false);
                transform.GetChild(5).transform.GetChild(CombatManager.PhysSelected).gameObject.SetActive(true);
            }
        }
    }
}