using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MBS.ProjectileSystem;
using MBS.Tools;
using static UnityEngine.ParticleSystem;
using UnityEngine.ResourceManagement.AsyncOperations;

[RequireComponent(typeof(Collider))]
public class DemoCatchReflectProjectile : MonoBehaviour, IMBSEventListener<ProjectileEvent>
{
    public float ReflectRate;

    protected Collider coll;
    protected List<ActiveProjectile> caughtProjectiles;
    protected Dictionary<string,ParticleSystem> particleSystems;
    protected Dictionary<Projectile, ProjectileEmitter> emitters;
    protected List<ParticleSystem> particleSystemsAsIenumerable;
    protected Vector3 changeInPos;
    protected Vector3 lastFramePos;
    protected float currentReflectRate;
    protected List<ActiveProjectile> reflectedProjectiles;

    private void Start()
    {
        coll = GetComponent<Collider>();
        caughtProjectiles = new List<ActiveProjectile>();
        particleSystems = new Dictionary<string, ParticleSystem>();
        emitters = new Dictionary<Projectile, ProjectileEmitter>();
        particleSystemsAsIenumerable = new List<ParticleSystem>();
        reflectedProjectiles = new List<ActiveProjectile>();
        changeInPos = Vector3.zero;
        lastFramePos = transform.position;
        currentReflectRate = 0;
    }

    public void OnMBSEvent(ProjectileEvent eventType)
    {
        if (eventType.EventType != ProjectileEventTypes.OnHitTrigger)
            return;

        if (!eventType.HitThisCollider(coll))
            return;

        if (reflectedProjectiles.Contains(eventType.Projectile))
            return;

        CatchProjectile(eventType.Projectile, eventType.RaycastHit.point);

    }

    protected void Update()
    {
        foreach (ActiveProjectile proj in caughtProjectiles)
        {
            proj.Position += changeInPos;
        }

        currentReflectRate += Time.deltaTime;
        if (currentReflectRate >= ReflectRate)
        {
            currentReflectRate = 0;
            ReflectCaughtProjectiles();
        }
    }

    protected void LateUpdate()
    {
        changeInPos = transform.position - lastFramePos;
        lastFramePos = transform.position;
        reflectedProjectiles.Clear();
    }

    protected void CatchProjectile(ActiveProjectile proj, Vector3 hitpos)
    {
        ActiveProjectile caughtProj = new ActiveProjectile(proj);
        caughtProjectiles.Add(caughtProj);
        proj.Position = hitpos;
        proj.Alive = false;
        //fake the projectile while it is "caught"
        //check if we have a projectile system for this type of projectile
        if (proj.ProjectileBlueprint.projectileParticleSystemPrefab.RuntimeKeyIsValid())
        {
            //if not, instanciate a new projectileSystem, then emit the projectile
            if (!particleSystems.ContainsKey(proj.ProjectileBlueprint.projectileParticleSystemPrefab.AssetGUID))
            {
               AsyncOperationHandle handle= SpawnAddressable.Spawn(proj.ProjectileBlueprint.projectileParticleSystemPrefab,this.transform);
                handle.Completed += (asyncOperationHandle) => 
                {
                    GameObject result = asyncOperationHandle.Result as GameObject;
                    ParticleSystem system = result.GetComponentInChildren<ParticleSystem>();
                    particleSystems.Add(proj.ProjectileBlueprint.projectileParticleSystemPrefab.AssetGUID, system);
                    particleSystemsAsIenumerable.Add(system);
                    MainModule main = system.main;
                    main.simulationSpace = ParticleSystemSimulationSpace.Local;
                    CollisionModule collision = system.collision;
                    collision.enabled = false;
                    // emit a projectile and place it at the location of tha caught projectile
                    EmitParams particleParams = new EmitParams();
                    particleParams.position = hitpos - transform.position;
                    particleParams.velocity = Vector3.zero;
                    particleParams.startLifetime = 100;
                    system.Emit(particleParams, 1);
                };
            }
            else
            {
                // emit a projectile and place it at the location of tha caught projectile
                EmitParams particleParams = new EmitParams();
                particleParams.position =hitpos- transform.position;
                particleParams.velocity = Vector3.zero;
                particleParams.startLifetime = 100;
                particleSystems[proj.ProjectileBlueprint.projectileParticleSystemPrefab.AssetGUID].Emit(particleParams, 1);
            }
        }

    }

    protected void ReflectCaughtProjectiles()
    {
        foreach(ActiveProjectile proj in caughtProjectiles)
        {
            //check if we have an emitter for the type of projectiles
            //if not, create the emitter
            if (!emitters.ContainsKey(proj.ProjectileBlueprint))
            {
                GameObject emitterObj = new GameObject("autoGen "+proj.ProjectileBlueprint.name+" emitter");
                emitterObj.transform.parent = transform;
                ProjectileEmitter newEmitter = emitterObj.AddComponent<ProjectileEmitter>();
                newEmitter.ProjectileSO = proj.ProjectileBlueprint;
                newEmitter.AtmosphereData = proj.AtmosphereData;
                newEmitter.TargetLayers = proj.TargetLayers;
                newEmitter.LocalTimescaleValue = Time.timeScale;
                emitters.Add(proj.ProjectileBlueprint, newEmitter);
            }
            //then emit
            reflectedProjectiles.Add(emitters[proj.ProjectileBlueprint].Launch(proj.Position,-transform.forward,null,false,null,false,false));
        }
        //then clear out the particle system graphcis
        foreach (var item in particleSystemsAsIenumerable)
        {
            item.Clear();
        }
        caughtProjectiles.Clear();


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
