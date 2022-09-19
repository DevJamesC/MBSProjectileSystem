using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MBS.Tools;
using static UnityEngine.ParticleSystem;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class ProjectileOnHitParticleEmitter : MBSSingleton<ProjectileOnHitParticleEmitter>
{
    protected Dictionary<GameObject, GameObject> prefabToInstance;//Unused
    protected Dictionary<Collider, Dictionary<GameObject, GameObject>> colliderToPrefabToInstance;//Unused
    protected Dictionary<GameObject, ParticleSystem> onHitParticleSystems;//Unused
    protected Dictionary<GameObject, ParticleSystem> gameObjectToParticleSys;

    public static void PreloadHitAndEmitEffects(ProjectileTag projectileTag, ProjectileMaterialEffectDictionary projMatEffectDict)
    {
        if (projMatEffectDict == null)
            return;

        List<MaterialTag> materialTags = projMatEffectDict.GetAllMaterialTags();

        foreach (var item in materialTags)
        {
            ProjectileMaterialEffectDictonaryLinkageItem linkItem = projMatEffectDict.Lookup(projectileTag, item);
            List<AssetReference> refs = linkItem.GetAssetReferences();

            foreach (AssetReference assetReference in refs)
            {
                SpawnAddressable.LoadAsset(assetReference);
            }

        }

    }


    /// <summary>
    /// Spawn Effect using Prefabs on the main thread
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="position"></param>
    /// <param name="direction"></param>
    /// <param name="newParent"></param>
    public static void SpawnEffect(GameObject obj, Vector3 position, Vector3 direction, Transform newParent = null, bool nonParticleSysEffect = false)
    {
        if (obj == null)
            return;

        if (Instance.gameObjectToParticleSys == null)
            Instance.gameObjectToParticleSys = new Dictionary<GameObject, ParticleSystem>();

        GameObject psObj = MBSSimpleObjectPooler.GetInstanceOrInstanciate(obj).GetPooledGameObject();
        ApplyManipulationToEffectObj(psObj, position, direction, newParent, nonParticleSysEffect);
    }

    /// <summary>
    /// Spawn effect using Addressable Prefabs using asyncronous code
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="position"></param>
    /// <param name="direction"></param>
    /// <param name="newParent"></param>
    /// <param name="scene">Leave blank to indicate the active scene</param>
    public static void SpawnEffect(AssetReference assetReference, Vector3 position, Vector3 direction, Transform newParent = null, bool nonParticleSysEffect = false, string scene = "")
    {

        if (!assetReference.RuntimeKeyIsValid())
            return;

        if (Instance.gameObjectToParticleSys == null)
            Instance.gameObjectToParticleSys = new Dictionary<GameObject, ParticleSystem>();


        //If the asset we want to instanciate is not loaded yet, then start the load
        if (!SpawnAddressable.AssetIsLoaded(assetReference))
        {

            AsyncOperationHandle handle = SpawnAddressable.LoadAsset(assetReference);
            handle.Completed += (asyncOperationHandle) =>
            {
                SpawnEffect(assetReference, position, direction, newParent, nonParticleSysEffect);
            };
            return;
        }

        MBSSimpleAddressablePooler pooler = MBSSimpleAddressablePooler.GetInstanceOrInstanciate(assetReference, scene);
        if (pooler != null)
        {
            pooler.GetPooledGameObject((gameObject) => 
            {
                ApplyManipulationToEffectObj(gameObject, position, direction, newParent, nonParticleSysEffect);
            });

            //JACRESC 9/19/2022
            //if (pooledObject == null)
            //    return;

            //if (pooledObject.IsEmpty())
            //    return;

            ////If we have a pooled object already instanciated, then use it to spawn the effect
            //if (pooledObject.Object != null)
            //{
            //    ApplyManipulationToEffectObj(pooledObject.Object, position, direction, newParent, nonParticleSysEffect);

            //}
            //else if (!pooledObject.IsEmpty())
            //{ //else if we will have a pooled object once it finishes instanciating asyncrounously, set it to spawn the effect once it appears
            //    pooledObject.Handle.Completed += (asyncOperationHandle) =>
            //        {
            //            ApplyManipulationToEffectObj(asyncOperationHandle.Result as GameObject, position, direction, newParent, nonParticleSysEffect);
            //        };
            //}
        }

    }

    private static void ApplyManipulationToEffectObj(GameObject psObj, Vector3 position, Vector3 direction, Transform newParent = null, bool nonParticleSysEffect = false)
    {
        if (!nonParticleSysEffect)
        {
            ParticleSystem ps;
            if (!Instance.gameObjectToParticleSys.ContainsKey(psObj))
            {
                ps = psObj.GetComponentInChildren<ParticleSystem>();
                if (ps == null)
                    return;
                Instance.gameObjectToParticleSys.Add(psObj, ps);
            }
        }
        //ps=Instance.gameObjectToParticleSys[psObj];
        Quaternion QuatDir = Quaternion.LookRotation(direction, Vector3.up);

        bool instanciateNewRotOffsetTracker = psObj.transform.childCount == 0;
        instanciateNewRotOffsetTracker = instanciateNewRotOffsetTracker ? true : psObj.transform.GetChild(psObj.transform.childCount - 1).name != "[AutoGenerated]LocalRotOffsetMarker";

        if (instanciateNewRotOffsetTracker)
        {
            GameObject rotOffsetTracker = new GameObject("[AutoGenerated]LocalRotOffsetMarker");
            rotOffsetTracker.transform.parent = psObj.transform;
            rotOffsetTracker.transform.localRotation = Quaternion.Euler(psObj.transform.rotation.eulerAngles);
        }

        Vector3 rotOffset = psObj.transform.GetChild(psObj.transform.childCount - 1).localRotation.eulerAngles;
        Vector3 posOffset = psObj.transform.childCount > 0 ? QuatDir * psObj.transform.GetChild(0).localPosition : Vector3.zero;

        psObj.transform.position = position + posOffset;
        psObj.transform.rotation = Quaternion.Euler(QuatDir.eulerAngles + rotOffset);
        Vector3 oldScale = psObj.transform.localScale;
        if (newParent != null)
        {
            psObj.transform.parent = newParent;
            psObj.transform.localScale = oldScale;
        }

        psObj.SetActive(true);
    }


    //CURRENTLY UNUSED. RATHER INFLEXIBLE, BUT LIGHER WEIGHT THAN SPAWNEFFECT()
    public static void EmitOnHit(GameObject obj, EmitParams particleParams, Vector3 direction, int emissionCount, Collider coll = null)
    {
        if (obj == null)
            return;

        if (Instance.prefabToInstance == null)
            Instance.prefabToInstance = new Dictionary<GameObject, GameObject>();

        if (Instance.onHitParticleSystems == null)
            Instance.onHitParticleSystems = new Dictionary<GameObject, ParticleSystem>();

        if (Instance.colliderToPrefabToInstance == null)
            Instance.colliderToPrefabToInstance = new Dictionary<Collider, Dictionary<GameObject, GameObject>>();

        //If we do not have this particle system instanciated, create it
        if (coll == null)
        {
            if (!Instance.prefabToInstance.ContainsKey(obj))
            {

                GameObject newObj = Instantiate(obj, Instance.transform);
                ParticleSystem ps = newObj.GetComponentInChildren<ParticleSystem>();
                if (ps == null)
                    return;

                Instance.prefabToInstance.Add(obj, newObj);
                Instance.onHitParticleSystems.Add(newObj, ps);
            }
        }
        else
        {
            if (!Instance.colliderToPrefabToInstance.ContainsKey(coll))
            {
                Instance.colliderToPrefabToInstance.Add(coll, new Dictionary<GameObject, GameObject>());
            }
            if (!Instance.colliderToPrefabToInstance[coll].ContainsKey(obj))
            {
                GameObject newObj = Instantiate(obj, coll.transform);
                ParticleSystem ps = newObj.GetComponentInChildren<ParticleSystem>();
                if (ps == null)
                    return;

                Instance.colliderToPrefabToInstance[coll].Add(obj, newObj);
                Instance.onHitParticleSystems.Add(newObj, ps);
            }
        }

        GameObject psGameObject = coll == null ? Instance.prefabToInstance[obj] : Instance.colliderToPrefabToInstance[coll][obj];
        ParticleSystem particleSystem = Instance.onHitParticleSystems[psGameObject];

        //Rotate the particle 
        Quaternion QuatDir = Quaternion.LookRotation(direction, Vector3.up);

        if (particleSystem.main.startRotation3D)
        {

            //MainModule mainMod = Instance.onHitParticleSystems[Instance.prefabToInstance[obj]].main;
            //mainMod.startRotationXMultiplier = 1;
            //mainMod.startRotationYMultiplier = 1;
            //mainMod.startRotationZMultiplier = 1;
            //mainMod.startRotationX = (QuatDir.eulerAngles.x) * Mathf.Deg2Rad; //Instance.myVector3.x * Mathf.Deg2Rad;//QuatDir.eulerAngles.x * Mathf.Deg2Rad;
            //mainMod.startRotationY = (QuatDir.eulerAngles.y) * Mathf.Deg2Rad;//Instance.myVector3.y * Mathf.Deg2Rad;//QuatDir.eulerAngles.y * Mathf.Deg2Rad;
            //mainMod.startRotationZ = (QuatDir.eulerAngles.z) * Mathf.Deg2Rad;//Instance.myVector3.z * Mathf.Deg2Rad;//QuatDir.eulerAngles.z * Mathf.Deg2Rad;
            particleParams.rotation3D = (QuatDir.eulerAngles) * Mathf.Deg2Rad;

        }
        else
        {
            // float angle;
            // Vector3 axis;
            // QuatDir.ToAngleAxis(out angle,out axis);
            //particleParams.rotation =QuatDir.eulerAngles.x;
            psGameObject.transform.rotation = QuatDir;
        }


        if (particleSystem.main.simulationSpace == ParticleSystemSimulationSpace.Local)
        {
            Debug.Log("TargetPos: " + particleParams.position + "  ParticleSysPos: " + particleSystem.gameObject.transform.position);
            Vector3 pos = new Vector3(particleSystem.transform.position.x - particleParams.position.x,
                particleParams.position.y - particleSystem.transform.position.y,
                Mathf.Sign(particleParams.position.z - particleSystem.transform.position.z) * -.05f);
            particleParams.position = pos;//Vector3.zero;//particleSystem.transform.position;
        }

        //emit from the particle system  
        particleSystem.Emit(particleParams, emissionCount);
    }

}
