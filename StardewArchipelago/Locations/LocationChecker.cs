﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using StardewArchipelago.Archipelago;
using StardewArchipelago.Goals;
using StardewModdingAPI;

namespace StardewArchipelago.Locations
{
    public class LocationChecker
    {
        private static IMonitor _monitor;
        private ArchipelagoClient _archipelago;
        private Dictionary<string, long> _checkedLocations;
        private Dictionary<string, string[]> _wordFilterCache;

        public LocationChecker(IMonitor monitor, ArchipelagoClient archipelago, List<string> locationsAlreadyChecked)
        {
            _monitor = monitor;
            _archipelago = archipelago;
            _checkedLocations = locationsAlreadyChecked.ToDictionary(x => x, x => (long)-1);
            _wordFilterCache = new Dictionary<string, string[]>();
        }

        public List<string> GetAllLocationsAlreadyChecked()
        {
            return _checkedLocations.Keys.ToList();
        }

        public bool IsLocationChecked(string locationName)
        {
            return _checkedLocations.ContainsKey(locationName);
        }

        public bool IsLocationNotChecked(string locationName)
        {
            return !IsLocationChecked(locationName);
        }

        public bool IsLocationMissingAndExists(string locationName)
        {
            return _archipelago.LocationExists(locationName) && IsLocationNotChecked(locationName);
        }

        public IReadOnlyCollection<long> GetAllMissingLocations()
        {
            return _archipelago.GetAllMissingLocations();
        }
        
        public void AddCheckedLocation(string locationName)
        {
            if (_checkedLocations.ContainsKey(locationName))
            {
                return;
            }

            var locationId = _archipelago.GetLocationId(locationName);

            if (locationId == -1)
            {
                _monitor.Log($"Location \"{locationName}\" could not be converted to an Archipelago id", LogLevel.Error);
            }

            _checkedLocations.Add(locationName, locationId);
            _wordFilterCache.Clear();
            SendAllLocationChecks();
            GoalCodeInjection.CheckAllsanityGoalCompletion();
        }

        public void SendAllLocationChecks()
        {
            if (!_archipelago.IsConnected)
            {
                return;
            }

            TryToIdentifyUnknownLocationNames();

            var allCheckedLocations = new List<long>();
            allCheckedLocations.AddRange(_checkedLocations.Values);

            allCheckedLocations = allCheckedLocations.Distinct().Where(x => x > -1).ToList();

            _archipelago.ReportCheckedLocations(allCheckedLocations.ToArray());
        }

        public void VerifyNewLocationChecksWithArchipelago()
        {
            var allCheckedLocations = _archipelago.GetAllCheckedLocations();
            foreach (var (locationName, locationId) in allCheckedLocations)
            {
                if (!_checkedLocations.ContainsKey(locationName))
                {
                    _checkedLocations.Add(locationName, locationId);
                    _wordFilterCache.Clear();
                }
            }
        }

        private void TryToIdentifyUnknownLocationNames()
        {
            foreach (var locationName in _checkedLocations.Keys)
            {
                if (_checkedLocations[locationName] > -1)
                {
                    continue;
                }

                var locationId = _archipelago.GetLocationId(locationName);
                if (locationId == -1)
                {
                    continue;
                }

                _checkedLocations[locationName] = locationId;
            }
        }

        public void ForgetLocations(IEnumerable<string> locations)
        {
            foreach (var location in locations)
            {
                if (!_checkedLocations.ContainsKey(location))
                {
                    continue;
                }

                _checkedLocations.Remove(location);
                _wordFilterCache.Clear();
            }
        }

        public IEnumerable<string> GetAllLocationsNotChecked()
        {
            if (!_archipelago.IsConnected)
            {
                return Enumerable.Empty<string>();
            }

            return _archipelago.Session.Locations.AllMissingLocations.Select(_archipelago.GetLocationName);
        }

        public IEnumerable<string> GetAllLocationsNotChecked(string filter)
        {
            return GetAllLocationsNotChecked().Where(x => x.Contains(filter, StringComparison.InvariantCultureIgnoreCase));
        }

        public string[] GetAllLocationsNotCheckedContainingWord(string wordFilter)
        {
            if (_wordFilterCache.ContainsKey(wordFilter))
            {
                return _wordFilterCache[wordFilter];
            }

            var filteredLocations = FilterLocationsForWord(GetAllLocationsNotChecked(wordFilter), wordFilter).ToArray();
            _wordFilterCache.Add(wordFilter, filteredLocations);
            return filteredLocations;
        }

        public bool IsAnyLocationNotChecked(string filter)
        {
            return GetAllLocationsNotChecked(filter).Any();
        }

        private static IEnumerable<string> FilterLocationsForWord(IEnumerable<string> locations, string filterWord)
        {
            foreach (var location in locations)
            {
                if (ItemIsRelevant(filterWord, location))
                {
                    yield return location;
                }
            }
        }

        private static bool ItemIsRelevant(string itemName, string locationName)
        {
            var startOfItemName = locationName.IndexOf(itemName, StringComparison.InvariantCultureIgnoreCase);
            if (startOfItemName == -1)
            {
                return false;
            }

            var charBefore = startOfItemName == 0 ? ' ' : locationName[startOfItemName - 1];
            var charAfter = locationName.Length <= startOfItemName + itemName.Length ? ' ' : locationName[startOfItemName + itemName.Length];
            return charBefore == ' ' && charAfter == ' ';
        }
    }
}
