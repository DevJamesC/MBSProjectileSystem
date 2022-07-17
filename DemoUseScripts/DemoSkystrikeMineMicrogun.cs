using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MBS.ProjectileSystem;

public class DemoSkystrikeMineMicrogun : ProjectileEmitter
{
    [HideInInspector]
    public float Damage = 1f;
    protected override void Start()
    {
        base.Start();
        LocalTimescaleValue = 1f;

    }

    protected override void OnHit(RaycastHit hit, ActiveProjectile proj)
    {
        base.OnHit(hit, proj);

        SimpleHealthSystem health = hit.collider.gameObject.GetComponentInParent<SimpleHealthSystem>();
        if (health != null)
        {
            SimpleHealthSystem.DamagePoint damagePoint = health.FindDamagePointFromColl(hit.collider);
            health.DealDamage(Damage, damagePoint);
        }
    }
}
