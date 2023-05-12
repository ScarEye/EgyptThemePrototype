using StarterAssets;
using UnityEngine;
using UnityEngine.AI;

public class AIController : MonoBehaviour
{         
    public float startWaitTime = 4;                 //  Wait time of every action
    public float timeToRotate = 2;                  //  Wait time when the enemy detect near the player without seeing
    public float speedWalk = 6;                     //  Walking speed, speed in the nav mesh agent
    public float speedRun = 9;                      //  Running speed

    public float viewRadius = 15;                   //  Radius of the enemy view
    public float viewAngle = 90;                    //  Angle of the enemy view
    public LayerMask playerMask;                    //  To detect the player with the raycast
    public LayerMask obstacleMask;                  //  To detect the obstacules with the raycast
    public float meshResolution = 1.0f;             //  How many rays will cast per degree
    public int edgeIterations = 4;                  //  Number of iterations to get a better performance of the mesh filter when the raycast hit an obstacule
    public float edgeDistance = 0.5f;               //  Max distance to calcule the a minimum and a maximum raycast when hits something

    public Light lineOfSightEffect;             // Line Of Sight Effect 

    [SerializeField]
    private Transform waypoints;                   //  All the waypoints where the enemy patrols

    private int m_CurrentWaypointIndex;                     //  Current waypoint where the enemy is going to

    private NavMeshAgent m_navMeshAgent;                      //  Nav mesh agent component
    private Animator m_Animator;

    private Vector3 playerLastPosition = Vector3.zero;      //  Last position of the player when was near the enemy
    private Vector3 m_PlayerPosition;                       //  Last position of the player when the player is seen by the enemy

    private float m_WaitTime;                               //  Variable of the wait time that makes the delay
    private float m_TimeToRotate;                           //  Variable of the wait time to rotate when the player is near that makes the delay
    private bool m_playerInRange;                           //  If the player is in range of vision, state of chasing
    private bool m_PlayerNear;                              //  If the player is near, state of hearing
    private bool m_IsPatrol;                                //  If the enemy is patrol, state of patroling
    private bool m_CaughtPlayer;                            //  if the enemy has caught the player
    private bool m_IsPlayerDead;                            //  if the enemy has attacked the player

    private Transform _player;

    private void Awake()
    {
        m_PlayerPosition = Vector3.zero;
        m_IsPatrol = true;
        m_CaughtPlayer = false;
        m_playerInRange = false;
        m_PlayerNear = false;
        m_IsPlayerDead = false;
        m_WaitTime = startWaitTime;                 //  Set the wait time variable that will change
        m_TimeToRotate = timeToRotate;

        lineOfSightEffect.spotAngle = viewAngle;
        lineOfSightEffect.innerSpotAngle = viewAngle;
        lineOfSightEffect.range = viewRadius;

        m_CurrentWaypointIndex = 0;                 //  Set the initial waypoint

        m_navMeshAgent = this.GetComponent<NavMeshAgent>();
        m_Animator = this.GetComponent<Animator>();

        _player = GameObject.FindGameObjectWithTag("Player").transform;
    }
    private void Start()
    {
        Invoke("StartNavMesh", 0.5f);
    }

    void StartNavMesh()
    {
        m_navMeshAgent.enabled = true;
        m_navMeshAgent.isStopped = false;
        m_navMeshAgent.speed = speedWalk;             //  Set the navemesh speed with the normal speed of the enemy
        m_navMeshAgent.SetDestination(waypoints.GetChild(m_CurrentWaypointIndex).position);    //  Set the destination to the first waypoint
    }

    private void Update()
    {
        if (!m_navMeshAgent.enabled)
            return;

        if (Vector3.Distance(transform.position, _player.position) >viewRadius*3)
        {
            return;
        }
        EnviromentView();                       //  Check whether or not the player is in the enemy's field of vision

        if (!m_IsPatrol)
        {
            Chasing();
        }
        else
        {
            Patroling();
        }
    }

    private void Chasing()
    {
        //  The enemy is chasing the player
        m_PlayerNear = false;                       //  Set false that hte player is near beacause the enemy already sees the player
        playerLastPosition = Vector3.zero;          //  Reset the player near position

        if (!m_CaughtPlayer)
        {
            Move(speedRun);
            m_navMeshAgent.SetDestination(m_PlayerPosition);          //  set the destination of the enemy to the player location
        }
        if (m_navMeshAgent.remainingDistance <= m_navMeshAgent.stoppingDistance)    //  Control if the enemy arrive to the player location
        {
            if (m_WaitTime <= 0 && !m_CaughtPlayer && Vector3.Distance(transform.position, _player.position) >= 6f)
            {
                //  Check if the enemy is not near to the player, returns to patrol after the wait time delay
                m_IsPatrol = true;
                m_PlayerNear = false;
                Move(speedWalk);
                m_TimeToRotate = timeToRotate;
                m_WaitTime = startWaitTime;
                m_navMeshAgent.SetDestination(waypoints.GetChild(m_CurrentWaypointIndex).position);
            }
            else
            {
                if (Vector3.Distance(transform.position, _player.position)>= 2.5f)
                    //  Wait if the current position is not the player position
                    Stop();
                else if(Vector3.Distance(transform.position, _player.position) < 1.2f)
                {
                    Stop();
                    CaughtPlayer();
                }

                m_WaitTime -= Time.deltaTime;
            }
        }
    }

