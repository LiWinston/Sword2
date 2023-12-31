using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AttributeRelatedScript;
using Behavior.Effect;
using Behavior.Health;
using ItemSystem;
using TMPro;
using UI;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using Utility;
using Cursor = UnityEngine.Cursor;
using Random = UnityEngine.Random;
using State = AttributeRelatedScript.State;


namespace Behavior
{
    public class PlayerController : MonoBehaviour, IDamageable, IFreezable
    {
        private static PlayerController _instance;
        public static PlayerController Instance
        {
            get
            {
            // 如果实例尚未创建，创建一个新实例
            if (_instance == null)
            {
                Debug.LogError("NO PLAYERController");
            }
            return _instance;
            }
        }

        public void Awake()
        {
            spineTransform = Find.FindDeepChild(transform, "clavicle_l");
            _effectTimeManager = GetComponent<EffectTimeManager>();
            _instance = this;
        }

        internal Rigidbody rb;
        [SerializeField]Transform viewPoint;
    
        [Header("Movement Settings")]
        public float crouchForceRate = 0.95f;
        [SerializeField] private float MaxCrouchPlySpeed = 1f;
        [SerializeField] private float MaxPlySpeed = 1.5f;
        [SerializeField] private float sprintSpeedRate = 1.5f;
    
        [Header("Mouse Look Settings")]
        public float mouseSensitivity = 100f;

        internal bool isMoving = false;
        internal bool isJumping = false;
        internal bool isCrouching = false;
        public float forwardForce = 100;
        public float backwardRate = 0.7f;
        public float jumpForce = 200;
    
    
        // private bool isClimbing = false;
    
        private Vector3 moveDirection;

        [SerializeField] private TextMeshPro textMeshProComponent;
    
        private float lastAttackTime = 0f; 
    
        [SerializeField]internal Camera mycamera;
    
    
        internal Animator animator;
        internal AudioSource audioSource;

        private float xRotation = 0f;

        private bool isNextAttackCritical = false;
        public delegate void OnAttackEndedHandler();
        public event OnAttackEndedHandler OnAttackEnded;

        [InspectorLabel("Freeze")]
        private bool isFrozen;
        private float originalAcceleration;
        private float originalAttackCooldownInterval;
        private float originalMaxPlySpeed;
        Transform spineTransform;
        private EffectTimeManager _effectTimeManager;
        
        public bool IsCrouching
        {
            get { return isCrouching; }
            set { isCrouching = value; }
        }

        private Vector3 originalPosition;
        [SerializeField] private float MAX_ALLOWED_INTERACT_RANGE = 3;
        internal bool isGrounded;
        public float moveForceTimer = 0.05f;
        public float moveForceTimerCounter = 0.05f;
    
        [FormerlySerializedAs("handTransform")] [SerializeField] internal Transform pickHandTransform;
        [FormerlySerializedAs("swordTransform")] [SerializeField] internal GameObject swordObject;
        private float speed_Ratio_Attack = 0.1f;
        public float rotationFriction = 4000f; // 调整旋转摩擦力的大小
        internal State state;
        [SerializeField] private Transform sword;
        internal bool cheatMode = false;
        private PositiveProportionalCurve _criticalHitCurve;
        private static readonly int AttSpeedMult = Animator.StringToHash("AttSpeedMult");
        private static readonly int IsGrounded = Animator.StringToHash("isGrounded");
        private static readonly int Standing = Animator.StringToHash("Standing");
        private static readonly int IsMoving = Animator.StringToHash("isMoving");
        private static readonly int BeginCrouch = Animator.StringToHash("BeginCrouch");
        private static readonly int Crouching = Animator.StringToHash("isCrouching");
        private static readonly int Jump = Animator.StringToHash("Jump");
        private static readonly int RunningJump = Animator.StringToHash("RunningJump");
        private static readonly int IsAttacking = Animator.StringToHash("isAttacking");
        // private static readonly int Die = Animator.StringToHash("Die");
        private static readonly int IsDead = Animator.StringToHash("isDead");

        private bool  hasActiveKeyBindings;

