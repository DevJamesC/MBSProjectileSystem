using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterGrabbableWeapon : CharacterGrabbable
{
    public Transform rightHandPos;
    //public Transform leftHandPos; 
    protected CameraSelectTarget cameraSelector;
    protected override void Start()
    {
        base.Start();
        cameraSelector = CameraSelectTarget.Instance;
    }

    public override void Pickup(CharacterAbilityPickupGrabbable grabbingObject,Transform palmTransform)
    {
        base.Pickup(grabbingObject,palmTransform);

        Gun newGun = cameraSelector.GetSelectedGun();
        if (newGun == null)
            return;

        grabbingObject.handleWeapon.EquipNewWeapon(newGun);

        //place weapon grip on the palm of the character
        transform.rotation = palmTransform.rotation;
        grabbingObject.rightHandConstraint.data.tip.transform.position = rightHandPos.transform.position+ (grabbingObject.rightHandConstraint.data.tip.transform.position-palmTransform.position);
                
        newGun.transform.parent = palmTransform;
        grabbingObject.rightHandConstraint.data.target.position = grabbingObject.rightHandConstraint.data.tip.position;

        grabbingObject.handleWeapon.rightThumbConstraint.weight = 1;
        grabbingObject.handleWeapon.rightPointerConstraint.weight = 1;      
    }
}
