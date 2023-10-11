using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CombatPhase{
    public class DefensivePoint : MonoBehaviour
    {
        public bool inUse = false;
        public bool set = false;
        public float distance = 100f;

        void OnDrawGizmos(){
            Gizmos.color = Color.black;
            Gizmos.DrawSphere(transform.position, 1f);
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, transform.position + transform.forward * 2);
        }

        void Update(){
            // The defensive points will initially be set in the air to work with the randomized terrain
            // When terrain is generated, send a raycast to the ground
            RaycastHit hit;

            // If contact with the ground found and hasn't been set yet, set the spawnpoint on the ground
            if(!set && Physics.Raycast(transform.position, Vector3.down, out hit, distance)) {
                Vector3 targetLocation = hit.point;
                targetLocation += new Vector3(0, transform.localScale.y / 2, 0);
                transform.position = targetLocation;
                set = true;
            }
        }
    }
}