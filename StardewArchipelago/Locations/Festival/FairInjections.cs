﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using StardewArchipelago.Archipelago;
using StardewArchipelago.Locations.Events;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using Object = StardewValley.Object;

namespace StardewArchipelago.Locations.Festival
{
    internal class FairInjections
    {
        private static IMonitor _monitor;
        private static IModHelper _modHelper;
        private static ArchipelagoClient _archipelago;
        private static LocationChecker _locationChecker;
        private static ShopReplacer _shopReplacer;

        public static void Initialize(IMonitor monitor, IModHelper modHelper, ArchipelagoClient archipelago, LocationChecker locationChecker, ShopReplacer shopReplacer)
        {
            _monitor = monitor;
            _modHelper = modHelper;
            _archipelago = archipelago;
            _locationChecker = locationChecker;
            _shopReplacer = shopReplacer;
        }

        // public override void update(GameTime time)
        public static bool StrengthGameUpdate_StrongEnough_Prefix(StrengthGame __instance, GameTime time)
        {
            try
            {
                var changeSpeedField = _modHelper.Reflection.GetField<float>(__instance, "changeSpeed");
                var changeSpeed = changeSpeedField.GetValue();
                if (changeSpeed != 0.0)
                {
                    return true; // run original logic
                }

                var powerField = _modHelper.Reflection.GetField<float>(__instance, "power");
                var power = powerField.GetValue();
                if (power >= 99.0 || (power < 2.0 && _archipelago.SlotData.FestivalObjectives != FestivalObjectives.Difficult))
                {
                    _locationChecker.AddCheckedLocation(FestivalLocationNames.STRENGTH_GAME);
                }
                return true; // run original logic
            }
            catch (Exception ex)
            {
                _monitor.Log($"Failed in {nameof(StrengthGameUpdate_StrongEnough_Prefix)}:\n{ex}", LogLevel.Error);
                return true; // run original logic
            }
        }

        // public void interpretGrangeResults()
        public static void InterpretGrangeResults_Success_Postfix(Event __instance)
        {
            try
            {
                var isEasyMode = _archipelago.SlotData.FestivalObjectives != FestivalObjectives.Difficult;
                if (__instance.grangeScore >= 90 || ((__instance.grangeScore >= 60 || __instance.grangeScore == -666) && isEasyMode))
                {
                    _locationChecker.AddCheckedLocation(FestivalLocationNames.GRANGE_DISPLAY);
                }

                return;
            }
            catch (Exception ex)
            {
                _monitor.Log($"Failed in {nameof(InterpretGrangeResults_Success_Postfix)}:\n{ex}", LogLevel.Error);
                return;
            }
        }

        // public ShopMenu(Dictionary<ISalable, int[]> itemPriceAndStock, int currency = 0, string who = null, Func<ISalable, Farmer, int, bool> on_purchase = null, Func<ISalable, bool> on_sell = null, string context = null)
        public static bool ShopMenu_HandleFairItems_Prefix(ShopMenu __instance, ref Dictionary<ISalable, int[]> itemPriceAndStock, int currency = 0, string who = null, Func<ISalable, Farmer, int, bool> on_purchase = null, Func<ISalable, bool> on_sell = null, string context = null)
        {
            try
            {
                if (currency != 1)
                {
                    return true; //  run original logic
                }

                foreach (var salableItem in itemPriceAndStock.Keys.ToArray())
                {
                    _shopReplacer.ReplaceShopItem(itemPriceAndStock, salableItem, FestivalLocationNames.RARECROW_1, item => item.IsScarecrow() && item.Name != "Rarecrow #1", 800);
                    _shopReplacer.ReplaceShopItem(itemPriceAndStock, salableItem, FestivalLocationNames.FAIR_STARDROP, item => item.ParentSheetIndex == 434, 2000);
                }
                return true; //  run original logic
            }
            catch (Exception ex)
            {
                _monitor.Log($"Failed in {nameof(ShopMenu_HandleFairItems_Prefix)}:\n{ex}", LogLevel.Error);
                return true; // run original logic
            }
        }
    }
}
