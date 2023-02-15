using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ON
{
    public class CombatStanceState : State
    {
        public AttackState attackState;
        public PursueTargetState pursueTargetState;
        public IdleState idleState;

        public override State Tick(EnemyManager enemyManager, EnemyStats enemyStats, EnemyAnimatorManager enemyAnimatorManager)
        {
            // Enemy checks during the fight informations about his target
            // Distance, is target dead or line of sight
            // Decides to either prepare an attack or start getting closer

            enemyManager.navMeshAgent.speed = 0;

            if (enemyManager.isInteracting)
                return this;

            if(enemyManager.currentTarget.isDead)
            {
                enemyManager.IfCurrentTargetDied();
            }

            if(enemyManager.currentTarget == null)
            {
                return idleState;
            }

            float distanceFromTarget = Vector3.Distance(enemyManager.currentTarget.transform.position, enemyManager.transform.position);

            HandleRotateTowardsTarget(enemyManager);

            if (enemyManager.isPerformingAction)
            {
                enemyAnimatorManager.anim.SetFloat("Vertical", 0, 0.1f, Time.deltaTime);
            }

            if (enemyManager.currentRecoveryTime <= 0 && distanceFromTarget <= enemyManager.maximumAttackRange && enemyManager.inLineOfSight)
            {
                return attackState;
            }
            else if (distanceFromTarget > enemyManager.maximumAttackRange)
            {
                return pursueTargetState;
            }
            else
            {
                return this;
            }
        }

        private void HandleRotateTowardsTarget(EnemyManager enemyManager)
        {
                Vector3 direction = enemyManager.currentTarget.transform.position - enemyManager.transform.position;
                direction.y = 0;
                direction.Normalize();

                if (direction == Vector3.zero)
                {
                    direction = enemyManager.transform.forward;
                }

                Quaternion targetRotation = Quaternion.LookRotation(direction);
                enemyManager.transform.rotation = Quaternion.Slerp(enemyManager.transform.rotation, targetRotation, enemyManager.rotationSpeed / Time.deltaTime);
        }
    }
}

