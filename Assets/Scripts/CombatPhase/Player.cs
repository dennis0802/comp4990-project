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

        private InputAction playerMove, playerShoot, weaponSwitch, playerReload;
        private CharacterController controller;
        private Transform cameraTransform;
        private float reloadTimer = 0.0f;
        public static int TotalAvailableAmmo = 0, AmmoLoaded = 0;
        private const int maxAmmoLoaded = 6;
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
                Vector2 input = playerMove.ReadValue<Vector2>();
                Vector3 move = new Vector3(input.x,0,input.y);
                move = move.x * cameraTransform.right.normalized + move.z * cameraTransform.forward.normalized;
                controller.Move(move * Time.deltaTime * 3.0f);

                // Shooting
                if(playerShoot.triggered){
                    if(UsingGun && AmmoLoaded > 0){
                        Debug.Log("Player wants to shoot");
                        AmmoLoaded -= CombatLoop.GunSelected == 2 ? 3 : 1;
                    }
                    else if(UsingGun && AmmoLoaded == 0){
                        Debug.Log("Player is out of ammo");
                    }
                    else{
                        Debug.Log("Player wants to attack");
                    }
                }   

                // Reloading
                if(playerReload.triggered){
                    if(UsingGun && AmmoLoaded != maxAmmoLoaded){
                        int totalReplaced = maxAmmoLoaded - AmmoLoaded;
                        Player.TotalAvailableAmmo -= totalReplaced;
                        AmmoLoaded = maxAmmoLoaded;
                    }
                }

                // Switching weapon
                if(weaponSwitch.triggered){
                    Debug.Log("Player wants to switch");
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

            dbConnection.Close();

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