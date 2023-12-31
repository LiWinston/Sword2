using System.Collections;
using Behavior.Effect;
using Behavior.Health;

using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using Utility;
using IPoolable = Utility.IPoolable;
using Random = UnityEngine.Random;
using State = AttributeRelatedScript.State;
using Target = UI.OffScreenIndicator.Target;
using AttributeRelatedScript;
using System.Linq;

namespace Behavior
{
    public class MonsterBehaviour : MonoBehaviour, IFreezable, IPoolable, IDamageable
    {
        public PlayerController targetPlayer;
        internal GameObject target;
        private LayerMask enemyLayer;
        private LayerMask playerLayer;
        public bool isBoss = false;

        private Rigidbody rb;
        internal NavMeshAgent agent;
        public Animator animator;
        internal HealthSystem health;
        [FormerlySerializedAs("mstForwardForce")] [SerializeField] private float mstAcceleration = 200;
        private float attackCooldownTimer;
        [SerializeField] internal float attackCooldownInterval = 2f;
        // private float moveForceTimerCounter;
        // [SerializeField] private float moveForceCooldownInterval = 0.05f;
        private float obstacleDetectionTimer = 0f;
        public float obstacleDetectionInterval = 3f; // 检测间隔，每隔3秒检测一次
    
        [SerializeField] internal float minAttackPower = 5;
        [SerializeField] internal float maxAttackPower = 10;
    
    
        public float rotationSpeed = 2f; // 调整旋转速度
     
        // private float gameTime = Time.timeSinceLevelLoad;
        internal float monsterLevel;
        internal int monsterExperience;
        internal PositiveProportionalCurve healthCurve;

        [SerializeField] private float aimDistance;
        [SerializeField] private float chaseDistance;
        // [SerializeField] private float stalkMstSpeed = 1f;
        [FormerlySerializedAs("MaxSpeed")] [FormerlySerializedAs("MaxMstSpeed")] [SerializeField] private float maxSpeed = 3f;
        // [SerializeField] private float stalkAccRatio = 0.8f;
        [SerializeField] internal float attackDistance = 1.5f;
        private bool isMoving;
        private State _state;
        private float curDistance;

        [InspectorLabel("Freeze")]
        private bool isFrozen; // 表示怪物是否处于冰冻状态
        private float originalAcceleration;
        private float originalAttackCooldownInterval;
        private float originalMaxMstSpeed;
        Transform spineTransform;
    
        
        private Target _targetComponent;
        private static readonly int Die = Animator.StringToHash("Die");
        internal EffectTimeManager _effectTimeManager;
        
        private bool _hasAppliedDeathEffect = false;

        public ObjectPool<GameObject> ThisPool { get; set; }
        public bool IsExisting { get; set; }

        public void SetPool(UnityEngine.Pool.ObjectPool<GameObject> pool)
        {
            ThisPool = pool;
        }

        public void actionOnGet()
        {
            gameObject.SetActive(true);
            _effectTimeManager.StopEffect("SelfKill");
            _effectTimeManager.StopEffect("Freeze");
            //Debug RT error, health Curve Init on get
            healthCurve = GetComponents<Component>().OfType<PositiveProportionalCurve>().FirstOrDefault(curve => curve.CurveName == "MonsterHealthLevelCurve");
            HealthSystemComponent healthSystemComponent = GetComponent<HealthSystemComponent>();
            if (healthSystemComponent != null)
            {
                health = healthSystemComponent.GetHealthSystem();
                // UIManager.ShowMessage2("health 已找到.");
            }
            InitializeMonsterLevel();
            _hasAppliedDeathEffect = false;
            target = PlayerController.Instance.gameObject;
            health.SetHealthMax(monsterLevel * 100 +100, true);
            initialAgent1();
            
        }

        public void actionOnRelease()
        {
            rb.velocity = Vector3.zero;
            if(!freezeEffectCoroutine.IsUnityNull())StopCoroutine(freezeEffectCoroutine);
            IsInSelfKill = false;
            _targetComponent.targetColor = Color.red;
        }

