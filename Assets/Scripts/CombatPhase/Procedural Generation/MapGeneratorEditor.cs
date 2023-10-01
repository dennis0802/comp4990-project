using CombatPhase;
using UnityEditor;
using UnityEngine;

[CustomEditor (typeof(MapGeneratorV3))]
public class MapGeneratorEditor : Editor {
    public override void OnInspectorGUI(){
        MapGeneratorV3 mapGen = (MapGeneratorV3)target;

        if(DrawDefaultInspector()){
            if(mapGen.autoUpdate){
                mapGen.GenerateMap();
            }
        }

        if(GUILayout.Button("Generate")){
            mapGen.GenerateMap();
        }
    }
}