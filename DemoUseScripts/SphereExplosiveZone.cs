using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MBS.LocalTimescale;
using static UnityEngine.ParticleSystem;

[RequireComponent(typeof(SphereCollider))]
public class SphereExplosiveZone : MonoBehaviour, ILocalTimeScale
{
    [Tooltip("Enable this to start the explosion as soon as this object is enabled")]
    public bool activateOnAwake = true;
    [Tooltip("How large the explosion will be")]
    public float explosionMaxRadius = 1f;
    [Tooltip("How large the explosion will be at the start")]
    public float explosionInitalRadius = .25f;
    [Tooltip("How long it takes for the explosion to expand")]
    public float TimeToMaxRadius = 0f;
    [Tooltip("Assign to this to sync up localTimescale")]
    public ParticleSystem ExplosionParticlSystem;
    public float Damage;
    public AnimationCurve DamageDropoffByDistanceFromCenter;
    public bool NeedLineOfSightForDamage;
    public bool UseDamageDropoff;
    public float WeakpointMultiplier;

    protected float _localTimescaleValue = 1;
    public float LocalTimescaleValue { get => _localTimescaleValue; set => _localTimescaleValue = value; }

    protected SphereCollider sphereCollider;
    protected bool isExploding;
    protected float currentTimeToMaxRadius;

    //Ideally, this class would only handle the sphere expansion, and not damage. For a real use case, separate these functionalities. 

    private void Awake()
    {
        if (sphereCollider == null)
            sphereCollider = GetComponent<SphereCollider>();

        isExploding = activateOnAwake;
        sphereCollider.enabled = isExploding;
        sphereCollider.radius = explosionInitalRadius;
        currentTimeToMaxRadius = 0;

        if (ExplosionParticlSystem != null)
        {
            MainModule m = ExplosionParticlSystem.main;
            m.simulationSpeed = _localTimescaleValue;
        }
    }

    // Update is called once per frame
    void Update()
    {
        HandleExplode();
    }

    protected void HandleExplode()
    {
        
        MainModule m = ExplosionParticlSystem.main;
        m.simulationSpeed = _localTimescaleValue;

        if (!isExploding)
            return;


        float percent = Mathf.Clamp01(currentTimeToMaxRadius / TimeToMaxRadius);
        sphereCollider.radius = Mathf.Lerp(explosionInitalRadius, explosionMaxRadius, percent);
        currentTimeToMaxRadius += LocalTimeScale.LocalDeltaTime(_localTimescaleValue);

        if (percent == 1) {
            isExploding=false;
            StartCoroutine(DisableSphereNextFrame());
        }
           
    }

    protected void OnEnable()
    {
        isExploding = activateOnAwake;
        sphereCollider.enabled = isExploding;
        sphereCollider.radius = explosionInitalRadius;
        currentTimeToMaxRadius = 0;

        if (ExplosionParticlSystem != null)
        {
            MainModule m = ExplosionParticlSystem.main;
            m.simulationSpeed = _localTimescaleValue;
        }
    }

    protected void OnTriggerEnter(Collider other)
    {
        //deal damage
        SimpleHealthSystem targHealth = other.gameObject.GetComponentInParent<SimpleHealthSystem>();
        if (targHealth == null)
            return;


        float outGoingDamage = Damage;

        //if we are using damage droppoff, apply the reduced damage
        if (UseDamageDropoff)
            outGoingDamage *= DamageDropoffByDistanceFromCenter.Evaluate(Vector3.Distance(transform.position, other.transform.position));

        //find out if the collider we hit is a weakpoint, invlulnerble point, or normal hit point
        SimpleHealthSystem.DamagePoint point = targHealth.FindDamagePointFromColl(other);

        //modify damage based on the type of point we hit
        switch (point.type)
        {
            case SimpleHealthSystem.DamagePointType.Weakpoint:
                outGoingDamage *= WeakpointMultiplier;
                break;
            case SimpleHealthSystem.DamagePointType.InvulnerblePoint:
                outGoingDamage = 0;
                break;
        }

        if (NeedLineOfSightForDamage)
        {
            Vector3 direction;
            float dist;
            RaycastHit hit;
            Physics.ComputePenetration(sphereCollider, sphereCollider.bounds.center, transform.rotation, other, other.bounds.center, other.transform.rotation, out direction, out dist);
            if (!other.Raycast(new Ray(transform.position, direction), out hit, sphereCollider.radius + .01f))
            {
                return;
            }
        }

        //deal damage
        targHealth.DealDamage(outGoingDamage, point);
    }


    public void OnValidate()
    {
        GetComponent<SphereCollider>().radius = explosionMaxRadius;
    }

    protected IEnumerator DisableSphereNextFrame()
    {
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        sphereCollider.enabled = false;
    }
}
