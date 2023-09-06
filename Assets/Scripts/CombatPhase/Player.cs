using System.Collections;
using System.Collections.Generic;
using System.Data;
using UnityEngine;
using UnityEngine.InputSystem;
using CombatPhase;
using Database;
using UI;
using Mono.Data.Sqlite;

namespace CombatPhase{
    [DisallowMultipleComponent]
    public class Player : MonoBehaviour
    {
        [Tooltip("Player input object")]
        [SerializeField]
        private PlayerInput playerInput;

        [Tooltip("Colors for players")]
        [SerializeField]
        private Material[] playerColors;

        private InputAction playerMove, playerShoot, weaponSwitch, playerReload;
        private CharacterController controller;
        private Transform cameraTransform;
        private float reloadTimer = 0.0f, gravity = -9.81f, playerSpeed = 3.0f, rotationSpeed = 5f;
        private Vector3 playerVelocity;
        public static int TotalAvailableAmmo = 0, AmmoLoaded = 0;
        private const int maxAmmoLoaded = 6;
        private bool isGrounded, busyReloading;
        public static bool UsingGun = true;

        // Start is called before the first frame update
        void Start()
        {
            playerShoot = playerInput.actions["LeftClick"];
            playerMove = playerInput.actions["Move"];
            playerReload = playerInput.actions["Reload"];
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
            if(CombatLoop.InCombat){
                // Stop falling
                isGrounded = controller.isGrounded;
                if(isGrounded && playerVelocity.y < 0){
                    playerVelocity.y = 0.0f;
                }

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
                        AmmoLoaded -= CombatLoop.GunSelected == 2 ? 3 : 1;
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
            dbCommandReadValue.CommandText = "SELECT * FROM ActiveCharactersTable WHERE id = " + GameLoop.FileId;
            IDataReader dataReader = dbCommandReadValue.ExecuteReader();
            dataReader.Read();

            int acc = dataReader.GetInt32(4), outfit = dataReader.GetInt32(5), color = dataReader.GetInt32(6), hat = dataReader.GetInt32(7);

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

            dbConnection = GameDatabase.CreateSavesAndOpenDatabase();
            dbCommandReadValue = dbConnection.CreateCommand();
            dbCommandReadValue.CommandText = "SELECT * FROM SaveFilesTable WHERE id = " + GameLoop.FileId;
            dataReader = dbCommandReadValue.ExecuteReader();
            dataReader.Read();

            TotalAvailableAmmo = dataReader.GetInt32(14);

            dbConnection.Close();
        }
    }
}