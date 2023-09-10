using System;
using Unity.Mathematics;
using UnityEngine;

namespace AttributeRelatedScript
{
    public class ManaSupplyItemEffect : ItemEffectStrategyBase
    {
        public ManaSupplyItemEffect(PlayerBuffEffect.EffectType et, float healAmount) : base(et, healAmount)
        {
        }

        public override void ApplyEffect(GameObject player)
        {
            player.GetComponent<State>().RestoreEnergy(effectValue);
            float currentmana = player.GetComponent<State>().CurrentEnergy; // 获取玩家当前的生命值
            ShowEffectMessage(effectValue, currentmana);
            var pos = SpellCast.FindDeepChild(player.transform, "root");
            ParticleEffectManager.Instance.PlayParticleEffect("MagicSupply",pos.gameObject,quaternion.identity, 
                Color.cyan,Color.green, 2f);
        }
    }
}