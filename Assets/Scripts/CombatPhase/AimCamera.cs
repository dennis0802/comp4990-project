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
        /// The aim action
        /// </summary>
        private InputAction weaponSwitch;

        /// <summary>
        /// If the camera has been switched
        /// </summary>
        public static bool IsSwitched = false;
        
        void Start()
        {
            vcam = GetComponent<CinemachineVirtualCamera>();
            aimAction = playerInput.actions["Zoom"];
            weaponSwitch = playerInput.actions["SwitchWeapon"];
        }

        private void Update(){
            // Switching to zoom in while holding a gun
            if(CombatManager.InCombat && CombatManager.GunSelected == 1 && Player.UsingGun && aimAction.triggered){
                if(IsSwitched){
                    IsSwitched = false;
                    CombatManager.ZoomReticle.SetActive(false);
                    CombatManager.NormalReticle.SetActive(true);
                    vcam.Priority -= priorityBoost;
                }
                else{
                    IsSwitched = true;
                    CombatManager.ZoomReticle.SetActive(true);
                    CombatManager.NormalReticle.SetActive(false);
                    vcam.Priority += priorityBoost;
                }
            }

            // Switching weapons while holding a gun
            else if(CombatManager.InCombat && CombatManager.GunSelected == 1 && IsSwitched && weaponSwitch.triggered){
                IsSwitched = false;
                CombatManager.ZoomReticle.SetActive(false);
                CombatManager.NormalReticle.SetActive(true);
                vcam.Priority -= priorityBoost;
                Player.ZoomedIn = false;
            }
        }
    }
}