using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterSelectable : MonoBehaviour
{
    public SimpleHealthSystem HealthComp { get; protected set; }
    public Gun GunComp { get; protected set; }

    public CharacterGrabbable GrabbableComp { get; protected set; }

    protected List<GameObject> childObjects;

    public static Dictionary<GameObject, CharacterSelectable> SelectableLookup;
    // Start is called before the first frame update
    void Start()
    {
        if (SelectableLookup == null)
            SelectableLookup = new Dictionary<GameObject, CharacterSelectable>();

        HealthComp = GetComponentInChildren<SimpleHealthSystem>();
        GunComp = GetComponentInChildren<Gun>();
        childObjects = new List<GameObject>();
        GrabbableComp = GetComponentInChildren<CharacterGrabbable>();


        foreach (var collider in gameObject.GetComponentsInChildren<Collider>())
        {
            if (childObjects.Contains(collider.gameObject) || SelectableLookup.ContainsKey(collider.gameObject))
                continue;

            childObjects.Add(collider.gameObject);
            SelectableLookup.Add(collider.gameObject, this);

        }
    }

    private void OnDestroy()
    {
        foreach (var item in childObjects)
        {
            if (SelectableLookup.ContainsKey(item))
                SelectableLookup.Remove(item);
        }

        
    }
}
