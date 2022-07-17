using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MBS.Tools;

public class CameraSelectTarget : MBSSingleton<CameraSelectTarget>
{
    new Camera camera;
    public CharacterSelectable SelectedSelectable{ get; protected set; }
    public LayerMask SelectableLayers;
    // Start is called before the first frame update
    void Start()
    {
        camera = Camera.main;
    }

    // Update is called once per frame
    void Update()
    {
        SelectedLookedAtTarget();
    }

    protected void SelectedLookedAtTarget()
    {
        RaycastHit hit;
        bool noSelectable = false;

        if (Physics.Raycast(camera.transform.position, camera.transform.forward, out hit, 10000, SelectableLayers))
        {
            
            if (CharacterSelectable.SelectableLookup.ContainsKey(hit.collider.gameObject))
            {
                SelectedSelectable = CharacterSelectable.SelectableLookup[hit.collider.gameObject];
            }
            else
            {
                noSelectable = true;
            }

        }
        else
        {
            noSelectable = true;
        }

        if (noSelectable)
            SelectedSelectable = null;

    }

    public SimpleHealthSystem GetSelectedHealth()
    {
        if (SelectedSelectable == null)
            return null;

        return SelectedSelectable.HealthComp;
    }

    public Gun GetSelectedGun()
    {
        if (SelectedSelectable == null)
            return null;

        return SelectedSelectable.GunComp;
    }

    public CharacterGrabbable GetSelectedGrabable()
    {
        if (SelectedSelectable == null)
            return null;

        return SelectedSelectable.GrabbableComp;
    }
}
