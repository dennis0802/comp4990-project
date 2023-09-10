using System.Collections;
using System.Collections.Generic;
using AI;
using UnityEngine;
using CombatPhase;

namespace AI.States{
    [CreateAssetMenu(menuName = "AI/States/Mutant Mind", fileName = "Mutant Mind")]
    public class MutantMind : BaseState
    {
        public override void Execute(BaseAgent agent){
            Mutant m = agent as Mutant;
            Debug.Log("test");
            if(m.Velocity.magnitude > 0.1f){
                agent.SetDestination(new Vector3(0f, agent.transform.position.y, 0f));
            }
        }
    }
}

