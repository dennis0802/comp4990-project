using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

namespace CombatPhase{
    public class CameraController : MonoBehaviour
    {
        /// <summary>
        /// The target to look at
        /// </summary> 
        private Transform Target;

        /// <summary>
        /// The player to look at
        /// </summary> 
        private GameObject player;

        /// <summary>
        /// The virtual camera in use
        /// </summary> 
        private CinemachineVirtualCamera vcam;

        // Start is called before the first frame update
        void Start()
        {
            vcam = GetComponent<CinemachineVirtualCamera>();
        }

        // Look for a target
        void LateUpdate()
        {
            if(player == null){
                player = GameObject.FindWithTag("Player");
                if(player != null){
                    Target = player.transform;
                    vcam.LookAt = Target;
                    vcam.Follow = Target;
                }
            }
        }
    }
}