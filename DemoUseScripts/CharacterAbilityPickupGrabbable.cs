using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.InputSystem;

public class CharacterAbilityPickupGrabbable : MonoBehaviour
{
    public float pickupRange = 1f;
    public float reachItemDuration=.25f;
    public TwoBoneIKConstraint rightHandConstraint;
    public Transform rightpalmTransform;
    protected CameraSelectTarget cameraSelector;
    public CharacterAbilityHandleWeapon handleWeapon { get; protected set; }
    // Start is called before the first frame update
    void Start()
    {
        cameraSelector = CameraSelectTarget.Instance;
        handleWeapon= GetComponent<CharacterAbilityHandleWeapon>();
    }

    // Update is called once per frame
    void Update()
    {

        if (!cameraSelector.GetSelectedGrabable())
            return;

        if (Vector3.Distance(cameraSelector.SelectedSelectable.GrabbableComp.gameObject.transform.position,transform.position) > pickupRange)
            return;

        cameraSelector.SelectedSelectable.GrabbableComp.ShowIcon();



        if (Keyboard.current.fKey.wasPressedThisFrame)
        {
            //cameraSelector.SelectedSelectable.GrabbableComp.Pickup(this);
            StartCoroutine(ReachTowards(cameraSelector.SelectedSelectable.GrabbableComp));
        }

        
    }

    protected IEnumerator ReachTowards(CharacterGrabbable grabbable)
    {
        float reachTime = 0;
        rightHandConstraint.data.target.position = grabbable.transform.position;

        while (reachTime < reachItemDuration)
        {
            reachTime += Time.deltaTime;
            float percent= Mathf.Clamp01(reachTime / reachItemDuration);
            rightHandConstraint.weight = percent;
            yield return new WaitForEndOfFrame();
        }

        grabbable.Pickup(this, rightpalmTransform);
    }
}
