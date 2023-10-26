using UnityEngine;
using System.Collections;


namespace CombatPhase.ProceduralGeneration.Data{
    [CreateAssetMenu()]
    public class UpdatableData : ScriptableObject {

        #if UNITY_EDITOR
            public event System.Action OnValuesUpdated;
            public bool autoUpdate;


            protected virtual void OnValidate(){
                if(autoUpdate){
                    NotifyOfUpdatedValues();
                }
            }

            public void NotifyOfUpdatedValues(){
                if(OnValuesUpdated != null){
                    OnValuesUpdated();
                }
            }
        #endif
    }
}