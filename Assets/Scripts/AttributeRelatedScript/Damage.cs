using UnityEngine;
using UnityEngine.Serialization;

namespace AttributeRelatedScript
{
    public class Damage : MonoBehaviour
    {
        [SerializeField] private float damage = 8f;
        [SerializeField] internal float attackAngle = 70f;
        [SerializeField] internal float attackRange = 0.9f;
        [SerializeField] public float attackCooldown = 1.0f; // 攻击冷却时间
        [SerializeField] public float HurricaneKickDamage = 8;
        [SerializeField] public float hurricaneKickKnockbackForce = 70;
        [SerializeField] public float hurricaneKickRange = 1.2f;
        [SerializeField] internal float criticalDmgRate = 4f;

        public void IncreaseDamage(float idmg)
        {
            damage += idmg;
            
        }
        public float CurrentDamage
        {
            get=>damage;
            set =>damage = value;
        }
    }
}