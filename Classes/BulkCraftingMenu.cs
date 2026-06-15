using System.Collections.Generic;
using HarmonyLib;
using TMPro;
using UnityEngine;

namespace BulkCrafting.Classes;

public class BulkCraftingMenu : MonoBehaviour
{
    private static readonly Dictionary<CraftingMenu, BulkCraftingMenu> Instances = new();
    private BulkCraftingButton _btnMinus5;
    private BulkCraftingButton _btnMinus1;
    private BulkCraftingButton _btnPlus1;
    private BulkCraftingButton _btnPlus5;
    private RepeatButton _craftButton;
    private TMP_Text _craftTextField;
    private CraftingMenu _menu;
    private InventoryItem.ID _currentRecipe;
    private string _originalCraftText;
    private int _amountToCraft = 1;
    private int _maxAmount;
    private bool _isCrafting;

    public static void Setup(CraftingMenu menu)
    {
        if (Instances.ContainsKey(menu))
            return;

        var craftButton = Traverse.Create(menu).Field("craftButton").GetValue<RepeatButton>();
        if (craftButton == null)
            return;

        var instance = menu.gameObject.AddComponent<BulkCraftingMenu>();
        instance.Init(craftButton);
        Instances[menu] = instance;
    }

    public static void RefreshVisibility(CraftingMenu menu)
    {
        if (Instances.TryGetValue(menu, out var instance))
            instance.UpdateVisibility();
    }

    public static void RecipeSelected(CraftingMenu menu, InventoryItem.ID recipeId)
    {
        if (!Instances.TryGetValue(menu, out var instance))
            return;

        var isNewRecipe = recipeId != instance._currentRecipe;
        instance.OnRecipeSelected(recipeId, isNewRecipe);
    }

    public static void CraftClicked(CraftingMenu menu)
    {
        if (!Instances.TryGetValue(menu, out var instance))
            return;

        instance.OnCraftClicked();
        instance.OnRecipeSelected(instance._currentRecipe, false);
    }

    public void Init(RepeatButton craftButton)
    {
        _menu = GetComponent<CraftingMenu>();
        _craftButton = craftButton;
        _craftTextField = Traverse.Create(_craftButton).Field("text").GetValue<TMP_Text>();
        _originalCraftText = _craftTextField?.text ?? "";

        CreateButtons();
    }

    private void CreateButtons()
    {
        var craftRect = _craftButton.GetComponent<RectTransform>();
        var font = _craftTextField?.font;

        var craftParent = craftRect.parent as RectTransform;
        var container = new GameObject("BulkCraftButtons").AddComponent<RectTransform>();
        container.SetParent(craftParent, false);
        container.anchorMin = Vector2.zero;
        container.anchorMax = Vector2.one;
        container.offsetMin = Vector2.zero;
        container.offsetMax = Vector2.zero;
        container.SetAsLastSibling();

        var btnWidth = craftRect.rect.width * 0.2f;
        var btnHeight = craftRect.rect.height;
        const float gap = 2f;
        var craftWidth = craftRect.rect.width;
        var totalWidth = btnWidth * 4 + craftWidth + gap * 4;
        var startX = -totalWidth / 2f + btnWidth / 2f;
        _btnMinus5 = BulkCraftingButton.Create(container, "-5", "BtnMinus5", font);
        _btnMinus1 = BulkCraftingButton.Create(container, "-1", "BtnMinus1", font);
        _btnPlus1 = BulkCraftingButton.Create(container, "+1", "BtnPlus1", font);
        _btnPlus5 = BulkCraftingButton.Create(container, "+5", "BtnPlus5", font);

        ConfigureButtonRect(_btnMinus5, btnWidth, btnHeight, startX);
        ConfigureButtonRect(_btnMinus1, btnWidth, btnHeight, startX + btnWidth + gap);
        ConfigureButtonRect(_btnPlus1, btnWidth, btnHeight, startX + btnWidth * 2.02f + gap * 2.02f + craftWidth);
        ConfigureButtonRect(_btnPlus5, btnWidth, btnHeight, startX + btnWidth * 3.02f + gap * 3.02f + craftWidth);

        _btnMinus5.Button.onClick.AddListener(() =>
        {
            AdjustAmount(-5);
            AudioController.instance.PlayUI(AudioController.UIFXID.Click);
        });
        _btnMinus1.Button.onClick.AddListener(() =>
        {
            AdjustAmount(-1);
            AudioController.instance.PlayUI(AudioController.UIFXID.Click);
        });
        _btnPlus1.Button.onClick.AddListener(() =>
        {
            AdjustAmount(1);
            AudioController.instance.PlayUI(AudioController.UIFXID.Click);
        });
        _btnPlus5.Button.onClick.AddListener(() =>
        {
            AdjustAmount(5);
            AudioController.instance.PlayUI(AudioController.UIFXID.Click);
        });

        UpdateVisibility();
    }

    public void UpdateVisibility()
    {
        if (_craftButton == null)
            return;

        var visible = _craftButton.gameObject.activeSelf;
        _btnMinus5.gameObject.SetActive(visible);
        _btnMinus5.BorderGo.SetActive(visible);
        _btnMinus1.gameObject.SetActive(visible);
        _btnMinus1.BorderGo.SetActive(visible);
        _btnPlus1.gameObject.SetActive(visible);
        _btnPlus1.BorderGo.SetActive(visible);
        _btnPlus5.gameObject.SetActive(visible);
        _btnPlus5.BorderGo.SetActive(visible);

        UpdateCraftButtonText();
        UpdateButtonStates();
        UpdateRecipeRequirements();
    }

