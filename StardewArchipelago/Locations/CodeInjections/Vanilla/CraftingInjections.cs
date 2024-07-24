﻿using System;
using StardewArchipelago.Archipelago;
using StardewArchipelago.Constants.Modded;
using StardewArchipelago.Stardew;
using StardewArchipelago.Stardew.NameMapping;
using StardewModdingAPI;
using StardewValley;
using EventIds = StardewArchipelago.Stardew.Ids.Vanilla.EventIds;

namespace StardewArchipelago.Locations.CodeInjections.Vanilla
{
    public static class CraftingInjections
    {
        public const string CRAFTING_LOCATION_PREFIX = "Craft ";

        private static IMonitor _monitor;
        private static IModHelper _helper;
        private static ArchipelagoClient _archipelago;
        private static LocationChecker _locationChecker;
        private static StardewItemManager _stardewItemManager;
        private static CompoundNameMapper _nameMapper;

        public static void Initialize(IMonitor monitor, IModHelper helper, ArchipelagoClient archipelago, StardewItemManager stardewItemManager, LocationChecker locationChecker)
        {
            _monitor = monitor;
            _helper = helper;
            _archipelago = archipelago;
            _stardewItemManager = stardewItemManager;
            _locationChecker = locationChecker;
            _nameMapper = new CompoundNameMapper(archipelago.SlotData);
        }

        // public void checkForCraftingAchievements()
        public static void CheckForCraftingAchievements_CheckCraftsanityLocation_Postfix(Stats __instance)
        {
            try
            {
                var craftedRecipes = Game1.player.craftingRecipes;
                foreach (var recipeId in craftedRecipes.Keys)
                {
                    if (craftedRecipes[recipeId] <= 0)
                    {
                        continue;
                    }

                    if (!TryGetExistingCraftsanityLocationName(recipeId, out var location))
                    {
                        continue;
                    }

                    _locationChecker.AddCheckedLocation(location);
                }

                return;
            }
            catch (Exception ex)
            {
                _monitor.Log($"Failed in {nameof(CheckForCraftingAchievements_CheckCraftsanityLocation_Postfix)}:\n{ex}", LogLevel.Error);
                return;
            }
        }

        private static bool TryGetExistingCraftsanityLocationName(string recipeId, out string locationName)
        {
            locationName = $"{CRAFTING_LOCATION_PREFIX}{recipeId}";
            if (_archipelago.LocationExists(locationName))
            {
                return true;
            }

            var itemName = _nameMapper.GetItemName(recipeId); // Some names are iffy
            locationName = $"{CRAFTING_LOCATION_PREFIX}{itemName}";
            if (_archipelago.LocationExists(locationName))
            {
                return true;
            }

            var recipe = _stardewItemManager.GetRecipeByName(recipeId);
            var yieldItemName = recipe.YieldItem.Name;
            locationName = $"{CRAFTING_LOCATION_PREFIX}{yieldItemName}";
            if (_archipelago.LocationExists(locationName))
            {
                return true;
            }

            if (IgnoredModdedStrings.Craftables.Contains(recipeId) ||
                IgnoredModdedStrings.Craftables.Contains(itemName) ||
                IgnoredModdedStrings.Craftables.Contains(yieldItemName))
            {
                return false;
            }

            _monitor.Log($"Tried to check Craftsanity locationName for recipe {recipeId}, but could not find it", LogLevel.Warn);
            return false;
        }

        // public static void AddCraftingRecipe(Event @event, string[] args, EventContext context)
        public static bool AddCraftingRecipe_SkipLearningFurnace_Prefix(Event @event, string[] args, EventContext context)
        {
            try
            {
                if (!@event.eventCommands[@event.CurrentCommand].Contains("Furnace"))
                {
                    return true; // run original logic
                }

                ++@event.CurrentCommand;
                return false; // don't run original logic
            }
            catch (Exception ex)
            {
                _monitor.Log($"Failed in {nameof(AddCraftingRecipe_SkipLearningFurnace_Prefix)}:\n{ex}", LogLevel.Error);
                return true; // run original logic
            }
        }

        // public void skipEvent()
        public static bool SkipEvent_FurnaceRecipe_Prefix(Event __instance)
        {
            try
            {
                if (__instance.id != EventIds.FURNACE_RECIPE)
                {
                    return true; // run original logic
                }
                
                EventInjections.BaseSkipEvent(__instance, () => Game1.player.addQuest("11"));

                return false; // don't run original logic
            }
            catch (Exception ex)
            {
                _monitor.Log($"Failed in {nameof(SkipEvent_FurnaceRecipe_Prefix)}:\n{ex}", LogLevel.Error);
                return true; // run original logic
            }
        }
    }
}
