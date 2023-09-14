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
            if(Agent is not Teammate teammate){
                return null;
            }

            Mutant[] mutants = FindObjectsOfType<Mutant>().Where(t => t.name.Contains("Mutant") 
                                  && Vector3.Distance(t.transform.position, teammate.transform.position) < teammate.DetectionRange).ToArray();

            if(mutants.Length == 0){
                return null;
            }

            return mutants.OrderBy(b => Vector3.Distance(Agent.transform.position, b.transform.position)).First();
        }
    }
}