        public void Release()
        {
            ThisPool.Release(gameObject);
        }


        private void Awake()
        {
            _effectTimeManager = GetComponent<EffectTimeManager>();
            target = PlayerController.Instance.gameObject;
            enemyLayer = LayerMask.GetMask("Enemy");
            playerLayer = LayerMask.GetMask("Player");
            _targetComponent = GetComponent<Target>();
            spineTransform = Find.FindDeepChild(transform, "spine_01");
        }
    
        private void Start()
        {
            target = PlayerController.Instance.gameObject;
            targetPlayer = PlayerController.Instance;

            initialAgent1();
            
            if(_effectTimeManager == null) _effectTimeManager = GetComponent<EffectTimeManager>();
            if (targetPlayer == null)
            {
                Debug.LogWarning("No GameObject with the name 'Player' found in the scene.");
            }
            _state = targetPlayer.GetComponent<State>();
            animator = GetComponentInChildren<Animator>();
            if (animator == null)
            {
                Debug.LogWarning("No Animator Compoment found.");
            }
            // 在子对象中查找 Rigidbody
            rb = GetComponentInChildren<Rigidbody>();
            if (rb != null)
            {
                // UIManager.ShowMessage2("MST 内部 Rigidbody找到.");
            }

            // 获取 HealthSystemComponent，并从中获取 HealthSystem
            HealthSystemComponent healthSystemComponent = GetComponent<HealthSystemComponent>();
            if (healthSystemComponent != null)
            {
                health = healthSystemComponent.GetHealthSystem();
                // UIManager.ShowMessage2("health 已找到.");
            }
            // 初始化怪物经验值和等级
            OriginalMoveForce = mstAcceleration;
            OriginalAttackCooldownInterval = attackCooldownInterval;
            OriginalMaxMstSpeed = MaxSpeed;
            // 初始化怪物经验值和等级
            healthCurve = GetComponents<Component>().OfType<PositiveProportionalCurve>().FirstOrDefault(curve => curve.CurveName == "MonsterHealthLevelCurve");
            InitializeMonsterLevel();
            
        }

        public IEnumerator initialAgent(){
            // yield return new WaitForSeconds(0.1f);
            yield return null;
            if(agent == null) agent = GetComponent<NavMeshAgent>();
            agent.enabled = true;
            agent.SetDestination(target.transform.position);
            agent.angularSpeed = rotationSpeed;
            
        }
        public void initialAgent1(){
            // yield return new WaitForSeconds(0.1f);
            // yield return null;
            if(agent == null) agent = GetComponent<NavMeshAgent>();
            agent.enabled = true;
            agent.SetDestination(target.transform.position);
            agent.angularSpeed = rotationSpeed;
            if (isBoss) agent.angularSpeed = 99999;

        }

