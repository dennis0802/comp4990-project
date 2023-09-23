using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CombatPhase{
    public class DefensivePoint : MonoBehaviour
    {
        public bool inUse = false;

        void OnDrawGizmos(){
            Gizmos.color = Color.black;
            Gizmos.DrawSphere(transform.position, 1f);
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, transform.position + transform.forward * 2);
        }
    }
}