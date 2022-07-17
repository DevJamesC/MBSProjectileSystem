using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterGrabbable : MonoBehaviour
{
    public GameObject icon;
    public Vector3 iconOffset;

    protected GameObject currentIcon;
    protected float timesinceLastIconShow;
    protected new Camera camera;
    // Start is called before the first frame update
    protected virtual void Start()
    {
        camera= Camera.main;
    }

    // Update is called once per frame
    protected virtual void Update()
    {
        if (currentIcon == null)
            return;

        timesinceLastIconShow += Time.deltaTime;
        if (timesinceLastIconShow > .25f)
        {
            currentIcon.SetActive(false);
        }
        else
        {
            currentIcon.transform.rotation = camera.transform.rotation;
        }
    }

    public virtual void ShowIcon()
    {
        if (currentIcon == null)
        {
            currentIcon= Instantiate(icon,transform);
            currentIcon.transform.position = transform.position + iconOffset;
        }
        currentIcon.SetActive(true);
        timesinceLastIconShow = 0;
    }

    public virtual void Pickup(CharacterAbilityPickupGrabbable grabbingObject,Transform palmTransform)
    {

    }
}
