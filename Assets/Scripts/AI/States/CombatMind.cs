using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AI;
using CombatPhase;
using AI.Sensors;

namespace AI.States{
    [CreateAssetMenu(menuName = "AI/States/Combat Mind", fileName = "Combat Mind")]
    public class CombatMind : BaseState
    {
        public override void Enter(BaseAgent agent){}

        public override void Execute(BaseAgent agent){
            switch(agent){
                // Mutant mind
                case Mutant mutant:
                    // Mutants are only concerned with attacking the player - find the nearest one
                    Mutant m = agent as Mutant;
                    Transform nearestChar = m.Sense<NearestMemberSensor, Transform>();
                    m.SetDestination(nearestChar.position);

                    // If player cannot be found, wander

                    // Attack

                    break;

                // Teammate mind
                case Teammate teammate:
                    Teammate t = agent as Teammate;
                    // If in no particular danger (enemies not visible), wander for collectibles to collect

                    // Find a defensive point and attack if enemies visible

                    break;
            }
        }

        public override void Exit(BaseAgent agent){}
    }
}
