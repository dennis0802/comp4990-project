using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

namespace CombatPhase{
    public class CameraController : MonoBehaviour
    {
        private Transform Target;
        private GameObject player;
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