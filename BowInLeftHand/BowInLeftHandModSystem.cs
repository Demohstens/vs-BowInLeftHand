﻿using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Vintagestory;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.GameContent;

public class CorrectlyHandedBowModSystem : ModSystem
{

    private ICoreAPI api;
    //private const int LogIntervalMs = 5000; // Log every 5 seconds


    public override void Start(ICoreAPI api)
    {
        this.api = api;
        //api.Event.RegisterGameTickListener(LogAnimations, LogIntervalMs);
    }

    private void LogAnimations(float dt)
    {
        Console.WriteLine("LogAnimations running...");

        if (api.World?.AllPlayers == null || api.World.AllPlayers.Length == 0)
        {
            return;
        }

        var player = api.World.AllPlayers[0];
        if (player?.Entity?.AnimManager == null)
        {
            return;
        }

        var curAnims = player.Entity.AnimManager.ActiveAnimationsByAnimCode;
        Console.WriteLine($"Active Animations: {string.Join(", ", curAnims.Keys)}");
    }
    // Called only on the client side
    public override void StartClientSide(ICoreClientAPI api)
    {

        var harmony = new Harmony("com.BowInleftHand.vsmod");
        try
        {
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            Console.WriteLine("Harmony PatchAll executed successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Harmony PatchAll failed: {ex}");
        }
    }

    // This method checks if the bow is in the right hand
    public static bool IsBowInRightHand(EntityShapeRenderer esr)
    {
        if (esr?.capi?.World?.Player?.InventoryManager.ActiveHotbarSlot?.Itemstack != null)
        {
            var isBow = esr.capi.World.Player.InventoryManager.ActiveHotbarSlot.Itemstack.GetName().ToLower().Contains("bow");
            if (isBow) {

                var anim = esr.capi.World.Player.Entity.Properties.Client.AnimationsByMetaCode["holdinglanternlefthand"];
                if (anim != null)
                {
                    anim.Weight = 0;
                    anim.BlendMode = EnumAnimationBlendMode.Add;
                }
                esr.capi.World.Player.Entity.Properties.Client.AnimationsByMetaCode["holdinglanternlefthand-fp"] = anim;

                var animFP = esr.capi.World.Player.Entity.Properties.Client.AnimationsByMetaCode["holdinglanternlefthand"];
                if (animFP != null)
                {
                    animFP.Weight = 0;
                    animFP.BlendMode = EnumAnimationBlendMode.Add;
                }
                esr.capi.World.Player.Entity.Properties.Client.AnimationsByMetaCode["holdinglanternlefthand-fp"] = animFP;
                
                return isBow;

            }
        }
        return false;

    }

    [HarmonyPatch(typeof(EntityShapeRenderer), "RenderHeldItem")]
    public class PatchRenderHeldItem
    {

        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            var codes = new List<CodeInstruction>(instructions);

            MethodInfo isBowMethod = typeof(CorrectlyHandedBowModSystem).GetMethod(nameof(IsBowInRightHand), BindingFlags.Static | BindingFlags.Public);

            if (isBowMethod == null)
            {
                Console.WriteLine("ERROR: Could not find method 'IsBowInRightHand'.");
                return codes;
            }


            int ldarg3c = 0;
            Console.WriteLine("ORIGINAL");
            for (int i = 0; i < codes.Count; i++)
            {
                Console.WriteLine($"Code {i}: {codes[i]}");
            }
            // Iterate through the instructions and look for 'Ldarg_3' to modify the hand
            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Ldarg_3)
                {
                    ldarg3c++;
                    if (ldarg3c == 2)
                    {
                        // Create a new label for the original code path
                        Label useOriginalValue = il.DefineLabel();

                        // Insert our check right before the Ldarg_3
                        // We preserve the original instruction's labels
                        List<Label> originalLabels = new List<Label>(codes[i].labels);

                        // Step 1: Insert code to load EntityShapeRenderer instance
                        var loadThis = new CodeInstruction(OpCodes.Ldarg_0);
                        loadThis.labels = originalLabels;

                        // Step 2: Call our method to check if it's a bow
                        var callCheckMethod = new CodeInstruction(OpCodes.Call, isBowMethod);

                        // Step 3: If not a bow, jump to original code
                        var skipIfNotBow = new CodeInstruction(OpCodes.Brfalse, useOriginalValue);

                        // Step 4: If it is a bow, load the value we want (0 for left hand, 1 for right hand)
                        // Assuming you want bows in right hand, we'd use Ldc_I4_1
                        var loadBowHandValue = new CodeInstruction(OpCodes.Ldc_I4_0);

                        // Step 5: Jump past the original Ldarg_3
                        Label afterOriginalLoad = il.DefineLabel();
                        var skipOriginalLoad = new CodeInstruction(OpCodes.Br, afterOriginalLoad);

                        // Step 6: The original Ldarg_3 gets the useOriginalValue label
                        codes[i].labels = new List<Label> { useOriginalValue };

                        // Step 7: Add afterOriginalLoad label to the instruction after Ldarg_3
                        if (i + 1 < codes.Count)
                        {
                            if (codes[i + 1].labels == null)
                                codes[i + 1].labels = new List<Label>();

                            codes[i + 1].labels.Add(afterOriginalLoad);
                        }

                        // Insert our new instructions before the original Ldarg_3
                        codes.InsertRange(i, new[]
                        {
                            loadThis,
                            callCheckMethod,
                            skipIfNotBow,
                            loadBowHandValue,
                            skipOriginalLoad
                        });

                        // We're done
                        break;

                    }

                }
            }
            Console.WriteLine("DONE WRITING TRANSPILER");
            for (int i = 0; i < codes.Count; i++)
            {
                Console.WriteLine($"Code {i}: {codes[i]}");
            }

            return codes;
        }

    }
}
