using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AI;

namespace CombatPhase.Pickups{
    [DisallowMultipleComponent]
    public class AmmoPickup : BasePickup
    {
        [Tooltip("The visuals object to rotate")]
        [SerializeField]
        private Transform visuals;

        protected override void OnPickup(Player player)
        {
            player.suppliesGathered[5] += 1;
            Player.TotalAvailableAmmo += 10;
            Destroy(gameObject);
        }

        protected override void OnPickup(Teammate teammate)
        {
            teammate.leader.suppliesGathered[0] += 1;
            teammate.ammoTotal += 10;
            Destroy(gameObject);
        }

        void Update(){
            // Spin visuals
            visuals.Rotate(0, Speed * Time.deltaTime, 0, Space.Self);
        }
    }
}

