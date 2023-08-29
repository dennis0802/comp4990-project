using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UI;

namespace TravelPhase{
    public class Town{
        // To track resources to generate
        List<int> missionDiff = new List<int>();
        List<int> missionReward = new List<int>();
        List<int> missionQty = new List<int>();
        List<int> missionType = new List<int>();
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
                // Generate a difficulty - 20% each for easy, normal, hard, 40% for no mission to generate
                // 1-20 = easy, 21-40 = medium, 41-60 = hard, 61-100 = no mission
                int diff = Random.Range(1,101);
                // 1-3 = food, 4-6 = gas, 7-9 = scrap, 10-12 = money, 13 = medkit, 14 = tire, 15 = battery, 16-18 = ammo
                int reward = Random.Range(1, 19);
                // 1 = combat, 2 = find a collectible
                int type = Random.Range(1,3);

                if(diff <= 60){
                    missionDiff.Add(diff);
                    missionReward.Add(reward);
                    missionType.Add(type);

                    // Generate quantity based on reward
                    int qty = (reward >= 13 && reward <= 15) || (reward >= 4 && reward <= 6) ? Random.Range(2,6) : Random.Range(10,21);
                    missionQty.Add(qty);
                }
                else{
                    missionDiff.Add(0);
                    missionReward.Add(0);
                    missionType.Add(0);
                    missionQty.Add(0);
                }
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
        /// Get mission difficulties in town
        /// </summary>
        /// <returns>The mission difficulty</returns>
        public List<int> GetMissionDifficulties(){
            return missionDiff;            
        }

        /// <summary>
        /// Get mission types in town
        /// </summary>
        /// <returns>The mission type</returns>
        public List<int> GetMissionTypes(){
            return missionType;           
        }

        /// <summary>
        /// Get mission rewards in town
        /// </summary>
        /// <returns>The reward type</returns>
        public List<int> GetMissionRewards(){
            return missionReward;           
        }

        /// <summary>
        /// Get mission quantities in town
        /// </summary>
        /// <returns>The reward quantity</returns>
        public List<int> GetMissionQty(){
            return missionQty;            
        }
    }
}