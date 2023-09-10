using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AI;
using CombatPhase;

namespace AI.States{
    [CreateAssetMenu(menuName = "AI/States/Combat Mind", fileName = "Combat Mind")]
    public class CombatMind : BaseState
    {
        public override void Enter(BaseAgent agent){
            if(agent is Mutant){

            }
            else if(agent is Teammate){

            }
        }

        public override void Execute(BaseAgent agent){
            switch(agent){
                case Mutant mutant:

                    break;
                case Teammate teammate:
                    break;
            }
        }
    }
}
