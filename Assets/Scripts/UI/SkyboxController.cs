using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UI;

namespace UI{
    /// <summary>
    /// Class to control the skybox
    /// </summary>
    public class SkyboxController : MonoBehaviour
    {
        /// <summary>
        /// Skybox material to signify midday
        /// </summary>
        public Material midday;

        /// <summary>
        /// Skybox material to signify morning/evening
        /// </summary>
        public Material dimming; 

        /// <summary>
        /// Skybox material to signify night
        /// </summary>
        public Material night;

        /// <summary>
        /// Skybox component displayed on the camera
        /// </summary>
        private Skybox skybox;

        // Start is called before the first frame update
        void Start()
        {
            skybox = GetComponent<Skybox>();
        }

        // Update is called once per frame
        void Update()
        {
            if(SceneManager.GetActiveScene().buildIndex > 0){
                // Depending on the 24-hour time, change the skybox material
                if(GameLoop.Hour >= 21 || GameLoop.Hour <= 5){
                    skybox.material = night;
                }
                else if(GameLoop.Hour >= 18 || GameLoop.Hour <= 9){
                    skybox.material = dimming;
                }
                else if(GameLoop.Hour >= 10 && GameLoop.Hour <= 17){
                    skybox.material = midday;
                }
            }
            else{
                skybox.material = night;
            }  
        }

        void FixedUpdate(){
            // Update the ambient lighting
            DynamicGI.UpdateEnvironment();
        }
    }
}

