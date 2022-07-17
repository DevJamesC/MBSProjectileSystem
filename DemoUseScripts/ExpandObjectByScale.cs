using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExpandObjectByScale : MonoBehaviour
{
    public bool ExpandOnAwake = true;
    public GameObject objToExpand;
    public float timeTillMaxSize;
    public Vector3 maxSize;
    public float delay = 0;
    public float durationAtMaxBeforeDisable;
    public float Damage = 0;

    protected Vector3 startingSize;
    protected float currentTimeToMaxSize;
    protected float currentDelay;
    protected float currentDurationAtMaxBeforeDisable;
    bool isExpanding;
    // Start is called before the first frame update
    void Start()
    {
        startingSize = objToExpand.transform.localScale;
        currentTimeToMaxSize = 0;
        currentDelay = 0;
        isExpanding = ExpandOnAwake;
        currentDurationAtMaxBeforeDisable = 0;
    }

    private void Awake()
    {
        isExpanding = ExpandOnAwake;
    }

    private void OnEnable()
    {
        objToExpand.SetActive(true);
        objToExpand.transform.localScale = startingSize;
        currentTimeToMaxSize = 0;
        currentDelay = 0;
        currentDurationAtMaxBeforeDisable = 0;
        isExpanding = ExpandOnAwake;
    }

    // Update is called once per frame
    void Update()
    {
        if (!isExpanding)
        {
            if (currentDurationAtMaxBeforeDisable < durationAtMaxBeforeDisable)
                currentDurationAtMaxBeforeDisable += Time.deltaTime;
            else
                objToExpand.SetActive(false);
            return;
        }

        if (currentDelay < delay)
        {
            currentDelay += Time.deltaTime;
            return;
        }
        objToExpand.SetActive(true);
        float percent = Mathf.Clamp01(currentTimeToMaxSize / timeTillMaxSize);
        objToExpand.transform.localScale = Vector3.Lerp(startingSize, maxSize, percent);


        currentTimeToMaxSize += Time.deltaTime;

        if (percent == 1)
            isExpanding = false;
    }

    protected void OnTriggerEnter(Collider other)
    {
        SimpleHealthSystem health = other.GetComponentInParent<SimpleHealthSystem>();
        if (health == null)
            return;

        SimpleHealthSystem.DamagePoint damagePoint = health.FindDamagePointFromColl(other);
        health.DealDamage(Damage, damagePoint);
    }
}
