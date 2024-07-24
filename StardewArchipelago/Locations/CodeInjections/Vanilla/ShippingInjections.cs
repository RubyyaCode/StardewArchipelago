﻿using System;
using System.Collections.Generic;
using System.Linq;
using StardewArchipelago.Archipelago;
using StardewArchipelago.Constants.Modded;
using StardewArchipelago.Stardew.NameMapping;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Objects;

namespace StardewArchipelago.Locations.CodeInjections.Vanilla
{
    public class NightShippingBehaviors
    {
        public const string SHIPSANITY_PREFIX = "Shipsanity: ";

        private IMonitor _monitor;
        private ArchipelagoClient _archipelago;
        private LocationChecker _locationChecker;
        private NameSimplifier _nameSimplifier;
        private CompoundNameMapper _nameMapper;

        public NightShippingBehaviors(IMonitor monitor, ArchipelagoClient archipelago, LocationChecker locationChecker, NameSimplifier nameSimplifier)
        {
            _monitor = monitor;
            _archipelago = archipelago;
            _locationChecker = locationChecker;
            _nameSimplifier = nameSimplifier;
            _nameMapper = new CompoundNameMapper(archipelago.SlotData);
        }

        // private static IEnumerator<int> _newDayAfterFade()
        public void CheckShipsanityLocationsBeforeSleep()
        {
            try
            {
                if (_archipelago.SlotData.Shipsanity == Shipsanity.None)
                {
                    return;
                }

                _monitor.Log($"Currently attempting to check shipsanity locations for the current day", LogLevel.Info);
                var allShippedItems = GetAllItemsShippedToday();
                _monitor.Log($"{allShippedItems.Count} items shipped", LogLevel.Info);
                CheckAllShipsanityLocations(allShippedItems);
            }
            catch (Exception ex)
            {
                _monitor.Log($"Failed in {nameof(CheckShipsanityLocationsBeforeSleep)}:\n{ex}", LogLevel.Error);
            }
        }

        private static List<Item> GetAllItemsShippedToday()
        {
            var allShippedItems = new List<Item>();
            allShippedItems.AddRange(Game1.getFarm().getShippingBin(Game1.player));
            foreach (var gameLocation in GetAllGameLocations())
            {
                foreach (var locationObject in gameLocation.Objects.Values)
                {
                    if (locationObject is not Chest { SpecialChestType: Chest.SpecialChestTypes.MiniShippingBin } chest)
                    {
                        continue;
                    }

                    allShippedItems.AddRange(chest.Items);
                }
            }

            return allShippedItems;
        }

        private static IEnumerable<GameLocation> GetAllGameLocations()
        {
            foreach (var location in Game1.locations)
            {
                yield return location;
                if (!location.IsBuildableLocation())
                {
                    continue;
                }

                foreach (var building in location.buildings.Where(building => building.indoors.Value != null))
                {
                    yield return building.indoors.Value;
                }
            }
        }

        private void CheckAllShipsanityLocations(List<Item> allShippedItems)
        {
            foreach (var shippedItem in allShippedItems)
            {
                var name = _nameSimplifier.GetSimplifiedName(shippedItem);
                name = _nameMapper.GetEnglishName(name);  // For the Name vs Display Name discrepencies in Mods.
                if (IgnoredModdedStrings.Shipments.Contains(name))
                {
                    continue;
                }
                var apLocation = $"{SHIPSANITY_PREFIX}{name}";
                if (_archipelago.GetLocationId(apLocation) > -1)
                {
                    _locationChecker.AddCheckedLocation(apLocation);
                }
                else
                {    
                    var wasSuccessful = DoBugsCleanup(shippedItem);
                    if (wasSuccessful)
                    {
                        continue;
                    }
                    _monitor.Log($"Unrecognized Shipsanity Location: {name} [{shippedItem.ParentSheetIndex}]", LogLevel.Error);
                }
            }
        }

        private bool DoBugsCleanup(Item shippedItem)
        {
            // In the beta async, backend names for SVE shippables are the internal names.  This fixes the mistake ONLY for that beta async.  Remove after it.
            var name = _nameSimplifier.GetSimplifiedName(shippedItem);
            var sveMappedItems = new List<string>() {"Smelly Rafflesia", "Bearberrys", "Big Conch", "Dried Sand Dollar", "Lucky Four Leaf Clover", "Ancient Ferns Seed"};
            if (sveMappedItems.Contains(name))
            {
                var apLocation = $"{SHIPSANITY_PREFIX}{name}";
                if (_archipelago.GetLocationId(apLocation) > -1)
                {
                    _monitor.Log($"Bugfix caught this for the beta async.  If this isn't that game, let the developers know there's a bug!", LogLevel.Warn);
                    _locationChecker.AddCheckedLocation(apLocation);
                    return true;
                }
            }
            return false;
        }
    }
}