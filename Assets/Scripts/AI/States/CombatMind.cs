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
            // NOTE: All movement operations need velocity to be checked to avoid weird looking movements from the destination changing constantly
            switch(agent){
                // ---------------------------------------- MUTANT MIND ------------------------------------------------------
                case Mutant mutant:
                    // Mutants are only concerned with attacking the player - find the nearest one
                    Mutant m = agent as Mutant;
                    m.TargetTransform = m.Sense<NearestPartySensor, Transform>();

                    // Add delay to shooting
                    m.shotDelay -= m.shotDelay <= 0.0f ? 0.0f : m.DeltaTime;

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
                        m.LookToTarget(m.TargetTransform);

                        // Detecting a character that is closer than the current target
                        if(altTarget != m.TargetTransform && altTarget != null && Vector3.Distance(m.transform.position, altTarget.position) < Vector3.Distance(m.transform.position, m.TargetTransform.position)){
                            m.TargetTransform = altTarget;
                        }
                        // Attempt physical attack (type 0 and 1) when close enough
                        else if(m.mutantType <= 1 && Vector3.Distance(m.TargetTransform.position, m.transform.position) < 2.0f){
                            Player player = m.TargetTransform.GetComponent<Player>();
                            Teammate partyMember = m.TargetTransform.GetComponent<Teammate>();

                            // Set as target and attack
                            if(player != null){
                                m.TargetTransform = player.transform;
                                player.Damage(m.strength);
                            }
                            else if(partyMember != null){
                                m.TargetTransform = partyMember.transform;
                                partyMember.Damage(m.strength);
                                if(partyMember.hp <= 0){
                                    m.TargetTransform = null;
                                }
                            }
                        }
                        // Attempt a ranged attack (type 2 and 3) when close enough
                        else if(m.mutantType >= 2 && Vector3.Distance(m.TargetTransform.position, m.transform.position) < 15.0f){
                            m.LookToTarget(m.TargetTransform);
                            if(m.shotDelay <= 0){
                                m.Shoot();
                            }
                        }
                        // Pursue the player until close enough
                        else{
                            m.SetDestination(m.TargetTransform.position);
                        }

                        // Losing sight of target
                        if(Vector3.Distance(m.TargetTransform.position, m.transform.position) > m.DetectionRange){
                            m.TargetTransform = null;
                        }
                    }
                    break;

                // ---------------------------------------- TEAMMATE MIND ------------------------------------------------------
                case Teammate teammate:
                    Teammate t = agent as Teammate;
                    Mutant nearestMutant = t.Sense<NearestMutantSensor, Mutant>();
                    DefensivePoint dp = t.Sense<NearestDefenseSensor, DefensivePoint>();

                    // Add delay to attacks
                    t.shotDelay -= t.shotDelay <= 0.0f ? 0.0f : t.DeltaTime;
                    t.reloadDelay -= t.reloadDelay <= 0.0f ? 0.0f : t.DeltaTime;
                    t.physDelay -= t.physDelay <= 0.0f ? 0.0f : t.DeltaTime;

                    // If in no particular danger (enemies not visible), wander for collectibles to collect or purely wander
                    if(nearestMutant == null){
                        Transform nearestCollectible = t.Sense<NearestCollectibleSensor, Transform>();
                        
                        // Wandering means leaving the defensive point
                        t.LeaveDefensivePoint();

                        // Collectible
                        if(nearestCollectible != null && !t.targetingCollectible){
                            t.SetDestination(nearestCollectible.position);
                            t.targetingCollectible = true;
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

                        // Attempt to keep a safe distance away (evade) if low hp OR reloading OR "recharging" a physical attack
                        if((Vector3.Distance(nearestMutant.transform.position, t.transform.position) < 1.0f && t.hp <= 25) || t.reloadDelay > 0.0f || t.physDelay > 0.0f){
                            t.LeaveDefensivePoint();
                            
                            // Calculate the steering behaviour for evasion
                            float mutantSpeed = nearestMutant.GetSpeed();
                            float lookaheadTime = (nearestMutant.transform.position - t.transform.position).magnitude/(t.GetSpeed() + mutantSpeed);
                            Vector3 mutantVelocity = nearestMutant.GetVelocity();

                            Vector3 fleePosition = (t.transform.position - (nearestMutant.transform.position + mutantVelocity) * t.DeltaTime).normalized * t.GetSpeed() 
                                                    - mutantVelocity;

                            t.SetDestination(new Vector3(fleePosition.x, t.transform.position.y, fleePosition.z));
                        }
                        // If the pickup is ammo and low on ammo, move towards it.
                        else if(t.ammoTotal <= 10 && nearestCollectible != null && t.GetComponent<AmmoPickup>() != null){
                            t.LeaveDefensivePoint();
                            t.SetDestination(nearestCollectible.position);
                        }

                        // If not at the defensive point using it, it is not in use.
                        else if(dp != null && t.defensivePointUsed == null){
                            t.LookToTarget(dp.transform);
                            t.SetDestination(dp.transform.position);
                            dp.inUse = true;
                            t.defensivePointUsed = dp;
                        }

                        // Prefer ranged attacks over physical attacks
                        // Range attack
                        else if(t.usingGun && t.reloadDelay <= 0.0f && t.ammoLoaded > 0){
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

                        // Physical attack within range, differs per weapon
                        else if(!t.usingGun && t.physDelay <= 0.0f && Vector3.Distance(nearestMutant.transform.position, t.transform.position) < CombatManager.PhysSelected * 2 && t.hp > 25){
                            t.LookToTarget(nearestMutant.transform);
                            nearestMutant.PhysicalDamage(t.physicalDamageOutput);
                            t.physDelay = CombatManager.PhysSelected == 3 ? 0.5f : CombatManager.PhysSelected == 4 ? 1.0f : 2.0f;
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