        private void Start()
        {
            hasActiveKeyBindings = false;
            IconManager.Instance.InitIconWithKeyBinding(IconManager.IconName.HurricaneKick, KeyCode.V);
            IconManager.Instance.InitIconWithKeyBinding(IconManager.IconName.Sprint, KeyCode.LeftShift);
            audioSource = GetComponent<AudioSource>();
            // _criticalHitCurve = GetComponent<PositiveProportionalCurve>();
            _criticalHitCurve = GetComponents<Component>().OfType<PositiveProportionalCurve>().FirstOrDefault(curve => curve.CurveName == "CriticalHitCurve");
            if(_criticalHitCurve == null) Debug.LogError("CriticalHitCurve not found!");
            if (!textMeshProComponent) textMeshProComponent = Find.FindDeepChild(transform, "PlayerHUD").GetComponent<TextMeshPro>();
            state = GetComponent<State>();
            rb = GetComponent<Rigidbody>();
            moveDirection = Vector3.zero;
            var model = transform.Find("Model"); 
            if (model != null)
            {
                animator = model.GetComponent<Animator>();
            }
            else
            {
                Debug.LogError("Didnt Find 'model'!");
            }

            viewPoint = Find.FindDeepChild(transform, "root");//调一下得了 不粘贴过来了
            sword = Find.FindDeepChild(transform, "blade");
            if (viewPoint == null)
            {
                Debug.LogError("viewPoint not found!");
            }
            if (animator == null)
            {
                Debug.LogError("Didnt Find animator!");
            }
        
            Cursor.lockState = CursorLockMode.Locked;
            originalPosition = transform.position;
            if (Camera.main != null) Camera.main.transform.rotation = Quaternion.identity; // 正前方
            animator.SetFloat("AttSpeedMult",1f);
            animator.Play("Getting_Up");
            
            state.UpdateAttackCooldown();//Solve init upd error
            
            
            //Freeze
            originalAcceleration = forwardForce;
            originalAttackCooldownInterval = state.AttackCooldown;
            originalMaxPlySpeed = MaxPlySpeed;
        }

        private void Update()

        {
            if (!hasActiveKeyBindings)
            {
                hasActiveKeyBindings = true;
                StartCoroutine(ShowKeys());
            }
            if (Mathf.Abs(rb.velocity.y) < 5e-3)
            {
                isGrounded = true;
                isJumping = false; // reset
            }
            else
            {
                isGrounded = false;
            }
            animator.SetBool(IsGrounded,isGrounded);
        
            isMoving = math.abs(rb.velocity.x) + math.abs(rb.velocity.z) > 0.1f;

            animator.SetBool(Standing,!isMoving && !isCrouching && !isJumping);
            animator.SetBool(IsMoving,isMoving);

            var horizontal = Input.GetAxis("Horizontal");
            var vertical = Input.GetAxis("Vertical");
            moveDirection = transform.right * horizontal + transform.forward * vertical;
        
            UserInput();
        

            if (Input.GetKeyDown(KeyCode.E))
            {
            
                // StartCoroutine("checkInteract");
                checkInteract();
            }
        
            if (Input.GetKeyDown(KeyCode.V)) 
            {
                IsCrouching = false;
                HurricaneKick();
            }
        
            // Crouch
            if (Input.GetKeyDown(KeyCode.LeftControl))
            {
                animator.SetTrigger(BeginCrouch);
                isCrouching = true;
            }
        
            if(Input.GetKeyUp(KeyCode.LeftControl))
            {
                isCrouching = false;
            }
        

            // Crouch
            animator.SetBool(Crouching,isCrouching);
        
            // Mouse look
            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;
            xRotation -= mouseY;
            xRotation = Mathf.Clamp(xRotation, -90f, 90f);
            mycamera.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
            transform.Rotate(Vector3.up * mouseX);
        }

