using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UI;

namespace TravelPhase{
    public class Town{
        // To track resources to generate
        List<Mission> missions = new List<Mission>();
        private int foodStock, foodPrice, gasStock, gasPrice, scrapStock, scrapPrice, medkitStock, medkitPrice, tireStock, tirePrice, batteryStock, batteryPrice,
                    ammoStock, ammoPrice;

        /// <summary>
        /// Create a new town instance.
        /// </summary>
        public Town(){
            // Generate town shop price and stock
            foodPrice = Random.Range(4,9);
            gasPrice = Random.Range(10,16);
            scrapPrice = Random.Range(5,15);
            medkitPrice = Random.Range(16,30);
            tirePrice = Random.Range(20,30);
            batteryPrice = Random.Range(25,40); 
            ammoPrice = Random.Range(15,30);
            foodStock = GameLoop.RoundTo10(100, 301);
            gasStock = Random.Range(6,15);
            scrapStock = Random.Range(10,20);
            medkitStock = Random.Range(1, 4);
            tireStock = Random.Range(1, 4);
            batteryStock = Random.Range(1, 4);
            ammoStock = GameLoop.RoundTo10(50, 151);

            // Generate the missions in towns
            for(int i = 0; i < 3; i++){
                missions.Add(new Mission());
            }
        }

        /// <summary>
        /// Sum up the town's resources, used to determine the amount of resources when displaying a destination to pick
        /// </summary>
        /// <returns>The amount of resources in the town</returns>
        public int SumTownResources(){
            return foodStock + gasStock + scrapStock + medkitStock + tireStock + batteryStock + ammoStock;
        }

        /// <summary>
        /// Get food stock in town
        /// </summary>
        /// <returns>The amount in stock</returns>
        public int GetFoodStock(){
            return foodStock;
        }

        /// <summary>
        /// Get gas stock in town
        /// </summary>
        /// <returns>The amount in stock</returns>
        public int GetGasStock(){
            return gasStock;            
        }

        /// <summary>
        /// Get scrap stock in town
        /// </summary>
        /// <returns>The amount in stock</returns>
        public int GetScrapStock(){
            return scrapStock;            
        }

        /// <summary>
        /// Get medkit stock in town
        /// </summary>
        /// <returns>The amount in stock</returns>
        public int GetMedkitStock(){
            return medkitStock;            
        }

        /// <summary>
        /// Get tire stock in town
        /// </summary>
        /// <returns>The amount in stock</returns>
        public int GetTireStock(){
            return tireStock;           
        }

        /// <summary>
        /// Get battery stock in town
        /// </summary>
        /// <returns>The amount in stock</returns>
        public int GetBatteryStock(){
            return batteryStock;            
        }

        /// <summary>
        /// Get ammo stock in town
        /// </summary>
        /// <returns>The amount in stock</returns>
        public int GetAmmoStock(){
            return ammoStock;            
        }

        /// <summary>
        /// Get food Price in town
        /// </summary>
        /// <returns>The amount in Price</returns>
        public int GetFoodPrice(){
            return foodPrice;
        }

        /// <summary>
        /// Get gas Price in town
        /// </summary>
        /// <returns>The amount in Price</returns>
        public int GetGasPrice(){
            return gasPrice;            
        }

        /// <summary>
        /// Get scrap Price in town
        /// </summary>
        /// <returns>The amount in Price</returns>
        public int GetScrapPrice(){
            return scrapPrice;            
        }

        /// <summary>
        /// Get medkit Price in town
        /// </summary>
        /// <returns>The amount in Price</returns>
        public int GetMedkitPrice(){
            return medkitPrice;            
        }

        /// <summary>
        /// Get tire Price in town
        /// </summary>
        /// <returns>The amount in Price</returns>
        public int GetTirePrice(){
            return tirePrice;           
        }

        /// <summary>
        /// Get battery Price in town
        /// </summary>
        /// <returns>The amount in Price</returns>
        public int GetBatteryPrice(){
            return batteryPrice;            
        }

        /// <summary>
        /// Get ammo Price in town
        /// </summary>
        /// <returns>The amount in Price</returns>
        public int GetAmmoPrice(){
            return ammoPrice;            
        }

        /// <summary>
        /// Get missions in town
        /// </summary>
        /// <returns>The missions in town</returns>
        public List<Mission> GetMissions(){
            return missions;
        }
    }

    /// <summary>
    /// Utility class to track missions
    /// </summary>
    public class Mission{
        private int missionType, missionQty, missionReward, missionDiff;

        /// <summary>
        /// Create a new mission instance.
        /// </summary>
        public Mission(){
            // Generate a difficulty - 20% each for easy, normal, hard, 40% for no mission to generate
            // 1-20 = easy, 21-40 = medium, 41-60 = hard, 61-100 = no mission
            missionDiff = Random.Range(1,101);
            missionDiff = missionDiff <= 60 ? missionDiff : 0;
            // 1-3 = food, 4-6 = gas, 7-9 = scrap, 10-12 = money, 13 = medkit, 14 = tire, 15 = battery, 16-18 = ammo
            missionReward = missionDiff != 0 ? Random.Range(1, 19) : 0;
            // 1 = combat, 2 = find a collectible
            missionType = missionDiff != 0 ? Random.Range(1,3) : 0;
            // Generate quantity based on reward
            missionQty = missionDiff != 0 ? (missionReward >= 13 && missionReward <= 15) || (missionReward >= 4 && missionReward <= 6) ? Random.Range(2,6) : Random.Range(10,21) : 0;
        }

        /// <summary>
        /// Get mission type in town
        /// </summary>
        /// <returns>The mission type</returns>
        public int GetMissionType(){
            return missionType;
        }

        /// <summary>
        /// Get mission quantity in town
        /// </summary>
        /// <returns>The reward quantity</returns>
        public int GetMissionQty(){
            return missionQty;
        }

        /// <summary>
        /// Get mission reward in town
        /// </summary>
        /// <returns>The reward type</returns>
        public int GetMissionReward(){
            return missionReward;
        }

        /// <summary>
        /// Get mission difficulty in town
        /// </summary>
        /// <returns>The mission difficulty</returns>
        public int GetMissionDifficulty(){
            return missionDiff;
        }
    }
}