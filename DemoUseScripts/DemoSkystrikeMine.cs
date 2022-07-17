using MBS.LocalTimescale;
using MBS.ProjectileSystem;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DemoSkystrikeMine : ProjectileEmitter
{
    [Tooltip("in seconds. 0 is every Update()")]
    public float Frequency;
    //public ProjectileSeekData.Seekmode SeekMode;
    //public Transform SeekTarget;
    //public Vector3 SeekPoint;
    [Range(0f, 2f)]
    public float localTimeScale = 1;
    public Vector3 localGravity = Physics.gravity;

    public float Damage;
    public bool DrawDebugTrajectory;
    public GameObject canisterObj;
    [Tooltip("when the projectile slows below the threshold, it can be assumed it is at its apogee, and the sideguns will fire.")]
    public float AtApogeeVelocityThreshold;
    public Projectile sideGunProjectileSO;
    public DemoSkystrikeMineMicrogun SidegunOne;
    public DemoSkystrikeMineMicrogun SidegunTwo;
    public DemoSkystrikeMineMicrogun SidegunThree;
    public DemoSkystrikeMineMicrogun SidegunFour;
    public GameObject detectionBox;

    private float currentCooldown;
    private bool isActive;
    private ActiveProjectile proj;
    private Vector3 canisterLocalStartingPos;
    private bool sideGunsFiredFlag;

    List<GameObject> targets;

    // Start is called before the first frame update
    protected override void Start()
    {
        base.Start();
        currentCooldown = Frequency;
        LocalTimescaleValue = localTimeScale;
        LocalGravityValue = localGravity;
        isActive = false;
        sideGunsFiredFlag = false;
        canisterLocalStartingPos = canisterObj.transform.localPosition;
        targets = new List<GameObject>();
        SidegunOne.Damage = Damage;
        SidegunTwo.Damage = Damage;
        SidegunThree.Damage = Damage;
        SidegunFour.Damage = Damage;
    }

    // Update is called once per frame
    protected override void Update()
    {
        base.Update();


        if (DrawDebugTrajectory)
            DrawTrajectory();

        LocalTimescaleValue = localTimeScale;
        LocalGravityValue = localGravity;

        if (isActive)//If we have fired, then handle being in the air and mirroring our canister to the ActiveProjectilePath
        {
            canisterObj.transform.position = proj.Position;

            if (!sideGunsFiredFlag && proj.Velocity.magnitude <= AtApogeeVelocityThreshold)
            {
                Vector3 dirOne = targets.Count > 0 ? (targets[0].transform.position - SidegunOne.Origin.position).normalized : SidegunOne.Origin.up;
                Vector3 dirTwo = targets.Count > 1 ? (targets[1].transform.position - SidegunTwo.Origin.position).normalized : SidegunTwo.Origin.up;
                Vector3 dirThree = targets.Count > 2 ? (targets[2].transform.position - SidegunThree.Origin.position).normalized : SidegunThree.Origin.up;
                Vector3 dirFour = targets.Count > 3 ? (targets[3].transform.position - SidegunFour.Origin.position).normalized : SidegunFour.Origin.up;
                SidegunOne.Launch(SidegunOne.Origin.position, dirOne, null);
                SidegunTwo.Launch(SidegunTwo.Origin.position, dirTwo, null);
                SidegunThree.Launch(SidegunThree.Origin.position, dirThree, null);
                SidegunFour.Launch(SidegunFour.Origin.position, dirFour, null);
                sideGunsFiredFlag = true;
            }

            if (!proj.Alive)
            {
                detectionBox.SetActive(false);
                canisterObj.transform.localPosition = canisterLocalStartingPos;
                proj = null;
                isActive = false;
                sideGunsFiredFlag = false;

            }
            return;
        }

        if (currentCooldown > 0)
        {
            currentCooldown -= LocalTimeScale.LocalDeltaTime(_localTimeScale);
            return;
        }
        //If we are not firing, then fire
        if (!isActive)
        {
            currentCooldown = Frequency;
            isActive = true;
            targets = new List<GameObject>();
            //ProjectileSeekData seekdata = new ProjectileSeekData(SeekPoint, SeekTarget, SeekMode);
            proj = Launch(Origin.position, Origin.forward, null);
            detectionBox.SetActive(true);
        }

    }

    protected void DrawTrajectory()
    {
        //ProjectileSeekData seekdata = new ProjectileSeekData(SeekPoint, SeekTarget, SeekMode);
        GetTrajectoryFull(Origin.position, Origin.forward, null, ProjectileSO, true);
    }

    protected void OnTriggerEnter(Collider collision)
    {
        if (collision.gameObject.GetComponentInParent<SimpleHealthSystem>() != null)
        {
            if (!targets.Contains(collision.gameObject))
                targets.Add(collision.gameObject);
        }
    }
}
