using MBS.Tools;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewMaterialTag", menuName = "MBS/Projectile System/Scriptable Objects/ New MaterialTag")]
public class MaterialTag : ScriptableObject
{
    [MBSReadOnly]
    public string Tag;

    public void Awake()
    {
        Tag = this.name;
    }

    private void OnValidate()
    {
        Tag = this.name;
    }
}
