using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.InputSystem;

public class CharacterAbilityHandleWeapon : MonoBehaviour
{
    public float RaiseWeaponDuration = .25f;
    public Transform aimedHandPosition;
    public Transform aimedHandHintPosition;
    public TwoBoneIKConstraint rightHandConstraint;
    public TwoBoneIKConstraint rightThumbConstraint;
    public TwoBoneIKConstraint rightPointerConstraint;

    public MultiAimConstraint headAimConstraint;
    public MultiAimConstraint bodyAimConstraint;
    public MultiAimConstraint handAimConstraint;


    public Gun currentGun { get; protected set; }

    // Update is called once per frame
    void Update()
    {
        if (currentGun == null)
            return;

        SetTransformData(rightHandConstraint.data.target, aimedHandPosition);
        SetTransformData(rightHandConstraint.data.hint, aimedHandHintPosition);
        SetTransformData(rightThumbConstraint.data.target, currentGun.ThumbTargetTransform);
        SetTransformData(rightThumbConstraint.data.hint, currentGun.ThumbHintTransform);
        SetTransformData(rightPointerConstraint.data.target, currentGun.PointerTargetTransform);
        SetTransformData(rightPointerConstraint.data.hint, currentGun.PointerHintTransform);


        bool tryToFire = currentGun.FullAuto ? Mouse.current.leftButton.isPressed : Mouse.current.leftButton.wasPressedThisFrame;

        if (tryToFire)
            currentGun.Fire();

        if (Keyboard.current.gKey.wasPressedThisFrame)
        {
            if (currentGun != null)
            {
                DiscardWeapon();
            }

        }

    }

    void SetTransformData(Transform toBeChanged, Transform changeTo)
    {
        toBeChanged.position = changeTo.position;
        toBeChanged.rotation = changeTo.rotation;
    }

    public void EquipNewWeapon(Gun newGun)
    {
        if (currentGun == null)
            StartCoroutine(RaiseWeapon());
        else
            DiscardWeapon(true);

        currentGun = newGun;
    }
    public void DiscardWeapon(bool isEquiptingNewWeapon=false)
    {
        currentGun.transform.parent = null;
        currentGun = null;

        if (isEquiptingNewWeapon)
            return;
 
        rightThumbConstraint.weight = 0;
        rightPointerConstraint.weight = 0;

        StartCoroutine(DropWeapon());
    }

    protected IEnumerator RaiseWeapon()
    {
        float reachTime = RaiseWeaponDuration/3;
        //rightHandConstraint.data.target.position = grabbable.transform.position;

        while (reachTime < RaiseWeaponDuration)
        {
            reachTime += Time.deltaTime;
            float percent = Mathf.Clamp01(reachTime / RaiseWeaponDuration);
            rightHandConstraint.weight = percent;
            headAimConstraint.weight = percent;
            bodyAimConstraint.weight =Mathf.Clamp(percent,0,.7f);
            handAimConstraint.weight = percent;
            yield return new WaitForEndOfFrame();
        }
    }

    protected IEnumerator DropWeapon()
    {
        float reachTime = 0;
        //rightHandConstraint.data.target.position = grabbable.transform.position;

        while (reachTime < RaiseWeaponDuration)
        {
            reachTime += Time.deltaTime;
            float percent = Mathf.Clamp01(reachTime / RaiseWeaponDuration);
            rightHandConstraint.weight = 1-percent;
            headAimConstraint.weight = 1 - percent;
            bodyAimConstraint.weight = 1 - percent;
            handAimConstraint.weight = 1 - percent;
            yield return new WaitForEndOfFrame();
        }

    }
}
