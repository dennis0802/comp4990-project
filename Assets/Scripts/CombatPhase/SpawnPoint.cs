using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CombatPhase{
    public class SpawnPoint : MonoBehaviour
    {
        public bool inUse = false;
        public bool set = false;
        public float distance = 50f;

        void OnDrawGizmos(){
            // Depending on tag, draw a sphere to indicate location and rotation on Unity Editor
            if(gameObject.tag == "PlayerSpawn"){
                Gizmos.color = Color.blue;
            }
            else if(gameObject.tag == "EnemySpawn"){
                Gizmos.color = Color.red;
            }
            else if(gameObject.tag == "AllySpawn"){
                Gizmos.color = Color.green;
            }
            else if(gameObject.tag == "PickupSpawn"){
                Gizmos.color = Color.cyan;
            }
            else{
                Gizmos.color = Color.white;
            }

            Gizmos.DrawSphere(transform.position, 1f);
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, transform.position + transform.forward * 2);
        }

        void Update(){
            // The spawnpoints will initially be set in the air to work with the randomized terrain
            // When terrain is generated, send a raycast to the ground
            RaycastHit hit;

            // If contact with the ground found and hasn't been set yet, set the spawnpoint on the ground
            if(!set && Physics.Raycast(transform.position, Vector3.down, out hit, distance)) {
                if(hit.collider != null){
                    Vector3 targetLocation = hit.point;
                    targetLocation += new Vector3(0, transform.localPosition.y/8.5f, 0);
                    transform.position = targetLocation;
                    set = true;
                }
            }

            if(set && transform.position.y >= 10.0f){
                Destroy(gameObject);
            }
        }
    }
}