        private void HurricaneKick(){
            if (!state.ConsumePower(12.5f)) return;
            IconManager.Instance.ShowIcon(IconManager.IconName.HurricaneKick);
            animator.SetTrigger("HurricaneKickTrigger");
            
            rb.velocity = Vector3.zero;
            Vector3 playerPosition = transform.position;
            Collider[] hitEnemies = Physics.OverlapSphere(playerPosition, state.hurricaneKickRange);

            if (hitEnemies.Length != 0)
            {

                foreach (Collider cld in hitEnemies)
                {
                    if (cld.CompareTag("Enemy"))
                    {
                        Vector3 enemyPosition = cld.transform.position;

                        Vector3 knockbackDirection = (enemyPosition - playerPosition).normalized;

                        HealthSystem enemyHealth = cld.GetComponent<HealthSystemComponent>().GetHealthSystem();

                        cld.GetComponentInChildren<Animator>().SetTrigger("HurricaneKickTrigger");
                        if (enemyHealth != null)
                        {
                            enemyHealth.Damage(state.HurricaneKickDamage);
                            // UI.UIManager.Instance.ShowMessage2("What a Hurricane Kick!");
                            
                            // NavMeshAgent enemyNavMeshAgent = cld.GetComponent<NavMeshAgent>();
                            // if (enemyNavMeshAgent)
                            // {
                            //     enemyNavMeshAgent.velocity = knockbackDirection * state.hurricaneKickKnockbackForce;
                            // }
                            
                            Rigidbody enemyRigidbody = cld.GetComponent<Rigidbody>();
                            if (enemyRigidbody != null)
                            {
                                enemyRigidbody.AddForce(knockbackDirection * state.hurricaneKickKnockbackForce,
                                    ForceMode.VelocityChange);
                                // 计算随机切向方向（左或右）
                                // Vector3 randomTangentDirection =
                                //     Quaternion.Euler(0, Random.Range(-90f, 90f), 0) * knockbackDirection;
                            
                                // 计算旋转摩擦力，不依赖于当前角速度
                                // Vector3 rotationFrictionForce = randomTangentDirection * rotationFriction;
                            
                                // 将旋转摩擦力施加到切向方向
                                // enemyRigidbody.AddTorque(rotationFrictionForce * Random.Range(0.5f, 1.5f),
                                //     ForceMode.Impulse);
                            }
                        }
                    }
                }
            }
        }
    
    
        public void UserInput()
        {
            // if(_isDead) return;
            if (Input.GetMouseButtonDown(0) && Time.time - lastAttackTime >= state.AttackCooldown)
            {
                IsCrouching = false;
                isMoving = false;
                rb.velocity *= speed_Ratio_Attack;
                float criticalHitChance = _criticalHitCurve.CalculateValueAt(state.GetCurrentLevel());
                // Debug.Log(state.GetCurrentLevel() + "级暴击率" + criticalHitChance*100 +"%");

                var randomValue = Random.Range(0.0f, 1.0f);
                isNextAttackCritical = randomValue <= criticalHitChance;
                StartCoroutine(isNextAttackCritical ? CriticalAttack() : NormalAttack());
                lastAttackTime = Time.time;
            }
        
            if (Input.GetKeyDown(KeyCode.Space) ) // Add a check to see if a jump has been made
            {
                IsCrouching = false;
                if(isGrounded){
                    if(isJumping) return;
                    if(!isMoving){
                        rb.AddForce(transform.up * (isCrouching ? 10f * jumpForce : jumpForce), ForceMode.Impulse);
                        animator.SetTrigger(Jump);
                        isJumping = true;
                        isGrounded = false;
                    }else if(isMoving){
                        rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
                        animator.SetTrigger(RunningJump);
                        isJumping = true;
                        isGrounded = false;
                    }
                }
            
            }
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                IconManager.ShowKeyBinding();
            }
            
