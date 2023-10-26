using CombatPhase;
using UnityEditor;
using UnityEngine;
using CombatPhase.ProceduralGeneration;

#if UNITY_EDITOR
    namespace CombatPhase.ProceduralGeneration {
        [CustomEditor (typeof(MapGenerator))]
        public class MapGeneratorEditor : Editor {
            public override void OnInspectorGUI(){
                MapGenerator mapGen = (MapGenerator)target;

                if(DrawDefaultInspector()){
                    if(mapGen.autoUpdate){
                        mapGen.DrawMapInEditor();
                    }
                }

                if(GUILayout.Button("Generate")){
                    mapGen.DrawMapInEditor();
                }
            }
        }
    }
#endif