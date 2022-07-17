using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MBS.Tools;

namespace MBS.ProjectileSystem
{
    public class MaterialToughness : MonoBehaviour
    {
        [Tooltip("The materialTag is used to determine any hit effects and sounds when a projectile makes contact with it. Leave it null for a projectile to use thier default sound and effect.")]
        public MaterialTag MaterialTag;
        [Tooltip("Drag is the rate of slowdown over time while moving through matter. faster movement means more slowdown. 0 means no drag, while greater numbers mean percent speed lost per second. Drywall is 1000-2000, Water is 100 to 200, while air is 1-3.")]
        public float Drag;
        [Tooltip("How much velocity does a projectile lose upon hitting the collider? Y is percent lost, X is the speed of projectile. It should range from 0 to 100.")]
        public AnimationCurve PecentSlowdownOnImpact;

        protected MeshRenderer meshFilter;
        protected Collider coll;

        protected Dictionary<Collider, ProjectileEmitter> CollToEmitProjectileDict;

        private void Start()
        {
            meshFilter = GetComponent<MeshRenderer>();
            coll = GetComponent<Collider>();
            CollToEmitProjectileDict = new Dictionary<Collider, ProjectileEmitter>();
        }

        private void OnTriggerStay(Collider other)
        {
            //Get the ProjectileEmitter script attached to the object that is intersecting us
            ProjectileEmitter ep;

            //try to get lp without calling .GetComponent()
            bool isInDict = CollToEmitProjectileDict.TryGetValue(other, out ep);

            //if we have not seen lp before, then GetComponent()
            if (!isInDict)
                ep = other.gameObject.GetComponent<ProjectileEmitter>();

            //If the object does not have a ProjectileEmitter script, then return
            if (ep == null)
                return;

            //Add lp to our dictonary for quick reference later
            if (!isInDict)
                CollToEmitProjectileDict.Add(other, ep);

            bool isInside = meshFilter.bounds.Contains(ep.Origin.position);

            //If the object is inside us, and we have already set the collider, or if we are outside, and the collider is null, then return
            if ((isInside && ep.ContainedByCollider == coll) || (!isInside && ep.ContainedByCollider == null))
                return;

            //Throw a message if we entered a collider while already inside a collider
            if (ep.ContainedByCollider != null && isInside)
            {
                Debug.Log(ep.gameObject.name + " Has entered a MaterialToughness region while they were already inside a MaterialToughness region! This will work until they exit back into the inital region, in which case" +
                   " the old region drag and whatnot will be null. To resolve this use case, add a condition here and in LaunchProjectile.cs to handle 'Being inside denser water while already inside water'");
                //Need to make ep.ContainedByCollider a list of colliders, and when we exit, we need to see if we are still in another, then assign that to be our current
            }

            //Set the 'contained within' varible for the ProjectileEmitter script, depending on if the origin launchpoint is inside or outside our bounds
            ep.ContainedByCollider = isInside ? coll : null;

            if (!isInside && isInDict)
                CollToEmitProjectileDict.Remove(other);
        }

        private void OnTriggerExit(Collider other)
        {
            //We need this in case we are leaving, but our origin is offset and still inside the collider

            //Get the LaunchProjectile script attached to the object that is intersecting us
            ProjectileEmitter ep;

            //try to get ep without calling .GetComponent()
            bool isInDict = CollToEmitProjectileDict.TryGetValue(other, out ep);

            //if we have not seen ep before, then GetComponent()
            if (!isInDict)
                other.gameObject.MBSGetComponentAround<ProjectileEmitter>();
            
            //If the object does not have an ProjectileEmitter script, then return
            if (ep == null)
                return;
            
            //Check if the launchpoint is contained within our bounds (removed due to the issue of "if the launchpoint is offset from the gun collider, we will exit while still being contained within"
            //if (meshFilter.bounds.Contains(ep.Origin.position))
                //return;

            //Set the 'contained within' varible for the ProjectileEmitter script, so it knows that it will no longer be shooting from within an object
            ep.ContainedByCollider = null;

            if (isInDict)
                CollToEmitProjectileDict.Remove(other);

            //check if we exited our collider, but we are still inside another
            

        }
    }
}
