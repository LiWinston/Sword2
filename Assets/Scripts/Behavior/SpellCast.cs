using System.Collections;
using System.Collections.Generic;
using AttributeRelatedScript;
using Behavior.Effect;
using Behavior.Health;
using Game;
using UI;
using UnityEngine;
using Utility;

namespace Behavior
{
    public class SpellCast : MonoBehaviour
    {
        private Animator animator;
        [SerializeField] internal Transform spellingPartTransform; // 施法的手
        [SerializeField] internal Transform innerSpellingTransform; // 施法的腰子
        [SerializeField] private float spellRange = 1.6f;
        private State state;
        private EffectTimeManager _effectTimeManager;
        
        [SerializeField] internal float JZZCostRate = 0.2f;
        Coroutine jzzCoroutine = null;
        ParticleSystem jzzi = null;
        [SerializeField] private KeyCode ExtremeColdKey = KeyCode.Mouse1;
        [SerializeField] private KeyCode GoldenBellKey = KeyCode.R;
        [SerializeField] private KeyCode ULTKey = KeyCode.Q;
        [SerializeField] private float basicMindcontrolDuration = 12f;


        private void Awake(){
            _effectTimeManager = GetComponent<EffectTimeManager>();
        }

        void Start()
        {
            IconManager.Instance.InitIconWithKeyBinding("ExtremeCold", ExtremeColdKey);
            IconManager.Instance.InitIconWithKeyBinding("GoldenBell", GoldenBellKey);
            IconManager.Instance.InitIconWithKeyBinding("Autophagy", ULTKey);//ULT
            state = GetComponent<State>();
            if (spellingPartTransform == null)
            {
                Debug.LogError("Weapon Transform 未指定，请在 Inspector 中将 Weapon 物体拖放到该字段中！");
            }

            if (innerSpellingTransform == null) innerSpellingTransform = Find.FindDeepChild(transform, "spine_03");
            var childTransform = transform.Find("Model");
            if (childTransform != null)
            {
                animator = childTransform.GetComponent<Animator>();
            }

            if (animator == null)
            {
                Debug.LogError("找不到 Animator 组件！");
            }
        }

        void Update()
        {
            if (Input.GetKeyDown(ExtremeColdKey))
            {
                PlayerController.Instance.isCrouching = false;
                animator.SetTrigger("Cast");
                CastSpell();
            }

            if (Input.GetKeyDown(ULTKey))
            {
                PlayerController.Instance.isCrouching = false;
                animator.SetTrigger("ULT");
                CastUlt();
            }
        
            if (Input.GetKeyDown(GoldenBellKey))
            {
                if(state.isJZZ) return;
                PlayerController.Instance.isCrouching = false;
                animator.SetTrigger("Cast");
                StartJZZ();
            }
        }

        private void StartJZZ()
        {
            if (!state.ConsumeEnergy(state.maxEnergy * JZZCostRate)) return;
            IconManager.Instance.ShowIcon(IconManager.IconName.GoldenBell);
            SoundEffectManager.Instance.PlaySound(new List<string>(){"Music/音效/法术/JZZ1","Music/音效/法术/JZZ2"}, gameObject);
            
            state.isJZZ = true;
            float d = 0;
            // var JZZPfbName = state.GetCurrentLevel() > 30 ? "JZZ2" : "JZZ";
            string JZZPfbName;
            switch (state.GetCurrentLevel())
            {
                case < 20:
                    JZZPfbName = "JZZ0";
                    d = 7f;
                    break;
                case < 40:
                    JZZPfbName = "JZZ";
                    d = 8f;
                    break;
                case < 60:
                    JZZPfbName = "JZZ2";
                    d = 9f;
                    break;
                default:
                    JZZPfbName = "JZZ2";
                    d = 10f;
                    break;
            }
            _effectTimeManager.CreateEffectBar("JZZ", Color.cyan, d);
            // GameObject.Find("Canvas").GetComponent<EffectTimeManager>().CreateEffectBar("JZZ", Color.cyan, 7f);
            
            ParticleSystem JZZ = Resources.Load<ParticleSystem>("Prefab/Skills/" + JZZPfbName);
            if(JZZ == null) Debug.LogError("NO JZZ" + JZZPfbName);
            
            jzzi = Instantiate(JZZ, innerSpellingTransform);
            jzzi.Play();
            
            jzzCoroutine = StartCoroutine(StopJZZAfterDuration(d));
            StartCoroutine(ObservePower(d, jzzCoroutine, jzzi));
        }

