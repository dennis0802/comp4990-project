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
        
        // Start is called before the first frame update
        void Start()
        {
            alert = GetComponent<TextMeshProUGUI>();
        }

        // Update is called once per frame
        void Update()
        {
            // If alert text is in use, keep it displayed for 5 seconds before "wiping"
            if(!Equals(alert.text, "")){
                timer += Time.deltaTime;
                if(timer >= 5.0f){
                    alert.text = "";
                    timer = 0.0f;
                }
            }
        }
    }
}