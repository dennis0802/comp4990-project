using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UI;

namespace UI{
    [DisallowMultipleComponent]
    public class Settings : MonoBehaviour
    {
        [Tooltip("Text object for description")]
        [SerializeField]
        private TextMeshProUGUI muteDesc, resText, windowText;

        [Tooltip("Slider to control volume")]
        [SerializeField]
        private Slider volumeSlider;

        [Tooltip("Options UI object")]
        [SerializeField]
        private GameObject optionsUI;

        [Tooltip("Pause UI object")]
        [SerializeField]
        private GameObject pauseUI;

        [Tooltip("Main Menu UI object")]
        [SerializeField]
        private GameObject mainMenuUI;

        // Flag variables for muting and full screen
        private bool isMuted, isFullScreen;

        void Start()
        {
            // Load based on previous player preferences
            isMuted = PlayerPrefs.GetInt("IsMuted") == 1;
            isFullScreen = PlayerPrefs.GetInt("IsFullScreen") == 1;
            muteDesc.text = isMuted ? "Audio is muted" : "Audio is not muted";
            SetResolutionText();

            AudioListener.pause = isMuted;
            if(!PlayerPrefs.HasKey("Volume")){
                PlayerPrefs.SetFloat("Volume", 1);
                volumeSlider.value = PlayerPrefs.GetFloat("Volume");
            }
            else{
                volumeSlider.value = PlayerPrefs.GetFloat("Volume");
            }
            windowText.text = isFullScreen ? "Toggle: Full-screen" : "Toggle: Windowed";
        }

        /// <summary>
        /// Mute game audio
        /// </summary>
        public void Mute(){
            isMuted = !isMuted;
            AudioListener.pause = isMuted;
            PlayerPrefs.SetInt("IsMuted", isMuted ? 1 : 0);
            muteDesc.text = isMuted ? "Audio is muted" : "Audio is not muted";
        }

        /// <sumamry>
        /// Change the volume of the game
        /// </summary>
        public void ChangeVolume(){
            AudioListener.volume = volumeSlider.value;
            PlayerPrefs.SetFloat("Volume", volumeSlider.value);
        }

        /// <summary>
        /// Change the window mode between fullscreen and windowed
        /// </summary>
        public void ChangeWindowMode(){
            #if UNITY_EDITOR
                Debug.Log("This setting is only visible in build versions.");
            #endif

            isFullScreen = !isFullScreen;
            switch(PlayerPrefs.GetInt("Resolution")){
                case 0:
                    Screen.SetResolution(1920, 1080, isFullScreen);
                    break;
                case 1:
                    Screen.SetResolution(1080, 960, isFullScreen);
                    break;
                case 2:
                    Screen.SetResolution(640, 480, isFullScreen);
                    break;
                default:
                    return;
            }
            PlayerPrefs.SetInt("IsFullScreen", isFullScreen ? 1 : 0);
            Vector3 pos = windowText.transform.localPosition;
            windowText.text = isFullScreen ? "Toggle: Full-screen" : "Toggle: Windowed";
        }

        /// <sumamry>
        /// Change the resolution of the window (size)
        /// </summary>
        public void ChangeResolution(int flag){
            #if UNITY_EDITOR
                Debug.Log("This setting is only visible in build versions.");
            #endif

            // 0 is high, 1 is medium, 2 is low
            switch(flag){
                case 2:
                    Screen.SetResolution(1920, 1080, PlayerPrefs.GetInt("IsFullScreen") == 1);
                    PlayerPrefs.SetInt("Resolution", flag);
                    break;
                case 1:
                    Screen.SetResolution(1080, 960, PlayerPrefs.GetInt("IsFullScreen") == 1);
                    PlayerPrefs.SetInt("Resolution", flag);
                    break;
                case 0:
                    Screen.SetResolution(640, 480, PlayerPrefs.GetInt("IsFullScreen") == 1);
                    PlayerPrefs.SetInt("Resolution", flag);
                    break;
                default:
                    return;
            }
            SetResolutionText();
        }

        /// <sumamry>
        /// Exit the settings menu
        /// </sumamry>
        public void ExitSettings(){
            if(PauseMenu.IsPaused){
                pauseUI.SetActive(true);
            }
            else{
                mainMenuUI.SetActive(true);
            }
            optionsUI.SetActive(false);
        }

        /// <sumamry>
        /// Change the text of the selected resolution
        /// </sumamry>
        private void SetResolutionText(){
            Vector3 pos = resText.transform.localPosition;
            switch(PlayerPrefs.GetInt("Resolution")){
                case 0:
                    resText.transform.localPosition = new Vector3(pos.x, 51f, pos.z);
                    break;
                case 1:
                    resText.transform.localPosition = new Vector3(pos.x, 0f, pos.z);
                    break;
                case 2:
                    resText.transform.localPosition = new Vector3(pos.x, -51f, pos.z);
                    break;
                default:
                    return;
            }
        }
    }
}

