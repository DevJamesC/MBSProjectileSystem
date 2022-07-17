using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MBS.ProjectileSystem;
using MBS.LocalTimescale;

public class DemoPopMine : ProjectileEmitter
{
    [Tooltip("in seconds. 0 is every Update()")]
    public float Frequency;
    //public ProjectileSeekData.Seekmode SeekMode;
    //public Transform SeekTarget;
    //public Vector3 SeekPoint;
    [Range(0f, 2f)]
    public float localTimeScale = 1;
    public Vector3 localGravity = Physics.gravity;

    public float Damage;

    private float currentCooldown;

    protected override void Update()
    {
        base.Update();

        LocalTimescaleValue = localTimeScale;
        LocalGravityValue = localGravity;

        if (currentCooldown > 0)
        {
            currentCooldown -= LocalTimeScale.LocalDeltaTime(_localTimeScale);
            return;
        }
        currentCooldown = Frequency;

        //ProjectileSeekData seekdata = new ProjectileSeekData(SeekPoint, SeekTarget, SeekMode);
        Launch(Origin.position, Origin.forward, null);
    }
}
