using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace AI.Sensors{
    public class NearestMutantSensor : BaseSensor
    {
        /// <summary>
        /// Sense the nearest mutant to the agent.
        /// </summary>
        /// <returns>The transform of the nearest mutant or null if none available</returns>
        public override object Sense(){
            Transform[] mutants = FindObjectsOfType<Transform>().Where(t => t.name.Contains("Mutant")).ToArray();

            if(mutants.Length == 0){
                return null;
            }

            return mutants.OrderBy(b => Vector3.Distance(Agent.transform.position, b.transform.position)).First();
        }
    }
}