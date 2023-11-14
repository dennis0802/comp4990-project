using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Unity.AI.Navigation;
using CombatPhase.ProceduralGeneration;

namespace CombatPhase.ProceduralGeneration{
    public class ObstacleGenerator : MonoBehaviour
    {
        private NavMeshSurface surface;
        private int maxObjects = 500, maxBarricades = 6;
        private float yOffset = 12f;
        public GameObject[] obstacleObjects;
        private GameObject environment;

        void Start(){
            environment = GameObject.FindWithTag("CombatEnvironment");
            SpawnObjects();
        }

        /// <summary>
        /// Spawn obstacles
        /// </spawn>
        void SpawnObjects(){
            int barricadesSpawned = 0;

            // Attempt to spawn up to the max possible number of objects
            for(int i = 0; i < maxObjects; i++){
                int objNum = Random.Range(0, obstacleObjects.Length);

                /// Limit the amount of barricades that can be spawned
                if(objNum == 2 && barricadesSpawned <= maxBarricades){
                    barricadesSpawned++;
                }
                else{
                    while(objNum == 2){
                        objNum = Random.Range(0, obstacleObjects.Length);
                    }
                }

                // Special case are barricades (elem 2) - these stay in the "center"
                float xPos = objNum == 2 ? Random.Range(-45, 45) : Random.Range(-110, 110);
                float zPos = objNum == 2 ? Random.Range(-45, 45) : Random.Range(-110, 110);
                float yRot = Random.Range(0, 361);
                Vector3 spawnPos = new Vector3(xPos, yOffset, zPos);


                // If no collisions found, spawn the object
                if(FindCollisions(spawnPos) < 1){
                    GameObject obj = Instantiate(obstacleObjects[objNum], spawnPos, Quaternion.identity, environment.transform);
                    obj.transform.rotation = Quaternion.Euler(0, yRot, 0);
                }
            }
        }

        /// <summary>
        /// Find collisions around a given position
        /// <summary>
        /// <param name="pos">The position to check for collisions</param>
        /// <returns>The number of collisions found</returns>
        private int FindCollisions(Vector3 pos){
            // Given a 13 unit-radius sphere around the point, check for collisions with objects that aren't the ground
            Collider[] hits = Physics.OverlapSphere(pos, yOffset);
            hits = hits.Where(c => !Equals(c.gameObject.name, "Terrain Chunk")).ToArray();
            return hits.Length;
        }
    }
}

