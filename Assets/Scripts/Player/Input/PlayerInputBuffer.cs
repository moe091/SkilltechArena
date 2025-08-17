using System.Collections.Generic;
using UnityEngine;

/// Stores recent PlayerInputData in a fixed-size ring buffer (newest wins).
public class PlayerInputBuffer
{
    private readonly PlayerInputData[] _buffer;
    private readonly uint[] _ticks;
    private readonly int _capacity;
    private int _head = -1;   // index of newest item
    private int _count = 0;

    public int Count => _count;
    public int Capacity => _capacity;

    public PlayerInputBuffer(int capacity = 256)
    {
        _capacity = Mathf.Max(8, capacity);
        _buffer = new PlayerInputData[_capacity];
        _ticks = new uint[_capacity];
    }

    public void Clear()
    {
        _head = -1;
        _count = 0;
    }

    /// Add (or overwrite oldest) with newest input.
    public void Push(PlayerInputData data)
    {
        _head = (_head + 1) % _capacity;
        _buffer[_head] = data;
        //_ticks[_head] = data.tick;
        if (_count < _capacity) _count++;
    }

    /// Try to get the exact tick.
    public bool TryGet(uint tick, out PlayerInputData data)
    {
        // Linear scan (buffer is small). Newest -> oldest for early exit.
        for (int i = 0; i < _count; i++)
        {
            int idx = (_head - i + _capacity) % _capacity;
            if (_ticks[idx] == tick)
            {
                data = _buffer[idx];
                return true;
            }
        }
        data = default;
        return false;
    }

    /// Copy inputs with tick >= fromTickInclusive into outList in ascending tick order.
    /// Returns number of items copied.
    public int CopySince(uint fromTickInclusive, List<PlayerInputData> outList)
    {
        outList.Clear();
        // Oldest -> newest
        for (int k = _count - 1; k >= 0; k--)
        {
            int idx = (_head - k + _capacity) % _capacity;
            if (_ticks[idx] >= fromTickInclusive)
                outList.Add(_buffer[idx]);
        }
        return outList.Count;
    }
}