        private void Update()
        {
            if (health.IsDead() && !_hasAppliedDeathEffect)
            {
                _hasAppliedDeathEffect = true;
                _state.AddExperience(this.monsterExperience);
                targetPlayer.ShowPlayerHUD("EXP " + this.monsterExperience);
                DeactiveAllEffect();
                StartCoroutine(nameof(PlayDeathEffects));
                return;
            }
            
            agent.speed = maxSpeed;
            agent.SetDestination(target.transform.position);
            agent.acceleration = mstAcceleration;
            // agent.angularSpeed = rotationSpeed;
            
            isMoving = rb.velocity.magnitude > 0.01f;
            animator.SetBool("isMoving", isMoving);

            // Decrease the move force cooldown timer  
            // moveForceTimerCounter -= Time.deltaTime;

            // Decrease the attack cooldown timer
            attackCooldownTimer -= Time.deltaTime;

            if (IsInSelfKill)
            {
                target = PickAlly();//距离更新已经写在择敌方法中
            }
            else
            {
                target = targetPlayer.GameObject();
                curDistance = Vector3.Distance(transform.position, target.transform.position);
            }
        
            if (curDistance <= chaseDistance && curDistance > attackDistance)
            {
                obstacleDetectionTimer -= Time.deltaTime;

                isMoving = rb.velocity.magnitude > 0.01f;
                animator.SetBool("isMoving", isMoving);

                // 如果计时器小于等于0，进行障碍物检测
                if (obstacleDetectionTimer <= 0f)
                {
                    // 在此处进行障碍物检测逻辑，包括尝试跳跃和避免障碍物的移动逻辑
                    ObstacleHandle();

                    // 重置计时器
                    obstacleDetectionTimer = obstacleDetectionInterval;
                }
                // if (rb.velocity.magnitude < MaxSpeed)
                // {
                //     // rb.AddForce(transform.forward * mstForwardForce, ForceMode.Force);
                //     moveForceTimerCounter = moveForceCooldownInterval;
                // }
            }
            else if (target && curDistance < attackDistance && attackCooldownTimer <= 0)
            {
                Attack();
                attackCooldownTimer = attackCooldownInterval;
            }
            else//追击距离外 瞄准距离内
            {
                animator.SetBool("Near",false);
            }
        }

        void FixedUpdate()
        {
            if (curDistance <= aimDistance) //追击距离内
            {
                animator.SetBool("Near",true);
            }
            var directionToPly = target.transform.position - transform.position;
            directionToPly.y = 0;
            directionToPly.Normalize();
            Quaternion targetRotation = Quaternion.LookRotation(directionToPly);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
        }
        
        private GameObject PickAlly()
        {
            if (target.layer == enemyLayer && !target.GetComponent<MonsterBehaviour>().health.IsDead())
            {
                Debug.LogWarning("keep on target");
                return target;
            }
            // get all enemies near the player
            Collider[] nearEnemies = Physics.OverlapSphere(transform.position, chaseDistance, enemyLayer);

            // initialise the nearest enemy to the player
            // GameObject nearestEnemy = null;
            // float nearestDistance = float.MaxValue;
            //
            // // cycle through all enemy
            // foreach (Collider enemyCollider in nearEnemies)
            // {
            //     if (enemyCollider.gameObject == gameObject) continue;
            //     //  check if enemy is still alive
            //     MonsterBehaviour enemyMonster = enemyCollider.GetComponent<MonsterBehaviour>();
            //     if (enemyMonster != null && !enemyMonster.health.IsDead())
            //     {
            //         // 计算距离
            //         float distance = Vector3.Distance(transform.position, enemyCollider.transform.position);
            //
            //         // 如果找到更近的敌人，更新最近敌人和距离
            //         if (distance < nearestDistance)
            //         {
            //             nearestEnemy = enemyCollider.gameObject;
            //             nearestDistance = distance;
            //         }
            //     }
            // }
            GameObject nearestEnemy = null;
            float nearestDistance = float.MaxValue;

            // cycle through all enemy
            foreach (Collider enemyCollider in nearEnemies)
            {
                if (enemyCollider.gameObject == gameObject) continue;
                //  check if enemy is still alive
                MonsterBehaviour enemyMonster = enemyCollider.GetComponent<MonsterBehaviour>();
                if (enemyMonster != null && !enemyMonster.health.IsDead())
                {
                    // 计算距离
                    float distance = Vector3.Distance(transform.position, enemyCollider.transform.position);

                    // 如果找到更近的敌人，更新最近敌人和距离
                    if (distance < nearestDistance)
                    {
                        nearestEnemy = enemyCollider.gameObject;
                        nearestDistance = distance;
                    }
                }
            }

            curDistance = nearestDistance;
        
            if (nearestEnemy != null)
            {
                return nearestEnemy;
            }
            else
            {
                // 如果没有找到最近的敌人，返回当前目标
                return gameObject;
            }
        }

