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

                    // Find a target if none set (this will also help mutants stay on one target and not move confused).
                    if(m.TargetTransform == null){
                        m.TargetTransform = m.Sense<NearestPartySensor, Transform>();
                    }

                    // If player cannot be found, wander (pick a random position on the map)
                    if(m.TargetTransform == null){
                        if(m.GetVelocity().magnitude < m.minStopSpeed){
                            Vector2 pos = CombatManager.RandomPosition;
                            m.SetDestination(new Vector3(pos.x, 0, pos.y + 60f));
                        }
                    }

                    // Move towards and attack
                    else{
                        Transform altTarget = m.Sense<NearestPartySensor, Transform>();

                        // Attempt attack when close enough, move closer otherwise
                        if(Vector3.Distance(m.TargetTransform.position, m.transform.position) < 1.0f){
                            Player player = m.TargetTransform.GetComponent<Player>();
                            Teammate partyMember = m.TargetTransform.GetComponent<Teammate>();

                            // Set as target and attack
                            if(player is not null){
                                m.TargetTransform = player.transform;
                                player.Damage(m.strength);
                            }
                            else if(partyMember is not null){
                                m.TargetTransform = partyMember.transform;
                                partyMember.Damage(m.strength);
                                if(partyMember.hp <= 0){
                                    m.TargetTransform = null;
                                }
                            }
                        }
                        // Detecting a character that is closer than the current target
                        else if(altTarget is not null && Vector3.Distance(m.transform.position, altTarget.position) < Vector3.Distance(m.transform.position, m.TargetTransform.position)){
                            m.TargetTransform = altTarget;
                        }
                        // Pursue the player until close enough
                        // NOTE: while there is steering behaviour for pursuit, would require knowing
                        else{
                            m.SetDestination(m.TargetTransform.position);
                        }
                    }

                    break;

                // Teammate mind
                case Teammate teammate:
                    Teammate t = agent as Teammate;
                    Mutant nearestMutant = t.Sense<NearestMutantSensor, Mutant>();
                    DefensivePoint dp = t.Sense<NearestDefenseSensor, DefensivePoint>();

                    // Add delay to shooting
                    t.shotDelay -= t.shotDelay <= 0.0f ? 0.0f : t.DeltaTime;

                    // If in no particular danger (enemies not visible), wander for collectibles to collect or purely wander
                    if(nearestMutant == null){
                        Transform nearestCollectible = t.Sense<NearestCollectibleSensor, Transform>();
                        
                        // Wandering means leaving the defensive point
                        t.LeaveDefensivePoint();

                        // Collectible
                        if(nearestCollectible != null){
                            if(t.GetVelocity().magnitude < t.minStopSpeed){
                                t.SetDestination(nearestCollectible.position);
                            }
                        }

                        // Wander
                        else if(t.GetVelocity().magnitude < t.minStopSpeed){
                            Vector2 pos = CombatManager.RandomPosition;
                            t.SetDestination(new Vector3(pos.x, 0, pos.y + 60f));
                        }
                    }

                    // Find a defensive point not in use and attack if enemies visible
                    else if(nearestMutant != null){
                        // Attempt to check for an ammo pickup
                        Transform nearestCollectible = t.Sense<NearestCollectibleSensor, Transform>();

                        // If the pickup is ammo and low on ammo, move towards it.
                        if(t.ammoTotal <= 10 && nearestCollectible != null && t.GetComponent<AmmoPickup>() != null){
                            if(t.GetVelocity().magnitude < t.minStopSpeed){
                                t.LeaveDefensivePoint();
                                t.SetDestination(nearestCollectible.position);
                            }
                        }

                        // If not at the defensive point using it, it is not in use.
                        else if(dp != null && t.defensivePointUsed == null){
                            t.SetDestination(dp.transform.position);
                            dp.inUse = true;
                            t.defensivePointUsed = dp;
                        }

                        // Attempt to keep a safe distance away (evade) if low hp
                        else if(Vector3.Distance(nearestMutant.transform.position, t.transform.position) < 1.0f && t.hp <= 25){
                            t.LeaveDefensivePoint();
                            
                            // Calculate the steering behaviour for evasion
                            float mutantSpeed = nearestMutant.GetSpeed();
                            float lookaheadTime = (nearestMutant.transform.position - t.transform.position).magnitude/(t.GetSpeed() + mutantSpeed);
                            Vector3 mutantVelocity = nearestMutant.GetVelocity();

                            Vector3 fleePosition = (t.transform.position - (nearestMutant.transform.position + mutantVelocity) * t.DeltaTime).normalized * t.GetSpeed() 
                                                    - mutantVelocity;

                            t.SetDestination(new Vector3(fleePosition.x, t.transform.position.y, fleePosition.z));
                        }

                        // Prefer ranged attacks over physical attacks
                        // Range attack
                        else if(t.usingGun && t.ammoLoaded > 0){
                            t.LookToTarget(nearestMutant.transform);

                            if(t.shotDelay <= 0.0f){
                                t.Shoot();
                            }
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
                            t.LookToTarget(nearestMutant.transform);
                            nearestMutant.PhysicalDamage(t.physicalDamageOutput);
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
