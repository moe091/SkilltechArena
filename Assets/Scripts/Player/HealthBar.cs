using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    [SerializeField] private Image foreground;
    [SerializeField] private Image background;

    private int _maxHealth;
    private int _curHealth;

    private void Start()
    {
        SetMaxHealth(100);
    }

    public void SetMaxHealth(int max)
    {
        _maxHealth = max;
        SetHealth(max);
    }

    public void SetHealth(int value)
    {
        if (_maxHealth <= 0) return;

        Debug.Log($"[HealthBar] SetHealth({value}). MaxHealth={_maxHealth}, CurHealth={_curHealth}.  cur/max={_curHealth / (float)_maxHealth} \n\n");
        _curHealth = value;
        foreground.fillAmount = _curHealth / (float)_maxHealth;
    }


    //NOTE:: think I can remove this, just going to use SetHealth instead and keep track of current health in PlayerController
    public bool ChangeHealth(int amount)
    {
        _curHealth += amount;
        if ( _curHealth <= 0 )
        {
            foreground.fillAmount = 0;
            return false;
        } else
        {
            foreground.fillAmount = (float)_curHealth / _maxHealth;
            return true;
        }
    }
}
