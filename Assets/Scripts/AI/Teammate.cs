using System.Collections;
using System.Collections.Generic;
using System.Data;
using UnityEngine;
using CombatPhase;
using Database;
using UI;
using TMPro;
using Mono.Data.Sqlite;

namespace AI{
    public class Teammate : BaseAgent
    {
        [Tooltip("Colors for players")]
        [SerializeField]
        private Material[] playerColors;

        [Tooltip("Name text")]
        [SerializeField]
        private TextMeshPro nameText;

        /// <summary>
        /// Min speed to be considered stopped
        /// </summary>
        public float minStopSpeed;

        public Player leader;

        public int id = 0, hp = 0, ammo = 0;
        public bool usingGun;

        protected override void Start(){
            base.Start();
            InitializeCharacter();
        }

        /// <summary>
        /// Initialize the ally with data
        /// </summary>
        private void InitializeCharacter(){
            IDbConnection dbConnection = GameDatabase.CreateActiveCharactersAndOpenDatabase();
            IDbCommand dbCommandReadValue = dbConnection.CreateCommand();
            dbCommandReadValue.CommandText = "SELECT * FROM ActiveCharactersTable WHERE id = " + GameLoop.FileId;
            IDataReader dataReader = dbCommandReadValue.ExecuteReader();
            dataReader.Read();

            int acc = dataReader.GetInt32(4+9*id), outfit = dataReader.GetInt32(5+9*id), color = dataReader.GetInt32(6+9*id), hat = dataReader.GetInt32(7+9*id),
                hpDB = dataReader.GetInt32(9+9*id), livingMembers = 0;

            for(int i = 0; i < 4; i++){
                if(!dataReader.IsDBNull(1+9*i)){
                    livingMembers++;
                }
            }
            
            nameText.text = dataReader.GetString(1+9*id);
            hp = hpDB;

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

            ammo = Player.TotalAvailableAmmo/livingMembers;
            Player.TotalAvailableAmmo -= ammo;

            dbConnection.Close();
        }

        public void UpdateModel(){

        }
    }
}

