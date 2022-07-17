using MBS.Tools;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewProjectileTag", menuName = "MBSTools/Scriptable Objects/ New ProjectileTag")]
public class ProjectileTag : ScriptableObject
{
    [MBSReadOnly]
    public string Tag;

    private void OnValidate()
    {
        Tag = this.name;
    }
}
