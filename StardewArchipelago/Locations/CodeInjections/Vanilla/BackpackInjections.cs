﻿using System;
using StardewArchipelago.Archipelago;
using HarmonyLib;
using StardewArchipelago.Constants;
using StardewModdingAPI;
using StardewValley;
using xTile.Dimensions;

namespace StardewArchipelago.Locations.CodeInjections.Vanilla
{
    public static class BackpackInjections
    {
        private const string LARGE_PACK = "Large Pack";
        private const string DELUXE_PACK = "Deluxe Pack";
        private const string PREMIUM_PACK = "Premium Pack";

        private static IMonitor _monitor;
        private static ArchipelagoClient _archipelago;
        private static LocationChecker _locationChecker;

        public static void Initialize(IMonitor monitor, ArchipelagoClient archipelago, LocationChecker locationChecker)
        {
            _monitor = monitor;
            _archipelago = archipelago;
            _locationChecker = locationChecker;
        }

        public static bool AnswerDialogueAction_BackPackPurchase_Prefix(GameLocation __instance, string questionAndAnswer, string[] questionParams, ref bool __result)
        {
            try
            {
                if (questionAndAnswer != "Backpack_Purchase")
                {
                    return true; // run original logic
                }

                __result = true;

                if (_locationChecker.IsLocationNotChecked(LARGE_PACK) && Game1.player.Money >= 2000)
                {
                    Game1.player.Money -= 2000;
                    _locationChecker.AddCheckedLocation(LARGE_PACK);
                    return false; // don't run original logic
                }

                if (_locationChecker.IsLocationNotChecked(DELUXE_PACK) && Game1.player.Money >= 10000)
                {
                    Game1.player.Money -= 10000;
                    _locationChecker.AddCheckedLocation(DELUXE_PACK);
                    return false; // don't run original logic
                }

                if (_locationChecker.IsLocationNotChecked(PREMIUM_PACK) && Game1.player.Money >= 50000)
                {
                    Game1.player.Money -= 50000;
                    _locationChecker.AddCheckedLocation(PREMIUM_PACK);
                    return false; // don't run original logic
                }


                return false; // don't run original logic
            }
            catch (Exception ex)
            {
                _monitor.Log($"Failed in {nameof(AnswerDialogueAction_BackPackPurchase_Prefix)}:\n{ex}", LogLevel.Error);
                return true; // run original logic
            }
        }

        public static bool PerformAction_BuyBackpack_Prefix(GameLocation __instance, string action, Farmer who, Location tileLocation, ref bool __result)
        {
            try
            {
                if (action == null || !who.IsLocalPlayer)
                {
                    return true; // run original logic
                }

                var actionParts = action.Split(' ');
                var actionName = actionParts[0];
                if (actionName == "BuyBackpack")
                {
                    BuyBackPackArchipelago(__instance, out __result);
                    return false; // don't run original logic
                }

                return true; // run original logic
            }
            catch (Exception ex)
            {
                _monitor.Log($"Failed in {nameof(PerformAction_BuyBackpack_Prefix)}:\n{ex}", LogLevel.Error);
                return true; // run original logic
            }
        }

        private static void BuyBackPackArchipelago(GameLocation __instance, out bool __result)
        {
            __result = true;

            var responsePurchaseLevel1 = new Response("Purchase",
                Game1.content.LoadString("Strings\\Locations:SeedShop_BuyBackpack_Response2000"));
            var responsePurchaseLevel2 = new Response("Purchase",
                Game1.content.LoadString("Strings\\Locations:SeedShop_BuyBackpack_Response10000"));
            var responseDontPurchase = new Response("Not",
                Game1.content.LoadString("Strings\\Locations:SeedShop_BuyBackpack_ResponseNo"));
            if (_locationChecker.IsLocationNotChecked(LARGE_PACK))
            {
                __instance.createQuestionDialogue(
                    Game1.content.LoadString("Strings\\Locations:SeedShop_BuyBackpack_Question24"),
                    new Response[2]
                    {
                        responsePurchaseLevel1,
                        responseDontPurchase
                    }, "Backpack");
            }
            else if (_locationChecker.IsLocationNotChecked(DELUXE_PACK) && _archipelago.HasReceivedItem("Progressive Backpack"))
            {
                __instance.createQuestionDialogue(
                    Game1.content.LoadString("Strings\\Locations:SeedShop_BuyBackpack_Question36"),
                    new Response[2]
                    {
                        responsePurchaseLevel2,
                        responseDontPurchase
                    }, "Backpack");
            }
            else if (_archipelago.SlotData.Mods.HasMod(ModNames.BIGGER_BACKPACK) && _locationChecker.IsLocationNotChecked(PREMIUM_PACK)
            && _archipelago.GetReceivedItemCount("Progressive Backpack") >= 2)
            {
                Response yes = new Response("Purchase", "Purchase (50,000g)");
                Response no = new Response("Not", Game1.content.LoadString("Strings\\Locations:SeedShop_BuyBackpack_ResponseNo"));
                Response[] resps = new Response[] { yes, no };
                __instance.createQuestionDialogue("Backpack Upgrade -- 48 slots", resps, "Backpack");
            }
        }
    }
}