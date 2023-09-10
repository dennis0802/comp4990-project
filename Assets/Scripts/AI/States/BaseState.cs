using System.Collections;
using System.Collections.Generic;
using AI;
using UnityEngine;

namespace AI.States{
public abstract class BaseState : ScriptableObject
    {
        /// <summary>
        /// Called when agent enters this state
        /// </summary>
        /// <param name="agent">The agent.</param>
        public virtual void Enter(BaseAgent agent){}

        /// <summary>
        /// Called when agent is in this state
        /// </summary>
        /// <param name="agent">The agent.</param>
        public virtual void Execute(BaseAgent agent){}

        /// <summary>
        /// Called when agent exits this state
        /// </summary>
        /// <param name="agent">The agent.</param>
        public virtual void Exit(BaseAgent agent){}
    }
}