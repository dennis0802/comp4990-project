using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AI;
using CombatPhase;
using AI.Sensors;
using CombatPhase.Pickups;

namespace AI.States{
    [CreateAssetMenu(menuName = "AI/States/Combat Mind", fileName = "Combat Mind")]
    public class CombatMind : BaseState
    {
        /// <summary>
        /// When agent first enters the state
        /// </summary> 
        public override void Enter(BaseAgent agent){}

        /// <summary>
        /// When agent executes the state
        /// </summary> 
        public override void Execute(BaseAgent agent){
            // NOTES:
            // All movement operations need velocity to be checked to avoid weird looking movements from the destination changing
            switch(agent){
                // Mutant mind
                case Mutant mutant:
                    // Mutants are only concerned with attacking the player - find the nearest one
                    Mutant m = agent as Mutant;
                    Transform nearestChar = m.Sense<NearestPartySensor, Transform>();

                    // If player cannot be found, wander (pick a random position on the map)
                    if(nearestChar == null){
                        if(m.GetVelocity().magnitude < m.minStopSpeed){
                            Vector2 pos = CombatManager.RandomPosition;
                            m.SetDestination(new Vector3(pos.x, 0, pos.y));
                        }
                    }

                    // Move towards and attack
                    else{
                        // Attempt attack when close enough, move closer otherwise
                        if(Vector3.Distance(nearestChar.position, m.transform.position) < 1.0f){
                            m.StopMoving();
                            Player player = null;
                            Teammate partyMember = null;

                            if(m.Target is null){
                                player = nearestChar.GetComponent<Player>();
                                partyMember = nearestChar.GetComponent<Teammate>();
                            }

                            if(player != null){
                                m.Target = player.transform;
                                player.Damage(m.strength);
                            }
                            else if(partyMember != null){
                                m.Target = partyMember.transform;
                                partyMember.Damage(m.strength);
                            }
                            else{
                                m.Target = null;
                            }
                        }
                        else{
                            m.SetDestination(nearestChar.position);
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

                        // Collectible
                        if(nearestCollectible != null){
                            if(t.GetVelocity().magnitude < t.minStopSpeed){
                                t.SetDestination(nearestCollectible.position);
                            }
                        }

                        // Wander
                        else if(t.GetVelocity().magnitude < t.minStopSpeed){
                            Vector2 pos = CombatManager.RandomPosition;
                            t.SetDestination(new Vector3(pos.x, 0, pos.y));
                        }
                    }

                    // Find a defensive point not in use and attack if enemies visible
                    else if(nearestMutant != null){
                        // Attempt to check for an ammo pickup
                        Transform nearestCollectible = t.Sense<NearestCollectibleSensor, Transform>();

                        // If the pickup is ammo and low on ammo, move towards it.
                        if(t.ammoTotal <= 10 && nearestCollectible != null && t.GetComponent<AmmoPickup>() != null){
                            if(t.GetVelocity().magnitude < t.minStopSpeed){
                                t.SetDestination(nearestCollectible.position);
                            }
                        }

                        // If not at the defensive point using it, it is not in use.
                        else if(dp != null){
                            if(Equals(t.GetDestination(), dp.transform.position) && !dp.inUse){
                                t.StopMoving();
                                dp.inUse = true;
                                if(t.GetVelocity().magnitude < t.minStopSpeed){
                                    t.SetDestination(dp.transform.position);
                                }
                            }
                            else if(!Equals(t.GetDestination(), dp.transform.position) && dp.inUse){
                                dp.inUse = false;
                            }
                        }

                        // Prefer ranged attacks over physical attacks
                        // Range attack
                        else if(t.usingGun && t.ammoLoaded > 0){
                            Vector3.RotateTowards(t.transform.position, nearestMutant.transform.position, 1.0f * Time.deltaTime, 0.0f);
                            t.Shoot();
                            Debug.Log(t.name + " within shooting range");
                        }
                        // Reload upon finding ammo
                        else if(!t.usingGun && t.ammoTotal != 0 && t.ammoLoaded == 0){
                            t.Reload();
                            t.usingGun = true;
                            t.UpdateModel();
                        }
                        // Reload
                        else if(t.usingGun && t.ammoLoaded == 0 && t.ammoTotal > 0){
                            t.Reload();
                        }
                        // Out of ammo, switch to physical weapon
                        else if(t.usingGun && t.ammoTotal == 0 && t.ammoLoaded == 0){
                            t.usingGun = false;
                            t.UpdateModel();
                        }
                        // Physical attack within range
                        else if(!t.usingGun && Vector3.Distance(nearestMutant.transform.position, t.transform.position) < 1.0f && t.hp > 25){
                            Debug.Log(t.name + " within physical range");
                            nearestMutant.PhysicalDamage(t.physicalDamageOutput);
                        }
                        // Attempt to keep a safe distance away if low hp
                        else if(!t.usingGun && Vector3.Distance(nearestMutant.transform.position, t.transform.position) < 1.0f && t.hp <= 25){
                            Debug.Log(t.name + " running away");
                        }
                    }
                    break;
            }
        }

        /// <summary>
        /// When agent exits the state
        /// </summary> 
        public override void Exit(BaseAgent agent){}
    }
}
