﻿using System;
using System.Linq;
using System.Collections.Generic;
using BetterJunimos.Utils;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Characters;
using StardewValley.TerrainFeatures;
using SObject = StardewValley.Object;

namespace BetterJunimos.Abilities {
    public class FertilizeAbility : IJunimoAbility {
        private List<int> _RequiredItems;
        
        public string AbilityName() {
            return "Fertilize";
        }

        public bool IsActionAvailable(GameLocation location, Vector2 pos, Guid guid) {
            if (!location.terrainFeatures.ContainsKey(pos)) return false;
            if (location.terrainFeatures[pos] is not HoeDirt hd) return false;
            if (hd.fertilizer.Value > 0) return false;
            if (hd.crop is null) return true;

            // now we allow fertilizing just-planted crops
            return hd.crop.currentPhase.Value <= 1;
        }

        public bool PerformAction(GameLocation location, Vector2 pos, JunimoHarvester junimo, Guid guid) {
            var chest = Util.GetHutFromId(guid).output.Value;
            var foundItem = chest.items.FirstOrDefault(item => item is {Category: SObject.fertilizerCategory});
            if (foundItem == null) return false;

            Fertilize(location, pos, foundItem.ParentSheetIndex);
            Util.RemoveItemFromChest(chest, foundItem);
            return true;
        }

        public List<int> RequiredItems() {
            // this is heavy, cache it
            if (_RequiredItems is not null) return _RequiredItems;
            var fertilizers = Game1.objectInformation
                    .Where(pair => pair.Value.Split('/')[3].EndsWith(StardewValley.Object.fertilizerCategory.ToString()))
                    .Where(pair => int.Parse(pair.Value.Split('/')[3].Split(' ').Last()) == StardewValley.Object.fertilizerCategory)
                    .Where(pair => pair.Value.Split('/')[0] != "Tree Fertilizer")
                ;
            // BetterJunimos.SMonitor.Log("RequiredItems called for Fertilize", LogLevel.Debug);
            _RequiredItems = (from kvp in fertilizers select kvp.Key).ToList();

            return _RequiredItems;
        }

        private static void Fertilize(GameLocation location, Vector2 pos, int index) {
            if (location.terrainFeatures[pos] is not HoeDirt hd) return;
            // BetterJunimos.SMonitor.Log($"Fertilize < {pos} {index} {hd.fertilizer.Value}", LogLevel.Debug);
            hd.fertilizer.Value = index;
            CheckSpeedGro(hd, hd.crop);
            if (Utility.isOnScreen(Utility.Vector2ToPoint(pos), 64, location)) {
                location.playSound("dirtyHit");
            }
            // BetterJunimos.SMonitor.Log($"Fertilize > {pos} {index} {hd.fertilizer.Value}", LogLevel.Debug);
        }

        // taken from SDV planting code [applySpeedIncreases()], updated for 1.5
        private static void CheckSpeedGro(HoeDirt hd, Crop crop) {
            var fertilizer = hd.fertilizer.Value;
            var who = Game1.player;

            if (crop == null) {
                return;
            }

            if (!(fertilizer is 465 or 466 or 918 || who.professions.Contains(5))) {
                return;
            }

            crop.ResetPhaseDays();
            var totalDaysOfCropGrowth = 0;
            for (var j = 0; j < crop.phaseDays.Count - 1; j++) {
                totalDaysOfCropGrowth += crop.phaseDays[j];
            }

            var speedIncrease = 0f;
            switch (fertilizer) {
                case 465:
                    speedIncrease += 0.1f;
                    break;
                case 466:
                    speedIncrease += 0.25f;
                    break;
                case 918:
                    speedIncrease += 0.33f;
                    break;
            }

            if (who.professions.Contains(5)) {
                speedIncrease += 0.1f;
            }

            var daysToRemove = (int) Math.Ceiling(totalDaysOfCropGrowth * speedIncrease);
            var tries = 0;
            while (daysToRemove > 0 && tries < 3) {
                for (var i = 0; i < crop.phaseDays.Count; i++) {
                    if ((i > 0 || crop.phaseDays[i] > 1) && crop.phaseDays[i] != 99999) {
                        crop.phaseDays[i]--;
                        daysToRemove--;
                    }

                    if (daysToRemove <= 0) {
                        break;
                    }
                }

                tries++;
            }
        }

        /* older API compat */
        public bool IsActionAvailable(Farm farm, Vector2 pos, Guid guid) {
            return IsActionAvailable((GameLocation) farm, pos, guid);
        }
        
        public bool PerformAction(Farm farm, Vector2 pos, JunimoHarvester junimo, Guid guid) {
            return PerformAction((GameLocation) farm, pos, junimo, guid);
        }
    }
}