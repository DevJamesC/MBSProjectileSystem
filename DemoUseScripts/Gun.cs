using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MBS.ProjectileSystem;

public class Gun : ProjectileEmitter
{
    public bool FullAuto;
    public float ShotsPerMinute;
    public float maximumInnacuracy=10;
    public float minimumInnacuracy=0;
    public float damage;
    public float weakpointMultiplier;
    public Transform ThumbTargetTransform;
    public Transform ThumbHintTransform;
    public Transform PointerTargetTransform;
    public Transform PointerHintTransform;


    private float FireCooldown;
    // Start is called before the first frame update
    protected override void Start()
    {
        base.Start();
        FireCooldown = 0;
    }

    // Update is called once per frame
    protected override void Update()
    {
        base.Update();

        if (FireCooldown > 0)
            FireCooldown -= Time.deltaTime;

    }

    public void Fire()
    {
        if (FireCooldown > 0)
            return;

        Vector3 direction = Origin.forward;
        Vector3 innacuracyValue = Random.insideUnitCircle;
        innacuracyValue.x *= Random.Range(minimumInnacuracy/100, maximumInnacuracy/100);
        innacuracyValue.y *= Random.Range(minimumInnacuracy/100, maximumInnacuracy/100);
        //need to rotate innacuracy value to face the origin direction, then add it
        Quaternion rotateVal = Quaternion.LookRotation(Origin.forward);
        innacuracyValue = rotateVal * innacuracyValue;
        direction += innacuracyValue;

        Launch(Origin.position, direction);
        FireCooldown = 60/ShotsPerMinute;
    }

    protected override void OnHit(RaycastHit hit, ActiveProjectile proj)
    {
        base.OnHit(hit, proj);

        if (hit.collider == null)
            return;

        SimpleHealthSystem health=hit.collider.gameObject.GetComponentInParent<SimpleHealthSystem>();
        if (health == null)
            return;

        SimpleHealthSystem.DamagePoint damagePoint = health.FindDamagePointFromColl(hit.collider);

        float outgoingDamage = damage;
        switch (damagePoint.type)
        {
            case SimpleHealthSystem.DamagePointType.InvulnerblePoint:
                outgoingDamage = 0;
                break;
            case SimpleHealthSystem.DamagePointType.Weakpoint:
                outgoingDamage *= weakpointMultiplier;
                break;
        }


        health.DealDamage(outgoingDamage, damagePoint);
    }
}
