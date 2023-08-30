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
        [Tooltip("Player input object")]
        [SerializeField]
        private PlayerInput playerInput;

        [Tooltip("Main game UI object")]
        [SerializeField]
        private GameObject mainGameUI;

        [Tooltip("Main rest UI object")]
        [SerializeField]
        private GameObject mainRestUI;

        [Tooltip("Main menu UI object")]
        [SerializeField]
        private GameObject mainMenuUI;

        [Tooltip("Main menu script")]
        [SerializeField]
        private MainMenu mainMenu;

        [Tooltip("Pause menu UI object")]
        [SerializeField]
        private GameObject pauseMenuUI;

        [Tooltip("Active game UI")]
        [SerializeField]
        private GameObject activeUI;

        [Tooltip("Settings UI")]
        [SerializeField]
        private GameObject settingsUI;
        
        [Tooltip("Button click audio")]
        [SerializeField]
        private AudioSource buttonClick;

        [Tooltip("Background used throughout the screens")]
        [SerializeField]
        private GameObject backgroundPanel;

        // To track if the game is paused.
        public static bool IsPaused = false;
        private InputAction pauseAction;

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
            mainMenu.SetFileDesc();
            backgroundPanel.SetActive(true);
            GameLoop.FileId = -1;
            Time.timeScale = 1.0f;
            SceneManager.LoadScene(0);
            mainGameUI.SetActive(false);
            mainRestUI.SetActive(false);
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

