using System.Collections;
using System.Collections.Generic;
using AttributeRelatedScript;
using Behavior.Health;
using UI;
using UnityEngine;

namespace Behavior
{
    public class Sword : MonoBehaviour
    {
        private PlayerController pCtrl;
        private Animator animator;
        [SerializeField] private BoxCollider swordCollider;
        private HashSet<Collider> hitEnemies = new HashSet<Collider>();
        private int enemyLayer;
        private bool hasDoneDieBehaviour = false;
        
        [Header("Vampiric")] 
        private int comboCount = 0;
        private float lastAttackTime = 0f;
        private const float comboResetTime = 8f;
        private const int comboThreshold = 6;

        private void Start()
        {
            pCtrl = PlayerController.Instance;
            if (pCtrl == null)
            {
                Debug.LogError("Player controller for sword not found!");
            }

            // 订阅结束攻击事件
            pCtrl.OnAttackEnded += HandleAttackEnded;

            animator = pCtrl.GetAnimator();
            if (!swordCollider) swordCollider = GetComponent<BoxCollider>();
            swordCollider.enabled = true;

            // 获取敌人层级
            enemyLayer = LayerMask.NameToLayer("Enemy");
        }

        private void Update()
        {
            if (Time.time - lastAttackTime > comboResetTime)
            {
                comboCount = 0; // 连击计时超过阈值，重置连击计数器
            }
            if (!animator) animator = pCtrl.GetAnimator();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!animator.GetBool("isAttacking") || other.gameObject.layer != enemyLayer || hitEnemies.Contains(other))
                return;
            
            var idmgb = other.GetComponent<IDamageable>();
            if (idmgb != null)
            {
                var dmg = pCtrl.GetDamage();
                UIManager.Instance.ShowMessage1("Made " + dmg + " Damage");
                // idmgb.TakeDamage(dmg); // Inflict damage on enemies
                State.Instance.MakeDamage(idmgb,dmg);
                hitEnemies.Add(other); // 记录已攻击过的敌人
            }
        }

        private void HandleAttackEnded()
        {
            animator.SetBool("isAttacking", false);
            comboCount += hitEnemies.Count; // 更新连击计数器
            lastAttackTime = Time.time; // 记录最近一次攻击时间
            hitEnemies.Clear();
            CheckVampiricMode(); // 检查吸血模式
        }
        
        private void CheckVampiricMode()
        {
            if (State.State_VampiricMode.None == State.Instance.StateVampiricMode)
            {
                if (comboCount > comboThreshold)
                {
                    // 进入 default 模式
                    State.Instance.StateVampiricMode = State.State_VampiricMode.Default;
                    pCtrl._effectTimeManager.CreateEffectBar("VampiricMode", new Color(255, 102, 51), comboResetTime);
                    UIManager.Instance.ShowMessage1("Entered Default Vampiric Mode");
                }
            }else
            {
                if (comboCount > comboThreshold + 7)
                {
                    // 进入 Violent 模式
                    State.Instance.StateVampiricMode = State.State_VampiricMode.Violent;
                    var time = PlayerController.Instance._effectTimeManager.GetEffectProgress("VampiricMode") * comboResetTime;
                    PlayerController.Instance._effectTimeManager.StopEffect("VampiricMode");
                    pCtrl._effectTimeManager.CreateEffectBar("VampiricMode", Color.red, time + 3f);
                }
                if (comboCount > comboThreshold + 4)
                {
                    // 进入 Focused 模式
                    State.Instance.StateVampiricMode = State.State_VampiricMode.Focused;
                    var time = PlayerController.Instance._effectTimeManager.GetEffectProgress("VampiricMode") * comboResetTime;
                    PlayerController.Instance._effectTimeManager.StopEffect("VampiricMode");
                    pCtrl._effectTimeManager.CreateEffectBar("VampiricMode", Color.yellow, time + 2f);
                }
            }

            // 其他逻辑，根据 comboCount 更新吸血模式和吸血计时器
            // ...
        }

        public void BehaviourOnHolderDie()
        {
            if(hasDoneDieBehaviour) return;
            hasDoneDieBehaviour = true;
            StartCoroutine(SwordOffHand());
        }

        private IEnumerator SwordOffHand()
        {
            var demonicSword = transform.parent.gameObject;
            var swrb = demonicSword.GetComponent<Rigidbody>();
            
            yield return new WaitForSeconds(0.8f);
            
            demonicSword.transform.SetParent(null);
            swrb.isKinematic = false;
            swrb.useGravity = true;
            // swordCollider.providesContacts = true;
            swordCollider.isTrigger = false;
            GetComponent<CapsuleCollider>().enabled = true;
            // swrb.AddForce(swrb.velocity.normalized * (swrb.mass * 4f), ForceMode.Impulse);
            swrb.AddForce(Vector3.back * (-2f * (swrb.mass * 2f)), ForceMode.Impulse);
        }
        
    }
}