        //JZZ observer, once power too low exit JZZ mode
        private IEnumerator ObservePower(float f, Coroutine c, ParticleSystem pts)
        {
            float timer = 0.0f;
    
            while (state.isJZZ && timer < f)
            {
                timer += Time.deltaTime;

                for (float waitTime = 0; waitTime < 0.5f; waitTime += Time.deltaTime)
                {
                    yield return null;
                }

                if (state.CurrentPower < 10)
                {
                    ReturnEnergy();
                    state.isJZZ = false;
                    StopCoroutine(c);
                    _effectTimeManager.StopEffect("JZZ");
                    if (pts != null)
                    {
                        Destroy(pts.gameObject);
                    }
                }

                yield return null;
            }
            _effectTimeManager.StopEffect("JZZ");
            // GameObject.Find("Canvas").GetComponent<EffectTimeManager>().StopEffect("JZZ");
            Destroy(pts.gameObject);
            yield return null;
        }


        private IEnumerator StopJZZAfterDuration(float t)
        {
            // ParticleEffectManager.Instance.PlayParticleEffect("JZZ", innerSpellingTransform.gameObject, Quaternion.identity, Color.clear, Color.clear,t);
            yield return new WaitForSeconds(t);
            // 停止金钟罩效果
            state.isJZZ = false;
            _effectTimeManager.StopEffect("JZZ");
            IconManager.Instance.HideIcon(IconManager.IconName.GoldenBell);
            // GameObject.Find("Canvas").GetComponent<EffectTimeManager>().StopEffect("JZZ");
        }

        public void StopJZZ(bool returnEnergy = false)
        {
            if (state.isJZZ)
            {
                //若已放完 不再返还能量 否则按比例返还
                if(returnEnergy) ReturnEnergy();
                
                state.isJZZ = false;
                _effectTimeManager.StopEffect("JZZ");
                if(jzzCoroutine != null) StopCoroutine(jzzCoroutine);
                if (jzzi != null)
                {
                    Destroy(jzzi.gameObject);
                }
                IconManager.Instance.HideIcon(IconManager.IconName.GoldenBell);
            }
        }
        private void ReturnEnergy()
        {
            state.CurrentEnergy += state.maxEnergy * JZZCostRate *_effectTimeManager.GetEffectProgress("JZZ");
        }
        
        private void CastSpell()
        {
            //TODO:更新此机制。
            if (!state.ConsumeEnergy(state.CurrentDamage))
            {
                return;
            };
            // 检查是否成功获取了 Weapon 物体的引用
            if (spellingPartTransform != null)
            {
                ParticleEffectManager.Instance.PlayParticleEffect("Spell", spellingPartTransform.gameObject,
                    Quaternion.identity,
                    Color.white, Color.white, 1f);
            }
            else
            {
                Debug.LogError("无法播放特效，因为 Weapon Transform 未指定！");
            }
            IconManager.Instance.ShowIcon(IconManager.IconName.ExtremeCold);
            
            SoundEffectManager.Instance.PlaySound("Music/音效/法术/极寒", spellingPartTransform.gameObject);
            // 检测在法术范围内的敌人 TODO:??? Layer就不行==要GetMask
            Collider[] hitEnemies = Physics.OverlapSphere(transform.position, spellRange, LayerMask.GetMask("Enemy"));
            // Debug.LogWarning("检测到 "+hitEnemies.Length + "Enemy");
            // Collider[] hitEnemies = Physics.OverlapSphere(playerPosition, spellRange);
            foreach (Collider enemy in hitEnemies)
            {
                // 检查是否敌人
                if (enemy.CompareTag("Enemy"))
                {
                    // 获取敌人的 HealthSystem 组件
                    HealthSystem enemyHealth = enemy.GetComponent<HealthSystemComponent>().GetHealthSystem();

                    if (enemyHealth != null)
                    {
                        // 对敌人造成伤害
                        float damageAmount = state.CurrentDamage;
                        enemyHealth.Damage(damageAmount);
                    
                        // 计算持续掉血的总量（20％的伤害）
                    
                        float freezeRemainingTime = 5f + state.GetCurrentLevel() * 0.2f / 10f;
                        float continuousDamageAmount = damageAmount * 0.2f;
                        enemy.GetComponent<IFreezable>().ActivateFreezeMode(freezeRemainingTime, continuousDamageAmount);
                    
                        // 播放特效
                        //挪到mst里了
                        
                    }
                }
            }
        }

