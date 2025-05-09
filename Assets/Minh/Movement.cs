using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Movement : MonoBehaviour
{
    public KeyCode rallyKey = KeyCode.R;
    public GameObject rallyParticle;
    public GameObject buildUI;
    public SpriteRenderer playerSprite;
    public Color originalColor;

    [Header("Movement Settings")]
    public float speed = 5f;
    public float desiredY;

    public GameObject moveParticle;

    [Header("Dash Settings")]
    public float dashSpeed = 15f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 1f;

    [Header("Health Settings")]
    public bool isGhost = false;
    public float ghostDuration = 20f;
    private bool wasGhost = false;

    [Header("Animation")]
    public Animator animator;
    public float detectionRadius = 2f;
    public LayerMask enemyLayer;
    private bool isAttacking = false;
    private bool attackTriggered = false;

    [Header("Easter Egg")]
    public Transform Point1;
    public GameObject specialPrefab1;
    public Transform Point2;
    public GameObject specialPrefab2;
    public Transform Point3;
    public GameObject specialPrefab3;

    private Vector3 targetPosition;
    private bool isMoving = false;
    private bool isDashing = false;
    private float dashCooldownTimer = 0f;
    private float shootCooldownTimer = 0f;

    private Coroutine attackSoundCoroutine;
    private Coroutine runSoundCoroutine;

    private int kPressCount = 0;
    private float lastKPressTime = 0f;
    private float kPressResetTime = 1f;



    private void Start()
    {
        playerSprite = GetComponentInChildren<SpriteRenderer>();
        originalColor = playerSprite.color;

        buildUI = GameObject.Find("Canvas_BuildButtons");
        animator = GetComponentInChildren<Animator>();
        SoundManager.Instance.PlayMusic("Background");
    }

    private void Update()
    {
        HandleMovement();
        HandleAnimation();
        if (isGhost && !wasGhost)
        {
            SoundManager.Instance.PlaySound("Dead");
            wasGhost = true;
        }
        if (!isGhost && wasGhost)
        {
            wasGhost = false;
        }

        HandleSpecialKeyCombo();

        if (isGhost) return;
        HandleDash();
        HandleRally();
    }

    void HandleSpecialKeyCombo()
    {
        if (Input.GetKeyDown(KeyCode.K))
        {
            if (Time.time - lastKPressTime < kPressResetTime)
            {
                kPressCount++;
            }
            else
            {
                kPressCount = 1;
            }
            lastKPressTime = Time.time;

            if (kPressCount >= 3)
            {
                StartCoroutine(ActivateSpecialAfterDelay());
                kPressCount = 0;
            }
        }
    }

    IEnumerator ActivateSpecialAfterDelay()
    {
        SoundManager.Instance.PlaySound("Allahu");
        yield return new WaitForSeconds(5.0f);
        Instantiate(specialPrefab1, Point1.position, Quaternion.identity);
        yield return new WaitForSeconds(0.02f);
        Instantiate(specialPrefab2, Point2.position, Quaternion.identity);
        yield return new WaitForSeconds(0.02f);
        Instantiate(specialPrefab3, Point3.position, Quaternion.identity);
        isGhost = true;
    }

    void HandleRally()
    {
        if (Input.GetKeyDown(rallyKey))
        {
            Debug.Log("Rally key pressed!");
            SoundManager.Instance.PlaySound("Rally");
            if (rallyParticle != null)
            {
                GameObject particleInstance = Instantiate(rallyParticle, new Vector3(this.gameObject.transform.position.x, this.gameObject.transform.position.y + 1.5f, this.gameObject.transform.position.z), Quaternion.identity);
                particleInstance.transform.SetParent(this.gameObject.transform);
                // Optional: Destroy the particle effect after a few seconds
                Destroy(particleInstance, 2f);
            }
            IssueRallyCommand();
        }
    }

    void IssueRallyCommand()
    {
        Vector3 rallyPoint = transform.position;
        TroopAI[] allTroops = FindObjectsOfType<TroopAI>();
        foreach (TroopAI troop in allTroops)
        {
            troop.RallyToPosition(rallyPoint);
        }
    }

    void HandleMovement()
    {
        if (Input.GetMouseButtonDown(1) && !isDashing)
        {
            SetTargetPosition();
        }

        if (isMoving && !isDashing)
        {
            MovePlayer();
        }
    }

    void SetTargetPosition()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            targetPosition = hit.point;

            //Particle effect to show where the player is moving to
            // Particle effect to show where the player is moving to
            if (moveParticle != null)
            {
                GameObject particleInstance = Instantiate(moveParticle,
                    new Vector3(hit.point.x, desiredY, hit.point.z),
                    Quaternion.identity);

                // Optional: Destroy the particle effect after a few seconds
                Destroy(particleInstance, 2f);
            }

            isMoving = true;
        }
    }

    void MovePlayer()
    {
        float moveSpeed = isAttacking ? speed * 0.5f : speed;

        transform.position = Vector3.MoveTowards(
            new Vector3(transform.position.x, desiredY, transform.position.z),
            new Vector3(targetPosition.x, desiredY, targetPosition.z),
            moveSpeed * Time.deltaTime
        );

        if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
        {
            isMoving = false;
        }
    }

    void HandleDash()
    {
        if (Input.GetMouseButtonDown(2) && dashCooldownTimer <= 0 && !isDashing)
        {
            StartCoroutine(Dash());
        }

        if (dashCooldownTimer > 0)
        {
            dashCooldownTimer -= Time.deltaTime;
        }
    }

    IEnumerator Dash()
    {
        isDashing = true;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        Vector3 dashTarget = transform.position;

        if (Physics.Raycast(ray, out hit))
        {
            dashTarget = hit.point;
        }

        Vector3 dashDirection = (dashTarget - transform.position).normalized;
        float dashEndTime = Time.time + dashDuration;

        while (Time.time < dashEndTime)
        {
            transform.position += dashDirection * dashSpeed * Time.deltaTime;
            yield return null;
        }

        isDashing = false;
        dashCooldownTimer = dashCooldown;
    }

    void HandleAnimation()
    {
        Collider[] hitEnemies = Physics.OverlapSphere(transform.position, detectionRadius, enemyLayer);
        bool wasAttacking = isAttacking;
        isAttacking = hitEnemies.Length > 0;

        if (isAttacking && !attackTriggered)
        {
            animator.SetTrigger("Attacking");
            attackTriggered = true;
            if (attackSoundCoroutine == null)
                attackSoundCoroutine = StartCoroutine(LoopAttackSound());
        }
        else if (!isAttacking && wasAttacking)
        {
            attackTriggered = false;
            animator.ResetTrigger("Attacking");
            if (attackSoundCoroutine != null)
            {
                StopCoroutine(attackSoundCoroutine);
                attackSoundCoroutine = null;
            }
        }

        float moveSpeed = isMoving ? (isAttacking ? speed * 0.5f : speed) : 0f;
        animator.SetFloat("Speed", moveSpeed);

        if (isMoving && !isAttacking && runSoundCoroutine == null)
        {
            runSoundCoroutine = StartCoroutine(LoopRunSound());
        }
        else if ((!isMoving || isAttacking) && runSoundCoroutine != null)
        {
            StopCoroutine(runSoundCoroutine);
            runSoundCoroutine = null;
        }
    }

    IEnumerator LoopAttackSound()
    {
        while (true)
        {
            SoundManager.Instance.PlaySound("Swoosh");
            yield return new WaitForSeconds(0.5f);
            Debug.Log("Attack Sound Playing");
        }
    }

    IEnumerator LoopRunSound()
    {
        while (true)
        {
            SoundManager.Instance.PlaySound("Run");
            yield return new WaitForSeconds(0.35f);
            Debug.Log("Footsteps Sound Playing");
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}