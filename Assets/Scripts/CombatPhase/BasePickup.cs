using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CombatPhase{
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
        /// The spawnpoint the collectible spawned on
        /// </summary>
        public SpawnPoint spawn;

        /// <summary>
        /// Implement behaviour for when picked up
        /// </summary>
        protected abstract void OnPickup(Player player);

        private void OnTriggerEnter(Collider other){
            DetectPickup(other);
        }

        /// <summary>
        /// Detect when picked up
        /// </summary>
        /// <param name="other">The object collided with</param>
        private void DetectPickup(Component other){
            Player player = other.gameObject.GetComponent<Player>();
            if(player != null){
                OnPickup(player);
            }
        }
    }
}