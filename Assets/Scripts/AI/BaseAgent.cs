using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using AI.Sensors;
using CombatPhase;
using System.Linq;

// Based off of coursework for Dr. Vasighizaker's COMP4770-2023W Artificial Intelligence for Games

namespace AI{
    /// <summary>
    /// Base class for all agents
    /// </summary>
    public abstract class BaseAgent : MonoBehaviour
    {
        public BaseSensor[] Sensors {get; private set;}

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
            // Find sensors
            List<BaseSensor> sensors = GetComponents<BaseSensor>().ToList();
            sensors.AddRange(GetComponentsInChildren<BaseSensor>());
            Sensors = sensors.Distinct().ToArray();
        }

        protected virtual void Start(){
            Setup();
        }
    }
}