        private void ObstacleHandle()
        {
            RaycastHit hit;
            if (Physics.Raycast(transform.position, transform.forward, out hit, 0.4f))
            {
                // 如果检测到障碍物，施加向上的力以跳跃
                rb.AddForce(Vector3.up * 1000, ForceMode.Impulse); // 添加跳跃力
                rb.AddForce(transform.forward * mstAcceleration, ForceMode.Impulse); // 添加向前的力
            }
        }
        
    
        private void Attack()
        {
            // UIManager.Instance.ShowMessage1("One Punch!");
            if (!IsInSelfKill)
            {
                animator.SetTrigger("AttackTrigger");
                targetPlayer.TakeDamage(5 + monsterLevel/20 * Random.Range(minAttackPower, maxAttackPower));
            }
            else
            {
                if (target.gameObject != gameObject)
                {
                    animator.SetTrigger("AttackTrigger");
                }
                target.GetComponent<IDamageable>().TakeDamage(70 + monsterLevel/20 * Random.Range(minAttackPower, maxAttackPower));
            }
        }

        public void TakeDamage(float dmg)
        {
            animator.SetTrigger("Hurt");
            health.Damage(dmg);
        }

        private IEnumerator PlayDeathEffects()
        {
            animator.SetTrigger(Die);
            ParticleEffectManager.Instance.PlayParticleEffect("MonsterDie", this.gameObject, Quaternion.identity, Color.red, Color.black, 1.2f);
            yield return new WaitForSeconds(1.2f);
            // Destroy(this.gameObject);
            if (isBoss) SceneManager.LoadScene("WinScene");
            
            Release();
        }

        //TODO:逻辑待更新。
        private void InitializeMonsterLevel()
        {
            if (isBoss)
            {
                //TODO : boss的等级和经验值
                monsterLevel = 75;
                health.SetHealthMax(1200000, true);
                monsterExperience = 9999;
                minAttackPower = 18;
                maxAttackPower = 36f;
                return;
            }
            // 计算怪物等级，使其在五分钟内逐渐增长到最大等级
            float maxGameTime = 300f; // 300秒
            float progress = Mathf.Clamp01(Time.timeSinceLevelLoad / maxGameTime); // 游戏时间进度（0到1之间）
            monsterLevel = progress * 100 + 1; // 从1到100逐渐增长
            monsterExperience = Mathf.FloorToInt(monsterLevel * 1.2f);
            // health.SetHealthMax(monsterLevel * 300 +100, true);//100
            health.SetHealthMax(healthCurve.CalculateValueAt(monsterLevel), true);
        }

        public Rigidbody Rb => rb;
        public bool IsFrozen { get => isFrozen; set => isFrozen = value; }
        public float OriginalMaxMstSpeed { get => originalMaxMstSpeed; set => originalMaxMstSpeed = value; }
        public float MaxSpeed { get => maxSpeed; set=> maxSpeed = value; }
        public float OriginalMoveForce { get => originalAcceleration; set => originalAcceleration = value; }
        public float OriginalAttackCooldownInterval { get => originalAttackCooldownInterval; set => originalAttackCooldownInterval = value; }

        public void ActivateFreezeMode(float duration, float continuousDamageAmount, float instantVelocityMultiplier = 0.05f, float attackCooldownIntervalMultiplier = 2f, float MaxSpeedMultiplier = 0.18f)
        {
            // if(!freezeEffectCoroutine.IsUnityNull()) StopCoroutine(freezeEffectCoroutine);
            var time = duration;
            if(isBoss) time = duration/3;
            DeactivateFreezeMode();
            freezeEffectCoroutine = StartCoroutine(FreezeEffectCoroutine(time, instantVelocityMultiplier, attackCooldownIntervalMultiplier, MaxSpeedMultiplier));
                    // 启动持续掉血的协程
            StartCoroutine(Effect.ContinuousDamage.MakeContinuousDamage(health, continuousDamageAmount, time ));
                        ParticleEffectManager.Instance.PlayParticleEffect("HitBySpell", (spineTransform != null ? spineTransform : transform).gameObject, 
                            Quaternion.identity, Color.red, Color.black, time);

            _effectTimeManager.StopEffect("Freeze");
            _effectTimeManager.CreateEffectBar("Freeze", Color.blue, time);
        }
        public Coroutine freezeEffectCoroutine { get; set; }

