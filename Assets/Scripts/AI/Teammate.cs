using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
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
        /// Text to alert the player if teammate has perished
        /// </summary>
        private TextMeshProUGUI alertText;

        /// <summary>
        /// Min speed to be considered stopped
        /// </summary>
        public float minStopSpeed;

        /// <summary>
        /// The teammate's leader
        /// </summary>
        public Player leader;

        /// <summary>
        /// Min speed to be considered stopped
        /// </summary>
        public int id = 0;

        /// <summary>
        /// Teammate's health
        /// </summary>
        public int hp = 0;

        /// <summary>
        /// Teammate's ammo on hand
        /// </summary>
        public int ammoTotal = 0;

        /// <summary>
        /// Teammate's ammo loaded
        /// </summary>
        public int ammoLoaded = 0;
        
        /// <summary>
        /// If teammate is using a gun
        /// </summary>
        public bool usingGun;

        /// <summary>
        /// If teammate was damaged recently (are invincibiltiy frames active?)
        /// </summary>
        public bool damagedRecently;

        /// <summary>
        /// Name of the teammate
        /// </summary>
        public string allyName;

        /// <summary>
        /// List of colliders on the agent
        /// </summary> 
        public Collider[] Colliders {get; private set;}

        protected override void Start(){
            base.Start();
            InitializeCharacter();

            List<Collider> colliders = GetComponents<Collider>().ToList();
            colliders.AddRange(GetComponentsInChildren<Collider>());
            Colliders = colliders.Distinct().ToArray();
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

            // An ally is living if their name is not null
            for(int i = 0; i < 4; i++){
                if(!dataReader.IsDBNull(1+9*i)){
                    livingMembers++;
                }
            }
            
            allyName = dataReader.GetString(1+9*id);
            nameText.text = allyName;
            hp = hpDB;

            dbConnection.Close();

            // Visuals
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

            alertText = GameObject.FindWithTag("AlertText").GetComponent<TextMeshProUGUI>();

            ammoTotal = Player.TotalAvailableAmmo/livingMembers;
            Player.TotalAvailableAmmo -= ammoTotal;
            Reload();
        }

        /// <summary>
        /// Update model based on weapon selected
        /// </summary>
        public void UpdateModel(){
            if(usingGun){

            }
            else{

            }
        }

        /// <summary>
        /// Receive damage from a mutant and apply "invincibility frames"
        /// </summary>
        /// <param name="amt">The amount of damaged received</param>
        private IEnumerator ReceiveDamage(int amt){
            damagedRecently = true;
            hp -= amt;

            if(hp <= 0){
                // Display on screen to alert player
                alertText.text = allyName + " has perished.";

                // Die
                CombatManager.RemoveAgent(this);
                Destroy(gameObject);
            }
            yield return new WaitForSeconds(2.0f);
            damagedRecently = false;
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
        /// Reload gun with ammo
        /// </summary>
        public void Reload(){
            ammoLoaded = ammoTotal - 6 > 0 ? 6 : ammoTotal;
            ammoTotal -= ammoLoaded;
        }
    }
}

