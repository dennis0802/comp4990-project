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
        [Tooltip("Name text")]
        [SerializeField]
        private TextMeshPro nameText;

        [Tooltip("Bullet prefab")]
        [SerializeField]
        private GameObject bulletPrefab;

        /// <summary>
        /// Text to alert the player
        /// </summary>
        private TextMeshProUGUI alertText;

        /// <summary>
        /// Location to spawn bullets
        /// </summary>
        private GameObject shootLocation;

        /// <summary>
        /// Additional locations to spawn bullets via shotgun
        /// </summary>
        private GameObject[] shotgunShootLocations;

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
        /// Delay to shoot
        /// </summary>
        public float shotDelay = 0.0f;

        /// <summary>
        /// Teammate's ammo on hand
        /// </summary>
        public int ammoTotal = 0;

        /// <summary>
        /// Teammate's ammo loaded
        /// </summary>
        public int ammoLoaded = 0;

        /// <summary>
        /// Teammate's physical damage output
        /// </summary>
        public int physicalDamageOutput;
        
        /// <summary>
        /// If teammate is using a gun
        /// </summary>
        public bool usingGun;

        /// <summary>
        /// If teammate is running away
        /// </summary>
        public bool isRunningAway;

        /// <summary>
        /// If teammate was damaged recently (are invincibiltiy frames active?)
        /// </summary>
        public bool damagedRecently;

        /// <summary>
        /// Name of the teammate
        /// </summary>
        public string allyName;

        /// <summary>
        /// The defensive point this ally is using
        /// </summary>
        public DefensivePoint defensivePointUsed;

        /// <summary>
        /// List of colliders on the agent
        /// </summary> 
        public Collider[] Colliders {get; private set;}

        /// <summary>
        /// Shooting audio
        /// </summary> 
        private AudioSource shootingAudio;

        /// <summary>
        /// If player has sharpshooter perk
        /// </summary>
        public bool isSharpshooter;

        /// <summary>
        /// If player has hotheaded trait
        /// </summary>
        public bool isHotHeaded;

        /// <summary>
        /// If player is actively poisoned
        /// </summary>
        public bool isPoisoned;

        protected override void Start(){
            base.Start();
            InitializeCharacter();

            List<Collider> colliders = GetComponents<Collider>().ToList();
            colliders.AddRange(GetComponentsInChildren<Collider>());
            Colliders = colliders.Distinct().ToArray();
            shootingAudio = GetComponents<AudioSource>()[0];

            physicalDamageOutput = CombatManager.PhysSelected == 3 ? 1 : CombatManager.PhysSelected == 4 ? 2 : 3;
            physicalDamageOutput += isHotHeaded ? 3 : 0;
        }

        /// <summary>
        /// Initialize the ally with data
        /// </summary>
        private void InitializeCharacter(){
            UpdateModel();
            ActiveCharacter teammate = DataUser.dataManager.GetCharacter(GameLoop.FileId, id);
            int acc = teammate.Acessory, outfit = teammate.Outfit, color = teammate.Color, hat = teammate.Hat, hpDB = teammate.Health;
            nameText.text = allyName;
            hp = hpDB;

            // Visuals
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

            alertText = GameObject.FindWithTag("AlertText").GetComponent<TextMeshProUGUI>();
            shootLocation = GameObject.FindGameObjectsWithTag("ShootLocation").Where(s => s.GetComponentInParent<Teammate>() == this).First();
            shotgunShootLocations = GameObject.FindGameObjectsWithTag("ShotgunShootLocation").Where(s => s.GetComponentInParent<Teammate>() == this).ToArray();

            ammoTotal = Player.TotalAvailableAmmo/DataUser.dataManager.GetActiveCharacters().Where<ActiveCharacter>(c=>c.FileId == GameLoop.FileId).Count();
            Player.TotalAvailableAmmo -= ammoTotal;
            Reload();
        }

        /// <summary>
        /// Update model based on weapon selected
        /// </summary>
        public void UpdateModel(){
            if(usingGun){
                transform.GetChild(5).transform.GetChild(CombatManager.GunSelected).gameObject.SetActive(true);
                transform.GetChild(5).transform.GetChild(CombatManager.PhysSelected).gameObject.SetActive(false);
            }
            else{
                transform.GetChild(5).transform.GetChild(CombatManager.GunSelected).gameObject.SetActive(false);
                transform.GetChild(5).transform.GetChild(CombatManager.PhysSelected).gameObject.SetActive(true);
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
                CombatManager.DeadMembers.Add(id);
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
        /// Attempt to range damage the teammate
        /// </summary>
        /// <param name="amt">The amount of damaged received</param>
        public void RangedDamage(int amt){
            // Since this relies on a collision (ie. not frame-by-frame in Update, no invincibility frames are needed)
            hp -= amt;
            if(hp <= 0){
                CombatManager.RemoveAgent(this);
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Reload gun with ammo
        /// </summary>
        public void Reload(){
            ammoLoaded = ammoTotal - 6 > 0 ? 6 : ammoTotal;
            ammoTotal -= ammoLoaded;
        }

        /// <summary>
        /// Attempt to shoot a mutant
        /// </summary>
        public void Shoot(){
            int gun = CombatManager.GunSelected;
            ammoLoaded -= gun == 2 ? 3 : 1;
            shootingAudio.Play();

            // Spawn the bullet here
            GameObject bullet = Instantiate(bulletPrefab, shootLocation.transform.position, shootLocation.transform.rotation);
            bullet.transform.SetParent(CombatManager.CombatEnvironment.transform);
            Projectile projectile = bullet.GetComponent<Projectile>();
            projectile.Shooter = gameObject;
            projectile.Velocity = gun == 0 || gun == 1 ? 20 : 15;
            projectile.Damage = gun == 0 ? 2 : gun == 2 ? 4 : 6;

            // 40% of piercing if a sharpshooter 
            if(isSharpshooter && Random.Range(1,101) <= 40){
                projectile.Damage += 5;
            }

            // Shoot 2 additional bullets if using a shotgun, 45 degrees left and right of the main one
            if(gun == 2){
                foreach(GameObject location in shotgunShootLocations){
                    bullet = Instantiate(bulletPrefab, location.transform.position, location.transform.rotation);
                    bullet.transform.SetParent(CombatManager.CombatEnvironment.transform);
                    bullet.GetComponent<Projectile>().Shooter = gameObject;
                    bullet.GetComponent<Projectile>().Velocity = 15;
                }
            }

            shotDelay = 1.0f;
        }

        /// <summary>
        /// Leave a defensive point
        /// </summary>
        public void LeaveDefensivePoint(){
            if(defensivePointUsed != null){
                defensivePointUsed.inUse = false;
                defensivePointUsed = null;
            }
        }
    }
}

