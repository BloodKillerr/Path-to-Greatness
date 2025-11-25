using System;
using UnityEngine;
public class AbilityCooldown : MonoBehaviour
{
    public float CooldownSeconds = 1.0f;

    private float cooldownTimer = 0f;

    public bool IsOnCooldown => cooldownTimer > 0f;
    public float TimeRemaining => Mathf.Max(0f, cooldownTimer);

    private void Update()
    {
        if (cooldownTimer > 0f)
        {
            cooldownTimer -= Time.deltaTime;
        }
    }

    public bool TryUse(Action onUse)
    {
        if (IsOnCooldown)
        {
            return false;
        }

        onUse?.Invoke();
        cooldownTimer = CooldownSeconds;
        return true;
    }

    public void ResetCooldown()
    {
        cooldownTimer = 0f;
    }
}
