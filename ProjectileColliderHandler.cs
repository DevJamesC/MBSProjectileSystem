using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MBS.ProjectileSystem
{
    [RequireComponent(typeof(Rigidbody))]
    public class ProjectileColliderHandler : MonoBehaviour
    {
        protected new Rigidbody rigidbody;
        List<GameObject> others;
        // Start is called before the first frame update
        void Start()
        {
            rigidbody = GetComponent<Rigidbody>();
            others = new List<GameObject>();
        }

        void OnParticleCollision(GameObject other)
        {

            if (others.Contains(other))
                return;

            bool addToIgnore = false;

            // Get particles
            ParticleSystem particleSystem = other.GetComponent<ParticleSystem>();
            ProjectileEmitter emitter = particleSystem.gameObject.GetComponentInParent<ProjectileEmitter>();

            //if we have no particle system, then we have nothing to test against to see if a particle has hit us (even if the raycast didn't)
            if (particleSystem == null || emitter == null)
                return;

            //create a particle array
            ParticleSystem.Particle[] allParticles = new ParticleSystem.Particle[particleSystem.particleCount];
            // Get all particles in the particle system (all projectile particles). because structs are weird, this is actually assigning to the allParticles array we made above
            particleSystem.GetParticles(allParticles);

            Bounds particleBounds;

            //for each particle, we check each of our colliders and see if any of them intersect the particle
            for (int i = 0; i < allParticles.Length; i++)
            {
                //check each collider which is being used by this object's rigidbody
                foreach (Collider collider in rigidbody.GetComponentsInChildren<Collider>())
                {
                    //if the collider is a trigger, we skip
                    if (collider.isTrigger)
                        continue;

                    //Get the particle bounds, which is the position of this particle, and the 3dSize of the particle
                    particleBounds = new Bounds(allParticles[i].position, allParticles[i].GetCurrentSize3D(particleSystem));

                    //check if our collider intersects the particle bounds. If so, check if the projectile's penetration is 0 or lower. If it is, manually tell the projectile it hit something
                    if (collider.bounds.Intersects(particleBounds) || collider.bounds.Contains(particleBounds.center))
                    {
                        ActiveProjectile proj = emitter.GetProjectileByIndex(i);

                        if (proj != null)
                        {
                            emitter.ManualHitByIndex(i, collider);
                            addToIgnore = true;
                            //allParticles[i].remainingLifetime = -1f;
                        }
                        break;
                    }
                }

            }

            if (addToIgnore)
                StartCoroutine(IgnoreGameobject(other));

            //particleSystem.SetParticles(allParticles);
        }

        protected IEnumerator IgnoreGameobject(GameObject other)
        {
            if (!others.Contains(other))
                others.Add(other);

            yield return new WaitForEndOfFrame();

            others.Remove(other);

        }
    }

}

