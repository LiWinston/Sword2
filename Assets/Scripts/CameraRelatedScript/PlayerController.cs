using System.Collections;
using System.Collections.Generic;
using AttributeRelatedScript;
using CameraView;
using CodeMonkey.HealthSystemCM;
using UI;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Timeline;
using UnityEngine.UIElements;
using Cursor = UnityEngine.Cursor;
using Random = UnityEngine.Random;

public class PlayerController : MonoBehaviour
{
    private Rigidbody rb;
    
    [Header("Movement Settings")]
    public float moveSpeed = 5f;

    public float crouchSpeed = 2f;
    
    [Header("Mouse Look Settings")]
    public float mouseSensitivity = 100f;

    private bool isMoving = false;
    private bool isJumping = false;
    private bool isCrouching = false;
    public float forwardForce = 100;
    public float jumpForce = 800;
    
    
    private bool isClimbing = false;
    
    private Vector3 moveDirection;

    
    private float lastAttackTime = 0f; 
    
    [SerializeField]private Camera mycamera;
    
    private Animator animator;
    

    private float xRotation = 0f;

    public bool IsCrouching
    {
        get { return isCrouching; }
        set { isCrouching = value; }
    }

    // private Vector3 originalScale;
    private Vector3 originalPosition;
    [SerializeField] private float MAX_ALLOWED_INTERACT_RANGE = 3;
    private bool isGrounded;
    public float moveForceTimer = 0.05f;
    public float moveForceTimerCounter = 0.05f;
    
    [SerializeField] private GameObject swordTransform;
    private Damage damage;

    private void Start()
    {
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

        if (animator == null)
        {
            Debug.LogError("Didnt Find animator!");
        }
        
        Cursor.lockState = CursorLockMode.Locked;
        originalPosition = transform.position;
        damage = GetComponent<Damage>();
    }

    private void Update()

