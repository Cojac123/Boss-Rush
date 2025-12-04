using UnityEngine;

public class BossHealthUI : MonoBehaviour
{
    public Damageable bossDamageable; // Assign BossModel
    public Bar healthBar;             // Assign Bar script

    void Start()
    {
        if (bossDamageable != null)
        {
            // Listen for HP changes
            bossDamageable.OnHealthChanged.AddListener(UpdateBar);
        }
    }

    void UpdateBar(int current, int max)
    {
        float pct = (float)current / max;
        healthBar.SetBar(pct);
    }
}
