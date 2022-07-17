using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MBS.ProjectileSystem
{
    [RequireComponent(typeof(Collider))]
    public class MaterialTagComponent : MonoBehaviour
    {
        public MaterialTag materialTag;

        public static Dictionary<Collider, MaterialTagComponent> Instances;

        public static MaterialTag GetMaterialTagByCollider(Collider coll)
        {
            if (coll == null)
                return null;

            if (Instances == null)
                Instances = new Dictionary<Collider, MaterialTagComponent>();

            if (Instances.ContainsKey(coll))
                return Instances[coll].materialTag;

            MaterialTagComponent comp = coll.GetComponent<MaterialTagComponent>();
            if (comp == null)
                return null;

            Instances.Add(coll, comp);
            return comp.materialTag;
        }
    }
}
