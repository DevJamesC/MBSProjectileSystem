using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MBS.ProjectileSystem;
using UnityEngine.InputSystem;

public class FireballAbility : MonoBehaviour
{
    public ProjectileEmitter emitter;
    private ProjectileSeekData seekData;
    CameraSelectTarget cameraSelector;
    Animator animator;
    // Start is called before the first frame update
    void Start()
    {
        seekData = null;
        cameraSelector = CameraSelectTarget.Instance;
        animator = GetComponentInParent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            animator.SetTrigger("ThrowFireball");
        }
    }

    public void ActivateAbility()
    {
        seekData = null;
        if (cameraSelector.GetSelectedHealth() != null)
        {
            seekData = new ProjectileSeekData(default, cameraSelector.SelectedSelectable.transform, ProjectileSeekData.Seekmode.SeekTransform);
        }
        
    }

    public void EmitProjectile()
    {
        emitter.Launch(emitter.Origin.position, Camera.main.transform.forward, seekData);
    }
}
