using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Collections;

namespace UI{
    [DisallowMultipleComponent]
    public class BackgroundAudio : MonoBehaviour{
        [Tooltip("Audio sources for bgm")]
        [SerializeField]
        private List<AudioClip> bgms;

        private static List<AudioClip> BGMs;

        private int currentScene = -1, sceneRead, prevScene;
        private static bool isPlaying;
        public AudioSource currentlyPlaying;
        public static BackgroundAudio instance;

        void Start(){
            if(instance == null){
                instance = this;
            }
            else{
                Destroy(gameObject);
            }
            BGMs = bgms;
        }

        void Update(){
            sceneRead = SceneManager.GetActiveScene().buildIndex;
            if(currentScene != sceneRead){
                prevScene = currentScene;
                currentScene = sceneRead;
                isPlaying = false;
                
                if(currentScene == 0 && !isPlaying){
                    LoadAudio(0);
                }

                else if(currentScene <= 2 && !isPlaying){
                    // Resting and travelling use the same audio, ignore if going from between the 2
                    if((prevScene == 1 && currentScene == 2) || (prevScene == 2 && currentScene == 1)){
                        return;
                    }
                    LoadAudio(1);
                }   

                // Audio start will be handled by an external function via a button press
                else if(currentScene == 3 && !isPlaying){
                    LoadAudio(2);
                }
            }
        }

        /// <summary>
        /// Load in an audio clip and start playing
        /// </summary>
        /// <param name="index">Index of the clip in the bgm list</param>
        void LoadAudio(int index){
            currentlyPlaying.Stop();
            currentlyPlaying.clip = BGMs[index];
            currentlyPlaying.loop = true;
            currentlyPlaying.volume = index == 2 ? 0.25f : 0.5f;
            currentlyPlaying.Play();
            isPlaying = true;
        }
    }
}
