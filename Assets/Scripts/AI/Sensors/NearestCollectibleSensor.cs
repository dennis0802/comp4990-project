using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace AI.Sensors{
    public class NearestCollectibleSensor : BaseSensor
    {
        /// <summary>
        /// Sense the nearest collectible to the agent.
        /// </summary>
        /// <returns>The transform of the nearest collectible or null if none available</returns>
        public override object Sense(){
            Transform[] itemSpawnPoints = FindObjectsOfType<Transform>().Where(t => Equals(t.tag, "PickupSpawn")).ToArray();

            if(itemSpawnPoints.Length == 0){
                return null;
            }

            return itemSpawnPoints.OrderBy(b => Vector3.Distance(Agent.transform.position, b.transform.position)).First();
        }
    }
}