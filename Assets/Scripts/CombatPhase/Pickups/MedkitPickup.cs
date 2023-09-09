using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CombatPhase.Pickups{
    [DisallowMultipleComponent]
    public class MedkitPickup : BasePickup
    {
        [Tooltip("The visuals object to rotate")]
        [SerializeField]
        private Transform visuals;

        protected override void OnPickup(Player player)
        {
            player.suppliesGathered[4] += 1;
            Destroy(gameObject);
        }

        void Update(){
            // Spin visuals
            visuals.Rotate(0, Speed * Time.deltaTime, 0, Space.Self);
        }
    }
}