        // 协程来实现持续掉血

        
        private float getUltEnergyConsumption()
        {
            if(GameSceneManager.Instance.IsFinalBattle) return state.maxEnergy * 0.1f;
            return state.CurrentDamage * 1.5f;
        }
    
        private void CastUlt()
        {
            if (!state.ConsumeEnergy(getUltEnergyConsumption()))
            {
                return;
            };
            IconManager.Instance.ShowIcon(IconManager.IconName.Autophagy);
            SoundEffectManager.Instance.PlaySound("Music/音效/法术/ULT", spellingPartTransform.gameObject);
            
            // 检查是否成功获取了 Weapon 物体的引用
            if (spellingPartTransform != null)
            {
                ParticleEffectManager.Instance.PlayParticleEffect("ULT", spellingPartTransform.gameObject,
                    Quaternion.identity,
                    Color.white, Color.white, 1.2f);
            }
            ParticleEffectManager.Instance.PlayParticleEffect("ULT1", innerSpellingTransform.gameObject,
                Quaternion.identity,
                Color.white, Color.white, 3f);
            // 获取玩家的位置
            Vector3 playerPosition = transform.position;

            // 检测在法术范围内的敌人
            Collider[] hitEnemies = Physics.OverlapSphere(playerPosition, spellRange);
            List<Collider> enemies = new List<Collider>();
            foreach (Collider enemy in hitEnemies)
            {
                // 检查是否敌人
                if (enemy.CompareTag("Enemy"))
                {
                    enemies.Add(enemy);
                    // 获取敌人的 HealthSystem 组件
                    HealthSystem enemyHealth = enemy.GetComponent<HealthSystemComponent>().GetHealthSystem();
                    if (enemyHealth != null)
                    {
                        // 对敌人造成伤害
                        enemyHealth.Damage(state.CurrentDamage * 0.5f);
                        // 播放特效
                        if (spellingPartTransform != null)
                        {
                            Transform spineTransform = Find.FindDeepChild(enemy.transform, "spine_01");
                            ParticleEffectManager.Instance.PlayParticleEffect("HitByUlt", (spineTransform != null ? spineTransform : enemy.transform).gameObject, 
                                Quaternion.identity, Color.white, Color.blue, 1.8f);
                        }
                    }
                }
            }
            StartCoroutine(MindControl(enemies));
        }

        IEnumerator MindControl(List<Collider> enemies)
        {
            yield return new WaitForSeconds(1.4f);
            foreach (var e in enemies)
            {
                
                if (state.ConsumeEnergy(0.015f*e.GetComponent<HealthSystemComponent>().GetHealthSystem().GetHealth()))
                {
                    var eMstbhv = e.GetComponent<MonsterBehaviour>();
                    if(eMstbhv != null) eMstbhv.ActivateSelfKillMode(basicMindcontrolDuration + state.GetCurrentLevel() * 0.1f);
                }
            }
        }
    }
}