    private void Patroling()
    {
        if (m_PlayerNear)
        {
            //  Check if the enemy detect near the player, so the enemy will move to that position
            if (m_TimeToRotate <= 0)
            {
                Move(speedWalk);
                LookingPlayer(playerLastPosition);
            }
            else
            {
                //  The enemy wait for a moment and then go to the last player position
                Stop();
                m_TimeToRotate -= Time.deltaTime;
            }
        }
        else
        {
            m_PlayerNear = false;           //  The player is no near when the enemy is platroling
            playerLastPosition = Vector3.zero;
            m_navMeshAgent.destination=(waypoints.GetChild(m_CurrentWaypointIndex).position);    //  Set the enemy destination to the next waypoint

            if (m_navMeshAgent.remainingDistance <= m_navMeshAgent.stoppingDistance)
            {
                //  If the enemy arrives to the waypoint position then wait for a moment and go to the next
                if (m_WaitTime <= 0)
                {
                    NextPoint();
                    Move(speedWalk);
                    m_WaitTime = startWaitTime;
                }
                else
                {
                    Stop();
                    m_WaitTime -= Time.deltaTime;
                }
            }
        }
    }

    private void OnAnimatorMove()
    {
        if (m_Animator)
        {
            if(m_navMeshAgent)
                m_Animator.SetFloat("Speed", m_navMeshAgent.speed);
            if (!m_IsPlayerDead && m_CaughtPlayer)
            {
                m_IsPlayerDead = true;
                m_Animator.SetTrigger("Attack");
            }
        }

    }

    void NextPoint()
    {
        m_CurrentWaypointIndex = (m_CurrentWaypointIndex + 1) % waypoints.childCount;
        m_navMeshAgent.SetDestination(waypoints.GetChild(m_CurrentWaypointIndex).position);
    }

    void Stop()
    {
        m_navMeshAgent.isStopped = true;
        m_navMeshAgent.speed = 0;
    }

    void Move(float speed)
    {
        m_navMeshAgent.isStopped = false;
        m_navMeshAgent.speed = speed;
    }

    void CaughtPlayer()
    {
        m_CaughtPlayer = true;
        GameController.instance.LosePanel();
        this.GetComponent<Rigidbody>().isKinematic = true;
    }

    void LookingPlayer(Vector3 player)
    {
        m_navMeshAgent.SetDestination(player);
        if (Vector3.Distance(transform.position, player) <= 0.3)
        {
            if (m_WaitTime <= 0)
            {
                m_PlayerNear = false;
                Move(speedWalk);
                m_navMeshAgent.SetDestination(waypoints.GetChild(m_CurrentWaypointIndex).position);
                m_WaitTime = startWaitTime;
                m_TimeToRotate = timeToRotate;
            }
            else
            {
                Stop();
                m_WaitTime -= Time.deltaTime;
            }
        }
    }

    void EnviromentView()
    {
        Collider[] playerInRange = Physics.OverlapSphere(transform.position, viewRadius, playerMask);   //  Make an overlap sphere around the enemy to detect the playermask in the view radius

        for (int i = 0; i < playerInRange.Length; i++)
        {
            Transform player = playerInRange[i].transform;
            Vector3 dirToPlayer = (player.position - transform.position).normalized;
            if (Vector3.Angle(transform.forward, dirToPlayer) < viewAngle / 2)
            {
                float dstToPlayer = Vector3.Distance(transform.position, player.position);          //  Distance of the enmy and the player
                if (!Physics.Raycast(transform.position, dirToPlayer, dstToPlayer, obstacleMask))
                {
                    m_playerInRange = true;             //  The player has been seeing by the enemy and then the nemy starts to chasing the player
                    m_IsPatrol = false;                 //  Change the state to chasing the player
                }
                else
                {
                    /*
                     *  If the player is behind a obstacle the player position will not be registered
                     * */
                    m_playerInRange = false;
                }
            }
            if (Vector3.Distance(transform.position, player.position) > viewRadius)
            {
                /*
                 *  If the player is further than the view radius, then the enemy will no longer keep the player's current position.
                 *  Or the enemy is a safe zone, the enemy will no chase
                 * */
                m_playerInRange = false;                //  Change the sate of chasing
            }
            if (m_playerInRange)
            {
                /*
                 *  If the enemy no longer sees the player, then the enemy will go to the last position that has been registered
                 * */
                m_PlayerPosition = player.transform.position;       //  Save the player's current position if the player is in range of vision
            }
        }
    }
}
