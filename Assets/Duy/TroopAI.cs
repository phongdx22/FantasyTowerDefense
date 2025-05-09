using System;
using System.Collections;
using System.Collections.Generic;
using TDTK;
using UnityEngine;

public class TroopAI : MonoBehaviour
{
    [Header("General Properties")]

    [SerializeField] bool isDead = false;

    public float detectionRange = 5f; // Range within which the troop detects creeps
    public float attackRange = 1.5f; // Range within which the troop can attack the creep
    public float attackCooldown = 1f; // Time between attacks
    public float moveSpeed = 3f; // Troop movement speed
    public int attackDamage = 1; // Damage dealt per attack
    public float separationRadius = 0.7f; // Radius within which troops will avoid each other
    public float separationForce = 2f; // Force with which troops push away from each other
    [SerializeField] Animator animator; // Animator component for troop animations

    private GameObject targetCreep;
    private float lastAttackTime;

    [Header("Rally Properties")]
    public bool beingRallied = false;
    private Vector3 rallyDestination;
    [SerializeField] private float rallySpeed = 2f; // Speed at which the troop moves to the rally point
    [SerializeField] private float rallyScatterRadius = 2f;
    [SerializeField] private bool checkForEnemiesWhileRallying = true; // Whether to check for enemies while rallying
    [SerializeField] private float enemyCheckIntervalWhileRallying = 0.5f; // How often to check for enemies while rallying
    private float lastEnemyCheckTime = 0f;

    [Header("Flocking Settings (Rally Mode)")]
    [SerializeField] private float rallyNeighborRadius = 3f;
    [SerializeField] private float rallyCohesionWeight = 1f;
    [SerializeField] private float rallyAlignmentWeight = 0.5f;
    [SerializeField] private float rallySeparationWeight = 1.5f;


    [Header("AOE Troop Properties")]
    [SerializeField] bool isAoe;
    [SerializeField] private Transform aoePos;

    [Header("Spawner Properties")]
    [SerializeField] private TroopSpawner spawnerParent;
    [SerializeField] private Transform spawnPoint;

    private void Start()
    {
        // Initialize the animator component
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
    }

    void FixedUpdate()
    {
        if (isDead) return;

        // Apply separation force regardless of state to prevent clumping
        ApplySeparation();

        if (!beingRallied)
        {
            // Normal behavior when not being rallied
            if (targetCreep == null)
            {
                FindTarget();
                animator.SetFloat("Movespeed", 0); // Set the animator's move speed parameter
            }
            else
            {
                // Calculate the distance to the target creep
                float distanceToCreep = Vector3.Distance(transform.position, targetCreep.transform.position);

                // If within attack range, stop moving and attack
                if (distanceToCreep <= attackRange)
                {
                    if (isAoe)
                    {
                        AttackAOE();
                    }
                    else
                    {
                        AttackTarget();
                    }
                }
                else
                {
                    // Keep moving towards the creep
                    ChaseTarget();
                }
            }
        }
        else
        {
            // Check for enemies periodically while rallying if enabled
            if (checkForEnemiesWhileRallying && Time.time - lastEnemyCheckTime > enemyCheckIntervalWhileRallying)
            {
                lastEnemyCheckTime = Time.time;
                FindTarget();

                // If found an enemy, stop rallying and attack
                if (targetCreep != null)
                {
                    beingRallied = false;
                    return;
                }
            }

            // Handle rally movement with flocking behavior
            MoveToRallyPoint();
        }
    }

    public void RallyToPosition(Vector3 rallyPoint)
    {
        // Don't rally troops that already have a target
        if (targetCreep != null)
        {
            return;
        }

        beingRallied = true;
        rallyDestination = rallyPoint + UnityEngine.Random.insideUnitSphere * rallyScatterRadius;
        rallyDestination.y = 0; // Ensure rally point is grounded
    }

    void MoveToRallyPoint()
    {
        // Direct vector toward the rally point
        Vector3 toRally = rallyDestination - transform.position;
        float distanceToRally = toRally.magnitude;

        if (distanceToRally < 0.5f)
        {
            beingRallied = false;
            animator.SetFloat("Movespeed", 0);
            return;
        }

        // Calculate flocking forces
        Vector3 cohesionForce = FlockCohesion();
        Vector3 alignmentForce = FlockAlignment();
        Vector3 separationForce = FlockSeparation();

        // Combine the forces with weights
        Vector3 direction = toRally.normalized +
                           (cohesionForce * rallyCohesionWeight) +
                           (alignmentForce * rallyAlignmentWeight) +
                           (separationForce * rallySeparationWeight);

        // Normalize the combined direction
        direction = direction.normalized;

        // Move in the calculated direction
        transform.position += direction * rallySpeed * Time.deltaTime;

        animator.SetFloat("Movespeed", rallySpeed);
    }

    Vector3 FlockCohesion()
    {
        // Find center of mass of nearby troops
        Collider[] neighbors = Physics.OverlapSphere(transform.position, rallyNeighborRadius, LayerMask.GetMask("Troop"));
        Vector3 centerOfMass = Vector3.zero;
        int count = 0;

        foreach (Collider neighbor in neighbors)
        {
            if (neighbor.gameObject != this.gameObject)
            {
                centerOfMass += neighbor.transform.position;
                count++;
            }
        }

        if (count > 0)
        {
            centerOfMass /= count;
            Vector3 cohesionVector = (centerOfMass - transform.position).normalized;
            return cohesionVector;
        }
        return Vector3.zero;
    }

