using System.Collections;
using System.Collections.Generic;
using UnityEngine.Scripting;
using SQLite.Attribute;

namespace Database{
    public class TownEntity
    {
        [PrimaryKey] public int Id {get; set;}
        public int FoodPrice {get; set;}
        public int GasPrice {get; set;}
        public int ScrapPrice {get; set;}
        public int MedkitPrice {get; set;}
        public int TirePrice {get; set;}
        public int BatteryPrice {get; set;}
        public int AmmoPrice {get; set;}
        public int FoodStock {get; set;}
        public int GasStock {get; set;}
        public int ScrapStock {get; set;}
        public int MedkitStock {get; set;}
        public int TireStock {get; set;}
        public int BatteryStock {get; set;}
        public int AmmoStock {get; set;}
        public int Side1Reward {get; set;}
        public int Side1Qty {get; set;}
        public int Side1Diff {get; set;}
        public int Side1Type {get; set;}
        public int Side2Reward {get; set;}
        public int Side2Qty {get; set;}
        public int Side2Diff {get; set;}
        public int Side2Type {get; set;}
        public int Side3Reward {get; set;}
        public int Side3Qty {get; set;}
        public int Side3Diff {get; set;}
        public int Side3Type {get; set;}
        public int CurTown {get; set;}
        public int PrevTown {get; set;}
        public int NextDistanceAway {get; set;}
        public string NextTownName {get; set;}
    }
}