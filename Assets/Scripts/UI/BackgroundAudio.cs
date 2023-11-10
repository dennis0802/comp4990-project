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
        private int sceneRead;
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

            if(GameLoop.FileId == -1 && currentlyPlaying.clip != BGMs[0]){
                LoadAudio(0);
            }
            else if(sceneRead == 0 && GameLoop.FileId > -1 && currentlyPlaying.clip != BGMs[1]){
                LoadAudio(1);
            }
            else if(sceneRead == 1 && currentlyPlaying.clip != BGMs[2]){
                LoadAudio(2);
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
        }
    }
}
