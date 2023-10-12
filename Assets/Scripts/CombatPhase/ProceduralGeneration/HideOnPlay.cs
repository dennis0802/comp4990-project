using UnityEngine;
using System.Collections;

namespace CombatPhase.ProceduralGeneration {
    public class HideOnPlay : MonoBehaviour{
        void Start(){
            gameObject.SetActive(false);
        }
    }
}