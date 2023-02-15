using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ON
{
    public class PatrolState : State
    {
        public bool isPatroling;
        public float detectionRadius = 2;
        public LayerMask detectionLayer;
        public IdleState idleState;
        public float patrolSpeed;

        public PursueTargetState pursueTargetState;

        public override State Tick(EnemyManager enemyManager, EnemyStats enemyStats, EnemyAnimatorManager enemyAnimatorManager)
        {
            // Constantly check for players in area
            // Otherwsie either move between patrol points
            // Or stand in one place

            if (!enemyManager.navMeshAgent.pathPending && enemyManager.navMeshAgent.remainingDistance < 1.5f)
            {
                if (!GotoNextPoint(enemyManager))
                    return idleState;
            }

            if (isPatroling && enemyManager.isInteracting == false)
            {
                enemyAnimatorManager.anim.SetFloat("Vertical", 1, 0.1f, Time.deltaTime);
                enemyManager.navMeshAgent.speed = patrolSpeed;
            }

            Collider[] colliders = Physics.OverlapSphere(enemyManager.transform.position, detectionRadius, detectionLayer);

            for (int i = 0; i < colliders.Length; i++)
            {
                CharacterStats characterStats = colliders[i].transform.GetComponent<CharacterStats>();

                if (characterStats != null)
                {
                    Vector3 targetsDirection = characterStats.transform.position - enemyManager.transform.position;
                    float viewableAngle = Vector3.Angle(targetsDirection, enemyManager.transform.forward);

                    if (viewableAngle > enemyManager.minimumDetectionAngle
                        && viewableAngle < enemyManager.maximumDetectionAngle)
                    {
                        enemyManager.currentTarget = characterStats;
                        isPatroling = false;
                    }
                }
            }

            if (enemyManager.currentTarget != null)
            {
                isPatroling = false;
                enemyManager.AlertOthers();
                enemyManager.initialPosition = transform.position;
                return pursueTargetState;
            }
            else
            {
                return this;
            }
        }

        bool GotoNextPoint(EnemyManager enemyManager)
        {
            // Returns if no points have been set up
            if (enemyManager.patrolPoints.Length == 0)
                return false;

            // Set the agent to go to the currently selected destination.
            enemyManager.navMeshAgent.destination = enemyManager.patrolPoints[enemyManager.destPoint].position;

            // Choose the next point in the array as the destination,
            // cycling to the start if necessary.
            enemyManager.destPoint = (enemyManager.destPoint + 1) % enemyManager.patrolPoints.Length;

            return true;
        }
    }
}

