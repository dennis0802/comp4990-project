using System.Collections;
using System.Collections.Generic;
using UnityEngine.Scripting;
using SQLite.Attribute;

namespace Database{
    public class Save
    {
        [PrimaryKey] public int Id {get; set;}
        public int Distance {get; set;}
        public int Difficulty {get; set;}
        public string CurrentLocation {get; set;}
        public int PhaseNum {get; set;}
        public int Food {get; set;}
        public float Gas {get; set;}
        public int Scrap {get; set;}
        public int Money {get; set;}
        public int Medkit {get; set;}
        public int Tire {get; set;}
        public int Battery {get; set;}
        public int Ammo {get; set;}
        public int CurrentTime {get; set;}
        public int OverallTime {get; set;}
        public int RationMode {get; set;}
        public int PaceMode {get; set;}
    }
}