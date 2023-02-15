using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using System.Threading.Tasks;
using UnityEngine.Audio;
using Mirror;

namespace ON
{
    public class EnemyManager : NetworkBehaviour
    {
        #region Variables       
        [Header("References")]
        public EnemyLocomotionManager enemyLocomotionManager;
        public EnemyAnimatorManager enemyAnimationManager;
        public EnemyStats enemyStats;
        public AudioSource audioSource;
        public State[] allStates = new State[0];
        public Rigidbody enemyRigidbody;
        public NavMeshAgent navMeshAgent;
        public State currentState;
        public EnemySpawnPoint enemySpawnPoint;
        public Vector3 initialPosition;
        public AudioClip[] takeDamageSounds = new AudioClip[0];
        public AudioClip[] dealDamageSounds = new AudioClip[0];
        public AudioClip deathSound;
        public LayerMask alertLayer;
        public Transform shootingPosition;

        [Header("A.I Abilities")]
        public int[] abilityPoints;
        public Transform[] abilityPositions;

        [Header("A.I Settings")]
        public float detectionRadius = 20;
        public float maximumDetectionAngle = 120;
        public float minimumDetectionAngle = -120;
        public float rotationSpeed = 15;
        public float maximumAttackRange = 1.5f;
        public float currentRecoveryTime = 0;
        public float currentThreatCheckTime = 10;
        public float threatCheckTimer = 10;
        public float alertRadius = 10;
        public float deathVolume;
        public int destPoint = 0;

        [Header("Realtime References")]
        public CharacterStats currentTarget;
        private List<PlayerThreat> playerThreats = new List<PlayerThreat>();
        public Transform[] patrolPoints;

        [Header("A.I Flags")]
        public bool isFumbled = false;
        public bool isPerformingAction;
        public bool isInteracting;
        public bool canGetInterrupted;
        public bool isRetreating;
        public bool isChaser = false;

        public NavMeshPath path;

        private PlayerThreat highestPlayerThreat;

        public LayerMask losMask;
        public bool inLineOfSight;

        #endregion

        #region Unity Callbacks

        void Awake()
        {
            enemyLocomotionManager = GetComponent<EnemyLocomotionManager>();
            enemyAnimationManager = GetComponentInChildren<EnemyAnimatorManager>();
            enemyStats = GetComponent<EnemyStats>();
            navMeshAgent = GetComponent<NavMeshAgent>();
            enemyRigidbody = GetComponent<Rigidbody>();
            audioSource = GetComponent<AudioSource>();

            losMask = 1 << 13;
        }

        private void Update()
        {
            if (!NetworkServer.active)
                return;

            HandleRecoveryTimer();
            HandleThreatCheck();
            HandleLineOfSight();

            isInteracting = enemyAnimationManager.anim.GetBool("isInteracting");
            canGetInterrupted = enemyAnimationManager.anim.GetBool("canBeInterrupted");
            isFumbled = enemyAnimationManager.anim.GetBool("fumbled");
        }

        public override void OnStartServer()
        {
            // This function is only run on the server,
            // Therefore only server will check every couple seconds
            // Mechanics like threat check etc.

            HandleTimers();
        }

        public void ClientHandleEnemiesVisible(bool oldBoolean, bool newBoolean)
        {
            gameObject.SetActive(newBoolean);
        }

        public async void HandleTimers()
        {
            await Timer(2);

            HandleTimers();
        }

        public async Task Timer(float duration)
        {
            // Simples time possible set to duration

            var end = Time.time + duration;
            while (Time.time < end)
            {
                await Task.Yield();
            }
        }

        private void FixedUpdate()
        {
            // FixedUpdate to handle state machine for the AI
            // since we do not need it to run each frame

            if (!NetworkServer.active)
                return;

            if (enemyStats.isDead)
                return;

            HandleStateMachine();
        }

