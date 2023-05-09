using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UI
{
    [DisallowMultipleComponent]
    public class MainMenu : MonoBehaviour {
        [SerializeField]
        [Tooltip("The sound that is played when a button is clicked.")]
        private AudioSource buttonClick;

        // Start is called before the first frame update
        void Start()
        {
            
        }

        // Update is called once per frame
        void Update()
        {
            
        }

        public void ExitGame(){
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                UnityEngine.Application.Quit();
            #endif
        }
    }
}

