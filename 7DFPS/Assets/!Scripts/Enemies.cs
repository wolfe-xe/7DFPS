using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using static UnityEngine.EventSystems.EventTrigger;

[RequireComponent(typeof(NavMeshAgent))]
public class Enemies : MonoBehaviour
{
    //Ref
    NavMeshAgent pathFindingEnemy;
    Transform target;

    //Assignables
    //public ParticleSystem deathFX;

    float enemyCollisionRadius;
    float targetCollisionRadius;

    public enum State { Idle, Chasing, Attacking };
    State currentState;
    bool hasTarget;
    bool dead;

    private void Awake()
    {
        pathFindingEnemy = GetComponent<NavMeshAgent>();

        if (GameObject.FindGameObjectWithTag("Player") != null)
        {
            hasTarget = true;

            target = GameObject.FindGameObjectWithTag("Player").transform;

            enemyCollisionRadius = GetComponent<CapsuleCollider>().radius;
            targetCollisionRadius = target.GetComponent<CapsuleCollider>().radius;

        }

    }

    protected void Start()
    {

        if (hasTarget)
        {
            currentState = State.Chasing;

            StartCoroutine(UpdatePath());
        }

    }

    IEnumerator UpdatePath()
    {
        float refreshRate = 0.25f;

        while (hasTarget)
        {
            if (currentState == State.Chasing)
            {
                Vector3 directionToTarget = (target.position - transform.position).normalized;
                Vector3 targetPosition = target.position - directionToTarget;

                if (!dead)
                {
                    pathFindingEnemy.SetDestination(targetPosition);
                    yield return new WaitForSeconds(refreshRate);
                }
            }

        }
    }
}
