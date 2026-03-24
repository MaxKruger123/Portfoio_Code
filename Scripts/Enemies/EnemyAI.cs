using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using Unity.VisualScripting;

public class EnemyAI : MonoBehaviour
{
    public enum State { Idle, Follow, Attack }
    private State currentState;

    [Header("References")]
    public Transform player;
    private NavMeshAgent agent;
    public GameObject explosion;
    public Transform modelToRotate;
    public GameObject alertedPNG;
    public GameObject inCombatPNG;

    [Header("Enemy Type")]
    public bool isScouter = false; // <-- Set this in Inspector or via spawn logic
    public bool isShooter = false;

    [Header("Detection")]
    public float detectionRange = 10f;
    public float attackRange = 2f;
    public float detectionAngle = 60f; // Field of view in degrees
    [SerializeField] private bool alerted = false;
    [SerializeField] private bool inCombat = false;
    private bool hasScanned = false;
    private bool isScanning = false;
    [SerializeField] private float playerVisibleTimer = 0f;
    public float timeToAlert = 1.5f;
    public float timeToCombat = 4f;



    [Header("Attack")]
    public float timeBetweenAttacks = 1.5f;
    private float attackCooldown;

    [Header("Patrolling")]
    public Transform[] waypoints;
    [SerializeField]private int currentWaypointIndex = 0;
    [SerializeField]private bool isWaiting = false;




    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        currentState = State.Idle;

        if (player == null && GameObject.FindGameObjectWithTag("Player"))
            player = GameObject.FindGameObjectWithTag("Player").transform;

