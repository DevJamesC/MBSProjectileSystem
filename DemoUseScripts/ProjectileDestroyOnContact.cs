using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MBS.ProjectileSystem;
using MBS.Tools;

[RequireComponent(typeof(Collider))]
public class ProjectileDestroyOnContact : MonoBehaviour, IMBSEventListener<ProjectileEvent>
{
    [Tooltip("If Blacklist is true, only projectile tags and projectiles SOs listed will be stopped. If it is false, only projectile tags and projectile SOs listed will be let through.")]
    public bool Blacklist;
    public List<ProjectileTag> projectileTagList;
    public List<Projectile> projectileSOList;
    [Tooltip("Any emitters in the list will have their projectiles exempt from the calculation.")]
    public List<ProjectileEmitter> friendlyEmitters;


    protected Collider coll;
    public void Start()
    {
        coll = GetComponent<Collider>();
        if (projectileTagList == null)
            projectileTagList = new List<ProjectileTag>();
        if (projectileSOList == null)
            projectileTagList = new List<ProjectileTag>();
        if (friendlyEmitters == null)
            friendlyEmitters = new List<ProjectileEmitter>();
    }

    public void OnMBSEvent(ProjectileEvent eventType)
    {
        if (eventType.EventType != ProjectileEventTypes.OnHitTrigger)
            return;

        if (!eventType.HitThisCollider(coll))
            return;

        KillProjectile(eventType.Projectile, eventType.RaycastHit.point);

    }

    public void KillProjectile(ActiveProjectile proj, Vector3 hitpos)
    {
        if (friendlyEmitters.Contains(proj.Emitter))
            return;

        bool contains = projectileTagList.Contains(proj.ProjectileBlueprint.tag);
        if (!contains)
            contains = projectileSOList.Contains(proj.ProjectileBlueprint);

        bool kill = Blacklist ? contains : !contains;

        if (kill)
        {
            proj.KillProjectile(true);
            //draw laser
            GameObject obj = new GameObject("autoCreated Laser");

            LineRenderer lineRenderer = obj.AddComponent<LineRenderer>();
            Vector3[] line = new Vector3[2];
            line[0] = transform.position;
            line[1] = hitpos;
            lineRenderer.startWidth = .1f;
            lineRenderer.endWidth = .1f;
            lineRenderer.SetPositions(line);
            StartCoroutine(DestroyLineRenderer(obj, .25f));
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
