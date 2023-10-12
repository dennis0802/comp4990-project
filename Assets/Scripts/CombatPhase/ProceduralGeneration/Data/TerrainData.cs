using UnityEngine;
using System.Collections;

namespace CombatPhase.ProceduralGeneration.Data{
    [CreateAssetMenu()]
    public class TerrainData : UpdatableData {
        
        public float uniformScale = 1.0f;
        public bool useFlatShading;
        public float meshHeightMultiplier;
        public AnimationCurve meshHeightCurve;
    }
}