        alertedPNG.SetActive(false);
        inCombatPNG.SetActive(false);
    }

    private void Update()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (isScouter)
        {
            ScouterBehavior(distanceToPlayer);
        }
        else if (isShooter)
        {
            
        }

        if (playerVisibleTimer >= timeToCombat)
        {
            inCombat = true;
            alertedPNG.SetActive(false);
            inCombatPNG.SetActive(true);
            StopAllCoroutines();
            StartCoroutine(Wait());            
            Debug.Log("In Combat!");
        }
    }

    void ScouterBehavior(float distance)
    {

        Vector3 directionToPlayer = (player.position - transform.position).normalized;

        if (inCombat)
        {
            agent.SetDestination(player.position);
        }


        if (distance <= detectionRange)
        {
            // Check if within field of view angle
            float angleToPlayer = Vector3.Angle(transform.forward, directionToPlayer);
            if (angleToPlayer <= detectionAngle / 2)
            {
                // Check if there's line of sight
                RaycastHit hit;
                if (Physics.Raycast(transform.position, directionToPlayer, out hit, detectionRange))
                {
                    
                    if (hit.transform == player)
                    {

                        playerVisibleTimer += Time.deltaTime;

                        if(playerVisibleTimer >= timeToAlert && !alerted)
                        {
                            alerted = true;
                            alertedPNG.SetActive(true);
                            Debug.Log("Player detected!");
                            isWaiting = false;
                            agent.ResetPath();
                            Alerted();
                        }
                        

                        if (distance <= attackRange)
                        {
                            Explode();
                        }

                    }
                    else
                    {
                        alerted = false;
                        alertedPNG.SetActive(false);
                        playerVisibleTimer = 0f;
                    }                                    
                }
            }
            //Patrolling
            else if (!isWaiting && !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance && !alerted && !isScanning)
            {
                StartCoroutine(WaitAndGoToNextWaypoint());
                alerted = false;
                alertedPNG.SetActive(false);
            }


        }
        else
        {
            alerted = false;
            alertedPNG.SetActive(false);

            //Patrolling
            if (!isWaiting && !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance && !alerted && !isScanning)
            {
                StartCoroutine(WaitAndGoToNextWaypoint());
            }

        }
    }

    void ShooterBehaviour(float distance)
    {
        Vector3 directionToPlayer = (player.position - transform.position).normalized;

        if (distance <= detectionRange)
        {
            // Check if within field of view angle
            float angleToPlayer = Vector3.Angle(transform.forward, directionToPlayer);
            if (angleToPlayer <= detectionAngle / 2)
            {
                
                // Check if there's line of sight
                RaycastHit hit;
                if (Physics.Raycast(transform.position, directionToPlayer, out hit, detectionRange))
                {
                    if (hit.transform == player)
                    {
                        Debug.Log("Player detected!");
                        Alerted();
                    }
                }
            }

            if (distance <= attackRange)
            {
                Explode();
            }
        }
    }

    void Alerted()
    {
        if (alerted)
        {
            if (waypoints.Length == 0) return;
            waypoints[3].position = player.position;
            agent.SetDestination(waypoints[3].position);
            Debug.Log("Going to waypoint 3");

            currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;

            


            if (!hasScanned)
            {
                hasScanned = true;
                StartCoroutine(WaitUntilStoppedThenScan());
            }


        }
    }




    IEnumerator ScanArea()
    {
        Debug.Log("Scanning Now!");
        isScanning = true;
        float originalAngle = detectionAngle;
        float targetAngle = originalAngle * 2f;
        float duration = 3f;
        float elapsed = 0f;

        
        while (elapsed < duration)
        {
            detectionAngle = Mathf.Lerp(originalAngle, targetAngle, elapsed / duration);
            elapsed += Time.deltaTime;
            
            yield return null;
        }
        detectionAngle = targetAngle;

        yield return new WaitForSeconds(2f);

        
        elapsed = 0f;
        while (elapsed < duration)
        {
            detectionAngle = Mathf.Lerp(targetAngle, originalAngle, elapsed / duration);
            elapsed += Time.deltaTime;
            
            yield return null;
        }
        detectionAngle = originalAngle;

        Debug.Log("Scan complete");

        yield return new WaitForSeconds(2f);
        isWaiting = false;
        hasScanned = false;
        isScanning = false;
        agent.ResetPath();

        if (!isWaiting && !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance && !alerted)
        {
            StartCoroutine(WaitAndGoToNextWaypoint());
        }

    }



    IEnumerator WaitUntilStoppedThenScan()
    {
        // Wait until the agent has stopped moving
        while (agent.pathPending || agent.remainingDistance > agent.stoppingDistance || agent.velocity.sqrMagnitude > 0.01f)
        {
            yield return null;
        }

        // Small buffer to ensure it's fully stopped
        yield return new WaitForSeconds(0.1f);

        StartCoroutine(ScanArea());
    }




    void GoToNextWaypoint()
    {
        if (waypoints.Length == 0) return;

        agent.SetDestination(waypoints[currentWaypointIndex].position);
        Debug.Log("Going to waypoint " + currentWaypointIndex);

        currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
    }



    IEnumerator WaitAndGoToNextWaypoint()
    {
        isWaiting = true;
        yield return new WaitForSeconds(2f); // Wait for 2 seconds
        GoToNextWaypoint();
        isWaiting = false;
    }


    //private void Patrol()
    //{
    //    int i = 0;

    //    while (agent.remainingDistance <= agent.stoppingDistance)
    //    {
    //        agent.SetDestination(waypoints[i].transform.position);
    //        Debug.Log("Going to waypoint " + i);
    //        i++;
    //        StartCoroutine(Wait());           

    //        if (i == waypoints.Length)
    //        {
    //            i = 0;
    //        }
    //    }
    //}

    IEnumerator Wait()
    {
        yield return new WaitForSeconds(3);
    }

    void Explode()
    {
        StartCoroutine(ExplodeAfterDelay());
    }

    IEnumerator ExplodeAfterDelay()
    {
        

        //Add a visual cue or sound here

        yield return new WaitForSeconds(1f); //2 second delay



        //explosion effect or damage logic
        Instantiate(explosion, transform.position, Quaternion.identity);
        Destroy(gameObject); // destroy self after explosion
    }

    void IdleBehavior(float distance)
    {
        agent.SetDestination(transform.position); // stay still

        if (distance <= detectionRange)
        {
            currentState = State.Follow;
        }
    }

    void FollowBehavior(float distance)
    {
        agent.SetDestination(player.position);

        if (distance <= attackRange)
        {
            currentState = State.Attack;
        }
        else if (distance > detectionRange)
        {
            currentState = State.Idle;
        }
    }

    void AttackBehavior(float distance)
    {
        agent.SetDestination(transform.position); // stop moving

        transform.LookAt(player); // face the player

        if (attackCooldown <= 0f)
        {
            Debug.Log("Enemy Attacks!");
            // Insert standard attack logic
            attackCooldown = timeBetweenAttacks;
        }

        if (distance > attackRange)
        {
            currentState = State.Follow;
        }
    }

    private void OnDrawGizmos()
    {
        // Set Gizmo color
        Gizmos.color = Color.yellow; // Red with transparency

        // Draw detection range
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Draw vision cone
        Vector3 forward = transform.forward * detectionRange;
        Quaternion leftRotation = Quaternion.Euler(0, -detectionAngle / 2, 0);
        Quaternion rightRotation = Quaternion.Euler(0, detectionAngle / 2, 0);

        Vector3 leftEdge = leftRotation * forward;
        Vector3 rightEdge = rightRotation * forward;

        Gizmos.DrawLine(transform.position, transform.position + leftEdge);
        Gizmos.DrawLine(transform.position, transform.position + rightEdge);
    }
}
