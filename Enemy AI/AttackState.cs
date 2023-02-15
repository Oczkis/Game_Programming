using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ON
{
    public class AttackState : State
    {
        public EnemyAttackAction[] enemyAttacks;
        public EnemyAttackAction currentAttack;

        public CombatStanceState combatStanceState;
        public EnemyManager enemyManager;

        public override State Tick(EnemyManager enemyManager, EnemyStats enemyStats, EnemyAnimatorManager enemyAnimatorManager)
        {
            enemyManager.navMeshAgent.speed = 0;

            Vector3 targetDirection = enemyManager.currentTarget.transform.position - enemyManager.transform.position;
            float distanceFromTarget = Vector3.Distance(enemyManager.currentTarget.transform.position, enemyManager.transform.position);
            float viewableAngle = Vector3.Angle(targetDirection, transform.forward);

            HandleRotateTowardsTarget(enemyManager);

            if (enemyManager.isPerformingAction || enemyManager.isFumbled)
                return combatStanceState;

            if (currentAttack != null)
            {
                if (distanceFromTarget < currentAttack.minimumDistanceNeededToAttack)
                {
                    return this;
                }
                else if (distanceFromTarget < currentAttack.maximumDistanceNeededToAttack)
                {
                    if (viewableAngle <= currentAttack.maximumAttackAngle && viewableAngle >= currentAttack.minimumAttackAngle)
                    {
                        if (enemyManager.currentRecoveryTime <= 0 && enemyManager.isPerformingAction == false && !enemyManager.isInteracting)
                        {
                            enemyAnimatorManager.anim.SetFloat("Vertical", 0, 0.1f, Time.deltaTime);
                            enemyAnimatorManager.anim.SetFloat("Horizontal", 0, 0.1f, Time.deltaTime);
                            enemyAnimatorManager.PlayTargetAnimation(currentAttack.actionAnimation, true);
                            enemyManager.isPerformingAction = true;
                            enemyManager.currentRecoveryTime = currentAttack.recoveryTime;
                            currentAttack = null;
                            return combatStanceState;
                        }
                    }
                }
            }
            else
            {
                GetNewAttack(enemyManager);
            }

            return combatStanceState;
        }

        private void GetNewAttack(EnemyManager enemyManager)
        {
            // This function loops through different attack prefabs
            // And check which one is most appropriate
            // For example if target is too far to atack with melee
            // Then the range attack will have more attackScore than melee attack

            Vector3 targetsDirection = enemyManager.currentTarget.transform.position - transform.position;
            float viewableAngle = Vector3.Angle(targetsDirection, transform.forward);
            float distanceFromTarget = Vector3.Distance(enemyManager.currentTarget.transform.position, transform.position);

            int maxScore = 0;

            for (int i = 0; i < enemyAttacks.Length; i++)
            {
                EnemyAttackAction enemyAttackAction = enemyAttacks[i];

                if (distanceFromTarget <= enemyAttackAction.maximumDistanceNeededToAttack
                    && distanceFromTarget >= enemyAttackAction.minimumDistanceNeededToAttack)
                {
                    if (viewableAngle <= enemyAttackAction.maximumAttackAngle
                        && viewableAngle >= enemyAttackAction.minimumAttackAngle)
                    {
                        if(enemyManager.abilityPoints[i] >= enemyAttackAction.neededAbilityPoints)
                            maxScore += enemyAttackAction.attackScore;
                    }
                }
            }

            int randomValue = Random.Range(0, maxScore);
            int temporaryScore = 0;

            for (int i = 0; i < enemyAttacks.Length; i++)
            {
                EnemyAttackAction enemyAttackAction = enemyAttacks[i];

                if (distanceFromTarget <= enemyAttackAction.maximumDistanceNeededToAttack
                    && distanceFromTarget >= enemyAttackAction.minimumDistanceNeededToAttack)
                {
                    if (viewableAngle <= enemyAttackAction.maximumAttackAngle
                        && viewableAngle >= enemyAttackAction.minimumAttackAngle)
                    {
                        if (enemyManager.abilityPoints[i] >= enemyAttackAction.neededAbilityPoints)
                        {
                            if (currentAttack != null)
                                return;

                            temporaryScore += enemyAttackAction.attackScore;

                            if (temporaryScore > randomValue)
                            {
                                currentAttack = enemyAttackAction;

                                enemyManager.abilityPoints[i] = 0;

                                for (int x = 0; x < enemyManager.abilityPoints.Length; x++)
                                {
                                    enemyManager.abilityPoints[x] = enemyManager.abilityPoints[x] + 1;
                                }
                            }
                        }                           
                    }
                }
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

