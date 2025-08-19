using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private int inputBufferCapacity = 256;

    private PlayerInputCollector _inputCollector;
    private PlayerMovementController _movement;
    private PlayerInputBuffer _inputBuffer;
    private WeaponController _weaponController;
    private readonly List<PlayerInputData> _replayScratch = new();


    void Awake()
    {
        _inputCollector = GetComponent<PlayerInputCollector>();
        _movement = GetComponent<PlayerMovementController>();
        _inputBuffer = new PlayerInputBuffer(inputBufferCapacity);
        _weaponController = GetComponent<WeaponController>();
    }

    void FixedUpdate()
    {
        //var input = _inputCollector.CurrentInput;
        //_inputCollector.RetrieveInput();

        // Store input for future replay
        //_inputBuffer.Push(input);

        // Simulate this tick locally
        //_movement.ApplyMovement(input, Time.fixedDeltaTime);
    }

    // Example reconciliation sketch (call when server sends an authoritative state at serverTick)
    public void ReconcileFrom(uint serverTick, Vector2 serverPos, Vector2 serverVel)
    {
        // 1) Snap to server state
        _movement.SetState(serverPos, serverVel); // add this helper in your movement script

        // 2) Replay inputs AFTER serverTick
        _inputBuffer.CopySince(serverTick + 1, _replayScratch);
        for (int i = 0; i < _replayScratch.Count; i++)
            _movement.ApplyMovement(_replayScratch[i], Time.fixedDeltaTime);
    }


}