            // enabling cheat mode.
            if (Input.GetKeyDown(KeyCode.Delete))
            {
                if (cheatMode == false)
                {
                    ShowPlayerHUD("CHEAT MODE ON");
                    cheatMode = true;
                }
                else
                {
                    ShowPlayerHUD("CHEAT MODE OFF");
                    cheatMode = false;
                }
            }
            // levels up the player using L.
            if (Input.GetKeyDown(KeyCode.L))
            {
                if (cheatMode)
                {
                    state.CheatLevelUp();
                }
            }
            if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.K))
            {
                if (cheatMode)
                {
                    // enabling cheat code dealts max dmg to player, this becomes the suicide button
                    state.TakeDamage(999999999f);
                }
            }

            Vector3 moveDirection = Vector3.zero;

            if (Input.GetKey(KeyCode.W))
            {
                moveDirection += transform.forward * 1f;
            }
            if (Input.GetKey(KeyCode.S))
            {
                moveDirection -= transform.forward * 0.7f;
            }
            if (Input.GetKey(KeyCode.A))
            {
                moveDirection -= transform.right * 0.7f;
            }
            if (Input.GetKey(KeyCode.D))
            {
                moveDirection += transform.right * 0.7f;
            }

            if (moveDirection.magnitude > 2f)
            {
                moveDirection.Normalize();
            }
        
            if (Input.GetKey(KeyCode.LeftShift))
            {
                if (Vector3.zero != moveDirection)
                {
                    if(state.ConsumePower(4 * Time.deltaTime))
                    {
                        IconManager.Instance.ShowIcon(IconManager.IconName.Sprint);
                        float sprintSpeed = sprintSpeedRate * (isCrouching ? MaxCrouchPlySpeed : MaxPlySpeed) * (isFrozen ? 0.6f : 1f);
                        var v = moveDirection * sprintSpeed;
                        rb.velocity = new Vector3(v.x,rb.velocity.y , v.z);
                        // IconManager.Instance.HideIcon(IconManager.IconName.Sprint);
                        return;
                    }
                }
            }

            if (Input.GetKeyUp(KeyCode.LeftShift))
            {
                IconManager.Instance.HideIcon(IconManager.IconName.Sprint);
            }
            // current lo
            moveForceTimerCounter -= Time.deltaTime;
            if (!(moveForceTimerCounter <= 0))
            {
                return;
            }

            moveForceTimerCounter += moveForceTimer;
            var f = isCrouching ? crouchForceRate * forwardForce : forwardForce;

            rb.AddForce(moveDirection * f, ForceMode.Force);
            float maxSpeed = isCrouching ? MaxCrouchPlySpeed : MaxPlySpeed;
            rb.velocity = Vector3.ClampMagnitude(rb.velocity, maxSpeed);
        }

        public bool _isDead { get; set; }

        private IEnumerator PerformAttack(string attackTrigger, float attackDuration)
        {
            animator.SetTrigger(attackTrigger);
            animator.SetBool(IsAttacking, true);

            yield return new WaitForSeconds(attackDuration);

            EndAttack();
        }

        private IEnumerator NormalAttack()
        {

            float attackDuration = 0.9375f / state.attackAnimationSpeedRate;
            if (Random.Range(0f, 1f) >= 0.5f)
            {
                //范围较大的普通攻击 消耗一定体力
                if (state.ConsumePower(1.5f))
                {
                    // float attackDuration = 0.75f * 1.25f / state.attackAnimationSpeedRate;
                    yield return PerformAttack("AttackTrigger1", attackDuration);
                }
            }
            else
            {
                //反手胸前刺，不消耗体力
                // float attackDuration = 0.75f * 1.25f / state.attackAnimationSpeedRate;
                yield return PerformAttack("AttackTrigger2", attackDuration);
            }
        }

        private IEnumerator CriticalAttack()
        {
            if (state.ConsumePower(3.66f))
            {
                float attackDuration = 0.875f / (0.75f * state.attackAnimationSpeedRate);
                yield return PerformAttack("CriticalAttackTrigger", attackDuration);
            }
        }




        private void checkInteract()
        {
            // InSightDetector sightDetector = new InSightDetector();
            var hasPickable = false;
            // Check Around
            var nearColliders = TryToInteract();
            if (nearColliders is { Length: > 0 })
            {
                // Create a list to store immediateUseItems objects and their distances.
                List<(ImmediateUseItems pickable, float distance)> pickableList = new List<(ImmediateUseItems, float)>();

                foreach (var cld in nearColliders)
                {
                    var pkb = cld.GetComponent<ImmediateUseItems>();
                    if (null == pkb) continue;

                    float currentDistance = Vector3.Distance(transform.position, pkb.transform.position);

                    // Only consider immediateUseItems objects within MAX_ALLOWED_INTERACT_RANGE.
                    if (currentDistance <= MAX_ALLOWED_INTERACT_RANGE)
                    {
                        pickableList.Add((pkb, currentDistance));
                    }
                }

                // Sort the list by distance, from nearest to farthest.
                pickableList.Sort((a, b) => a.distance.CompareTo(b.distance));

                foreach (var (pickable, distance) in pickableList)
                {
                    // if ((distance < immediateUseItems.pickupRange) && sightDetector.IsInLineOfSight(viewPoint,immediateUseItems))  //the ray tracer detect always obstacle the picking, 先关了
                    if ((distance < pickable.pickupRange))
                    {
                        // transform.LookAt(immediateUseItems.transform);
                    
                        //TODO:动画播完在触发Pick()
                        rb.velocity = Vector3.zero;
                        isCrouching = false;
                        StartCoroutine(GoPick(pickable));
                        hasPickable = true;
                        break;
                    }
                }
            }

            if (!hasPickable)
            {
                UIManager.Instance.ShowMessage1("Noooooooooooooooooo way!");
            }
        }


        private IEnumerator GoPick(ImmediateUseItems immediateUseItems)
        {
            // 获取目标方向
            Vector3 targetDirection = immediateUseItems.transform.position - transform.position;
            targetDirection.y = 0f; // 将Y轴分量置零，以确保只在水平面上旋转

            // 计算旋转角度
            Quaternion targetRotation = Quaternion.LookRotation(targetDirection);

            // 设置最大旋转角度
            float maxRotationAngle = 360f; // 调整最大旋转角度

            // 旋转到目标方向
            while (Quaternion.Angle(transform.rotation, targetRotation) > 0.1f)
            {
                // 计算旋转步长
                float step = maxRotationAngle * Time.deltaTime;

                // 使用RotateTowards旋转
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, step);
        
                yield return null;
            }

            // 触发"Picking"动画
            animator.SetTrigger("Picking");

            // 获取动画的长度
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            float animationLength = stateInfo.length;
            
            // 等待动画播放一半
            yield return new WaitForSeconds(animationLength);
            // 执行捡取操作
            immediateUseItems.Pick();
        }


    
        private Collider[] TryToInteract()
        {
            return Physics.OverlapSphere(transform.position, MAX_ALLOWED_INTERACT_RANGE);
        }

    
        public void ShowPlayerHUD(string expText)
        {
            if (textMeshProComponent != null)
            {
                textMeshProComponent.text = expText;
                textMeshProComponent.alpha = 1f; // 设置初始透明度为1，完全可见
                textMeshProComponent.gameObject.SetActive(true);
                
                StartCoroutine(FadeOutPlayerHUDText(textMeshProComponent));
            }
            else
            {
                Debug.LogError("TextMeshPro component not found on an PlayerHUD GameObject.");
            }
        }

        private static IEnumerator FadeOutPlayerHUDText(TMP_Text textMesh, float time = 0.8f, float fadeTime = 0.5f)
        {
            // 延迟一段时间以便观察经验值文本
            yield return new WaitForSeconds(time);

            float fadeDuration = fadeTime;
            float startAlpha = textMesh.alpha;
            float currentTime = 0f;

            while (currentTime < fadeDuration)
            {
                currentTime += Time.deltaTime;
                float newAlpha = Mathf.Lerp(startAlpha, 0f, currentTime / fadeDuration);
                textMesh.alpha = newAlpha;
                yield return null;
            }

            textMesh.gameObject.SetActive(false);
        }

        public void TakeDamage(float dmg)
        {
            if (state.IsEmptyHealth())
            {
                _isDead = true;
                animator.Play("Flying Back Death");
                swordObject.GetComponent<Sword>().BehaviourOnHolderDie();
                // HeadOff();
                
                animator.SetBool(IsDead,_isDead);
                StartCoroutine(GameOver());
                return;
            }
            
            switch (state.isJZZ)
            {
                case true:
                    var damage = dmg * (1 - state.damageReduction * state.JZZReduceMutiplier);
                    var powerReduce = state.maxPower * damage / state.maxHealth;
                    if (!state.ConsumePower(powerReduce))
                    {
                        isCrouching = false;
                        state.TakeDamage(damage);
                    }
                    break;
                default:
                    isCrouching = false;
                    state.TakeDamage(dmg);
                    break;
            }
        }

        private IEnumerator GameOver()
        {
            yield return new WaitForSeconds(1.9f);
            SceneManager.LoadScene("LoseScene"); 
        }

        public float GetDamage()
        {
            return (isNextAttackCritical ? state.criticalDmgRate * state.CurrentDamage : state.CurrentDamage);
        }

        public void EndAttack()
        {
            OnAttackEnded();
        }
    
        public Animator GetAnimator()
        {
            return animator;
        }

        public void UpdateAttackAnimationTime(float attackSpeedRate)
        {
            animator.SetFloat(AttSpeedMult,attackSpeedRate);
        }

        public Rigidbody Rb => rb;

        public bool IsFrozen { get => isFrozen; set => isFrozen = value; }
        public float OriginalMaxMstSpeed { get => originalMaxPlySpeed; set => originalMaxPlySpeed = value; }
        public float MaxSpeed { get => MaxPlySpeed; set=> MaxPlySpeed = value; }
        public float OriginalMoveForce { get => originalAcceleration; set => originalAcceleration = value; }
        public float OriginalAttackCooldownInterval { get => originalAttackCooldownInterval; set => originalAttackCooldownInterval = value; }

        public void ActivateFreezeMode(float duration, float continuousDamageAmount, float instantVelocityMultiplier = 0.05f, float attackCooldownIntervalMultiplier = 2f, float MaxSpeedMultiplier = 0.18f)
        {
            // if(!freezeEffectCoroutine.IsUnityNull()) StopCoroutine(freezeEffectCoroutine);
            var time = duration;
            // if(isBoss) time = duration/3;
            DeactivateFreezeMode();
            freezeEffectCoroutine = StartCoroutine(FreezeEffectCoroutine(time, instantVelocityMultiplier, attackCooldownIntervalMultiplier, MaxSpeedMultiplier));
            // 启动持续掉血的协程
            StartCoroutine(Effect.ContinuousDamage.MakeContinuousDamage(this, continuousDamageAmount, time ));
            _effectTimeManager.StopEffect("Freeze");
            _effectTimeManager.CreateEffectBar("Freeze", new Color(153, 0, 255), time);
            
            ParticleEffectManager.Instance.PlayParticleEffect("Charge_03.1 Rave Party", spineTransform.gameObject, 
                Quaternion.identity, Color.red, Color.black, time);
            // Debug.Log("Charge_03.1 Rave Party");
        }
        public Coroutine freezeEffectCoroutine { get; set; }

        public void DeactivateFreezeMode()
        {
            if(!freezeEffectCoroutine.IsUnityNull()) StopCoroutine(freezeEffectCoroutine);
            // 恢复原始推力和攻击间隔
            forwardForce = OriginalMoveForce;
            // attackCooldownInterval = OriginalAttackCooldownInterval;
            MaxSpeed = OriginalMaxMstSpeed;
            IsFrozen = false;
            
            _effectTimeManager.StopEffect("Freeze");
        }

        public IEnumerator FreezeEffectCoroutine(float duration, float instantVelocityMultiplier = 0.25f, float attackCooldownIntervalMultiplier = 2f, float MaxSpeedMultiplier = 0.36f)
        {
            OriginalMoveForce = forwardForce;
            IsFrozen = true;
            Rb.velocity *= instantVelocityMultiplier;
            // 减小加速推力和增加攻击间隔
            forwardForce *= 0.95f; // 降低至60%
            // attackCooldownInterval *= attackCooldownIntervalMultiplier; // 增加至200%
            OriginalMaxMstSpeed = MaxSpeed;
            MaxSpeed *= MaxSpeedMultiplier;
            // 等待冰冻效果持续时间
            yield return new WaitForSeconds(duration);

            // 恢复原始推力和攻击间隔
            forwardForce = originalAcceleration;
            // attackCooldownInterval = OriginalAttackCooldownInterval;
            MaxSpeed = OriginalMaxMstSpeed;
            IsFrozen = false;
        }
        
        private IEnumerator ShowKeys()
        {
            yield return new WaitForSeconds(1f);
            UIManager.Instance.ShowMessage2("Press TAB to Hide/Show Key Bindings!");
            IconManager.ShowKeyBinding();
        }
    }
}
