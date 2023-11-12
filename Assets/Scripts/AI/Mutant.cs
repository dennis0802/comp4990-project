using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using CombatPhase;
using RestPhase;
using TravelPhase;

namespace AI{
    public class Mutant : BaseAgent
    {
        
        [Tooltip("Bullet prefab")]
        [SerializeField]
        private GameObject bulletPrefab;

        /// <summary>
        /// Min speed to be considered stopped.
        /// </summary>
        public float minStopSpeed;

        /// <summary>
        /// Delay to shoot
        /// </summary>
        public float shotDelay = 0.0f;

        /// <summary>
        /// Type of mutant (0 = standard, 1 = big, 2 = ranged, 3 = boss)
        /// </summary>
        public int mutantType;

        /// <summary>
        /// Strength of mutant's attacks (damage that can be dealt to players)
        /// </summary>
        public int strength;

        /// <summary>
        /// Mutant hp.
        /// </summary>
        public int hp;

        /// <summary>
        /// If teammate was damaged recently (are invincibiltiy frames active?)
        /// </summary>
        public bool damagedRecently;

        /// <summary>
        /// The target transform
        /// </summary>
        public Transform TargetTransform;

        /// <summary>
        /// List of colliders on the agent
        /// </summary> 
        public Collider[] Colliders {get; private set;}

        /// <summary>
        /// Attack audio
        /// </summary> 
        private AudioSource attackAudio;

        /// <summary>
        /// Hurt audio
        /// </summary> 
        private AudioSource hurtAudio;

        /// <summary>
        /// Location to spawn bullets regularly
        /// </summary>
        private GameObject shootLocation;

        protected override void Start(){
            base.Start();
            List<Collider> colliders = GetComponents<Collider>().ToList();
            colliders.AddRange(GetComponentsInChildren<Collider>());
            Colliders = colliders.Distinct().ToArray();
            shootLocation = GameObject.FindGameObjectsWithTag("ShootLocation").Where(s => s.GetComponentInParent<Mutant>() == this).FirstOrDefault();
            attackAudio = GetComponents<AudioSource>()[0];
            hurtAudio = GetComponents<AudioSource>()[1];
        }

        /// <summary>
        /// Receive physical damage from a party member and apply "invincibility frames"
        /// </summary>
        /// <param name="amt">The amount of damaged received</param>
        private IEnumerator ReceivePhysicalDamage(int amt){
            damagedRecently = true;
            hurtAudio.Play();
            hp -= amt;

            // Die
            if(hp <= 0){
                Die();
            }
            yield return new WaitForSeconds(2.0f);
            damagedRecently = false;
        }

        /// <summary>
        /// Attempt to physically damage the mutant
        /// </summary>
        /// <param name="amt">The amount of damaged received</param>
        public void PhysicalDamage(int amt){
            // If "invinciblity frames" are active, ignore the attempt. Added to avoid frame-by-frame damage, hp would go down quick
            if(!damagedRecently){
                damagedRecently = true;
                StartCoroutine(ReceivePhysicalDamage(amt));
            }
        }

        /// <summary>
        /// Attempt to range damage the mutant
        /// </summary>
        /// <param name="amt">The amount of damaged received</param>
        public void RangedDamage(int amt){
            // Since this relies on a collision (ie. not frame-by-frame in Update, no invincibility frames are needed)
            hp -= amt;
            hurtAudio.Play();
            if(hp <= 0){
                Die();
            }
        }

        public void Die(){
            // Check if counters need to change
            if(CombatManager.JobType == 1 || TravelLoop.GoingToCombat || TravelLoop.InFinalCombat){
                CombatManager.EnemiesToKill--;
            }

            CombatManager.RemoveAgent(this);
            Destroy(gameObject);
        }

        /// <summary>
        /// Set mutant hp
        /// </summary>
        public void SetHP(int hp){
            this.hp = hp;
        }

        /// <summary>
        /// Set mutant strength
        /// </summary>
        public void SetStrength(int amt){
            strength = amt;
        }

        /// <summary>
        /// Attempt to shoot a team member
        /// </summary>
        public void Shoot(){
            if(this.mutantType <= 1){
                return;
            }
            else{
                // Spawn the bullet here
                GameObject bullet = Instantiate(bulletPrefab, shootLocation.transform.position, shootLocation.transform.rotation);
                bullet.transform.SetParent(CombatManager.CombatEnvironment.transform);
                Projectile projectile = bullet.GetComponent<Projectile>();
                projectile.Shooter = gameObject;
                projectile.Velocity = mutantType == 3 ? 20 : 15;
                projectile.Damage = mutantType == 3 ? 7 : 5;

                attackAudio.Play();
                shotDelay = 1.0f;
            }
        }
    }
}

