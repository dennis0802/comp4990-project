using UnityEditor;
using UnityEngine;
using System.Collections;

#if UNITY_EDITOR
    namespace CombatPhase.ProceduralGeneration.Data{

        [CustomEditor(typeof(UpdatableData), true)]
        public class UpdatableDataEditor : Editor {
            public override void OnInspectorGUI(){
                base.OnInspectorGUI();
                UpdatableData data = (UpdatableData)target;

                if(GUILayout.Button("Update")){
                    data.NotifyOfUpdatedValues();
                }
            }
        }
    }
#endif