using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace CombatPhase{
    /// <summary>
    /// Text to alert the player that an ally has perished
    /// </summary> 
    [DisallowMultipleComponent]
    public class AlertText : MonoBehaviour
    {
        /// <summary>
        /// TextMeshPro component to display alert
        /// </summary> 
        private TextMeshProUGUI alert;

        /// <summary>
        /// Time how long the message stays up
        /// </summary> 
        private float timer = 0.0f;

        /// <summary>
        /// Time how long the message stays up
        /// </summary> 
        public static int MaxTime;

        /// <summary>
        /// If the alert played recently
        /// </summary> 
        private bool soundPlayed = false;

        /// <summary>
        /// Audio for alerts
        /// </summary> 
        private AudioSource alertSound;

        // Start is called before the first frame update
        void Start()
        {
            alert = GetComponent<TextMeshProUGUI>();
            alertSound = GetComponent<AudioSource>();
        }

        // Update is called once per frame
        void Update()
        {
            // If alert text is in use, keep it displayed for 3 seconds before "wiping"
            if(!Equals(alert.text, "")){
                if(alert.text.Contains("perished.") && !soundPlayed){
                    soundPlayed = true;
                    alertSound.Play();
                }

                timer += Time.deltaTime;
                if(timer >= MaxTime){
                    alert.text = "";
                    timer = 0.0f;
                    soundPlayed = false;
                }
            }
        }
    }
}