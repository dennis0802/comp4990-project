using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CombatPhase{
    public class SpawnPoint : MonoBehaviour
    {
        private bool inUse = false;

        void OnDrawGizmos(){
            if(gameObject.tag == "PlayerSpawn"){
                Gizmos.color = Color.blue;
            }
            else if(gameObject.tag == "EnemySpawn"){
                Gizmos.color = Color.red;
            }
            else if(gameObject.tag == "AllySpawn"){
                Gizmos.color = Color.green;
            }
            else{
                Gizmos.color = Color.white;
            }

            Gizmos.DrawSphere(transform.position, 1f);
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, transform.position + transform.forward * 2);
        }
    }
}