    {
        // Vector3 handPosition = handBoneTransform.transform.position;
        //
        // // 设置剑的位置为手部骨骼的位置
        // swordTransform.transform.position = handPosition;
        
        // WASD movement
        animator.SetBool("Standing",!isMoving);
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        moveDirection = transform.right * horizontal + transform.forward * vertical;

        if (math.abs(rb.velocity.x) + math.abs(rb.velocity.z) > 0.01f)
        {
            isMoving = true;
            animator.SetBool("isMoving",isMoving);
        }
        else
        {
            isMoving = false;
            animator.SetBool("isMoving",isMoving);
        }
        
        UserInput();
        
        
    
        
        if (Input.GetMouseButtonDown(0) && Time.time - lastAttackTime >= damage.attackCooldown)
        {
            animator.SetTrigger("AttackTrigger");
            Attack();
            
            lastAttackTime = Time.time;
        }
        
        checkInteract();
        
        if (Input.GetKeyDown(KeyCode.V)) 
        {
            animator.SetTrigger("HurricaneKickTrigger");
            rb.velocity = Vector3.zero;
            HurricaneKick();
        }
        
        // Crouch
        if (Input.GetKeyDown(KeyCode.LeftControl))
        {
            animator.SetTrigger("BeginCrouch");
            isCrouching = true;
        }
        
        if(Input.GetKeyUp(KeyCode.LeftControl))
        {
            isCrouching = false;
        }

        
        if (Mathf.Abs(rb.velocity.y) < 0.01f)
        {
            isGrounded = true;
            isJumping = false; // reset
        }
        else
        {
            isGrounded = false;
        }

        

        // Crouch
        animator.SetBool("isCrouching",isCrouching);
        
        // Mouse look
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);
        mycamera.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }

    private void HurricaneKick()
    {
        
        Vector3 playerPosition = transform.position;
        
        Collider[] hitEnemies = Physics.OverlapSphere(playerPosition, damage.hurricaneKickRange);
    
        if (hitEnemies.Length == 0)
        {
            UI.UIManager.ShowMessage2("What are you kicking?");
        }
        else
        {
            UI.UIManager.ShowMessage2("Lets KICK!");
        }

        foreach (Collider cld in hitEnemies)
        {
            if (cld.CompareTag("Enemy"))
            {
                Vector3 enemyPosition = cld.transform.position;
                
                Vector3 knockbackDirection = (enemyPosition - playerPosition).normalized;
                
                HealthSystem enemyHealth = cld.GetComponent<HealthSystemComponent>().GetHealthSystem();

                if (enemyHealth != null)
                {
                    enemyHealth.Damage(damage.HurricaneKickDamage);
                    UI.UIManager.ShowMessage2("What a Hurricane Kick!");
                    Rigidbody enemyRigidbody = cld.GetComponent<Rigidbody>();
                    if (enemyRigidbody != null)
                    {
                        enemyRigidbody.AddForce(knockbackDirection * damage.hurricaneKickKnockbackForce, ForceMode.VelocityChange);
                    }
                }
            }
        }
    } 


    private void Attack()
    {
        UI.UIManager.ShowMessage2("Taste My Sword !!!(While a little stupid)");
        animator.SetTrigger("AttackTrigger");
        var sword = SpellCast.FindDeepChild(transform, "Scabbard");
        ParticleEffectManager.Instance.PlayParticleEffect("Attack", sword.gameObject, Quaternion.identity,Color.white, Color.white);
        Vector3 characterPosition = transform.position;
        
        Vector3 targetDirection = transform.forward;
        
        var attackAngle = damage.attackAngle;
        var attackRange = damage.attackRange;
        float attackRadius = Mathf.Tan(Mathf.Deg2Rad * (attackAngle / 2f)) * attackRange;
        
        Collider[] enemies = Physics.OverlapSphere(characterPosition, attackRange);
        foreach (Collider enemyCollider in enemies)
        {
            // Check if enemy
            if (enemyCollider.CompareTag("Enemy"))
            {
                // Calculate the vector of the main character to the enemy
                Vector3 enemyPosition = enemyCollider.transform.position;
                Vector3 direction = enemyPosition - characterPosition;

                // Calculate the angle between the protagonist and the enemy
                float angle = Vector3.Angle(targetDirection, direction);

                // Check if the angle is within the attack angle range
                if (angle <= attackAngle / 2f)
                {
                    // Check for HealthSystem components
                    HealthSystem healthSystem = enemyCollider.GetComponent<HealthSystemComponent>().GetHealthSystem();
                    if (healthSystem != null)
                    {
                        UIManager.ShowMessage1("A "+damage.CurrentDamage+" Cut~");
                        healthSystem.Damage(damage.CurrentDamage); // Inflict damage on enemies
                    }
                }
            }
        }
    }


    public void UserInput()
    {
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded && !isJumping) // Add a check to see if a jump has been made
        {
            rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
            animator.SetTrigger("Jump");
            isJumping = true;
            isGrounded = false;
        }
        
        moveForceTimerCounter -= Time.deltaTime;
    
        if (moveForceTimerCounter <= 0)
        {
            moveForceTimerCounter += moveForceTimer;
        
            if (Input.GetKey(KeyCode.W))
            {
                rb.AddForce(transform.forward * forwardForce, ForceMode.Impulse);
            }
        
            if (Input.GetKey(KeyCode.S))
            {
                rb.AddForce(transform.forward * (-forwardForce * 0.7f), ForceMode.Impulse);
            }
        
            if (Input.GetKey(KeyCode.A))
            {
                rb.AddForce(transform.right * -forwardForce, ForceMode.Impulse);
            }
        
            if (Input.GetKey(KeyCode.D))
            {
                rb.AddForce(transform.right * forwardForce, ForceMode.Impulse);
            }
        }
    }

    
    private void checkInteract()
    {
        InSightDetect sightDetector = new InSightDetect();
        if (!Input.GetKeyDown(KeyCode.E)) return;
        var hasPickable = false;
        // Check Around
        var nearColliders = TryToInteract();
        if (nearColliders is { Length: > 0 })
        {
            // Create a list to store pickable objects and their distances.
            List<(Pickable pickable, float distance)> pickableList = new List<(Pickable, float)>();

            foreach (var cld in nearColliders)
            {
                var pkb = cld.GetComponent<Pickable>();
                if (null == pkb) continue;

                float currentDistance = Vector3.Distance(transform.position, pkb.transform.position);

                // Only consider pickable objects within MAX_ALLOWED_INTERACT_RANGE.
                if (currentDistance <= MAX_ALLOWED_INTERACT_RANGE)
                {
                    pickableList.Add((pkb, currentDistance));
                }
            }

            // Sort the list by distance, from nearest to farthest.
            pickableList.Sort((a, b) => a.distance.CompareTo(b.distance));

            foreach (var (pickable, distance) in pickableList)
            {
                if ((distance < pickable.pickupRange) && sightDetector.IsInLineOfSight(pickable))
                {
                    pickable.Pick();
                    hasPickable = true;
                    break;
                }
            }
        }

        if (!hasPickable)
        {
            UIManager.ShowMessage1("Noooooooooooooooooo way!");
        }
    }


    private Collider[] TryToInteract()
    {
        return Physics.OverlapSphere(transform.position, MAX_ALLOWED_INTERACT_RANGE);
    }
}