        public bool IsNetworkServer()
        {
            // Cheat way to check on other object withouth networkbehaviour
            // Wether it is server on not

            if(NetworkServer.active)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        ///////////// State Machine //////////////////////

        private void HandleStateMachine()
        {
            if (currentState != null)
            {
                State nextState = currentState.Tick(this, enemyStats, enemyAnimationManager);

                if (nextState != null)
                {
                    SwitchToNextState(nextState);
                }
            }
        }

        private void SwitchToNextState(State state)
        {
            currentState = state;
        }

        public void Idle()
        {
            currentState = allStates[2];
        }

        public void Patrol()
        {
            currentState = allStates[1];
        }

        public void Chase(int who)
        {
            currentTarget = ((GameNetworkManager)NetworkManager.singleton).Players[who].GetComponent<CharacterStats>();
            currentState = allStates[4];
            isChaser = true;
        }

        ///////////// Functions //////////////////////

        public void SetPosition(Vector3 position)
        {
            navMeshAgent.Warp(position);
        }

        public void Reposition()
        {
            Vector3 pos = new Vector3(transform.position.x, 0, transform.position.z);

            SetPosition(pos);
        }

        public bool IsServer()
        {
            if (NetworkServer.active)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public void HandleLineOfSight()
        {
            if (this == null)
                return;

            if (currentTarget == null)
                return;

            if(currentTarget != null)
            {
                RaycastHit hit;
                // Does the ray intersect any objects excluding the player layer
                if (Physics.Raycast(enemyStats.indicatorPoint.position, currentTarget.indicatorPoint.position, out hit, Vector3.Distance(enemyStats.indicatorPoint.position,currentTarget.indicatorPoint.position), losMask))
                {
                    inLineOfSight = false;
                }
                else
                {
                    inLineOfSight = true;
                }
            }

        }

        public void AlertOthers()
        {
            Collider[] colliders = Physics.OverlapSphere(transform.position, alertRadius, alertLayer);

            for (int i = 0; i < colliders.Length; i++)
            {
                EnemyManager otherEnemyManager = colliders[i].transform.GetComponent<EnemyManager>();

                if (otherEnemyManager != null)
                {
                    if (currentTarget != null)
                        otherEnemyManager.currentTarget = currentTarget;
                }
            }
        }

        

        private void HandleRecoveryTimer()
        {
            if(currentRecoveryTime > 0)
            {
                currentRecoveryTime -= Time.deltaTime;
            }
            else
            {
                currentRecoveryTime = 0;
            }
          
            if (isPerformingAction)
            {
                if (currentRecoveryTime <= 0)
                {
                    isPerformingAction = false;
                }
            }
        }

        ///////////// Threat Mechanic //////////////////////
        
        // Threat system that each time a player deals damage to this enemy
        // This enemy will reconsider who the enemy wants to target
        // Depending on which player dealt more damage
        // Stone ward is a "turret" that players build

        public void IfCurrentTargetDied()
        {
            currentTarget = null;

            if (highestPlayerThreat == null)
            {
                playerThreats.Remove(highestPlayerThreat);
            }
            else
            {
                ClearThreat();
            }
            
            HandleThreatCheck();
        }

        private void HandleThreatCheck()
        {
            if (this == null)
                return;

            // if there is less than 1 elements in dictionary return
            if (playerThreats.Count < 1)
                return;

            int playerID = -1;
            int mostThreatAmount = 0;

            // loop through all list elements and check who has most threat amount
            for (int i = 0; i < playerThreats.Count; i++)
            {
                if (playerThreats[i].threatAmount < mostThreatAmount)
                    continue;

                playerID = playerThreats[i].playerID;
                mostThreatAmount = playerThreats[i].threatAmount;

                highestPlayerThreat = playerThreats[i];
            }

            // change current target to dictonary by looping through all players on the server and comparing their ids
            List<NetworkPlayer> players = ((GameNetworkManager)NetworkManager.singleton).Players;

            foreach (NetworkPlayer player in players)
            {
                if (player.playerID == playerID)
                {
                    currentTarget = player.GetComponent<CharacterStats>();

                    return;
                }
            }
        }

        public void AddThreat(int playerid, int dmgAmount)
        {
            foreach (PlayerThreat playerThreat in playerThreats)
            {
                // check if int of playerID already exists in the list
                if (playerThreat.playerID == playerid)
                {
                    // if it does, add dmg amount to id
                    playerThreat.threatAmount = playerThreat.threatAmount + dmgAmount;
                    return;
                }
            }
            //if no players in threats then we gotta go on that boi!
            if(playerThreats.Count < 1)
            {
                if (playerid > 100)
                {
                    List<StoneWardStats> wards = ((GameNetworkManager)NetworkManager.singleton).stoneWards;

                    foreach (StoneWardStats ward in wards)
                    {
                        if (ward.wardID == playerid)
                        {
                            currentTarget = ward.GetComponent<CharacterStats>();
                        }
                    }
                }
                else
                {
                    List<NetworkPlayer> players = ((GameNetworkManager)NetworkManager.singleton).Players;

                    foreach (NetworkPlayer player in players)
                    {
                        if (player.playerID == playerid)
                        {
                            currentTarget = player.GetComponent<CharacterStats>();
                        }
                    }
                }
            }
            // if not make it with dmg amount
            playerThreats.Add(new PlayerThreat(playerid, dmgAmount));

            currentThreatCheckTime = 0.5f;
        }

        public void ClearThreat()
        {
            playerThreats.Clear();
        }

        #endregion

        #region Mirror Callbacks

        ///////////// Server //////////////////////

        

        ///////////// Client //////////////////////

        

        #endregion            
    }
}

