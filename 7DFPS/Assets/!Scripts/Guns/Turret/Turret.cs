using UnityEngine;

public class Turret : Entity
{
    Transform target;

    public enum FireMode { auto, burst, single };
    public FireMode fireMode;

    //Assignables
    [SerializeField] private Transform[] muzzle;
    [SerializeField] private Projectile bullet;
    [SerializeField] private Transform turretHead;
    [SerializeField] private float fireRate = 100f;
    [SerializeField] private float muzzleVelocity = 35f;
    [SerializeField] private int burstCount;

    Entity targetEntity;
    public static event System.Action OnDeathStatic;
    public ParticleSystem deathFX;
    float damage = 10f;

    int shotsRemainingInBurst;

    public float turretRadius;
    public bool hasTarget = false;
    public bool inRange = false;
    float nextShot;

    private void Awake()
    {
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
        if(hasTarget)
        {   
            targetEntity.OnDeath += OnTargetDeath;
        }
        shotsRemainingInBurst = burstCount; 
    }

    protected void Update()
    {
        Aim();
    }

    void Aim()
    {
        if (hasTarget)
        {
            float dist = Vector3.Distance(transform.position, target.position);
            if (dist <= turretRadius)
            {
                inRange = true;
                turretHead.LookAt(target.localPosition);
                Shoot();
            }
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

    void OnTargetDeath()
    {
        hasTarget = false;
    }

    private void Shoot()
    {
        if (Time.time > nextShot)
        {
            if (fireMode == FireMode.burst)
            {
                if (shotsRemainingInBurst == 0)
                {
                    return;
                }
                shotsRemainingInBurst--;
            }
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