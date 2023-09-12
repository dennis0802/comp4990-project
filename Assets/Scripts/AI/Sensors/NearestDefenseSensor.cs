using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace AI.Sensors{
    public class NearestDefenseSensor : BaseSensor
    {
        /// <summary>
        /// Sense the nearest defense to the agent.
        /// </summary>
        /// <returns>The transform of the nearest defense or null if none available</returns>
        public override object Sense(){
            Transform[] defensivePoints = FindObjectsOfType<Transform>().Where(t => Equals(t.tag, "DefensivePoint")).ToArray();

            if(defensivePoints.Length == 0){
                return null;
            }

            return defensivePoints.OrderBy(b => Vector3.Distance(Agent.transform.position, b.transform.position)).First();
        }
    }
}