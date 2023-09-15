using System.Collections;
using System.Collections.Generic;
using System.Data;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using CombatPhase;
using Database;
using UI;
using TMPro;
using Mono.Data.Sqlite;

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

        [Tooltip("Colors for players")]
        [SerializeField]
        private Material[] playerColors;

        private Slider playerHealthBar;

        private TextMeshProUGUI playerHealthText;

        /// <summary>
        /// Player input actions
        /// </summary> 
        private InputAction playerMove, playerShoot, weaponSwitch, playerReload, startRun, endRun;
        
        /// <summary>
        /// Controller for the player
        /// </summary> 
        private CharacterController controller;

        /// <summary>
        /// Camera transform following the player
        /// </summary> 
        private Transform cameraTransform;

        /// <summary>
        /// Player reload timer
        /// </summary>  
        private float reloadTimer = 0.0f;

        /// <summary>
        /// Values for player movement physics
        /// </summary>  
        private float gravity = -9.81f, playerSpeed = 3.0f, rotationSpeed = 5f;

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
        private bool isGrounded, busyReloading, isRunning = false;

        public int hp;

        /// <summary>
        /// Flag if gun is being used
        /// </summary> 
        public static bool UsingGun = true;

        // Start is called before the first frame update
        void Start()
        {
            playerShoot = playerInput.actions["LeftClick"];
            playerMove = playerInput.actions["Move"];
            playerReload = playerInput.actions["Reload"];
            startRun = playerInput.actions["RunStart"];
            endRun = playerInput.actions["RunEnd"];
            startRun.performed += x => PressSprint();
            endRun.performed += x => ReleaseSprint();
            weaponSwitch = playerInput.actions["SwitchWeapon"];
            cameraTransform = Camera.main.transform;
            controller = gameObject.GetComponent<CharacterController>();

            // Read the database: customize character with values read and get ammo available.
            InitializeCharacter();
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

                // Running
                playerSpeed = isRunning ? 6.0f : 3.0f;

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

                // Shooting
                if(playerShoot.triggered){
                    if(UsingGun && AmmoLoaded > 0){
                        AmmoLoaded -= CombatManager.GunSelected == 2 ? 3 : 1;
                    }
                    else if(UsingGun && AmmoLoaded == 0){
                        // Play empty gun sound here.
                        Debug.Log("Player is out of ammo");
                    }
                    else{
                        Debug.Log("Player wants to attack");
                    }
                }   

                // Reloading 
                if(playerReload.triggered){
                    if(TotalAvailableAmmo > 0 && AmmoLoaded != maxAmmoLoaded){
                        int totalReplaced = maxAmmoLoaded - AmmoLoaded > 0 ? maxAmmoLoaded - AmmoLoaded : 0;
                        Player.TotalAvailableAmmo -= totalReplaced;
                        AmmoLoaded = totalReplaced > 0 ? maxAmmoLoaded : 0;
                    }
                    else{

                    }
                }

                // Check health
                if(hp <= 0){
                    // Die (end the game)
                }

                // Switching weapon
                if(weaponSwitch.triggered){
                    UsingGun = !UsingGun;
                }
            }
        }

        /// <summary>
        /// Initialize the player with data and ammo
        /// </summary>
        private void InitializeCharacter(){
            IDbConnection dbConnection = GameDatabase.CreateActiveCharactersAndOpenDatabase();
            IDbCommand dbCommandReadValue = dbConnection.CreateCommand();
            dbCommandReadValue.CommandText = "SELECT leaderAcc, leaderOutfit, leaderColor, leaderHat, leaderName, leaderHealth FROM ActiveCharactersTable WHERE id = " +
                                             GameLoop.FileId;
            IDataReader dataReader = dbCommandReadValue.ExecuteReader();
            dataReader.Read();

            int acc = dataReader.GetInt32(0), outfit = dataReader.GetInt32(1), color = dataReader.GetInt32(2), hat = dataReader.GetInt32(3), hp = dataReader.GetInt32(5);
            nameText.text = dataReader.GetString(4);

            dbConnection.Close();

            transform.GetChild(0).transform.GetChild(0).GetComponent<MeshRenderer>().material = playerColors[color-1];
            transform.GetChild(0).transform.GetChild(1).GetComponent<MeshRenderer>().material = playerColors[color-1];

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

            playerHealthBar = GameObject.FindWithTag("PlayerHealthBar").GetComponent<Slider>();
            playerHealthText = GameObject.FindWithTag("PlayerHealthText").GetComponent<TextMeshProUGUI>();
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

            dbConnection.Close();
        }
    
        /// <summary>
        /// Toggle running when shift key is hit
        /// </summary>
        void PressSprint(){
            isRunning = true;
        }

        /// <summary>
        /// Toggle running when shift key is released
        /// </summary>
        void ReleaseSprint(){
            isRunning = false;
        }

        /// <summary>
        /// Receive damage from a mutant
        /// </summary>
        public void ReceiveDamage(int amt){
            hp -= amt;
            playerHealthBar.value = hp;
            playerHealthText.text = "HP: " + hp.ToString() + "/100";

            if(hp <= 0){
                // Die
                Debug.Log("deadge");
            }
        }
    }
}