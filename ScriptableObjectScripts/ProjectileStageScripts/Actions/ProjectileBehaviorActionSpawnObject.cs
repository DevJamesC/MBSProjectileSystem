using MBS.LocalTimescale;
using MBS.Tools;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace MBS.ProjectileSystem
{
    [CreateAssetMenu(fileName = "StageActionSpawnObject", menuName = "MBS/Projectile System/Scriptable Objects/ Projectiles/ StageAction/ Spawn Object")]
    public class ProjectileBehaviorActionSpawnObject : ProjectileBehaviorAction
    {
        [Tooltip("Assign either a Prefab or an Addressable. If a prefab and addressable are assigned, the addressable will be ignored.")]
        public GameObject PrefabToSpawn;
        [Tooltip("Assign either a Prefab or an Addressable. If a prefab and addressable are assigned, the addressable will be ignored.")]
        public AssetReference AddressableToSpawn;
        public SpawnPlacementType spawnPositionType;
        [Tooltip("How far back we offset, based on the projectile direction.")]
        public float spawnOffsetFromVelocity;
        [Tooltip("How far up we offset, based on the hit surface normal")]
        public float spawnOffsetFromHitNormal;
        public bool UseObjectPooling;
        public bool SpawnedObjectInheritsProjectileLocalTimescale;

        public override void Tick(ActiveProjectile proj)
        {
            if (PrefabToSpawn != null)
            {
                SpawnPrefab(proj, null);
                return;
            }
            else if (AddressableToSpawn.RuntimeKeyIsValid())
            {
                MBSSimpleAddressablePooler pooler = MBSSimpleAddressablePooler.GetInstanceOrInstanciate(AddressableToSpawn);
                if (pooler != null)
                {
                    GameObjectOrHandle<GameObject> obj = pooler.GetPooledGameObject();
                    if (!obj.IsEmpty())
                    {
                        if (obj.Object != null)
                        {
                            SpawnPrefab(proj, obj.Object);
                        }
                        else if (obj.IsLoadingHandle)
                        {
                            ActiveProjectile projClone = new ActiveProjectile(proj);
                            obj.Handle.Completed += (asyncOperationHandle) =>
                            {
                                GameObjectOrHandle<GameObject> obj = pooler.GetPooledGameObject();
                                if (!obj.IsEmpty())
                                {
                                    if (obj.Object != null)
                                    {
                                        SpawnPrefab(projClone, obj.Object);
                                    }
                                    else
                                    {
                                        obj.Handle.Completed += (asyncOpnHandle) =>
                                        {
                                            SpawnPrefab(projClone, asyncOpnHandle.Result as GameObject);
                                        };
                                    }
                                }

                            };
                        }
                        else
                        {
                            ActiveProjectile projClone = new ActiveProjectile(proj);
                            obj.Handle.Completed += (asyncOperationHandle) =>
                                {
                                    SpawnPrefab(projClone, asyncOperationHandle.Result as GameObject);
                                };
                        }
                    }

                }
            }




        }

        protected void SpawnPrefab(ActiveProjectile proj, GameObject obj)
        {
            Vector3 position = Vector3.zero;
            Vector3 offset = (-proj.VelocityNormal * spawnOffsetFromVelocity);

            switch (spawnPositionType)
            {
                case SpawnPlacementType.SpawnAtProjectilePosition:
                    position = proj.Position + offset;
                    break;

                case SpawnPlacementType.SpawnAtLastHitPosition:
                    if (proj.LastRaycastHit.collider == null)
                    {
                        position = proj.Position + offset;
                        break;
                    }
                    offset += (proj.LastRaycastHit.normal * spawnOffsetFromHitNormal);
                    position = proj.LastRaycastHit.point + offset;
                    break;
            }

            if (obj == null)
            {

                obj = UseObjectPooling ? MBSSimpleObjectPooler.GetInstanceOrInstanciate(PrefabToSpawn).GetPooledGameObject() : Instantiate(PrefabToSpawn, position, Quaternion.identity);

            }


            if (UseObjectPooling)
            {
                obj.transform.position = position;
                obj.SetActive(true);
            }

            if (SpawnedObjectInheritsProjectileLocalTimescale)
            {

                foreach (var item in obj.MBSGetComponentsAround<ILocalTimeScale>(false, true))
                {
                    item.LocalTimescaleValue = proj.LocalTimescaleValue;
                }
            }
        }

        protected void ManipulateObject(GameObject obj)
        {

        }

        public enum SpawnPlacementType
        {
            SpawnAtProjectilePosition,
            SpawnAtLastHitPosition
        }
    }
}
