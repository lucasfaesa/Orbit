using System;
using System.Collections;
using System.Collections.Generic;
using Networking;
using UnityEngine;

public class PlayerDamageable : MonoBehaviour, IDamageable
{
    [SerializeField] private HealthStatsSO healthStats;
    
    private bool _isDead;

    private void OnEnable()
    {
        healthStats.Respawn += OnRespawn;
    }

    private void OnDisable()
    {
        healthStats.Respawn += OnRespawn;
    }

    private void Start()
    {
        InitializeHealth();
    }

    public void InitializeHealth()
    {
        healthStats.CurrentHealth = healthStats.MaxHealth;
    }

    public void OnDamageTaken(float damage)
    {
        if (_isDead)
            return;
        
        healthStats.CurrentHealth -= damage;
        healthStats.OnHealthUpdated();
        healthStats.OnGotAttacked(default, default);
        
        if (healthStats.CurrentHealth <= 0)
        {
            _isDead = true;
            healthStats.CurrentHealth = 0;
            OnDeath();
        }
    }

    public void RegenerateHealth()
    {
        healthStats.CurrentHealth = healthStats.MaxHealth;
        healthStats.OnHealthUpdated();
    }

    public void OnDamageTakenRPC(float damage, Vector3 position)
    {
    }

    private void OnDeath()
    {
        healthStats.OnDeath(0);
    }

    private void OnRespawn()
    {
        _isDead = false;
        healthStats.CurrentHealth = healthStats.MaxHealth;
        healthStats.OnHealthUpdated();
    }
}
