using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using static Gun;

public class Turret : MonoBehaviour
{
    Transform target;

    //Assignables
    [SerializeField] private Transform[] muzzle;
    [SerializeField] private Projectile bullet;
    [SerializeField] private Transform turretHead;
    [SerializeField] private float fireRate = 100f;
    [SerializeField] private float muzzleVelocity = 35f;

    public enum State { Idle, Attacking };
    public float turretRadius;
    Vector3 offsetRotation;
    float turretSpeed = 20f;
    float nextShot;
    State currentState;
    bool hasTarget;
    bool isInRange;
    bool dead;

    private void Awake()
    {
        if (GameObject.FindGameObjectWithTag("Player") != null)
        {
            hasTarget = true;

            target = GameObject.FindGameObjectWithTag("Player").transform;
        }

    }

    protected void Update()
    {
        Aim();
    }

    void Aim()
    {
        float dist = Vector3.Distance(transform.position, target.position);
        if (dist <= turretRadius)
        {
            turretHead.LookAt(target.localPosition);
            Shoot();

        }
    }

    void Shoot()
    {
        if (Time.time > nextShot)
        {
            for (int i = 0; i < muzzle.Length; i++)
            {
                nextShot = Time.time + fireRate / 1000;
                Projectile newBullet = Instantiate(bullet, muzzle[i].position, muzzle[i].rotation) as Projectile;
                newBullet.setSpeed(muzzleVelocity);
            }
        }
    }

    private void OnDrawGizmos()
    {   
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, turretRadius);
    }
}
