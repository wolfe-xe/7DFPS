using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using static UnityEngine.EventSystems.EventTrigger;

[RequireComponent(typeof(NavMeshAgent))]
public class Enemies : Entity
{
    //Ref
    NavMeshAgent pathFindingEnemy;
    Transform target;
    Entity targetEntity;

    public static event System.Action OnDeathStatic;
    public ParticleSystem deathFX;

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
            targetEntity = target.GetComponent<Entity>();
        }

    }

    protected override void Start()
    {
        base.Start();

        if (hasTarget)
        {
            currentState = State.Chasing;

            StartCoroutine(UpdatePath());
        }

    }

    public override void TakeHit(float damage, Vector3 hitPoint, Vector3 hitDirection)
    {
        if (damage >= health)
        {
            if (OnDeathStatic != null)
            {
                OnDeathStatic();
            }
            //AudioManager.instance.PlaySound("Enemy Death", transform.position);
            Destroy(Instantiate(deathFX.gameObject, hitPoint, Quaternion.FromToRotation(Vector3.forward, hitDirection)) as GameObject, deathFX.startLifetime);
        }
        base.TakeHit(damage, hitPoint, hitDirection);
    }

    IEnumerator UpdatePath()
    {
        float refreshRate = 0.25f;

        while (hasTarget != null)
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