using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CombatPhase;

namespace AI{
    public class Mutant : BaseAgent
    {
        /// <summary>
        /// Min speed to be considered stopped
        /// </summary>
        public float minStopSpeed;

        public int hp;

        /// <summary>
        /// Receive damage from a party member
        /// </summary>
        /// <param name="amt">The amount of damage received</param>
        public void ReceiveDamage(int amt){

        }

        /// <summary>
        /// Set mutant hp
        /// </summary>
        public void SetHP(int hp){
            this.hp = hp;
        }
    }
}

