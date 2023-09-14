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
                    Transform nearestChar = m.Sense<NearestPartySensor, Transform>();

                    // If player cannot be found, wander (pick a random position on the map)
                    if(nearestChar == null){

                    }

                    // Move towards and attack
                    else{
                        m.SetDestination(nearestChar.position);
                        // Attacking behaviour here
                    }

                    break;

                // Teammate mind
                case Teammate teammate:
                    Teammate t = agent as Teammate;
                    Mutant nearestMutant = t.Sense<NearestMutantSensor, Mutant>();
                    DefensivePoint dp = t.Sense<NearestDefenseSensor, DefensivePoint>();

                    // If not at the defensive point, it is not in use.
                    /*if(dp != null && !Equals(t.GetDestination(), dp.transform.position)){
                        dp.inUse = false;
                    }

                    // If in no particular danger (enemies not visible), wander for collectibles to collect or purely wander
                    if(nearestMutant == null){
                        Transform nearestCollectible = t.Sense<NearestCollectibleSensor, Transform>();

                        if(nearestCollectible != null){
                            t.SetDestination(nearestCollectible.position);
                        }
                    }

                    // Find a defensive point not in use and attack if enemies visible
                    else if(dp != null){
                        if(!dp.inUse){
                            dp.inUse = true;
                            t.SetDestination(dp.transform.position);
                        }

                        // Range attack
                        if(t.usingGun && t.ammo > 0){

                        }
                        // Physical attack within range
                        else if(Vector3.Distance(nearestMutant.transform.position, t.transform.position) < 1.0f){

                        }
                        // Attempt to keep a safe distance away (mutant velocity should be faster than party members) [evasion steering]
                        else if(Vector3.Distance(nearestMutant.transform.position, t.transform.position) >= 1.0f){

                        }


                        // Determine if weapon needs to be switched (change model as well)
                        if(t.ammo == 0 && t.usingGun){
                            t.usingGun = false;
                        }
                        else if(!t.usingGun && t.ammo != 0){
                            t.usingGun = true;
                        }
                    }

                    // Wander if nothing can be found (random position on the map)
                    else{
                        Vector2 pos = CombatManager.RandomPosition;
                        t.SetDestination(new Vector3(pos.x, 0, pos.y));
                    }*/
                    if(t.CanSetMove()){
                        Vector2 pos = CombatManager.RandomPosition;
                        t.SetDestination(new Vector3(pos.x, 0, pos.y));
                    }
                    break;
            }
        }

        public override void Exit(BaseAgent agent){}
    }
}
