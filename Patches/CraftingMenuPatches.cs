using HarmonyLib;
using BulkCrafting.Classes;

namespace BulkCrafting.Patches;

[HarmonyPatch(typeof(CraftingMenu))]
public class CraftingMenuPatches
{
    [HarmonyPostfix]
    [HarmonyPatch("Awake")]
    private static void Awake_Postfix(CraftingMenu __instance)
    {
        BulkCraftingMenu.Setup(__instance);
    }

    [HarmonyPostfix]
    [HarmonyPatch("OnSelectedRecipe")]
    private static void OnSelectedRecipe_Postfix(CraftingMenu __instance, InventoryItem.ID id)
    {
        BulkCraftingMenu.RecipeSelected(__instance, id);
    }

    [HarmonyPrefix]
    [HarmonyPatch("OnClickedCraft")]
    private static bool OnClickedCraft_Prefix(CraftingMenu __instance)
    {
        BulkCraftingMenu.CraftClicked(__instance);
        return false;
    }

    [HarmonyPostfix]
    [HarmonyPatch("SetRecipeVisible")]
    private static void SetRecipeVisible_Postfix(CraftingMenu __instance)
    {
        BulkCraftingMenu.RefreshVisibility(__instance);
    }
}
