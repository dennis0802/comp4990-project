using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Based off of coursework for Dr. Vasighizaker's COMP4770-2023W Artificial Intelligence for Games

namespace AI.Sensors{
    /// <summary>
    /// Base class for all sensors
    /// </summary>
    public abstract class BaseSensor : MonoBehaviour
    {
        public BaseAgent Agent {get; set;}

        /// <summary>
        /// Implement what will be sent back to the agent
        /// </summary>
        public abstract object Sense();
    }
}