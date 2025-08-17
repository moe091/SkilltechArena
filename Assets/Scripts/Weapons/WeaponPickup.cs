using FishNet.Object;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponPickup : NetworkBehaviour
{
    [SerializeField] public WeaponDefinition weaponDef;
    [SerializeField] private GameObject prompt; // assign the Prompt child in the prefab
    [SerializeField] private KeyCode interactKey = KeyCode.E;

    private SpriteRenderer _spriteRenderer;
    private NetworkObject _localPlayerNobInTrigger;


    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log("Weapon pickup in range: " + weaponDef.name);
        if (!IsClient) return;

        // Find the NetworkObject on the player root (or a parent)
        var nob = other.GetComponentInParent<NetworkObject>();
        if (nob != null && nob.IsOwner)    // only the local player's collider matters on this client
        {
            Debug.Log("YOU CAN PICK UP THIS " + weaponDef.name);
            _localPlayerNobInTrigger = nob;
            // TODO: Show prompt
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!IsClient) return;

        var nob = other.GetComponentInParent<NetworkObject>();
        if (nob != null && nob == _localPlayerNobInTrigger)
        {
            _localPlayerNobInTrigger = null;
            if (prompt && prompt.activeSelf) prompt.SetActive(false);
        }
    }


    [ServerRpc(RequireOwnership = false)]
    private void ServerTryEquip(NetworkObject playerNob)
    {
        Debug.Log("ServerTryEquip called!");
        if (!IsServer || !playerNob) return;

        // (Optional) validate proximity server-side if you need to be strict
        var controller = playerNob.GetComponent<WeaponController>();
        if (controller == null || weaponDef == null) return;

        controller.Equip(weaponDef);

        // Despawn the pickup so it can't be taken twice
        Despawn();
    }


    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        if (prompt) prompt.SetActive(false); // start hidden
    }

    private void Start()
    {
        if (weaponDef != null && weaponDef.icon != null)
        {
            _spriteRenderer.sprite = weaponDef.icon;
        }
        else
        {
            Debug.LogWarning($"{name}: WeaponPickup has no icon set in {weaponDef}.");
        }
    }

    private void Update()
    {
        if (!IsClient) return;                 // UI/prompt only matters on clients
        if (!_localPlayerNobInTrigger) return; // only when local player is overlapping

        if (prompt && !prompt.activeSelf) prompt.SetActive(true);

        if (Input.GetKeyDown(interactKey))
        {
            // Ask server to equip this weapon for the overlapping player
            Debug.Log("Calling ServerTryEquip!");
            ServerTryEquip(_localPlayerNobInTrigger);
        }
    }
}
