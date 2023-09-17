using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Cinemachine;

namespace CombatPhase{
    public class AimCamera : MonoBehaviour
    {
        [Tooltip("Player input to trigger aiming")]
        [SerializeField]
        private PlayerInput playerInput;

        /// <summary>
        /// Amount to adjust the camera priority
        /// </summary>
        private int priorityBoost = 10;

        /// <summary>
        /// Virtual camera on the object
        /// </summary>
        private CinemachineVirtualCamera vcam;

        /// <summary>
        /// The aim action
        /// </summary>
        private InputAction aimAction;

        /// <summary>
        /// If the camera has been switched
        /// </summary>
        private bool isSwitched = false;
        
        void Start()
        {
            vcam = GetComponent<CinemachineVirtualCamera>();
            aimAction = playerInput.actions["Zoom"];
        }

        private void Update(){
            if(CombatManager.InCombat && CombatManager.GunSelected == 1 && Player.UsingGun && aimAction.triggered){
                if(isSwitched){
                    isSwitched = false;
                    CombatManager.ZoomReticle.SetActive(false);
                    CombatManager.NormalReticle.SetActive(true);
                    vcam.Priority -= priorityBoost;
                }
                else{
                    isSwitched = true;
                    CombatManager.ZoomReticle.SetActive(true);
                    CombatManager.NormalReticle.SetActive(false);
                    vcam.Priority += priorityBoost;
                }
            }
        }
    }
}