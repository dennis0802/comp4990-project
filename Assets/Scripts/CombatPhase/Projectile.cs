using System.Collections;
using System.Collections.Generic;
using AI;
using CombatPhase;
using UnityEngine;

namespace CombatPhase{
    /// <summary>
    /// Projectiles (bullets)
    /// </summary> 
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(Rigidbody))]
    public class Projectile : MonoBehaviour
    {
        /// <summary>
        /// Rigidbody attached to the projectile
        /// </summary> 
        private Rigidbody _rb;

        /// <summary>
        /// Timer for the bullet to exist
        /// </summary> 
        private float timer = 0.0f;

        /// <summary>
        /// Velocity of the projectile
        /// </summary> 
        public float Velocity {get; set;}

        /// <summary>
        /// Velocity of the projectile
        /// </summary> 
        public int Damage {get; set;}

        /// <summary>
        /// The party member who shot the bullet (GameObject to generalize)
        /// </summary> 
        public GameObject Shooter {get; set;}

        // Start is called before the first frame update
        void Start()
        {
            Collider col = GetComponent<Collider>();
            Player player = Shooter.GetComponent<Player>();
            Teammate teammate = Shooter.GetComponent<Teammate>();

            // Ignore collisions with the party member who shot
            if(player != null){
                foreach(Collider hitbox in player.Colliders){
                    if(hitbox != null && hitbox.enabled){
                        Physics.IgnoreCollision(col, hitbox, true);
                    }
                }
            }
            else if(teammate != null){
                foreach(Collider hitbox in teammate.Colliders){
                    if(hitbox != null && hitbox.enabled){
                        Physics.IgnoreCollision(col, hitbox, true);
                    }
                }
            }

            _rb = GetComponent<Rigidbody>();
            _rb.useGravity = false;
            _rb.AddRelativeForce(Vector3.forward * Velocity, ForceMode.VelocityChange);
        }

        private void OnCollisionEnter(Collision collision){
            HandleCollision(collision.transform);
        }

        /// <summary>
        /// Handle the collision
        /// </summary> 
        /// <param name="tr">The transform that was hit</param>
        private void HandleCollision(Transform tr){
            // See if a mutant was hit
            Mutant mutant;
            do{
                mutant = tr.GetComponent<Mutant>();
                tr = tr.parent;
            } while (mutant == null && tr != null);

            Debug.Log(mutant);
            if(mutant != null){
                Debug.Log("Mutant hit");
                mutant.RangedDamage(Damage);
            }

            // Destroy projectile
            Destroy(gameObject);
        }

        // Update is called once per frame
        void Update()
        {
            // Despawn bullets that don't hit anyone
            timer += Time.deltaTime;
            if(timer >= 10.0f){
                Destroy(gameObject);
            }
        }
    }
}