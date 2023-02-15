using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ON
{
    public class RetreatState : State
    {
        public float retreatSpeed;
        public PatrolState patrolState;
        public float positionError;

        public override State Tick(EnemyManager enemyManager, EnemyStats enemyStats, EnemyAnimatorManager enemyAnimatorManager)
        {
            float distanceFromTarget = Vector3.Distance(transform.position, enemyManager.initialPosition);

            HandleRotateTowardsInitialPosition(enemyManager);

            if (distanceFromTarget > positionError)
            {
                enemyAnimatorManager.anim.SetFloat("Vertical", 2, 0.1f, Time.deltaTime);
                enemyManager.navMeshAgent.destination = enemyManager.initialPosition;
                enemyManager.navMeshAgent.speed = retreatSpeed;
                return this;
            }
            else
            {
                enemyAnimatorManager.anim.SetFloat("Vertical", 0);
                enemyManager.currentTarget = null;
                enemyManager.navMeshAgent.destination = enemyManager.initialPosition;
                enemyManager.isRetreating = false;
                return patrolState;
            }       
        }

        private void HandleRotateTowardsInitialPosition(EnemyManager enemyManager)
        {

            Vector3 relativeDirection = transform.InverseTransformDirection(enemyManager.navMeshAgent.desiredVelocity);
            Vector3 targetVelocity = enemyManager.enemyRigidbody.velocity;

            enemyManager.navMeshAgent.enabled = true;
            enemyManager.navMeshAgent.SetDestination(enemyManager.initialPosition);
            enemyManager.enemyRigidbody.velocity = targetVelocity;
            enemyManager.transform.rotation = Quaternion.Slerp(enemyManager.transform.rotation, enemyManager.navMeshAgent.transform.rotation, enemyManager.rotationSpeed / Time.deltaTime);
        }
    }
}

