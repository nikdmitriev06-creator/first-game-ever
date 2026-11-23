using System;
using UnityEngine;

/// <summary>
/// Health, mana, and stamina for the player. Each stat starts full.
/// </summary>
public class PlayerVitals : MonoBehaviour
{
    [SerializeField] float maxHealth = 100f;
    [SerializeField] float maxMana = 100f;
    [SerializeField] float maxStamina = 100f;
    [SerializeField] float healthRegenPerSecond = 2f;
    [SerializeField] float manaRegenPerSecond = 8f;
    [SerializeField] float manaRegenDelay = 0.45f;
    [SerializeField] float staminaDrainPerSecond = 20f;
    [SerializeField] float staminaRegenPerSecond = 25f;
    [SerializeField] float emptyRegenDelay = 1f;
    [SerializeField] float emptyRegenStart = 15f;
    [SerializeField] float emptyRegenStep = 5f;
    [SerializeField] float emptyRegenStepInterval = 0.5f;
    [SerializeField] float emptyRegenMax = 30f;

    bool _emptyRecovery;
    float _emptyDelay;
    float _emptyRampTime;
    float _manaDelay;

    public float Health { get; private set; }
    public float Mana { get; private set; }
    public float Stamina { get; private set; }

    public float MaxHealth => maxHealth;
    public float MaxMana => maxMana;
    public float MaxStamina => maxStamina;

    public event Action Changed;

    void Awake()
    {
        Health = maxHealth;
        Mana = maxMana;
        Stamina = maxStamina;
    }

    void Update()
    {
        RecoverHealth(Time.deltaTime);
        RecoverMana(Time.deltaTime);
    }

    public void RecoverHealth(float deltaTime)
    {
        if (deltaTime <= 0f || healthRegenPerSecond <= 0f || Health <= 0f || Health >= maxHealth)
            return;

        SetHealth(Health + healthRegenPerSecond * deltaTime);
    }

    /// <summary>
    /// Restores mana only after the delay from the last spend. A spend and a restore do not land in the same call.
    /// </summary>
    public void RecoverMana(float deltaTime)
    {
        if (deltaTime <= 0f)
            return;

        if (_manaDelay > 0f)
        {
            _manaDelay = Mathf.Max(0f, _manaDelay - deltaTime);
            return;
        }

        if (manaRegenPerSecond <= 0f || Health <= 0f || Mana >= maxMana)
            return;

        SetMana(Mana + manaRegenPerSecond * deltaTime);
    }

    public bool TrySpendMana(float amount)
    {
        if (amount <= 0f)
            return true;
        if (Mana + 0.0001f < amount)
            return false;

        SetMana(Mana - amount);
        _manaDelay = Mathf.Max(0f, manaRegenDelay);
        return true;
    }

    public void TakeDamage(float amount)
    {
        if (amount <= 0f || Health <= 0f)
            return;

        SetHealth(Health - amount);
        Fx.Play(FxEvent.Hurt, transform.position);
    }

    public void SetHealth(float value)
    {
        Health = Mathf.Clamp(value, 0f, maxHealth);
        Changed?.Invoke();
    }

    public void SetMana(float value)
    {
        Mana = Mathf.Clamp(value, 0f, maxMana);
        Changed?.Invoke();
    }

    public void SetStamina(float value)
    {
        float next = Mathf.Clamp(value, 0f, maxStamina);
        if (Mathf.Abs(next - Stamina) <= 0.0001f)
            return;

        Stamina = next;
        Changed?.Invoke();
    }

    /// <summary>
    /// Spends stamina for this frame of sprinting. Hitting zero starts the slow recovery.
    /// </summary>
    public bool TrySpendStamina(float deltaTime)
    {
        if (deltaTime <= 0f || Stamina <= 0f || staminaDrainPerSecond <= 0f)
            return false;

        SetStamina(Stamina - staminaDrainPerSecond * deltaTime);
        if (Stamina <= 0f)
            BeginEmptyRecovery();
        else
            CancelEmptyRecovery();

        return true;
    }

    /// <summary>
    /// Spends a fixed amount, such as one weapon attack. Fails when the bar cannot cover it.
    /// </summary>
    public bool TrySpendStaminaAmount(float amount)
    {
        if (amount <= 0f)
            return true;
        if (Stamina + 0.0001f < amount)
            return false;

        SetStamina(Stamina - amount);
        if (Stamina <= 0f)
            BeginEmptyRecovery();

        return true;
    }

    /// <summary>
    /// Restores stamina while the player is not sprinting.
    /// A bar that was not emptied returns at a flat rate. An empty bar waits,
    /// then climbs through faster rates up to a cap.
    /// </summary>
    public void RecoverStamina(float deltaTime)
    {
        if (deltaTime <= 0f || Stamina >= maxStamina)
        {
            if (Stamina >= maxStamina)
                CancelEmptyRecovery();
            return;
        }

        if (Stamina <= 0f)
            _emptyRecovery = true;

        if (_emptyRecovery)
        {
            _emptyDelay += deltaTime;
            if (_emptyDelay < emptyRegenDelay)
                return;

            _emptyRampTime += deltaTime;
            SetStamina(Stamina + EmptyRegenRate() * deltaTime);
            if (Stamina >= maxStamina)
                CancelEmptyRecovery();
            return;
        }

        SetStamina(Stamina + staminaRegenPerSecond * deltaTime);
    }

    float EmptyRegenRate()
    {
        float interval = Mathf.Max(0.01f, emptyRegenStepInterval);
        int steps = Mathf.FloorToInt(_emptyRampTime / interval);
        float rate = emptyRegenStart + steps * emptyRegenStep;
        return Mathf.Min(rate, emptyRegenMax);
    }

    void BeginEmptyRecovery()
    {
        _emptyRecovery = true;
        _emptyDelay = 0f;
        _emptyRampTime = 0f;
    }

    void CancelEmptyRecovery()
    {
        _emptyRecovery = false;
        _emptyDelay = 0f;
        _emptyRampTime = 0f;
    }
}
