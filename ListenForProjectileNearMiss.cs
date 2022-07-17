using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MBS.Tools;


namespace MBS.ProjectileSystem
{
    [RequireComponent(typeof(MaterialTagComponent))]
    public class ListenForProjectileNearMiss:MonoBehaviour
    {
        //This is used as a tag by projectiles to know that when they hit an object with this component, they spawn a near miss noise
        public static Dictionary<Collider,ListenForProjectileNearMiss> Instances;
        [HideInInspector]
        public MaterialTagComponent materialTagComp;

        public void Start()
        {
            materialTagComp = GetComponent<MaterialTagComponent>();
        }
        public static ListenForProjectileNearMiss GetListenForProjectileNearMissByCollider(Collider coll)
        {
            if (coll == null)
                return null;

            if (Instances == null)
                Instances = new Dictionary<Collider, ListenForProjectileNearMiss>();

            if (Instances.ContainsKey(coll))
                return Instances[coll];

            ListenForProjectileNearMiss comp = coll.GetComponent<ListenForProjectileNearMiss>();
            if (comp == null)
                return null;

            Instances.Add(coll, comp);
            return comp;
        }
    }
}
