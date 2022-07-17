using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MBS.ProjectileSystem;
using MBS.Tools;
using System;

[RequireComponent(typeof(Collider))]
public class ProjectileOneWayTrigger : MonoBehaviour, IMBSEventListener<ProjectileEvent>
{
    public MaterialTag materialForProjectileDeathFX;
    protected Collider coll;
    public void Start()
    {
        coll = GetComponent<Collider>();


    }

    public void OnMBSEvent(ProjectileEvent eventType)
    {
        if (eventType.EventType != ProjectileEventTypes.OnHitTrigger)
            return;

        if (!eventType.HitThisCollider(coll))
            return;

        KillProjectile(eventType.Projectile, eventType.RaycastHit);

    }

    public void KillProjectile(ActiveProjectile proj, RaycastHit hit)
    {
        //need to get the angle of hit, and check wether it is on the opposite of the front face
        bool kill = true;
        Vector3 rearangedNormal = new Vector3(-hit.normal.z, hit.normal.y, hit.normal.x);
        if (rearangedNormal == transform.forward)
        {
            kill = false;
        }

        if (kill)
        {
            proj.KillProjectile(true);
            //emit death or hit FX for projectile
            ProjectileEmitter.SpawnProjectileEffect(hit, proj, ProjectileEffectType.OnHit, null, materialForProjectileDeathFX);
            ProjectileEmitter.SpawnProjectileEffect(hit, proj, ProjectileEffectType.OnHitAudio, null, materialForProjectileDeathFX);
        }
    }

    protected IEnumerator DestroyLineRenderer(GameObject obj, float duration)
    {
        yield return new WaitForSeconds(duration);
        Destroy(obj);
    }

    /// <summary>
    /// OnEnable, we start listening to events.
    /// </summary>
    protected virtual void OnEnable()
    {
        this.MBSEventStartListening<ProjectileEvent>();
    }

    /// <summary>
    /// OnDisable, we stop listening to events.
    /// </summary>
    protected virtual void OnDisable()
    {
        this.MBSEventStopListening<ProjectileEvent>();
    }
}
