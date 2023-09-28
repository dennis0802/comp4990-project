using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Unity.AI.Navigation;

namespace CombatPhase{
    public class ObstacleGenerator : MonoBehaviour
    {
        private NavMeshSurface surface;
        public GameObject myObj;
        void Start(){
            SpawnObjects();
            surface = GetComponent<NavMeshSurface>();
            surface.BuildNavMesh();
        }

        void Update(){

        }

        void SpawnObjects(){
            for(int i = 0; i < 50; i++){
                float xPos = Random.Range(-100, 100);
                float zPos = Random.Range(-100, 100);
                Vector3 spawnPos = new Vector3(xPos, 25f, zPos);
                if(FindCollisions(spawnPos) < 1){
                    Instantiate(myObj, spawnPos, Quaternion.identity);
                }
            }
        }

        private int FindCollisions(Vector3 pos){
            Collider[] hits = Physics.OverlapSphere(pos, 100f);
            hits = hits.Where(c => !Equals(c.gameObject.name, "Plane")).ToArray();
            return hits.Length;
        }
    }
}

