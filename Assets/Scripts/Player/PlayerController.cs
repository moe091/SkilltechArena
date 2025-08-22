using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private int inputBufferCapacity = 256;

    private PlayerInputCollector _inputCollector;
    private PlayerInputBuffer _inputBuffer;
    private WeaponController _weaponController;
    private readonly List<PlayerInputData> _replayScratch = new();


    void Awake()
    {
        _inputCollector = GetComponent<PlayerInputCollector>();
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




}
