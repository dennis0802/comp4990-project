using System.Collections;
using System.Collections.Generic;
using AI;
using UnityEngine;

namespace CombatPhase.Pickups{
    /// <summary>
    /// Base class for pickups
    /// </summary>
    [DisallowMultipleComponent]
    public abstract class BasePickup : MonoBehaviour
    {
        /// <summary>
        /// How fast to spin the visual in degrees per second
        /// </summary>
        public const float Speed = 180;

        /// <summary>
        /// Implement behaviour for when picked up
        /// </summary>
        protected abstract void OnPickup(Player player);
        
        /// <summary>
        /// Implement behaviour for when picked up
        /// </summary>
        protected abstract void OnPickup(Teammate teammate);

        private void OnTriggerEnter(Collider other){
            DetectPickup(other);
        }

        /// <summary>
        /// Detect when picked up
        /// </summary>
        /// <param name="other">The object collided with</param>
        private void DetectPickup(Component other){
            Player player = other.gameObject.GetComponent<Player>();
            Teammate teammate = other.gameObject.GetComponent<Teammate>();
            CombatManager.itemCollected.Play();

            if(player != null){
                OnPickup(player);
            }
            else if(teammate != null){
                OnPickup(teammate);
            }
        }
    }
}