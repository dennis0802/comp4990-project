using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AI;

namespace CombatPhase.Pickups{
    [DisallowMultipleComponent]
    public class TargetPickup : BasePickup
    {
        [Tooltip("The visuals object to rotate")]
        [SerializeField]
        private Transform visuals;

        protected override void OnPickup(Player player)
        {
            CombatManager.TargetItemFound = true;
            Destroy(gameObject);
        }

        protected override void OnPickup(Teammate teammate)
        {
            CombatManager.TargetItemFound = true;
            Destroy(gameObject);
        }

        void Update(){
            // Spin visuals
            visuals.Rotate(0, Speed * Time.deltaTime, 0, Space.Self);
        }
    }
}

