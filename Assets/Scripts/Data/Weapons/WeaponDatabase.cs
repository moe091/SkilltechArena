// WeaponDatabase.cs
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "WeaponDatabase", menuName = "Game/Weapons/Weapon Database")]
public class WeaponDatabase : ScriptableObject
{
    public List<WeaponDefinition> all;

    private Dictionary<WeaponId, WeaponDefinition> _map;
    public WeaponDefinition Get(WeaponId id)
    {
        if (_map == null)
        {
            _map = new Dictionary<WeaponId, WeaponDefinition>(all.Count);
            foreach (var w in all)
                if (w) _map[w.weaponId] = w;
        }
        return _map != null && _map.TryGetValue(id, out var def) ? def : null;
    }

    // Convenient static accessor
    private static WeaponDatabase _cached;
    public static WeaponDatabase Load()
    {
        if (_cached == null)
            _cached = Resources.Load<WeaponDatabase>("WeaponDatabase");
        return _cached;
    }
}
