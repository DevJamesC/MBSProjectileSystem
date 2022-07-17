using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraAim : MonoBehaviour
{
    public Transform AimConstraintTransform;
    public LayerMask gameWorldLayers;
    new Camera camera;
    // Start is called before the first frame update
    void Start()
    {
        camera = Camera.main;
    }

    // Update is called once per frame
    void Update()
    {
        RaycastHit hit;

        if (Physics.Raycast(camera.transform.position, camera.transform.forward, out hit, int.MaxValue, gameWorldLayers))
        {
            if (hit.collider.gameObject.transform.root.tag == "Player")
            {
                //if we hit the player, raycast again
                if(Physics.Raycast(hit.point+camera.transform.forward, camera.transform.forward, out hit, int.MaxValue, gameWorldLayers))
                {
                    AimConstraintTransform.position = hit.point;
                }
            }
            else
            {
                AimConstraintTransform.position = hit.point;
            }
        }
    }
}
