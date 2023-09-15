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
            // NOTES:
            // All movement operations need velocity to be checked to avoid weird looking movements from the destination changing
            switch(agent){
                // Mutant mind
                case Mutant mutant:
                    // Mutants are only concerned with attacking the player - find the nearest one
                    Mutant m = agent as Mutant;
                    Transform nearestChar = m.Sense<NearestPartySensor, Transform>();

                    float time = 0.0f;
                    time += m.DeltaTime;
                    Debug.Log(name + ": " + time);

                    // If player cannot be found, wander (pick a random position on the map)
                    if(nearestChar == null){
                        if(m.Velocity.magnitude < m.minStopSpeed){
                            Vector2 pos = CombatManager.RandomPosition;
                            m.SetDestination(new Vector3(pos.x, 0, pos.y));
                        }
                    }

                    // Move towards and attack
                    else{
                        // Attack when close enough, move closer otherwise
                        if(Vector3.Distance(nearestChar.position, m.transform.position) < 1.0f){
                            Player player = nearestChar.GetComponent<Player>();
                            Teammate partyMember = nearestChar.GetComponent<Teammate>();

                            if(player != null){
                                player.ReceiveDamage(1);
                            }
                            else if(partyMember != null){
                                partyMember.ReceiveDamage(1);
                            }
                        }
                        else{
                            if(m.Velocity.magnitude < m.minStopSpeed){
                                m.SetDestination(nearestChar.position);
                            }
                        }
                    }

                    break;

                // Teammate mind
                case Teammate teammate:
                    Teammate t = agent as Teammate;
                    Mutant nearestMutant = t.Sense<NearestMutantSensor, Mutant>();
                    DefensivePoint dp = t.Sense<NearestDefenseSensor, DefensivePoint>();

                    // If in no particular danger (enemies not visible), wander for collectibles to collect or purely wander
                    if(nearestMutant == null){
                        Transform nearestCollectible = t.Sense<NearestCollectibleSensor, Transform>();

                        if(nearestCollectible != null){
                            if(t.Velocity.magnitude < t.minStopSpeed){
                                t.SetDestination(nearestCollectible.position);
                            }
                        }
                        else if(t.Velocity.magnitude < t.minStopSpeed){
                            Vector2 pos = CombatManager.RandomPosition;
                            t.SetDestination(new Vector3(pos.x, 0, pos.y));
                        }
                    }

                    // Find a defensive point not in use and attack if enemies visible
                    else if(dp != null){
                        
                        // If not at the defensive point using it, it is not in use.
                        if(Equals(t.GetDestination(), dp.transform.position) && !dp.inUse){
                            dp.inUse = true;
                            if(t.Velocity.magnitude < t.minStopSpeed){
                                t.SetDestination(dp.transform.position);
                            }
                        }
                        else{
                            dp.inUse = false;
                        }

                        // Range attack
                        if(t.usingGun && t.ammoLoaded > 0){
                            Debug.Log(t.name + " within shooting range");
                        }
                        // Physical attack within range
                        else if(Vector3.Distance(nearestMutant.transform.position, t.transform.position) < 1.0f){
                            Debug.Log(t.name + " within physical range");
                            nearestMutant.ReceiveDamage(1);
                        }
                        // Attempt to keep a safe distance away (mutant velocity should be faster than party members) [evasion steering]
                        else if(Vector3.Distance(nearestMutant.transform.position, t.transform.position) >= 1.0f){
                            Debug.Log(t.name + " running away");
                        }

                        if(t.ammoLoaded == 0 && t.ammoTotal > 0){
                            t.Reload();
                        }
                        // Determine if weapon needs to be switched (change model as well)
                        else if(t.ammoTotal == 0 && t.ammoLoaded == 0 && t.usingGun){
                            t.usingGun = false;
                            t.UpdateModel();
                        }
                        else if(!t.usingGun && t.ammoTotal != 0 && t.ammoLoaded == 0){
                            t.Reload();
                            t.usingGun = true;
                            t.UpdateModel();
                        }
                    }

                    // Mutants nearby and no defence, wander
                    else if(t.Velocity.magnitude < t.minStopSpeed){
                        Vector2 pos = CombatManager.RandomPosition;
                        t.SetDestination(new Vector3(pos.x, 0, pos.y));
                    }

                    break;
            }
        }

        public override void Exit(BaseAgent agent){}
    }
}
