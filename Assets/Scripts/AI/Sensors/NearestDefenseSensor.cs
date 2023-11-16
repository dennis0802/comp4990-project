using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using CombatPhase;

namespace AI.Sensors{
    public class NearestDefenseSensor : BaseSensor
    {
        /// <summary>
        /// Sense the nearest defense to the agent.
        /// </summary>
        /// <returns>The transform of the nearest defense or null if none available</returns>
        public override object Sense(){
            if(Agent is not Teammate teammate){
                return null;
            }

            DefensivePoint[] defensivePoints = FindObjectsOfType<DefensivePoint>().Where(t => Equals(t.tag, "DefensivePoint") 
                                          && Vector3.Distance(t.transform.position, teammate.transform.position) < teammate.DetectionRange).ToArray();
            if(defensivePoints.Length == 0){
                return null;
            }

            return defensivePoints.OrderBy(b => Vector3.Distance(Agent.transform.position, b.transform.position)).First();
        }
    }
}