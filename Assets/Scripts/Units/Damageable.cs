using System;
using UnityEngine;
using RedAlert.Core;

namespace RedAlert.Units
{
    /// <summary>
    /// Damageable with armor multipliers and death event publishing.
    /// </summary>
    public class Damageable : MonoBehaviour
    {
        [SerializeField] private float _maxHealth = 100f;
        [SerializeField] private float _currentHealth = 100f;
        [SerializeField] private string _armorTag = "Light"; // simple tag string for Week 2

        public float MaxHealth => _maxHealth;
        public float CurrentHealth => _currentHealth;
        public string ArmorTag => _armorTag;

        public struct DamageInfo
        {
            public float amount;
            public string weaponTag;
            public GameObject source;
        }

        public event Action OnDeath;
        public event Action<DamageInfo> OnDamaged;

        private void Awake()
        {
            _currentHealth = Mathf.Clamp(_currentHealth, 0, _maxHealth);
        }

        public void ApplyDamage(float amount, string attackerWeaponVsArmorTag, GameObject source = null)
        {
            // Simple multiplier table (expand later). If no mapping, use 1.0
            float mult = GetMultiplier(attackerWeaponVsArmorTag, _armorTag);
            float dmg = Mathf.Max(0, amount * mult);
            if (dmg <= 0) return;

            _currentHealth -= dmg;
            EventBus.PublishUnderAttack(gameObject);

            // Fire non-alloc style event using small struct
            OnDamaged?.Invoke(new DamageInfo { amount = dmg, weaponTag = attackerWeaponVsArmorTag, source = source });

            if (_currentHealth <= 0)
            {
                _currentHealth = 0;
                Die();
            }
        }

        private float GetMultiplier(string weaponTag, string armorTag)
        {
            // Minimal static mapping example
            if (string.IsNullOrEmpty(weaponTag) || string.IsNullOrEmpty(armorTag)) return 1f;
            if (weaponTag == "SmallArms")
            {
                if (armorTag == "Light") return 1.0f;
                if (armorTag == "Heavy") return 0.7f;
                if (armorTag == "Structure") return 0.5f;
            }
            if (weaponTag == "AntiArmor")
            {
                if (armorTag == "Light") return 0.8f;
                if (armorTag == "Heavy") return 1.2f;
                if (armorTag == "Structure") return 0.9f;
            }
            return 1f;
        }

        private void Die()
        {
            OnDeath?.Invoke();
            EventBus.PublishUnitDeath(gameObject);
            Destroy(gameObject);
        }

        public void DebugSetHealth(float value)
        {
            _currentHealth = Mathf.Clamp(value, 0, _maxHealth);
        }
    }
}