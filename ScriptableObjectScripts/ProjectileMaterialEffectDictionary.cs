using MBS.Tools;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

[CreateAssetMenu(fileName = "ProjectileMaterialEffectDictionary", menuName = "MBSTools/Scriptable Objects/ New ProjectileMaterialEffectDictionary")]
public class ProjectileMaterialEffectDictionary : ScriptableObject
{
    //make the ability to add material tag -- projectile tag linkages, and have a sound and particle effect inside the linkage
    public List<ProjectileMaterialEffectDictonaryLinkageItem> MaterialToProjectileEffects;

    public ProjectileMaterialEffectDictonaryLinkageItem Lookup(ProjectileTag projTag, MaterialTag matTag)
    {
        if (MaterialToProjectileEffects == null)
            return null;

        ProjectileMaterialEffectDictonaryLinkageItem returnVal = null;

        foreach (var item in MaterialToProjectileEffects)
        {
            //If we are looking for "If anything hits anything", then search for a item with projectileTag and MaterialTag blank
            if (projTag == null && matTag == null)
            {
                if (item.MaterialTag == null && item.ProjectileTag == null)
                    returnVal = item;
            }
            else if (projTag == null)//If we are looking for "If anything hits Material", then search for an item with no projectileTag and with the specified materialTag.
            {
                if (item.MaterialTag != null)
                {
                    if (item.MaterialTag.Tag == matTag.Tag && item.ProjectileTag == null)
                        returnVal = item;
                }

            }
            else if (matTag == null)//If we are looking for "If Projectile hits Anything", then search for an item with no materialTag and with the specified ProjectileTag.
            {
                if (item.ProjectileTag != null)
                {
                    if (item.ProjectileTag.Tag == projTag.Tag && item.MaterialTag == null)
                        returnVal = item;
                }
            }
            else//else if we are looking for "If Projectile hits Material", then search for an item with the specified materialTag and ProjectileTag.
            {
                if (item.ProjectileTag != null && item.MaterialTag != null)
                {
                    if (item.ProjectileTag.Tag == projTag.Tag && item.MaterialTag.Tag == matTag.Tag)
                        returnVal = item;
                }
            }

        }

        return returnVal;

    }

    public List<MaterialTag> GetAllMaterialTags()
    {
        if (MaterialToProjectileEffects == null)
            return null;

        List<MaterialTag> matList = new List<MaterialTag>();
        foreach (var item in MaterialToProjectileEffects)
        {
            if(!matList.Contains(item.MaterialTag))
                matList.Add(item.MaterialTag);
        }

        return matList;
    }


    public void OnValidate()
    {
        foreach (var item in MaterialToProjectileEffects)
        {
            string projectileName = item.ProjectileTag != null ? item.ProjectileTag.Tag : "Unspecified Projectile";
            string MaterialName = item.MaterialTag != null ? item.MaterialTag.Tag : "Unspecified Material";
            string newName = projectileName + " to " + MaterialName;

            item.Name = newName;
        }
    }
}

[Serializable]
public class ProjectileMaterialEffectDictonaryLinkageItem
{
    [MBSReadOnly]
    public string Name;
    [Tooltip("The projectile which these effects correspond to.")]
    public ProjectileTag ProjectileTag;
    [Tooltip("The material which these effects correspond to. Leave this blank for this to be the default projectile effect for this projectile tag while not contained in or hitting anything in particular. ")]
    public MaterialTag MaterialTag;
    [Tooltip("The particle effect to play when the projectile hits this material.")]
    public AssetReference OnHitParticleEffectPrefab;
    [Tooltip("The particle effect to play when a bullet hits and ricochets off of the specified material.")]
    public AssetReference BulletMarkParticleEffectPrefab;
    [Tooltip("The particle effect to play when a bullet hits and dies on the specified material.")]
    public AssetReference BulletHoleParticleEffectPrefab;
    [Tooltip("The particle effect to play when a bullet hits and penetrates the specified material.")]
    public AssetReference BulletHolePenetrationParticleEffectPrefab;
    [Tooltip("The particle effect to play when the projectile is emitted while contained within this material.")]
    public AssetReference OnEmitParticleEffectPrefab;
    [Tooltip("The sound effect to play when the projectile hits this material.")]
    public AssetReference OnHitSoundEffectPrefab;
    [Tooltip("The sound effect to play when the projectile is emitted while contained within this material.")]
    public AssetReference OnEmitSoundEffectPrefab;
    [Tooltip("The sound effect to play when the projectile wizzes close past a listener with a ListenForProjectileNearMiss component. For slower projectiles, just attach an audio source to a trail.")]
    public AssetReference OnNearMissSoundEffectPrefab;
    [Range(0, 1), Tooltip("The amount that the particles velocity is turned in the direction of the projectiles velocity.")]
    public float OnHitParticleSlantTowardsHitVelocity = 0;

    public List<AssetReference> GetAssetReferences()
    {
        List<AssetReference> assetReferences = new List<AssetReference>();
        if(OnHitSoundEffectPrefab.RuntimeKeyIsValid())
            assetReferences.Add(OnHitSoundEffectPrefab);
        if (OnHitParticleEffectPrefab.RuntimeKeyIsValid())
            assetReferences.Add(OnHitParticleEffectPrefab);
        if (BulletMarkParticleEffectPrefab.RuntimeKeyIsValid())
            assetReferences.Add(BulletMarkParticleEffectPrefab);
        if (BulletHoleParticleEffectPrefab.RuntimeKeyIsValid())
            assetReferences.Add(BulletHoleParticleEffectPrefab);
        if (BulletHolePenetrationParticleEffectPrefab.RuntimeKeyIsValid())
            assetReferences.Add(BulletHolePenetrationParticleEffectPrefab);
        if (OnEmitParticleEffectPrefab.RuntimeKeyIsValid())
            assetReferences.Add(OnEmitParticleEffectPrefab);
        if (OnEmitSoundEffectPrefab.RuntimeKeyIsValid())
            assetReferences.Add(OnEmitSoundEffectPrefab);

        return assetReferences;
    }

}
public enum ProjectileEffectType
{
    OnHit,
    BulletMark,
    BulletHole,
    BulletHolePenetration,
    OnHitAudio,
    NearMissAudio
}
