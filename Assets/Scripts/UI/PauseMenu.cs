using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UI;

namespace UI{
    [DisallowMultipleComponent]
    public class PauseMenu : MonoBehaviour
    {
        [Tooltip("Pause action as an input action")]
        [SerializeField]
        private InputAction pauseAction;

        [Tooltip("Player input object")]
        [SerializeField]
        private PlayerInput playerInput;

        [Tooltip("Main game UI object")]
        [SerializeField]
        private GameObject mainGameUI;

        [Tooltip("Main menu UI object")]
        [SerializeField]
        private GameObject mainMenuUI;

        [Tooltip("Pause menu UI object")]
        [SerializeField]
        private GameObject pauseMenuUI;

        [Tooltip("Active game UI")]
        [SerializeField]
        private GameObject activeUI;

        [Tooltip("Settings UI")]
        [SerializeField]
        private GameObject settingsUI;
        
        [Tooltip("Pause menu UI object")]
        [SerializeField]
        private AudioSource buttonClick;

        // To track if the game is paused.
        public static bool IsPaused = false;

        void Start(){
            pauseAction = playerInput.actions["Pause"];
        }

        void Update(){
            if(pauseAction.triggered){
                if(IsPaused){
                    Resume();
                }
                else{
                    Pause();
                }
            }
        }

        /// <summary>
        /// Resume the game
        /// </summary>
        public void Resume(){
            buttonClick.Play();
            pauseMenuUI.SetActive(false);
            activeUI.SetActive(true);
            Time.timeScale = 1.0f;
            IsPaused = false;
        }

        /// <summary>
        /// Pause the game
        /// </summary>
        public void Pause(){
            buttonClick.Play();
            pauseMenuUI.SetActive(true);
            activeUI.SetActive(false);
            Time.timeScale = 0.0f;
            IsPaused = true;
        }

        /// <summary>
        /// Load the main menu
        /// </summary>
        public void LoadMenu(){
            IsPaused = false;
            pauseMenuUI.SetActive(false);
            activeUI.SetActive(true);
            Time.timeScale = 1.0f;
            SceneManager.LoadScene(0);
            mainGameUI.SetActive(false);
            mainMenuUI.SetActive(true);
        }

        /// <sumamry>
        /// Load settings
        /// </sumamry>
        public void LoadSettings(){
            pauseMenuUI.SetActive(false);
            settingsUI.SetActive(true);
        }

        /// <summary>
        /// Quit the game
        /// </summary>
        public void QuitGame(){
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                UnityEngine.Application.Quit();
            #endif
        }
    }
}

