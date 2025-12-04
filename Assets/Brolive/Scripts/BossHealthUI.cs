using UnityEngine;

public class BossHealthUI : MonoBehaviour
{
    public Damageable bossDamageable; // Drag BossModel here
    public Bar healthBar;             // Drag your Bar script object here

    int maxHP = 0;

    void Start()
    {
        if (bossDamageable == null || healthBar == null)
        {
            Debug.LogError("BossHealthUI missing references!");
            return;
        }

        // Listen for initial HP setup
        bossDamageable.OnInitialize.AddListener(InitializeBar);

        // Listen for HP changes
        bossDamageable.OnHealthChanged.AddListener(UpdateBar);
    }

    void InitializeBar(int startingMaxHP)
    {
        maxHP = startingMaxHP;
        healthBar.SetMax(maxHP);   // This calls Bar.SetMax()
    }

    void UpdateBar(int damageTaken, int newCurrentHP)
    {
        healthBar.UpdateBar(damageTaken, newCurrentHP);
    }
}