    private void UpdateCraftButtonText()
    {
        var amountToShow = _maxAmount <= 0 ? 0 : _amountToCraft;
        if (_craftTextField != null)
            _craftTextField.text = $"{_originalCraftText} ({amountToShow}/{_maxAmount})";
    }

    private void UpdateButtonStates()
    {
        const int min = 1;
        _btnMinus5.Button.interactable = _amountToCraft - 5 >= min;
        _btnMinus1.Button.interactable = _amountToCraft - 1 >= min;
        _btnPlus1.Button.interactable = _amountToCraft + 1 <= _maxAmount;
        _btnPlus5.Button.interactable = _amountToCraft + 5 <= _maxAmount;
    }

    private void UpdateRecipeRequirements()
    {
        if (_menu == null)
            _menu = GetComponent<CraftingMenu>();

        if (_menu == null)
            return;

        var recipe = GetRecipeFor(_currentRecipe);
        if (recipe == null)
            return;

        var recipeView = Traverse.Create(_menu).Field("recipeView").GetValue<CraftingRecipeView>();
        if (recipeView == null)
            return;

        var materialsListView =
            Traverse.Create(recipeView).Field("materialsListView").GetValue<CraftingMaterialsListView>();
        if (materialsListView == null)
            return;

        var items = Traverse.Create(materialsListView).Field("items").GetValue<CraftingRecipeViewItem[]>() ??
                    materialsListView.GetComponentsInChildren<CraftingRecipeViewItem>(true);
        if (items == null)
            return;

        var inv = CraftingMenu.FindPlayerInventory;
        if (inv == null)
            return;

        var multiplier = Mathf.Max(1, _amountToCraft);
        var recipeMaterials = recipe.materials;
        for (var i = 0; i < recipeMaterials.Length; i++)
        {
            if (i >= items.Length)
                break;

            var id = recipeMaterials[i].id;
            var num = inv.TotalItemCount(id);
            var required = recipeMaterials[i].stack * multiplier;
            items[i].Show(id, num, required);
        }
    }

    public void OnRecipeSelected(InventoryItem.ID recipeId, bool resetAmnt = true)
    {
        _currentRecipe = recipeId;
        _maxAmount = CalculateMaxCraftable();

        if (_isCrafting)
        {
            UpdateCraftButtonText();
            UpdateButtonStates();
            UpdateRecipeRequirements();
            return;
        }

        var maxAmntForClamp = Mathf.Max(_maxAmount, 1);
        _amountToCraft = resetAmnt ? 1 : Mathf.Clamp(_amountToCraft, 1, maxAmntForClamp);
        UpdateCraftButtonText();
        UpdateButtonStates();
        UpdateRecipeRequirements();
    }

    public void OnCraftClicked()
    {
        if (_maxAmount <= 0)
            return;

        var recipe = GetRecipeFor(_currentRecipe);
        if (recipe == null)
            return;

        var inv = CraftingMenu.FindPlayerInventory;
        if (inv == null)
            return;

        var targetAmount = _amountToCraft;
        _isCrafting = true;

        var crafted = 0;
        for (var i = 0; i < targetAmount; i++)
        {
            if (!CraftingRecipe.CanCraft(inv, recipe.materials))
                break;

            recipe.CraftInto(inv);
            crafted++;
        }

        _isCrafting = false;
        if (crafted <= 0)
            return;

        AudioController.instance.PlayUIPack(AudioController.UIFXPack.Craft);
        InventoryDisplay.instance.Show(inv);
    }

    private void AdjustAmount(int delta)
    {
        _amountToCraft = Mathf.Clamp(_amountToCraft + delta, 1, _maxAmount);

        UpdateCraftButtonText();
        UpdateButtonStates();
        UpdateRecipeRequirements();
    }

    private static void ConfigureButtonRect(BulkCraftingButton btn, float width, float height, float xPos)
    {
        var rect = btn.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);

        const int inset = 6;
        rect.sizeDelta = new Vector2(width - inset, height - inset);
        rect.anchoredPosition = new Vector2(xPos, 45f);

        var border = btn.BorderRect;
        border.anchorMin = new Vector2(0.5f, 0.5f);
        border.anchorMax = new Vector2(0.5f, 0.5f);
        border.pivot = new Vector2(0.5f, 0.5f);
        border.sizeDelta = new Vector2(width, height);
        border.anchoredPosition = new Vector2(xPos, 45f);
    }

    private int CalculateMaxCraftable()
    {
        var recipe = GetRecipeFor(_currentRecipe);
        if (recipe == null)
            return 0;

        var inv = CraftingMenu.FindPlayerInventory;
        if (inv == null)
            return 0;

        var max = int.MaxValue;
        foreach (var mat in recipe.materials)
        {
            if (mat.stack <= 0)
                continue;

            var available = inv.TotalItemCount(mat.id);
            max = Mathf.Min(max, available / mat.stack);
        }

        return max == int.MaxValue ? 0 : max;
    }

    private static CraftingRecipe GetRecipeFor(InventoryItem.ID recipeId)
    {
        var db = CraftingDatabase.instance;
        return db == null ? null : Traverse.Create(db).Method("GetRecipeFor", recipeId).GetValue<CraftingRecipe>();
    }
}
