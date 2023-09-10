using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace AI.Sensors{
    public class NearestMemberSensor : BaseSensor
    {
        /// <summary>
        /// Sense the nearest member to the agent (mutant).
        /// </summary>
        /// <returns>The transform of the nearest member or null if none available</returns>
        public override object Sense(){
            Transform[] party = FindObjectsOfType<Transform>().Where(t => t.name.Contains("Teammate") || t.name.Contains("Player")).ToArray();

            if(party.Length == 0){
                return null;
            }

            return party.OrderBy(b => Vector3.Distance(Agent.transform.position, b.transform.position)).First();
        }
    }
}