        public void DeactivateFreezeMode()
        {
            if(!freezeEffectCoroutine.IsUnityNull()) StopCoroutine(freezeEffectCoroutine);
            // 恢复原始推力和攻击间隔
            mstAcceleration = OriginalMoveForce;
            attackCooldownInterval = OriginalAttackCooldownInterval;
            MaxSpeed = OriginalMaxMstSpeed;
            IsFrozen = false;
            
            _effectTimeManager.StopEffect("Freeze");
        }
        public IEnumerator FreezeEffectCoroutine(float duration, float instantVelocityMultiplier = 0.1f, float attackCooldownIntervalMultiplier = 2f, float MaxSpeedMultiplier = 0.36f)
        {
            OriginalMoveForce = mstAcceleration;
            IsFrozen = true;
            Rb.velocity *= instantVelocityMultiplier;
            // 减小加速推力和增加攻击间隔
            mstAcceleration *= 0.6f; // 降低至60%
            attackCooldownInterval *= attackCooldownIntervalMultiplier; // 增加至200%
            MaxSpeed *= MaxSpeedMultiplier;
            // 等待冰冻效果持续时间
            yield return new WaitForSeconds(duration);

            // 恢复原始推力和攻击间隔
            mstAcceleration = originalAcceleration;
            attackCooldownInterval = OriginalAttackCooldownInterval;
            MaxSpeed = OriginalMaxMstSpeed;
            IsFrozen = false;
        }


    
        public void ActivateSelfKillMode(float elapseT)
        {
            DeactivateSelfKillMode();
            var time = elapseT;
            if(isBoss) time = elapseT/4;
            if(IsInSelfKill) StopCoroutine(selfKillCoroutine);
            selfKillCoroutine = StartCoroutine(SelfKillCoroutine(time));
            // Debug.Log("SelfKillMode Activated");
            _effectTimeManager.StopEffect("SelfKill");
            _effectTimeManager.CreateEffectBar("SelfKill", Color.white, time);
            // Debug.Log("SelfKill timerCpn Activated");
        }

        public void DeactivateSelfKillMode()
        {
            IsInSelfKill = false;
            _effectTimeManager.StopEffect("SelfKill");
            GetComponent<Target>().NeedBoxIndicator = false;
            MaxSpeed = originalMaxMstSpeed;
            if(!selfKillCoroutine.IsUnityNull()) StopCoroutine(selfKillCoroutine);
        }
        public Coroutine selfKillCoroutine { get; set; }

        private IEnumerator SelfKillCoroutine(float elapseT)
        {
            IsInSelfKill = true;
            MaxSpeed *= 1.2f;
            
            // var orgTargetColor = targetComponent.targetColor;
            // Color startColor = Color.green;
            float elapsedTime = 0f;

            while (elapsedTime < elapseT)
            {
                // float t = Mathf.Clamp01(elapsedTime / elapseT);
                // targetComponent.targetColor = Color.Lerp(startColor, orgTargetColor, t);

                yield return new WaitForSeconds(1f); // 等待1秒钟
                elapsedTime += 1f;
            }

            // Ensure the final color is exactly the original color.
            // targetComponent.targetColor = orgTargetColor;

            yield return null; // 保证协程执行完整

            IsInSelfKill = false;
        }

        public bool IsInSelfKill { get; set; }
        private void DeactiveAllEffect()
        {
            DeactivateSelfKillMode();
            DeactivateFreezeMode();
        }
    }
    
}
