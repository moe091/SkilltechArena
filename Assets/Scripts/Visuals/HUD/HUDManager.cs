using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HUDManager : MonoBehaviour
{
    [Header("Assign in Inspector")]
    [SerializeField] private RectTransform weaponPanel;  // parent panel (bottom-right)
    [SerializeField] private RectTransform ammoPanel;    // top row container
    [SerializeField] private Image weaponImage;          // image to show the current weapon

    [Header("Secondary Ability")]
    [SerializeField] private RectTransform secondaryAbilRoot;
    [SerializeField] private Image secondaryIcon;
    [SerializeField] private Image secondaryMask;
    [SerializeField] private TextMeshProUGUI secondaryText;

    [Header("Dash Ability")]
    [SerializeField] private RectTransform dashRoot;
    [SerializeField] private Image dashIcon;
    [SerializeField] private Image dashMask;
    [SerializeField] private TextMeshProUGUI dashText;


    [Header("Visuals")]
    [Range(0f, 1f)]
    [SerializeField] private float spentAlpha = 0.5f;    // opacity for spent ammo icons

    private readonly List<Image> _ammoImages = new List<Image>();
    private Sprite _currentAmmoSprite;
    private int _currentMaxAmmo;



    // ============================
    // DASH ABILITY
    // ============================

    #region Dash Ability
    public void InitDashAbil(string resourcePath)
    {
        if (dashRoot == null)
        {
            Debug.Log("[HUDManager.InitDashAbil] dashRoot not set, unable to initialize HUD");
            return;
        }

        Sprite sprite = Resources.Load<Sprite>(resourcePath);
        if (dashIcon != null)
        {
            dashIcon.sprite = sprite;
            dashIcon.enabled = sprite != null;
        }
        if (dashMask != null)
        {
            dashMask.sprite = sprite;
            dashMask.enabled = sprite != null;
        }

        dashRoot.gameObject.SetActive(sprite != null);
        SetDashCooldownVisuals(0f, 0f);
    }


    public void UpdateDashAbility(float timeRemaining, float maxTime)
    {
        float t = (maxTime > 0f && timeRemaining != maxTime) ? Mathf.Clamp01(timeRemaining / maxTime) : 0f;
        dashMask.fillAmount = t;

        if (dashText != null)
        {
            // optional text display (countdown or blank)
            dashText.text = (timeRemaining > 0f && timeRemaining < maxTime)
                ? Mathf.CeilToInt(timeRemaining).ToString()
                : string.Empty;
        }
    }


    private void SetDashCooldownVisuals(float cooldown, float maxCooldown)
    {
        if (dashMask != null)
            dashMask.fillAmount = (maxCooldown > 0f) ? Mathf.Clamp01(cooldown / maxCooldown) : 0f;

        if (dashText != null)
            dashText.text = "";
    }
    #endregion  


    #region SecondaryAbility
    public void InitSecondaryAbil(string resourcePath)
    {
        if (secondaryAbilRoot == null)
        {
            Debug.Log("[HUDManager.INitSecondaryAbil] secondaryAbilRoot not set, unable to initialize HUD");
            return;
        }

        Sprite sprite = Resources.Load<Sprite>(resourcePath);
        if (secondaryIcon != null)
        {
            secondaryIcon.sprite = sprite;
            secondaryIcon.enabled = sprite != null;
        }
        if (secondaryMask != null)
        {
            secondaryMask.sprite = sprite;
            secondaryMask.enabled = sprite != null;
        }

        secondaryAbilRoot.gameObject.SetActive(sprite != null);
        SetSecondaryCooldownVisuals(0f, 0f);

    }


    public void UpdateSecondaryAbility(int curCharges, float timeRemaining, float maxTime)
    {
        float t = (maxTime > 0f && timeRemaining != maxTime) ? Mathf.Clamp01(timeRemaining / maxTime) : 0f;
        secondaryMask.fillAmount = t;

        secondaryText.text = curCharges.ToString();
    }

    private void SetSecondaryCooldownVisuals(float cooldown, float maxCooldown)
    {
        if (secondaryMask != null)
            secondaryMask.fillAmount = (maxCooldown > 0f) ? Mathf.Clamp01(cooldown / maxCooldown) : 0f;

        if (secondaryText != null)
            secondaryText.text = "";
    }

    #endregion



    #region Ammo
    /// <summary>
    /// Updates weapon icon, (re)creates ammo icons to match maxAmmo, and sets their opacity based on curAmmo.
    /// </summary>
    public void UpdateVisual(Sprite weaponIcon, Sprite ammoIcon, int maxAmmo, int curAmmo)
    {
        Debug.Log("[HUDManager] UPDATING HUD VISUALS");
        // Weapon icon
        if (weaponImage != null)
        {
            Debug.Log("[HUDManager] Setting weaponImage");
            weaponImage.enabled = (weaponIcon != null);
            weaponImage.sprite = weaponIcon;
            weaponImage.preserveAspect = true;
            Color c = weaponImage.color;
            c.a = 1f;
            weaponImage.color = c;
        }

        // Rebuild ammo row only if max count or sprite changed
        bool needsRebuild = (ammoIcon != _currentAmmoSprite) || (maxAmmo != _currentMaxAmmo) || (_ammoImages.Count != maxAmmo);
        if (needsRebuild)
        {
            Debug.Log("[HUDManager] Rebuilding ammo row");
            RebuildAmmoRow(ammoIcon, maxAmmo);
        }

        // Apply current ammo opacity
        SetAmmoAmount(curAmmo);
    }

    /// <summary>
    /// Updates only the opacity of existing ammo icons to reflect the current ammo count.
    /// </summary>
    public void SetAmmoAmount(int curAmmo)
    {
        int filled = Mathf.Clamp(curAmmo, 0, _ammoImages.Count);

        for (int i = 0; i < _ammoImages.Count; i++)
        {
            var img = _ammoImages[i];
            if (img == null) continue;

            Color c = img.color;
            c.a = (i < filled) ? 1f : spentAlpha;
            c.r = (i < filled) ? 1f : 0f;
            c.g = (i < filled) ? 1f : 0f;
            c.b = (i < filled) ? 1f : 0f;
            img.color = c;
        }
    }



    // --- helpers ---

    private void RebuildAmmoRow(Sprite ammoSprite, int maxAmmo)
    {
        // Clear old
        for (int i = 0; i < _ammoImages.Count; i++)
        {
            if (_ammoImages[i] != null)
                Destroy(_ammoImages[i].gameObject);
        }
        _ammoImages.Clear();

        _currentAmmoSprite = ammoSprite;
        _currentMaxAmmo = Mathf.Max(0, maxAmmo);

        // Build new
        for (int i = 0; i < _currentMaxAmmo; i++)
        {
            var go = new GameObject($"Ammo_{i}", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(ammoPanel, false);

            var img = go.GetComponent<Image>();
            img.raycastTarget = false;
            img.preserveAspect = true;
            img.sprite = ammoSprite;

            _ammoImages.Add(img);
        }
    }

    #endregion



    internal void SetGrenadeCount(int next)
    {
        //Debug.Log("Current Grenade Count = " + next);
    }
}
