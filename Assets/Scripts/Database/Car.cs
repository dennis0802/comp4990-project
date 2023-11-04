using System.Collections;
using System.Collections.Generic;
using UnityEngine.Scripting;
using SQLite.Attribute;

namespace Database{
    [UnityEngine.Scripting.Preserve]
    public class Car
    {
        [PrimaryKey] public int Id {get; set;}
        public int CarHP {get; set;}
        public int WheelUpgrade {get; set;}
        public int BatteryUpgrade {get; set;}
        public int EngineUpgrade {get; set;}
        public int ToolUpgrade {get; set;}
        public int MiscUpgrade1 {get; set;}
        public int MiscUpgrade2 {get; set;}
        public int IsBatteryDead {get; set;}
        public int IsTireFlat {get; set;}
    }
}