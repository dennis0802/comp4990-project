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
        private int maxObjects = 50;
        private float yOffset = 12f;
        public GameObject[] obstacleObjects;

        void Start(){
            SpawnObjects();
        }

        /// <summary>
        /// Spawn obstacles
        /// </spawn>
        void SpawnObjects(){
            // Attempt to spawn up to the max possible number of objects
            for(int i = 0; i < maxObjects; i++){
                int objNum = Random.Range(0, obstacleObjects.Length);
                float xPos = Random.Range(-45, 45);
                float zPos = Random.Range(-45, 45);
                float yRot = Random.Range(0, 361);
                Vector3 spawnPos = new Vector3(xPos, yOffset, zPos);
                

                // If no collisions found, spawn the object
                if(FindCollisions(spawnPos) < 1){
                    GameObject obj = Instantiate(obstacleObjects[objNum], spawnPos, Quaternion.identity);
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

