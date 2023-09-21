using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CombatPhase;

namespace AI{
    public class Mutant : BaseAgent
    {
        /// <summary>
        /// Min speed to be considered stopped.
        /// </summary>
        public float minStopSpeed;

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
        /// If teammate was damaged recently (are invincibiltiy frames active?)
        /// </summary>
        public Transform Target;

        /// <summary>
        /// Receive physical damage from a party member and apply "invincibility frames"
        /// </summary>
        /// <param name="amt">The amount of damaged received</param>
        private IEnumerator ReceivePhysicalDamage(int amt){
            damagedRecently = true;
            hp -= amt;

            if(hp <= 0){
                // Display on screen "<allyName> has perished."

                // Die
                CombatManager.RemoveAgent(this);
                Destroy(gameObject);
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
            if(hp <= 0){
                CombatManager.RemoveAgent(this);
                Destroy(gameObject);
            }
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
    }
}