    Vector3 FlockAlignment()
    {
        // Align with the average direction of nearby troops
        Collider[] neighbors = Physics.OverlapSphere(transform.position, rallyNeighborRadius, LayerMask.GetMask("Troop"));
        Vector3 averageHeading = Vector3.zero;
        int count = 0;

        foreach (Collider neighbor in neighbors)
        {
            TroopAI neighborTroop = neighbor.GetComponent<TroopAI>();
            if (neighbor.gameObject != this.gameObject && neighborTroop != null && neighborTroop.beingRallied)
            {
                averageHeading += neighbor.transform.forward;
                count++;
            }
        }

        if (count > 0)
        {
            averageHeading /= count;
            return averageHeading.normalized;
        }
        return Vector3.zero;
    }

    Vector3 FlockSeparation()
    {
        // Stronger separation for flocking behavior than the regular separation
        Collider[] neighbors = Physics.OverlapSphere(transform.position, rallyNeighborRadius * 0.6f, LayerMask.GetMask("Troop"));
        Vector3 separationVector = Vector3.zero;
        int count = 0;

        foreach (Collider neighbor in neighbors)
        {
            if (neighbor.gameObject != this.gameObject)
            {
                Vector3 awayFromNeighbor = transform.position - neighbor.transform.position;
                float distance = awayFromNeighbor.magnitude;

                // The closer the neighbor, the stronger the repulsion
                if (distance > 0)
                {
                    separationVector += awayFromNeighbor.normalized / distance;
                    count++;
                }
            }
        }

        if (count > 0)
        {
            separationVector /= count;
            return separationVector.normalized;
        }
        return Vector3.zero;
    }


    public void SetSpawner(TroopSpawner spawner, Transform spawnPoint)
    {
        this.spawnerParent = spawner;
        this.spawnPoint = spawnPoint;
        //Debug.Log("Spawner is:" + spawner);
    }

    void FindTarget()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRange);
        foreach (Collider hit in hits)
        {
            if (hit.CompareTag("Creep") || hit.gameObject.layer == 27) // Ensure the target has the "Creep" tag
            {
                //Debug.Log("found target");
                targetCreep = hit.gameObject;
                break;
            }
        }
    }

    void ChaseTarget()
    {
        if (targetCreep != null)
        {
            animator.SetFloat("Movespeed", moveSpeed);
            Vector3 direction = (targetCreep.transform.position - transform.position).normalized;
            Vector3 newPosition = transform.position + direction * moveSpeed * Time.deltaTime;

            // Lock Y position to 0
            newPosition.y = 0;

            transform.position = newPosition;
        }
    }

    void ApplySeparation()
    {
        Collider[] nearbyTroops = Physics.OverlapSphere(transform.position, separationRadius);
        Vector3 separationMove = Vector3.zero;
        int neighborCount = 0;

        foreach (Collider troop in nearbyTroops)
        {
            if (troop != this.GetComponent<Collider>() && troop.GetComponent<TroopAI>() != null)
            {
                // Calculate direction away from the neighboring troop
                Vector3 directionAway = transform.position - troop.transform.position;
                float distance = directionAway.magnitude;

                // Normalize and weight by inverse distance for stronger effect when closer
                if (distance > 0)
                {
                    directionAway = directionAway.normalized / distance;
                    separationMove += directionAway;
                    neighborCount++;
                }
            }
        }

        if (neighborCount > 0)
        {
            separationMove.Normalize();
            transform.position += separationMove * separationForce * Time.deltaTime;
        }
    }

    void AttackTarget()
    {
        if (!isAoe && Time.time >= lastAttackTime + attackCooldown)
        {
            Debug.Log("Attacking creep");
            lastAttackTime = Time.time;
            animator.SetTrigger("Attack");

            UnitCreep creepComponent = targetCreep.GetComponent<UnitCreep>();
            creepComponent.TakeTroopDamage(attackDamage);

            if (creepComponent.IsDestroyed())
            {
                targetCreep = null;
            }
        }
    }

    void AttackAOE()
    {
        if (isAoe && Time.time >= lastAttackTime + attackCooldown)
        {
            lastAttackTime = Time.time;

            animator.SetTrigger("Attack");

            Collider[] enemies = Physics.OverlapSphere(aoePos.transform.position, 0.5f, LayerMask.GetMask("Creep"));
            Debug.Log("Found " + enemies.Length + " enemies");

            foreach (Collider enemy in enemies)
            {
                Debug.Log("Enemy found: " + enemy.name);

                UnitCreep creep = enemy.GetComponent<UnitCreep>();
                if (creep != null)
                {
                    Debug.Log("Applying damage to " + enemy.name);
                    creep.TakeTroopDamage(attackDamage);
                }
                else
                {
                    Debug.Log("No creep found on " + enemy.name);
                }
            }

            targetCreep = null;
        }
    }

    public void Die()
    {
        // Notify the spawner that this troop has been destroyed
        if (spawnerParent != null)
        {
            isDead = true;
            //Play death animation
            animator.SetTrigger("Dead");
            spawnerParent.OnTroopDestroyed(this.gameObject, spawnPoint);
            Debug.Log("Troop died, notifying spawner");
        }
    }

    // Optional: Draw the detection range in the editor
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, separationRadius);

        if (isAoe)
        {
            //AOE attack range
            Gizmos.color = Color.black;
            Gizmos.DrawWireSphere(aoePos.transform.position, 0.5f);
        }
    }
}
