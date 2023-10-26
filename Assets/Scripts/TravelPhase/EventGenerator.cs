using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System;
using UnityEngine;
using Random = UnityEngine.Random;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Mono.Data.Sqlite;
using UI;
using TMPro;
using Database;

namespace TravelPhase{
    public class EventGenerator : MonoBehaviour
    {
        [Tooltip("Popup text")]
        [SerializeField]
        private TextMeshProUGUI popupText;

        /// <summary>
        /// Generate a random event while driving
        /// </summary>
        /// <param name="eventChance">The probability of the event happening, 44 or less guaranteed to be passed in</param>
        /// <returns>A string displaying the event that occurred</returns>
        public string GenerateEvent(int eventChance){
            string msg = "";

            // Get difficulty, perks, and traits, some events will play differently depending on it (more loss, more damage, etc.)
            IDbConnection dbConnection = GameDatabase.OpenDatabase();
            IDbCommand dbCommandReadValue = dbConnection.CreateCommand();
            dbCommandReadValue.CommandText = "SELECT difficulty FROM SaveFilesTable WHERE id = " + GameLoop.FileId;
            IDataReader dataReader = dbCommandReadValue.ExecuteReader();
            dataReader.Read();
            int diff = dataReader.GetInt32(0);

            dbCommandReadValue = dbConnection.CreateCommand();
            dbCommandReadValue.CommandText = "SELECT * FROM ActiveCharactersTable WHERE id = " + GameLoop.FileId;
            dataReader = dbCommandReadValue.ExecuteReader();
            dataReader.Read();

            List<int> availablePerks = new List<int>();
            List<int> availableTraits = new List<int>();
            List<int> livingMembers = new List<int>();
            for(int i = 2; i <= 29; i+= 9){
                if(dataReader.IsDBNull(i-1)){
                    availablePerks.Add(-1);
                    availableTraits.Add(-1);
                    continue;
                }
                availablePerks.Add(dataReader.GetInt32(i));
                availableTraits.Add(dataReader.GetInt32(i+1));
            }

            for(int i = 0; i <= 3; i++){
                if(!dataReader.IsDBNull(1+9*i)){
                    livingMembers.Add(i);
                }
            } 

            // 1-30 are base events, 31-40 depend on if someone in the party has a trait.
            // 4/44 possibility for a random player to take extra damage (Ex. Bob breaks a rib/leg)
            if(eventChance <= 4){
                dbCommandReadValue = dbConnection.CreateCommand();
                dbCommandReadValue.CommandText = "SELECT miscUpgrade2 FROM CarsTable WHERE id = " + GameLoop.FileId;
                dataReader = dbCommandReadValue.ExecuteReader();
                dataReader.Read();

                int cushioned = dataReader.GetInt32(0);

                dbCommandReadValue = dbConnection.CreateCommand();
                dbCommandReadValue.CommandText = "SELECT * FROM ActiveCharactersTable WHERE id = " + GameLoop.FileId;
                dataReader = dbCommandReadValue.ExecuteReader();
                dataReader.Read();

                int rand = 0, index = 0;
                // Keep randomly picking until not a dead player
                do
                {
                    rand = Random.Range(0,4);
                    index = 1 + 9 * rand;
                } while (dataReader.IsDBNull(index));

                string name = dataReader.GetString(index);
                string[] temp = {" breaks a rib.", " breaks a leg.", " breaks an arm.", " sits down wrong."};
                int hpLoss = diff % 2 == 0 ? Random.Range(13,20) : Random.Range(5,13), curHealth = dataReader.GetInt32(index+8);
                curHealth = curHealth - hpLoss > 0 ? curHealth - hpLoss : 0;

                // Lose less HP if cushion upgrade found
                hpLoss -= cushioned == 1 ? 5 : 0;

                string commandText = "UPDATE ActiveCharactersTable SET ";
                commandText += index == 1 ? "leaderHealth = " + curHealth : "friend" + rand + "Health = " + curHealth;
                commandText += " WHERE id = " + GameLoop.FileId;

                IDbCommand dbCommandUpdateValue = dbConnection.CreateCommand();
                dbCommandUpdateValue.CommandText = commandText;
                dbCommandUpdateValue.ExecuteNonQuery();

                msg = name + temp[rand];
                dbConnection.Close();
            }
            // 3/44 possibility for a random resource type decay more (ex. 10 cans of gas goes missing. Everyone blames Bob.)
            else if(eventChance <= 7){
                dbCommandReadValue = dbConnection.CreateCommand();
                dbCommandReadValue.CommandText = "SELECT toolUpgrade FROM CarsTable WHERE id = " + GameLoop.FileId;
                dataReader = dbCommandReadValue.ExecuteReader();
                dataReader.Read();

                // If tool upgrade was found, treat as uneventful drive.
                if(dataReader.GetInt32(0) == 1){
                    dbConnection.Close();
                    return "";
                }
                else{
                    dbCommandReadValue = dbConnection.CreateCommand();
                    dbCommandReadValue.CommandText = "SELECT * FROM SaveFilesTable WHERE id = " + GameLoop.FileId;
                    dataReader = dbCommandReadValue.ExecuteReader();
                    dataReader.Read();

                    string temp = "", name = "", commandText = "UPDATE SaveFilesTable SET ";
                    int type = Random.Range(7,15), lost = diff % 2 == 0 ? Random.Range(15,30) : Random.Range(10,20), curStock = 0, rand = 0, index = 0;
                    bool breakCondition = false;
                    float curGasStock = 0;
                    List<string> tempTexts = new List<string>(){"kg of food", "cans of gas", "scrap", "dollars", "medkits", "tires", "batteries", "ammo"};
                    List<string> commandTexts = new List<string>(){"food = ", "gas = ", "scrap = ", "money = ", "medkit = ", "tire = ", "battery = ", "ammo = "};

                    // Randomize the item until it is an item in stock
                    do
                    {
                        type = Random.Range(7,15);
                        if(type == 8){
                            breakCondition = dataReader.GetFloat(type) > 0.0f;
                        }
                        else{
                            breakCondition = dataReader.GetInt32(type) > 0;
                        }
                    } while (!breakCondition);

                    if(type >= 11 && type <= 13){
                        lost = diff % 2 == 0 ? Random.Range(3,6) : Random.Range(1,3);
                    }

                    temp = tempTexts[type-7];
                    commandText += commandTexts[type-7];

                    // Gas is a float variable, requires a separate branch.
                    if(type != 8){
                        curStock = dataReader.GetInt32(type);
                        curStock = curStock - lost > 0 ? curStock - lost : 0;
                        commandText += curStock.ToString();
                        lost = lost > curStock ? curStock : lost;
                    }
                    else{
                        curGasStock = dataReader.GetFloat(type);
                        curGasStock = curGasStock - (float)(lost) > 0.0f ? curGasStock - (float)(lost) : 0.0f;
                        commandText += curGasStock.ToString();
                        lost = lost > (int)(curGasStock) ? (int)(curGasStock) : lost;
                    }

                    // If nothing was lost (ie. above check resulted with lost = 0), drive is uneventful
                    if(lost == 0){
                        return "";
                    }

                    commandText += " WHERE id = " + GameLoop.FileId;

                    IDbCommand dbCommandUpdateValue = dbConnection.CreateCommand();
                    dbCommandUpdateValue.CommandText = commandText;
                    dbCommandUpdateValue.ExecuteNonQuery();

                    dbCommandReadValue = dbConnection.CreateCommand();
                    dbCommandReadValue.CommandText = "SELECT * FROM ActiveCharactersTable WHERE id = " + GameLoop.FileId;
                    dataReader = dbCommandReadValue.ExecuteReader();
                    dataReader.Read();

                    // Change grammar if singular for some items
                    if(lost == 1){
                        if(type == 8){
                            temp = "can of gas";
                        }
                        else if(type >= 10 && type <= 12){
                            temp = temp.Remove(temp.Length-1, 1);
                        }
                        else if(type == 13){
                            temp = "battery";
                        }
                    }

                    // Keep randomly picking until not a dead player
                    do
                    {
                        rand = Random.Range(0,4);
                        index = 1 + 9 * rand;
                    } while (dataReader.IsDBNull(index));

                    name = dataReader.GetString(index);
                    msg = lost.ToString() + " " + temp + " goes missing.\nEveryone blames " + name + ".";
                } 
                dbConnection.Close();
            }
            // 3/44 possibility for the car to take more damage (ex. The car drives over some rough terrain)
            else if(eventChance <= 10){
                dbCommandReadValue = dbConnection.CreateCommand();
                dbCommandReadValue.CommandText = "SELECT carHp FROM CarsTable WHERE id = " + GameLoop.FileId;
                dataReader = dbCommandReadValue.ExecuteReader();
                dataReader.Read();

                int hpLoss = diff % 2 == 0 ? Random.Range(20,30) : Random.Range(10,20), curHealth = dataReader.GetInt32(0);
                curHealth = curHealth - hpLoss > 0 ? curHealth - hpLoss : 0;
                string commandText = "UPDATE CarsTable SET carHP = " + curHealth + " WHERE id = " + GameLoop.FileId;
                
                IDbCommand dbCommandUpdateValue = dbConnection.CreateCommand();
                dbCommandUpdateValue.CommandText = commandText;
                dbCommandUpdateValue.ExecuteNonQuery();

                msg = "The car struggles to drive over some terrain.";
                dbConnection.Close();
            }
            // 3/44 possibility for more resources to be found (ex. Bob finds 10 cans of gas in an abandoned car)
            else if(eventChance <= 13){
                dbCommandReadValue = dbConnection.CreateCommand();
                dbCommandReadValue.CommandText = "SELECT * FROM SaveFilesTable WHERE id = " + GameLoop.FileId;
                dataReader = dbCommandReadValue.ExecuteReader();
                dataReader.Read();

                string temp = "", name = "", commandText = "UPDATE SaveFilesTable SET ";
                int type = Random.Range(7,15), gain = diff % 2 == 0 ? Random.Range(15,30) : Random.Range(10,20), curStock = 0, rand = 0, index = 0;
                float curGasStock = 0;
                List<string> tempTexts = new List<string>(){"kg of food", "cans of gas", "scrap", "dollars", "medkits", "tires", "batteries", "ammo"};
                List<string> commandTexts = new List<string>(){"food = ", "gas = ", "scrap = ", "money = ", "medkit = ", "tire = ", "battery = ", "ammo = "};

                if(type >= 11 && type <= 13){
                    gain = diff % 2 == 0 ? Random.Range(3,6) : Random.Range(1,3);
                }

                temp = tempTexts[type-7];
                commandText += commandTexts[type-7];

                if(type != 8){
                    curStock = dataReader.GetInt32(type) + gain;
                    commandText += curStock.ToString();
                }
                else{
                    curGasStock = dataReader.GetFloat(type) + (float)(gain);
                    commandText += curGasStock.ToString();
                }
                commandText += " WHERE id = " + GameLoop.FileId;

                IDbCommand dbCommandUpdateValue = dbConnection.CreateCommand();
                dbCommandUpdateValue.CommandText = commandText;
                dbCommandUpdateValue.ExecuteNonQuery();

                dbCommandReadValue = dbConnection.CreateCommand();
                dbCommandReadValue.CommandText = "SELECT * FROM ActiveCharactersTable WHERE id = " + GameLoop.FileId;
                dataReader = dbCommandReadValue.ExecuteReader();
                dataReader.Read();

                // Change grammar if singular for some items
                if(gain == 1){
                    if(type == 8){
                        temp = "can of gas";
                    }
                    else if(type >= 10 && type <= 12){
                        temp = temp.Remove(temp.Length-1, 1);
                    }
                    else if(type == 13){
                        temp = "battery";
                    }
                }

                // Keep randomly picking until not a dead player
                do
                {
                    rand = Random.Range(0,4);
                    index = 1 + 9 * rand;
                } while (dataReader.IsDBNull(index));

                name = dataReader.GetString(index);
                msg = name + " finds " + gain + " " + temp + " in an abandoned car.";
                dbConnection.Close();
            }
            // 5/44 possibility to find a new party member (ex. The party meets Bob. They have the Perk surgeon and Trait paranoid.)
            else if(eventChance <= 18){
                // Check that a slot is available.
                dbCommandReadValue = dbConnection.CreateCommand();
                dbCommandReadValue.CommandText = "SELECT friend1Name, friend2Name, friend3Name, customIdLeader, customId1, customId2, customId3 FROM ActiveCharactersTable WHERE id = " + GameLoop.FileId;
                dataReader = dbCommandReadValue.ExecuteReader();
                dataReader.Read();

                List<string> names = new List<string>();
                List<int> customIds = new List<int>(){dataReader.GetInt32(3)};
                for(int i = 0; i < 3 ; i++){
                    string name = dataReader.IsDBNull(i) ? "_____TEMPNULL" : dataReader.GetString(i);
                    int id = dataReader.IsDBNull(i+4) ? -1 : dataReader.GetInt32(i+4);
                    names.Add(name);
                    customIds.Add(id);
                }

                if(names.Where(n => Equals(n, "_____TEMPNULL")).Count() > 0){
                    int perk = -1, trait = -1, acc = -1, outfit = -1, color = -1, hat = -1, idRead = -1;
                    string name = "", perkRoll = "", traitRoll = "";
                    int index = names.IndexOf(names.Where(n => Equals(n, "_____TEMPNULL")).First());
                    
                    dbCommandReadValue = dbConnection.CreateCommand();
                    dbCommandReadValue.CommandText = "SELECT COUNT(*) FROM PerishedCustomTable WHERE saveFileId = " + GameLoop.FileId;
                    int deadCount = Convert.ToInt32(dbCommandReadValue.ExecuteScalar());

                    dbCommandReadValue = dbConnection.CreateCommand();
                    dbCommandReadValue.CommandText = "SELECT COUNT(*) FROM CustomCharactersTable";
                    int customCharacterCount = Convert.ToInt32(dbCommandReadValue.ExecuteScalar());

                    // Generate randomized character - standard or out of unused custom characters because they are all either in the party or dead
                    if(diff == 1 || diff == 3 || customCharacterCount == customIds.Where(c => c != -1).Count() + deadCount){
                        perk = Random.Range(0,GamemodeSelect.Perks.Count()); 
                        trait = Random.Range(0, GamemodeSelect.Traits.Count());
                        acc = Random.Range(1,4); 
                        outfit = Random.Range(1,4); 
                        color = Random.Range(1,10); 
                        hat = Random.Range(1,9);
                        name = GamemodeSelect.RandomNames[Random.Range(0, GamemodeSelect.RandomNames.Count())];
                        perkRoll = GamemodeSelect.Perks[perk];
                        traitRoll = GamemodeSelect.Traits[trait];
                    }
                    // Generate custom character
                    else{
                        int rand = -1;

                        do
                        {
                            rand = Random.Range(0, customCharacterCount);
                        } while (customIds.Contains(rand));

                        dbCommandReadValue = dbConnection.CreateCommand();
                        dbCommandReadValue.CommandText = "SELECT id, name, perk, trait, accessory, hat, color, outfit FROM CustomCharactersTable WHERE id = " + rand;
                        dataReader = dbCommandReadValue.ExecuteReader();
                        dataReader.Read();

                        perk = dataReader.GetInt32(2);
                        trait = dataReader.GetInt32(3);
                        acc = dataReader.GetInt32(4);
                        outfit = dataReader.GetInt32(7);
                        color = dataReader.GetInt32(6);
                        hat = dataReader.GetInt32(5);
                        idRead = dataReader.GetInt32(0);
                        name = dataReader.GetString(1);
                        perkRoll = GamemodeSelect.Perks[perk];
                        traitRoll = GamemodeSelect.Traits[trait];
                    }
                    
                    string commandText = "UPDATE ActiveCharactersTable SET friend" + (index+1) + "Name = '" + name + "', friend" + (index+1) + "Perk = " + perk + 
                                            ", friend" + (index+1) + "Trait = " + trait + ", friend" + (index+1) + "Acc = " + acc + ", friend" + (index+1) + "Color = " + color + 
                                            ", friend" + (index+1) + "Hat = " + hat + ", friend" + (index+1) + "Outfit = " + outfit + ", friend" + (index+1) + "Health = 100" +
                                            ", friend" + (index+1) + "Morale = 75, customId" + (index + 1) + " = " + idRead + " WHERE id = " + GameLoop.FileId;
                    IDbCommand dbCommandUpdateValue = dbConnection.CreateCommand();
                    dbCommandUpdateValue.CommandText = commandText;
                    dbCommandUpdateValue.ExecuteNonQuery();

                    // Add a medkit if healthcare trait.
                    if(perk == 2){
                        dbCommandUpdateValue = dbConnection.CreateCommand();
                        dbCommandUpdateValue.CommandText = "UPDATE SaveFilesTable SET medkit = medkit + 1 WHERE id = " + GameLoop.FileId;
                        dbCommandUpdateValue.ExecuteNonQuery();
                    }

                    msg = "The party meets " + name + " and allows them to join.\nThey have the " + perkRoll + " perk and the " + traitRoll + " trait.";
                }
                else{
                    msg = "You drive by someone on the road but your car is full.";
                }
                dbConnection.Close();
            }
            // 1/44 possibility for an upgrade to be found. (ex. The party searches an abandoned car and finds nothing of interest.)
            else if(eventChance <= 19){
                // Check that a slot is available.
                dbCommandReadValue = dbConnection.CreateCommand();
                dbCommandReadValue.CommandText = "SELECT wheelUpgrade, batteryUpgrade, engineUpgrade, toolUpgrade, miscUpgrade1, miscUpgrade2 FROM CarsTable WHERE id = " + 
                                                    GameLoop.FileId;
                dataReader = dbCommandReadValue.ExecuteReader();
                dataReader.Read();

                List<int> curUpgrades = new List<int>(){dataReader.GetInt32(0), dataReader.GetInt32(1), dataReader.GetInt32(2), dataReader.GetInt32(3), dataReader.GetInt32(4),
                                                        dataReader.GetInt32(5)};
                // At least one slot is available.
                if(curUpgrades.Where(c => c == 0).Count() > 0){
                    int selected;
                    string found = "", commandTemp = "";

                    do
                    {
                        selected = Random.Range(0, curUpgrades.Count);
                    } while (curUpgrades[selected] != 0);

                    found = selected == 0 ? "durable tires" : selected == 1 ? "a durable battery" : selected == 2 ? "a fuel-efficient engine" : selected == 3 ? "a secure travel chest" :
                            selected == 4 ? "a travel garden" : "cushioned seating";
                    commandTemp = selected == 0 ? "wheelUpgrade = 1 " : selected == 1 ? "batteryUpgrade = 1" : selected == 2 ? "engineUpgrade = 1" : selected == 3 ? "toolUpgrade == 1" :
                                    selected == 4 ? "miscUpgrade1 = 1" : "miscUpgrade2 = 1";

                    IDbCommand dbCommandUpdateValue = dbConnection.CreateCommand();
                    dbCommandUpdateValue.CommandText = "UPDATE CarsTable SET " + commandTemp + " WHERE id = " + GameLoop.FileId;
                    dbCommandUpdateValue.ExecuteNonQuery();

                    msg = "The party searches an abandoned car and finds " + found + ".";

                    dbConnection.Close();
                }
                // No slot available
                else{
                    msg = "The party searches an abandoned car and finds nothing of interest.";
                }

                dbConnection.Close();
            }
            // 2/44 possibility for party-wide damage. (ex. The party cannot find clean water. Everyone is dehydrated.)
            else if(eventChance <= 21){
                dbCommandReadValue = dbConnection.CreateCommand();
                dbCommandReadValue.CommandText = "SELECT * FROM ActiveCharactersTable WHERE id = " + GameLoop.FileId;
                dataReader = dbCommandReadValue.ExecuteReader();
                dataReader.Read();

                int hpLoss = diff % 2 == 0 ? Random.Range(10,15) : Random.Range(5,10);
                List<int> teamHp = new List<int>(){dataReader.GetInt32(9), dataReader.GetInt32(18), dataReader.GetInt32(27), dataReader.GetInt32(36)};
                for(int i = 0; i < teamHp.Count; i++){
                    teamHp[i] = teamHp[i] - hpLoss > 0 ? teamHp[i] - hpLoss : 0;
                }

                string commandText = "UPDATE ActiveCharactersTable SET leaderHealth = " + teamHp[0] + ", friend1Health = " + teamHp[1] + ", friend2Health = " + teamHp[2] +
                                        ", friend3Health = " + teamHp[3] + " WHERE id = " + GameLoop.FileId;
                IDbCommand dbCommandUpdateValue = dbConnection.CreateCommand();
                dbCommandUpdateValue.CommandText = commandText;
                dbCommandUpdateValue.ExecuteNonQuery();

                msg = "The party cannot find clean water. Everyone is dehydrated.";

                dbConnection.Close();
            }
            // 3/44 possibility for a tire to go flat
            else if(eventChance <= 24){
                // If the car has upgraded tires, display the attempt at popping the tire.
                dbCommandReadValue = dbConnection.CreateCommand();
                dbCommandReadValue.CommandText = "SELECT wheelUpgrade FROM CarsTable WHERE id = " + GameLoop.FileId;
                dataReader = dbCommandReadValue.ExecuteReader();
                dataReader.Read();

                if(dataReader.GetInt32(0) != 0){
                    msg = "The car goes over some rough terrain but the durable tires remain intact.";
                }
                else{
                    dbCommandReadValue = dbConnection.CreateCommand();
                    dbCommandReadValue.CommandText = "SELECT tire FROM SaveFilesTable WHERE id = " + GameLoop.FileId;
                    dataReader = dbCommandReadValue.ExecuteReader();
                    dataReader.Read();

                    int tires = dataReader.GetInt32(0);

                    // Determine if the car can still move.
                    if(tires > 0){
                        tires--;
                        IDbCommand dbCommandUpdateValue = dbConnection.CreateCommand();
                        dbCommandUpdateValue.CommandText = "UPDATE SaveFilesTable SET tire = " + tires + " WHERE id = " + GameLoop.FileId;
                        dbCommandUpdateValue.ExecuteNonQuery();
                        msg = "The car goes over some rough terrain and the tire pops.\nYou replace your flat tire.";
                    }
                    else{
                        string commandText = "UPDATE CarsTable SET isTireFlat = 1 WHERE id = " + GameLoop.FileId;
                        IDbCommand dbCommandUpdateValue = dbConnection.CreateCommand();
                        dbCommandUpdateValue.CommandText = commandText;
                        dbCommandUpdateValue.ExecuteNonQuery();
                        msg = "The car goes over some rough terrain and the tire pops.\nYou don't have a tire to replace.\nTrade for another one.";
                    }
                }
                dbConnection.Close();
            }
            // 3/44 possibility for a car battery to die.
            else if(eventChance <= 27){
                // If the car has upgraded battery, display the attempt at breaking.
                dbCommandReadValue = dbConnection.CreateCommand();
                dbCommandReadValue.CommandText = "SELECT batteryUpgrade FROM CarsTable WHERE id = " + GameLoop.FileId;
                dataReader = dbCommandReadValue.ExecuteReader();
                dataReader.Read();

                if(dataReader.GetInt32(0) != 0){
                    msg = "The car battery starts making noises but go away after some time.";
                }
                else{
                    dbCommandReadValue = dbConnection.CreateCommand();
                    dbCommandReadValue.CommandText = "SELECT battery FROM SaveFilesTable WHERE id = " + GameLoop.FileId;
                    dataReader = dbCommandReadValue.ExecuteReader();
                    dataReader.Read();

                    int batteries = dataReader.GetInt32(0);

                    // Determine if the car can still move.
                    if(batteries > 0){
                        batteries--;
                        IDbCommand dbCommandUpdateValue = dbConnection.CreateCommand();
                        dbCommandUpdateValue.CommandText = "UPDATE SaveFilesTable SET battery = " + batteries + " WHERE id = " + GameLoop.FileId;
                        dbCommandUpdateValue.ExecuteNonQuery();
                        msg = "There is smoke coming from the hood - the car battery is dead.\nYou replace your dead battery.";
                    }
                    else{
                        string commandText = "UPDATE CarsTable SET isBatteryDead = 1 WHERE id = " + GameLoop.FileId;
                        IDbCommand dbCommandUpdateValue = dbConnection.CreateCommand();
                        dbCommandUpdateValue.CommandText = commandText;
                        dbCommandUpdateValue.ExecuteNonQuery();
                        msg = "There is smoke coming from the hood - the car battery is dead.\nYou don't have a battery to replace.\nTrade for another one.";
                    }
                }
                dbConnection.Close();
            }
            // 3/44 possibility for someone (other than the leader) with low morale to ditch. Cases where morale is high, treat as a typical drive with no evet
            else if(eventChance <= 30){
                dbCommandReadValue = dbConnection.CreateCommand();
                dbCommandReadValue.CommandText = "SELECT * FROM ActiveCharactersTable WHERE id = " + GameLoop.FileId;
                dataReader = dbCommandReadValue.ExecuteReader();
                dataReader.Read();

                List<int> morale = new List<int>();
                for(int i = 10; i <= 28 ; i+= 9){
                    if(!dataReader.IsDBNull(i)){
                        int moraleRead = dataReader.IsDBNull(i+7) ? -1 : dataReader.GetInt32(i+7);
                        morale.Add(moraleRead);
                    }
                }

                int lowMorale = morale.Where(m => m >= 0 && m <= 20).Count();
                if(lowMorale > 0){
                    int lowestIndex = morale.IndexOf(morale.Min()), nameIndex = lowestIndex == 0 ? 10 : lowestIndex == 1 ? 19 : 28;
                    string name = dataReader.GetString(nameIndex), commandText = "UPDATE ActiveCharactersTable SET ";
                    commandText += lowestIndex == 0 ? "friend1Name = null " : lowestIndex == 1 ? "friend2Name = null " : "friend3Name = null ";
                    commandText += "WHERE id = " + GameLoop.FileId;

                    IDbCommand dbCommandUpdateValue = dbConnection.CreateCommand();
                    dbCommandUpdateValue.CommandText = commandText;
                    dbCommandUpdateValue.ExecuteNonQuery();
                    dbConnection.Close();

                    msg = "In despair, " + name + " ditches the party, saying their chances are better without the party.";
                }
                else{
                    dbConnection.Close();
                    return "";
                }
            }
            
            // 2/44 possibility for musician characters to raise party morale (ex. Bob serenades the party, reminding them of better times. The party is in high spirits.)
            else if(eventChance <= 32 && availablePerks.Where(p => p == 5).Count() > 0){
                // Get the name of the member who has the musician trait
                int nameIndex = availablePerks.IndexOf(5);
                List<int> partyMorale = new List<int>();
                nameIndex = nameIndex == 0 ? 1 : nameIndex == 1 ? 10 : nameIndex == 2 ? 19 : 28;

                dbCommandReadValue = dbConnection.CreateCommand();
                dbCommandReadValue.CommandText = "SELECT * FROM ActiveCharactersTable WHERE id = " + GameLoop.FileId;
                dataReader = dbCommandReadValue.ExecuteReader();
                dataReader.Read();

                string name = dataReader.GetString(nameIndex);
                string commandText = "UPDATE ActiveCharactersTable SET ";
                int moraleGain = diff % 2 == 0 ? 5 : 10;

                // Raise only for players who are not dead (ie. name is not null in db)
                for(int i = 0; i < 4; i++){
                    if(!dataReader.IsDBNull(1+9*i)){
                        int moraleFound = dataReader.GetInt32(8+9*i) + moraleGain > 100 ? 100 : dataReader.GetInt32(8+9*i) + moraleGain;
                        commandText += i == 0 ? "leaderMorale = " + moraleFound : ", friend" + i + "Morale = " + moraleFound;
                    }
                }
                commandText += " WHERE id = " + GameLoop.FileId;
                IDbCommand dbCommandUpdateValue = dbConnection.CreateCommand();
                dbCommandUpdateValue.CommandText = commandText;
                dbCommandUpdateValue.ExecuteNonQuery();
                dbConnection.Close();

                msg = name + " serenades the party with a guitar, reminding them of better times.\nThe party is in high spirits.";
            }
            // 2/44 possibility for bandits to lower party morale (ex. Bob attempts to rob a helpless group but is caught and drags the party with him. The party feels guilty.)
            else if(eventChance <= 34 && availableTraits.Where(t => t == 3).Count() > 0){
                // Get the name of the member who has the bandit trait
                int nameIndex = availableTraits.IndexOf(3);
                List<int> partyMorale = new List<int>();
                nameIndex = nameIndex == 0 ? 1 : nameIndex == 1 ? 10 : nameIndex == 2 ? 19 : 28;

                dbCommandReadValue = dbConnection.CreateCommand();
                dbCommandReadValue.CommandText = "SELECT * FROM ActiveCharactersTable WHERE id = " + GameLoop.FileId;
                dataReader = dbCommandReadValue.ExecuteReader();
                dataReader.Read();

                string name = dataReader.GetString(nameIndex);
                string commandText = "UPDATE ActiveCharactersTable SET ";
                int moraleLoss = diff % 2 == 0 ? 10 : 5;

                // Lower only for players who are not dead (ie. name is not null in db)
                for(int i = 0; i < 4; i++){
                    if(!dataReader.IsDBNull(1+9*i)){
                        int moraleFound = dataReader.GetInt32(8+9*i) - moraleLoss > 0 ? dataReader.GetInt32(8+9*i) - moraleLoss : 0;
                        commandText += i == 0 ? "leaderMorale = " + moraleFound : ", friend" + i + "Morale = " + moraleFound;
                    }
                }
                commandText += " WHERE id = " + GameLoop.FileId;
                IDbCommand dbCommandUpdateValue = dbConnection.CreateCommand();
                dbCommandUpdateValue.CommandText = commandText;
                dbCommandUpdateValue.ExecuteNonQuery();
                dbConnection.Close();

                msg = name + " attempts to rob a helpless group but is caught and drags the party with them.\nThe party is forced to flee and feels guilty.";
            } 
            // 2/44 possibility for hot headed characters to lower another character's hp. (ex. Bob, annoyed with Ann for a minor issue, lashes out mid-argument.)
            else if(eventChance <= 36 && availableTraits.Where(t => t == 4).Count() > 0 && livingMembers.Count > 1){
                // Get the name of the first member who has the hot-headed trait
                int nameIndex = availableTraits.IndexOf(4), hurtMember = 0;
                nameIndex = nameIndex == 0 ? 1 : nameIndex == 1 ? 10 : nameIndex == 2 ? 19 : 28;

                dbCommandReadValue = dbConnection.CreateCommand();
                dbCommandReadValue.CommandText = "SELECT * FROM ActiveCharactersTable WHERE id = " + GameLoop.FileId;
                dataReader = dbCommandReadValue.ExecuteReader();
                dataReader.Read();

                // Select a living party member to hurt, not including their self.
                do
                {
                    hurtMember = Random.Range(0,4);
                } while (hurtMember == nameIndex || !dataReader.IsDBNull(1+9*hurtMember) || !livingMembers.Contains(hurtMember));

                string name = dataReader.GetString(nameIndex), hurtName = dataReader.GetString(1+9*hurtMember);
                int hpLoss = diff % 2 == 0 ? 10 : 5, hurtHP = dataReader.GetInt32(9+9*hurtMember) - hpLoss > 0 ? dataReader.GetInt32(9+9*hurtMember) - hpLoss : 0;
                string commandText = "UPDATE ActiveCharactersTable SET ";
                commandText += hurtMember == 0 ? "leaderHealth = " + hurtHP : "friend" + hurtMember + "Health = " + hurtHP;

                commandText += " WHERE id = " + GameLoop.FileId;
                IDbCommand dbCommandUpdateValue = dbConnection.CreateCommand();
                dbCommandUpdateValue.CommandText = commandText;
                dbCommandUpdateValue.ExecuteNonQuery();
                dbConnection.Close();

                msg = name + ", annoyed with " + hurtName + " for a minor issue, lashes out mid-argument.";
            }
            // 2/44 possibility for surgeon characters to fully heal an injured character (ex. Bob's medical skills come in handy for mid-drive surgery on Ann)
            else if(eventChance <= 38 && availablePerks.Where(p => p == 3).Count() > 0 && livingMembers.Count > 1){
                // Get the name of the first member who has the surgeon trait
                int index = availablePerks.IndexOf(3), healMember = 0;
                int nameIndex = index == 0 ? 1 : index == 1 ? 10 : index == 2 ? 19 : 28;

                if(!livingMembers.Contains(index)){
                    return "";
                }

                dbCommandReadValue = dbConnection.CreateCommand();
                dbCommandReadValue.CommandText = "SELECT * FROM ActiveCharactersTable WHERE id = " + GameLoop.FileId;
                dataReader = dbCommandReadValue.ExecuteReader();
                dataReader.Read();

                // Select a living party member to heal, not including their self.
                do
                {
                    healMember = Random.Range(0,4);
                } while (healMember == index || dataReader.IsDBNull(1+9*healMember) || !livingMembers.Contains(healMember));

                string name = dataReader.GetString(nameIndex);
                string healName = dataReader.GetString(1+9*healMember);
                int hpGain = diff % 2 == 0 ? 5 : 10, healHP = dataReader.GetInt32(9+9*healMember) + hpGain > 100 ? 100 : dataReader.GetInt32(9+9*healMember) + hpGain;
                string commandText = "UPDATE ActiveCharactersTable SET ";
                commandText += healMember == 0 ? "leaderHealth = " + healHP : "friend" + healMember + "Health = " + healHP;

                commandText += " WHERE id = " + GameLoop.FileId;
                IDbCommand dbCommandUpdateValue = dbConnection.CreateCommand();
                dbCommandUpdateValue.CommandText = commandText;
                dbCommandUpdateValue.ExecuteNonQuery();
                dbConnection.Close();

                msg = name + "'s medical skills come in handy using medicinal herbs to treat " + healName + ".";
            } 
            // 2/44 possibility for creative/programmer characters to act (ex. Bob has a creative solution for a car upgrade and succeeds/fails.)
            // Uses an extra roll to determine positive/negative.
            else if(eventChance <= 40 && (availableTraits.Where(t => t == 5).Count() > 0 || availablePerks.Where(p => p == 4).Count() > 0)){
                // Get the name of the first member who has the creative OR programmer trait
                int nameIndex = availableTraits.Where(t => t == 5).Count() > 0 ? availableTraits.IndexOf(5) : availablePerks.IndexOf(4), healMember = 0;
                nameIndex = nameIndex == 0 ? 1 : nameIndex == 1 ? 10 : nameIndex == 2 ? 19 : 28;
                string solType = availableTraits.Where(t => t == 5).Count() > 0 ? "creative" : "systematic and thought-out";

                dbCommandReadValue = dbConnection.CreateCommand();
                dbCommandReadValue.CommandText = "SELECT * FROM ActiveCharactersTable WHERE id = " + GameLoop.FileId;
                dataReader = dbCommandReadValue.ExecuteReader();
                dataReader.Read();

                string name = dataReader.GetString(nameIndex), healName = dataReader.GetString(1+9*healMember);
                dbConnection.Close();

                // Check that a slot is available.
                dbCommandReadValue = dbConnection.CreateCommand();
                dbCommandReadValue.CommandText = "SELECT wheelUpgrade, batteryUpgrade, engineUpgrade, toolUpgrade, miscUpgrade1, miscUpgrade2 FROM CarsTable WHERE id = " + 
                                                    GameLoop.FileId;
                dataReader = dbCommandReadValue.ExecuteReader();
                dataReader.Read();

                List<int> curUpgrades = new List<int>(){dataReader.GetInt32(0), dataReader.GetInt32(1), dataReader.GetInt32(2), dataReader.GetInt32(3), dataReader.GetInt32(4),
                                                        dataReader.GetInt32(5)};

                // 1/4 chance for creative, 1/2 for programmer.
                int successRoll = availableTraits.Where(t => t == 5).Count() > 0 ? Random.Range(0,4) : Random.Range(0,2);
                // Check for success, then check if a slot is available. Otherwise an uneventful drive.
                if(successRoll == 0){
                    msg = name + " has a " + solType + " solution for a car upgrade but fails.";
                }
                else if(curUpgrades.Where(c => c == 0).Count() > 0){
                    int selected;
                    string commandTemp = "";

                    do
                    {
                        selected = Random.Range(0, curUpgrades.Count);
                    } while (curUpgrades[selected] != 0);

                    commandTemp = selected == 0 ? "wheelUpgrade = 1 " : selected == 1 ? "batteryUpgrade = 1" : selected == 2 ? "engineUpgrade = 1" : selected == 3 ? "toolUpgrade == 1" :
                                selected == 4 ? "miscUpgrade1 = 1" : "miscUpgrade2 = 1";

                    IDbCommand dbCommandUpdateValue = dbConnection.CreateCommand();
                    dbCommandUpdateValue.CommandText = "UPDATE CarsTable SET " + commandTemp + " WHERE id = " + GameLoop.FileId;
                    dbCommandUpdateValue.ExecuteNonQuery();
                    
                    msg = name + " has a " + solType + " solution for a car upgrade and succeeds.";
                }
                else{
                    dbConnection.Close();
                    return "";
                }
                dbConnection.Close();
            }   
            // 2/44 possibility for a combat event to occur if travelling with higher or more activity
            else if(eventChance <= 42 && GameLoop.Activity >= 3){
                msg = "You suddenly find yourself surrounded by mutants.";
            }
            // 2/44 possibility for someone to be pulled out of the car and left for dead if travelling with ravenous activity
            // Morale will determine if member fights them off.
            else if(eventChance <= 44 && GameLoop.Activity == 4){
                dbCommandReadValue = dbConnection.CreateCommand();
                dbCommandReadValue.CommandText = "SELECT * FROM ActiveCharactersTable WHERE id = " + GameLoop.FileId;
                dataReader = dbCommandReadValue.ExecuteReader();
                dataReader.Read();

                List<int> morale = new List<int>();
                int selected;

                for(int i = 10; i <= 28 ; i+= 9){
                    if(!dataReader.IsDBNull(i)){
                        int moraleRead = dataReader.IsDBNull(i+7) ? -1 : dataReader.GetInt32(i+7);
                        morale.Add(moraleRead);
                    }
                }

                // Select a living party member to atttack, not including the leader
                do
                {
                    selected = Random.Range(1,4);
                } while (!dataReader.IsDBNull(1+9*selected));

                int nameIndex = selected == 1 ? 10 : selected == 2 ? 19 : 28;
                string name = dataReader.GetString(nameIndex), commandText = "UPDATE ActiveCharactersTable SET ";

                if(morale[nameIndex+7] < 40){
                    commandText += selected == 0 ? "friend1Name = null " : selected == 1 ? "friend2Name = null " : "friend3Name = null ";
                    commandText += "WHERE id = " + GameLoop.FileId;

                    IDbCommand dbCommandUpdateValue = dbConnection.CreateCommand();
                    dbCommandUpdateValue.CommandText = commandText;
                    dbCommandUpdateValue.ExecuteNonQuery();
                    dbConnection.Close();

                    msg = name + " is pulled out of the car and is unable to fight back against the mutants.";
                }
                else{
                    dbConnection.Close();
                    msg = "Mutants attempt to pull " + name + " out of the car, but fail to do so.";
                }
            }
            // Capture any events from 32-44 that don't have their other conditions met (living members, perks)
            else if(eventChance <= 44){
                return "";
            }

            return msg;
        }
    }
}