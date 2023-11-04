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
            Save save = DataUser.dataManager.GetSaveById(GameLoop.FileId);
            int diff = save.Difficulty;

            IEnumerable<ActiveCharacter> characters = DataUser.dataManager.GetActiveCharacters().Where<ActiveCharacter>(c=>c.FileId == GameLoop.FileId && c.CharacterName != null).OrderByDescending(c=>c.IsLeader);
            ActiveCharacter[] tempCharacters = characters.ToArray<ActiveCharacter>();
            List<int> availablePerks = new List<int>(), availableTraits = new List<int>();
            for(int i = 0; i < tempCharacters.Count(); i++){
                availablePerks.Add(tempCharacters[i].Perk);
                availableTraits.Add(tempCharacters[i].Trait);
            }

            Car car = DataUser.dataManager.GetCarById(GameLoop.FileId);

            // 1-30 are base events, 31-40 depend on if someone in the party has a trait.
            // 4/44 possibility for a random player to take extra damage (Ex. Bob breaks a rib/leg)
            if(eventChance <= 4){
                int cushioned = car.MiscUpgrade2, rand = 0;
                
                // Pick a player
                rand = Random.Range(0,tempCharacters.Count());
                string name = tempCharacters[rand].CharacterName;
                string[] temp = {" breaks a rib.", " breaks a leg.", " breaks an arm.", " sits down wrong."};
                int hpLoss = diff % 2 == 0 ? Random.Range(13,20) : Random.Range(5,13), curHealth = tempCharacters[rand].Health;
                // Lose less HP if cushion upgrade found
                hpLoss -= cushioned == 1 ? 5 : 0;
                curHealth = curHealth - hpLoss > 0 ? curHealth - hpLoss : 0;

                List<int> teamHealth = new List<int>(), ids = new List<int>();
                foreach(ActiveCharacter character in characters){
                    teamHealth.Add(character.Health);
                    ids.Add(character.Id);
                }
                while(teamHealth.Count < 4){
                    teamHealth.Add(0);
                }

                string commandText = "UPDATE ActiveCharacter SET Health = ? WHERE Id = ?";
                for(int i = 0; i < teamHealth.Count(); i++){
                    TravelLoop.queriesToPerform.Add(commandText);
                    List<object> parameters = new List<object>();
                    parameters.Add(teamHealth[i]);
                    parameters.Add(ids[i]);
                    TravelLoop.parametersForQueries.Add(parameters);
                }
                msg = name + temp[rand];
            }
            // 3/44 possibility for a random resource type decay more (ex. 10 cans of gas goes missing. Everyone blames Bob.)
            else if(eventChance <= 7){
                // If tool upgrade was found, treat as uneventful drive.
                if(car.ToolUpgrade == 1){
                    return "";
                }
                else{
                    string temp = "", name = "", commandText = "UPDATE Save SET ";
                    int type = Random.Range(0,8), lost = diff % 2 == 0 ? Random.Range(15,30) : Random.Range(10,20), curStock = 0, rand = 0;
                    bool breakCondition = false;
                    float curGasStock = 0;
                    List<string> tempTexts = new List<string>(){"kg of food", "cans of gas", "scrap", "dollars", "medkits", "tires", "batteries", "ammo"},
                                commandTexts = new List<string>(){"food = ? ", "gas = ? ", "scrap = ? ", "money = ? ", "medkit = ? ", "tire = ? ", "battery = ? ", "ammo = ? "};
                    List<int> partyStock = new List<int>(){save.Food, (int)(save.Gas), save.Scrap, save.Money, save.Medkit, save.Tire, save.Battery, save.Ammo};

                    // Randomize the item until it is an item in stock
                    do
                    {
                        type = Random.Range(0,8);
                        if(type == 1){
                            breakCondition = save.Gas > 0.0f;
                        }
                        else{
                            breakCondition = partyStock[type] > 0;
                        }
                    } while (!breakCondition);

                    if(type >= 4 && type <= 6){
                        lost = diff % 2 == 0 ? Random.Range(3,6) : Random.Range(1,3);
                    }

                    temp = tempTexts[type];
                    commandText += commandTexts[type];
                    List<object> parameters = new List<object>();

                    // Gas is a float variable, requires a separate branch.
                    if(type != 8){
                        curStock = partyStock[type];
                        curStock = curStock - lost > 0 ? curStock - lost : 0;
                        parameters.Add(curStock);
                        lost = lost > curStock ? curStock : lost;
                    }
                    else{
                        curGasStock = save.Gas;
                        curGasStock = curGasStock - (float)(lost) > 0.0f ? curGasStock - (float)(lost) : 0.0f;
                        parameters.Add(curGasStock);
                        lost = lost > (int)(curGasStock) ? (int)(curGasStock) : lost;
                    }

                    // If nothing was lost (ie. above check resulted with lost = 0), drive is uneventful
                    if(lost == 0){
                        return "";
                    }
                    commandText += " WHERE Id = ?";
                    parameters.Add(GameLoop.FileId);

                    TravelLoop.queriesToPerform.Add(commandText);
                    TravelLoop.parametersForQueries.Add(parameters);

                    // Change grammar if singular for some items
                    if(lost == 1){
                        if(type == 1){
                            temp = "can of gas";
                        }
                        else if(type >= 3 && type <= 5){
                            temp = temp.Remove(temp.Length-1, 1);
                        }
                        else if(type == 6){
                            temp = "battery";
                        }
                    }

                    // Randomly pick a player to blame
                    rand = Random.Range(0,tempCharacters.Count());
                    name = tempCharacters[rand].CharacterName;
                    msg = lost.ToString() + " " + temp + " goes missing.\nEveryone blames " + name + ".";
                } 
            }
            // 3/44 possibility for the car to take more damage (ex. The car drives over some rough terrain)
            else if(eventChance <= 10){
                int hpLoss = diff % 2 == 0 ? Random.Range(20,30) : Random.Range(10,20), curHealth = car.CarHP;
                curHealth = curHealth - hpLoss > 0 ? curHealth - hpLoss : 0;
                string commandText = "UPDATE CarsTable SET carHP = ? WHERE Id = ?";    
                TravelLoop.queriesToPerform.Add(commandText);
                List<object> parameters = new List<object>{curHealth, GameLoop.FileId};
                TravelLoop.parametersForQueries.Add(parameters);

                msg = "The car struggles to drive over some terrain.";
            }
            // 3/44 possibility for more resources to be found (ex. Bob finds 10 cans of gas in an abandoned car)
            else if(eventChance <= 13){
                string temp = "", name = "", commandText = "UPDATE SaveFilesTable SET ";
                int type = Random.Range(0,8), gain = diff % 2 == 0 ? Random.Range(15,30) : Random.Range(10,20), curStock = 0, rand = 0;
                float curGasStock = 0;
                List<string> tempTexts = new List<string>(){"kg of food", "cans of gas", "scrap", "dollars", "medkits", "tires", "batteries", "ammo"},
                             commandTexts = new List<string>(){"food = ? ", "gas = ? ", "scrap = ? ", "money = ? ", "medkit = ? ", "tire = ? ", "battery = ? ", "ammo = ? "};
                List<int> partyStock = new List<int>(){save.Food, (int)(save.Gas), save.Scrap, save.Money, save.Medkit, save.Tire, save.Battery, save.Ammo};
                if(type >= 4 && type <= 6){
                    gain = diff % 2 == 0 ? Random.Range(3,6) : Random.Range(1,3);
                }

                temp = tempTexts[type];
                commandText += commandTexts[type];
                List<object> parameters = new List<object>();

                if(type != 8){
                    curStock = partyStock[type] + gain;
                    parameters.Add(curStock);
                    commandText += curStock.ToString();
                }
                else{
                    curGasStock = partyStock[type] + (float)(gain);
                    parameters.Add(curGasStock);
                    commandText += curGasStock.ToString();
                }
                commandText += " WHERE Id = ?";
                parameters.Add(GameLoop.FileId);
                
                TravelLoop.queriesToPerform.Add(commandText);
                TravelLoop.parametersForQueries.Add(parameters);

                // Change grammar if singular for some items
                if(gain == 1){
                    if(type == 1){
                        temp = "can of gas";
                    }
                    else if(type >= 3 && type <= 5){
                        temp = temp.Remove(temp.Length-1, 1);
                    }
                    else if(type == 6){
                        temp = "battery";
                    }
                }

                // Randomly pick a player
                rand = Random.Range(0,tempCharacters.Count());
                name = tempCharacters[rand].CharacterName;
                msg = name + " finds " + gain + " " + temp + " in an abandoned car.";
            }
            // 5/44 possibility to find a new party member (ex. The party meets Bob. They have the Perk surgeon and Trait paranoid.)
            else if(eventChance <= 18){
                // Check that a slot is available.
                List<int> customIds = new List<int>();
                for(int i = 0; i < tempCharacters.Count(); i++){
                    customIds.Add(tempCharacters[i].CustomCharacterId);
                }

                if(tempCharacters.Count() < 4){
                    int perk = -1, trait = -1, acc = -1, outfit = -1, color = -1, hat = -1, idRead = -1;
                    string name = "", perkRoll = "", traitRoll = "";

                    int perishedCount = DataUser.dataManager.GetPerishedCustomCharacters().Count(), customCount = DataUser.dataManager.GetCustomCharacters().Count();
                    // Generate randomized character - standard or out of unused custom characters because they are all either in the party or dead
                    if(diff == 1 || diff == 3 || customCount == customIds.Where(c => c != -1).Count() + perishedCount){
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
                    // Generate random character
                    else{
                        int rand = -1;

                        do
                        {
                            rand = Random.Range(0, customCount);
                        } while (customIds.Contains(rand));

                        CustomCharacter cc = DataUser.dataManager.GetCustomCharacterById(rand);
                        perk = cc.Perk;
                        trait = cc.Trait;
                        acc = cc.Acessory;
                        outfit = cc.Outfit;
                        color = cc.Color;
                        hat = cc.Hat;
                        idRead = cc.Id;
                        name = cc.CharacterName;
                        perkRoll = GamemodeSelect.Perks[perk];
                        traitRoll = GamemodeSelect.Traits[trait];
                    }

                    string commandText = "INSERT OR REPLACE INTO ActiveCharacter(CharacterName, Perk, Trait, Outfit, Accessory, Color, Hat, IsLeader, Health, Morale, CustomCharacterId) " + 
                                         "VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)";
                    List<object> parameters = new List<object>(){name, perk, trait, outfit, acc, color, hat, 0, 100, 75, idRead, GameLoop.FileId};
                    TravelLoop.queriesToPerform.Add(commandText);
                    TravelLoop.parametersForQueries.Add(parameters);

                    if(perk == 2){
                        commandText = "UPDATE SaveFilesTable SET medkit = ? WHERE Id = ?";
                        parameters = new List<object>(){save.Medkit + 1, GameLoop.FileId};
                        TravelLoop.queriesToPerform.Add(commandText);
                        TravelLoop.parametersForQueries.Add(parameters);
                    }
                    msg = "The party meets " + name + " and allows them to join.\nThey have the " + perkRoll + " perk and the " + traitRoll + " trait.";
                }
                else{
                    msg = "You drive by someone on the road but your car is full.";
                }
            }
            // 1/44 possibility for an upgrade to be found. (ex. The party searches an abandoned car and finds nothing of interest.)
            else if(eventChance <= 19){
                // Check that a slot is available.
                List<int> curUpgrades = new List<int>(){car.WheelUpgrade, car.BatteryUpgrade, car.EngineUpgrade, car.ToolUpgrade, car.MiscUpgrade1, car.MiscUpgrade2};
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
                    commandTemp = selected == 0 ? "wheelUpgrade = ? " : selected == 1 ? "batteryUpgrade = ? " : selected == 2 ? "engineUpgrade = ? " : selected == 3 ? "toolUpgrade == ? " :
                                    selected == 4 ? "miscUpgrade1 = ? " : "miscUpgrade2 = ? ";

                    string commandText = "UPDATE CarsTable SET " + commandTemp + " WHERE Id = ?";
                    List<object> parameters = new List<object>(){1, GameLoop.FileId};
                    TravelLoop.queriesToPerform.Add(commandText);
                    TravelLoop.parametersForQueries.Add(parameters);

                    msg = "The party searches an abandoned car and finds " + found + ".";

                }
                // No slot available
                else{
                    msg = "The party searches an abandoned car and finds nothing of interest.";
                }
            }
            // 2/44 possibility for party-wide damage. (ex. The party cannot find clean water. Everyone is dehydrated.)
            else if(eventChance <= 21){
                int hpLoss = diff % 2 == 0 ? Random.Range(10,15) : Random.Range(5,10);

                List<object> parameters = new List<object>();
                foreach(ActiveCharacter character in characters){
                    string commandText = "UPDATE ActiveCharacter SET Health = ? WHERE Id = ?";
                    TravelLoop.queriesToPerform.Add(commandText);
                    parameters = new List<object>();
                    int newHp = character.Health - hpLoss > 0 ? character.Health - hpLoss : 0;
                    parameters.Add(character.Health);
                    parameters.Add(character.Id);
                }

                msg = "The party cannot find clean water. Everyone is dehydrated.";
            }
            // 3/44 possibility for a tire to go flat
            else if(eventChance <= 24){
                List<object> parameters = new List<object>();

                // If the car has upgraded tires, display the attempt at popping the tire.
                if(car.WheelUpgrade != 0){
                    msg = "The car goes over some rough terrain but the durable tires remain intact.";
                }
                else{
                    int tires = save.Tire;

                    // Determine if the car can still move.
                    if(tires > 0){
                        tires--;
                        string commandText = "UPDATE SaveFilesTable SET tire = ? WHERE Id = ?";
                        parameters.Add(tires);
                        parameters.Add(GameLoop.FileId);
                        TravelLoop.queriesToPerform.Add(commandText);
                        TravelLoop.parametersForQueries.Add(parameters);
                        msg = "The car goes over some rough terrain and the tire pops.\nYou replace your flat tire.";
                    }
                    else{
                        string commandText = "UPDATE CarsTable SET isTireFlat = ? WHERE Id = ?";
                        parameters.Add(1);
                        parameters.Add(GameLoop.FileId);
                        TravelLoop.queriesToPerform.Add(commandText);
                        TravelLoop.parametersForQueries.Add(parameters);
                        msg = "The car goes over some rough terrain and the tire pops.\nYou don't have a tire to replace.\nTrade for another one.";
                    }
                }
            }
            // 3/44 possibility for a car battery to die.
            else if(eventChance <= 27){
                List<object> parameters = new List<object>();

                // If the car has upgraded battery, display the attempt at breaking.
                if(car.BatteryUpgrade != 0){
                    msg = "The car battery starts making noises but go away after some time.";
                }
                else{
                    int batteries = save.Battery;
            
                    // Determine if the car can still move.
                    if(batteries > 0){
                        batteries--;
                        string commandText = "UPDATE SaveFilesTable SET battery = ? WHERE Id = ?";
                        parameters.Add(batteries);
                        parameters.Add(GameLoop.FileId);
                        TravelLoop.queriesToPerform.Add(commandText);
                        TravelLoop.parametersForQueries.Add(parameters);
                        msg = "There is smoke coming from the hood - the car battery is dead.\nYou replace your dead battery.";
                    }
                    else{
                        string commandText = "UPDATE CarsTable SET isBatteryDead = ? WHERE Id = ?";
                        parameters.Add(1);
                        parameters.Add(GameLoop.FileId);
                        TravelLoop.queriesToPerform.Add(commandText);
                        TravelLoop.parametersForQueries.Add(parameters);
                        msg = "There is smoke coming from the hood - the car battery is dead.\nYou don't have a battery to replace.\nTrade for another one.";
                    }
                }
            }
            // 3/44 possibility for someone (other than the leader) with low morale to ditch. Cases where morale is high, treat as a typical drive with no evet
            else if(eventChance <= 30){
                tempCharacters = characters.Where<ActiveCharacter>(c=>c.IsLeader == 0).ToArray<ActiveCharacter>();
                List<object> parameters = new List<object>();

                List<int> morale = new List<int>();
                foreach(ActiveCharacter character in tempCharacters){
                    morale.Add(character.Morale);
                }

                int lowMorale = morale.Where(m => m >= 0 && m <= 20).Count();
                if(lowMorale > 0){
                    ActiveCharacter lowestMorale = tempCharacters.OrderBy(c=>c.Morale).First();
                    string commandText = "UPDATE ActiveCharactersTable SET CharacterName = ? WHERE Id = ?";
                    parameters.Add(null);
                    parameters.Add(lowestMorale.Id);
                    TravelLoop.queriesToPerform.Add(commandText);
                    TravelLoop.parametersForQueries.Add(parameters);
                    msg = "In despair, " + lowestMorale.CharacterName + " ditches the party, saying their chances are better without the party.";
                }
                else{
                    return "";
                }
            }
            
            // 2/44 possibility for musician characters to raise party morale (ex. Bob serenades the party, reminding them of better times. The party is in high spirits.)
            else if(eventChance <= 32 && availablePerks.Where(p => p == 5).Count() > 0){
                // Get the name of the member who has the musician trait
                int nameIndex = availablePerks.IndexOf(5), moraleGain = diff % 2 == 0 ? 5 : 10;;
                List<int> partyMorale = new List<int>();
                string name = tempCharacters[nameIndex].CharacterName;

                // Raise for living characters
                foreach(ActiveCharacter character in characters){
                    string commandText = "UPDATE ActiveCharactersTable SET Morale = ? WHERE FileId = ?";
                    TravelLoop.queriesToPerform.Add(commandText);
                    List<object> parameters = new List<object>(){character.Morale + moraleGain, GameLoop.FileId};
                    TravelLoop.parametersForQueries.Add(parameters);                
                }

                msg = name + " serenades the party with a guitar, reminding them of better times.\nThe party is in high spirits.";
            }
            // 2/44 possibility for bandits to lower party morale (ex. Bob attempts to rob a helpless group but is caught and drags the party with him. The party feels guilty.)
            else if(eventChance <= 34 && availableTraits.Where(t => t == 3).Count() > 0){
                // Get the name of the member who has the bandit trait
                int nameIndex = availableTraits.IndexOf(3), moraleLoss = diff % 2 == 0 ? 10 : 5;
                List<int> partyMorale = new List<int>();
                string name = tempCharacters[nameIndex].CharacterName;

                // Lower for living characters
                foreach(ActiveCharacter character in characters){
                    string commandText = "UPDATE ActiveCharactersTable SET Morale = ? WHERE FileId = ?";
                    TravelLoop.queriesToPerform.Add(commandText);
                    List<object> parameters = new List<object>(){character.Morale - moraleLoss, GameLoop.FileId};
                    TravelLoop.parametersForQueries.Add(parameters);                
                }
                msg = name + " attempts to rob a helpless group but is caught and drags the party with them.\nThe party is forced to flee and feels guilty.";
            } 
            // 2/44 possibility for hot headed characters to lower another character's hp. (ex. Bob, annoyed with Ann for a minor issue, lashes out mid-argument.)
            else if(eventChance <= 36 && availableTraits.Where(t => t == 4).Count() > 0 && tempCharacters.Count() > 1){
                // Get the name of the first member who has the hot-headed trait
                int nameIndex = availableTraits.IndexOf(4), hurtMember = 0;
                ActiveCharacter attacker = tempCharacters[nameIndex];

                tempCharacters.ToList<ActiveCharacter>().RemoveAt(nameIndex);
                hurtMember = Random.Range(0, tempCharacters.Length);
                ActiveCharacter hurtCharacter = tempCharacters[hurtMember];

                string name = attacker.CharacterName, hurtName = hurtCharacter.CharacterName;
                int hpLoss = diff % 2 == 0 ? 10 : 5, hurtHP = hurtCharacter.Health - hpLoss > 0 ? hurtCharacter.Health - hpLoss : 0;

                string commandText = "UPDATE ActiveCharactersTable SET Health = ? WHERE Id = ?";
                TravelLoop.queriesToPerform.Add(commandText);
                List<object> parameters = new List<object>(){hurtHP, hurtCharacter.Id};
                TravelLoop.parametersForQueries.Add(parameters); 
                msg = name + ", annoyed with " + hurtName + " for a minor issue, lashes out mid-argument.";
            }
            // 2/44 possibility for surgeon characters to fully heal an injured character (ex. Bob's medical skills come in handy for mid-drive surgery on Ann)
            else if(eventChance <= 38 && availablePerks.Where(p => p == 3).Count() > 0 && tempCharacters.Count() > 1){
                // Get the name of the first member who has the surgeon trait
                int nameIndex = availablePerks.IndexOf(3), healMember = 0;
                ActiveCharacter surgeon = tempCharacters[nameIndex];

                tempCharacters.ToList<ActiveCharacter>().RemoveAt(nameIndex);
                healMember = Random.Range(0, tempCharacters.Length);
                ActiveCharacter healed = tempCharacters[healMember];

                string name = surgeon.CharacterName, healName = healed.CharacterName;
                int hpGain = diff % 2 == 0 ? 5 : 10, healHP = healed.Health + hpGain > 100 ? 100 : healed.Health + hpGain;
                string commandText = "UPDATE ActiveCharactersTable SET Health = ? WHERE Id = ?";
                TravelLoop.queriesToPerform.Add(commandText);
                List<object> parameters = new List<object>(){healHP, healed.Id};
                TravelLoop.parametersForQueries.Add(parameters); 

                msg = name + "'s medical skills come in handy using medicinal herbs to treat " + healName + ".";
            } 
            // 2/44 possibility for creative/programmer characters to act (ex. Bob has a creative solution for a car upgrade and succeeds/fails.)
            // Uses an extra roll to determine positive/negative.
            else if(eventChance <= 40 && (availableTraits.Where(t => t == 5).Count() > 0 || availablePerks.Where(p => p == 4).Count() > 0)){
                // Get the name of the first member who has the creative OR programmer trait
                int nameIndex = availableTraits.Where(t => t == 5).Count() > 0 ? availableTraits.IndexOf(5) : availablePerks.IndexOf(4);
                string solType = availableTraits.Where(t => t == 5).Count() > 0 ? "creative" : "systematic and thought-out";
                string name = tempCharacters[nameIndex].CharacterName;

                // Check that a slot is available.
                List<int> curUpgrades = new List<int>(){car.WheelUpgrade, car.BatteryUpgrade, car.EngineUpgrade, car.ToolUpgrade, car.MiscUpgrade1, car.MiscUpgrade2};

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

                    commandTemp = selected == 0 ? "wheelUpgrade = ? " : selected == 1 ? "batteryUpgrade = ?" : selected == 2 ? "engineUpgrade = ?" : selected == 3 ? "toolUpgrade == ?" :
                                selected == 4 ? "miscUpgrade1 = ?" : "miscUpgrade2 = ?";

                    string commandText = "UPDATE CarsTable SET " + commandTemp + " WHERE Id = ?";
                    List<object> parameters = new List<object>(){1, GameLoop.FileId};
                    TravelLoop.queriesToPerform.Add(commandText);
                    TravelLoop.parametersForQueries.Add(parameters);
                    msg = name + " has a " + solType + " solution for a car upgrade and succeeds.";
                }
                else{
                    return "";
                }
            }   
            // 2/44 possibility for a combat event to occur if travelling with higher or more activity
            else if(eventChance <= 42 && GameLoop.Activity >= 3){
                msg = "You suddenly find yourself surrounded by mutants.";
            }
            // 2/44 possibility for someone to be pulled out of the car and left for dead if travelling with ravenous activity
            // Morale will determine if member fights them off.
            else if(eventChance <= 44 && GameLoop.Activity == 4){
                List<int> morale = new List<int>();
                int selected;
                tempCharacters = tempCharacters.Where<ActiveCharacter>(c=>c.IsLeader == 0).ToArray<ActiveCharacter>();

                foreach(ActiveCharacter character in tempCharacters){
                    morale.Add(character.Morale);
                }

                // Select a living party member to attack
                selected = Random.Range(0, tempCharacters.Count());
                ActiveCharacter victim = tempCharacters[selected];
                string name = victim.CharacterName, commandText = "DELETE FROM ActiveCharactersTable WHERE Id = ?";

                if(victim.Morale < 40){
                    List<object> parameters = new List<object>(){victim.Id};
                    TravelLoop.queriesToPerform.Add(commandText);
                    TravelLoop.parametersForQueries.Add(parameters);                    
                }
                else{
                    msg = "Mutants attempt to pull " + victim.CharacterName + " out of the car, but fail to do so.";
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