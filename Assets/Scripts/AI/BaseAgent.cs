using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using AI.Sensors;
using CombatPhase;
using System.Linq;
using AI.States;

// Based off of coursework for Dr. Vasighizaker's COMP4770-2023W Artificial Intelligence for Games

namespace AI{
    /// <summary>
    /// Base class for all agents
    /// </summary>
    public abstract class BaseAgent : MonoBehaviour
    {
        public BaseSensor[] Sensors {get; private set;}

        /// <summary>
        /// NavMeshAgent of the agent
        /// </summary>
        [Tooltip("Navmesh agent controlling the agent")]
        [SerializeField]
        private NavMeshAgent NavMeshAgent;

        /// <summary>
        /// Current destination of the agent
        /// </summary>
        private Vector3 TargetDest;

        /// <summary>
        /// Current velocity of the agent
        /// </summary>
        public Vector3 Velocity {get; private set;}

        /// <summary>
        /// Current state of the agent
        /// </summary>
        public BaseState State {get; private set;}

        /// <summary>
        /// Read a sensor and receive given data piece.
        /// </summary>
        /// <typeparam name="TSensor">The sensor type to read.</typeparam>
        /// <typeparam name="TData">The expected data to return.</typeparam>
        /// <returns>The data piece if returned by the given sensor type, default otherwise.</returns>
        public TData Sense<TSensor, TData>() where TSensor : BaseSensor{
            // Get relevant sensors
            foreach(BaseSensor sensor in Sensors){
                if(sensor is not TSensor){
                    continue;
                }

                // If correct type and data returned, return it.
                object data = sensor.Sense();
                if(data is TData correctType){
                    return correctType;
                }
            }

            return default;
        }

        /// <summary>
        /// Setup the agent
        /// </summary>
        public void Setup(){
            CombatManager.AddAgent(this);

            // Find sensors
            List<BaseSensor> sensors = GetComponents<BaseSensor>().ToList();
            sensors.AddRange(GetComponentsInChildren<BaseSensor>());
            Sensors = sensors.Distinct().ToArray();

            foreach(BaseSensor sensor in Sensors){
                sensor.Agent = this;
            }
        }

        /// <summary>
        /// Set the state of the agent
        /// </summary>
        /// <typeparam name="T">The state to put the agent in</typeparam>
        public void SetState<T>() where T : BaseState{
            BaseState value = CombatManager.GetState<T>();

            if(State = value){
                return;
            }

            if(State != null){
                State.Exit(this);
            }

            State = value;

            if(State != null){
                State.Enter(this);
            }
        }

        /// <summary>
        /// Set the state of the agent
        /// </summary>
        /// <param name="dest">The destination to move the agent to</typeparam>
        public void SetDestination(Vector3 dest){
            TargetDest = dest;
            NavMeshAgent.SetDestination(TargetDest);
        }

        protected virtual void Start(){
            Setup();

            if(CombatManager.Mind != null){
                CombatManager.Mind.Enter(this);
            }

            if(State != null){
                State.Enter(this);
            }
        }

        public virtual void Perform(){
            if(CombatManager.Mind != null){
                CombatManager.Mind.Execute(this);
            }
            
            if(State != null){
                State.Execute(this);
            }
        }
    }
}