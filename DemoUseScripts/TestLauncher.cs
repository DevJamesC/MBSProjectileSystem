using MBS.LocalTimescale;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MBS.ProjectileSystem
{
    public class TestLauncher : ProjectileEmitter
    {

        [Tooltip("in seconds. 0 is every Update()")]
        public float Frequency;
        public ProjectileSeekData.Seekmode SeekMode;
        public Transform SeekTarget;
        public Vector3 SeekPoint;
       // [Range(0f, 2f)]
        //public float localTimeScale=1;
        //public Vector3 localGravity = Physics.gravity;

        public float Damage;
        public AnimationCurve DamageDropoffBySpeed;
        public bool UseDamageDropoff;
        public float WeakpointMultiplier;
        public bool DrawDebugTrajectory;

        private float currentCooldown;

        // Start is called before the first frame update
        protected override void Start()
        {
            base.Start();
            currentCooldown = Frequency;
            //LocalTimescaleValue = localTimeScale;
            //LocalGravityValue = localGravity;
        }

        // Update is called once per frame
        protected override void Update()
        {
            base.Update();
            

            if (DrawDebugTrajectory)
                DrawTrajectory();

            //LocalTimescaleValue = localTimeScale;
            //LocalGravityValue = localGravity;

            if (currentCooldown > 0)
            {
                currentCooldown -= LocalTimeScale.LocalDeltaTime(_localTimeScale);
                return;
            }
            currentCooldown = Frequency;

            ProjectileSeekData seekdata = new ProjectileSeekData(SeekPoint, SeekTarget, SeekMode);
            Launch(Origin.position, Origin.forward, seekdata);

        }

        protected void DrawTrajectory()
        {
            ProjectileSeekData seekdata = new ProjectileSeekData(SeekPoint, SeekTarget, SeekMode);
            GetTrajectoryFull(Origin.position, Origin.forward, seekdata, ProjectileSO, true);
        }

        //Example code to check the distance of each projectile. This demonstrates that any extended classes have access to ActiveProjectile data
        //NOTE: Use Projectile Stages to make a projectile do 'stuff' when 'X' condition is met... an emitter should not change the projectile behavior.
        //Essentially, if the projectile is put on a different emitter without code like below, it should work exactly the same.
        protected void CheckProjectileDistance()
        {
            foreach (ActiveProjectile proj in inFlightProjectiles)
            {
                if (proj.LifetimeDistance > 10)
                {
                    //do something
                }

            }
        }

        protected override void OnHit(RaycastHit hit, ActiveProjectile proj)
        {
            base.OnHit(hit, proj);
            //Check if the thing we hit is the transform we are seeking (example code) (This also exists as a stage condition)
            bool hitTarget = false;
            if (proj.SeekData.CurrentSeekMode == ProjectileSeekData.Seekmode.SeekTransform)
                hitTarget = hit.collider.transform == proj.SeekData.TargetTransform;

            //Deal damage (example code)
            if (hit.collider == null)
                return;
            SimpleHealthSystem targHealth=hit.collider.gameObject.GetComponentInParent<SimpleHealthSystem>();
            if (targHealth == null)
                return;


            float outGoingDamage = Damage;

            //if we are using damage droppoff, apply the reduced damage
            if (UseDamageDropoff)
                outGoingDamage *= DamageDropoffBySpeed.Evaluate(proj.Velocity.magnitude);

            //find out if the collider we hit is a weakpoint, invlulnerble point, or normal hit point
            SimpleHealthSystem.DamagePoint point = targHealth.FindDamagePointFromColl(hit.collider);

            //modify damage based on the type of point we hit
            switch (point.type)
            {
                case SimpleHealthSystem.DamagePointType.Weakpoint:
                    outGoingDamage *= WeakpointMultiplier;
                    break;
                case SimpleHealthSystem.DamagePointType.InvulnerblePoint:
                    outGoingDamage = 0;
                    break;
            }

            //deal damage
            targHealth.DealDamage(outGoingDamage, point);

        }
    }
}
