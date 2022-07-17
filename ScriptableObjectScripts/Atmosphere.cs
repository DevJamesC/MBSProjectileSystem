using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewAtmosphere", menuName = "MBS/Projectile System/Scriptable Objects/ New Atmosphere")]
public class Atmosphere : ScriptableObject
{
    [Tooltip("Should be 0 for vaccum, and .5 to 2 for normal to thick atmosphres")]
    public float Drag=1f;
    [Tooltip("Should be faster for thinner atmospheres, and slower for thicker atmosphers. This is the speed of sound in seconds per meter. On earth at sea level it is 331.5")]
    public float SpeedOfSound = 331.5f;
}
