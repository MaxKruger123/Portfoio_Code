using UnityEngine;

public class GrapplingRope : MonoBehaviour
{

   
    public LineRenderer lr;

    public Graplpling grapplingGun;
    public PlayerMovement pm;

    public int quality;
    private Spring spring;
    public float damper;
    public float strength;
    public float velocity;
    public float waveCount;
    public float waveHeight;
    public AnimationCurve affectCurve;

    private void Awake()
    {
        spring = new Spring();
        spring.SetTarget(0);
    }

    private void LateUpdate()
    {
        if (grapplingGun.grappling)
            lr.SetPosition(0, grapplingGun.gunTip.position);
    }

    private void StartGrapple()
    {
        if (grapplingGun.grapplingCdTimer > 0) return;

        grapplingGun.grappling = true;

        pm.freeze = true;

        RaycastHit hit;
        if (Physics.Raycast(grapplingGun.cam.position, grapplingGun.cam.forward, out hit, grapplingGun.maxGrappleDistance, grapplingGun.whatIsGrappeable))
        {
            grapplingGun.grapplePoint = hit.point;

            Invoke(nameof(grapplingGun.ExecuteGrapple), grapplingGun.grappleDelayTime);
        }
        else
        {
            grapplingGun.grapplePoint = grapplingGun.cam.position + grapplingGun.cam.forward * grapplingGun.maxGrappleDistance;
            spring.Reset();
            if (lr.positionCount > 0)
                lr.positionCount = 0;

            Invoke(nameof(grapplingGun.StopGrapple), grapplingGun.grappleDelayTime);
        }

        if (lr.positionCount == 0)
        {
            spring.SetVelocity(velocity);
            lr.positionCount = quality + 1;
        }

        spring.SetDamper(damper);
        spring.SetStrength(strength);
        spring.Update(Time.deltaTime);

        var grapplePoint = grapplingGun.grapplePoint;
        var gunTipPosition = grapplingGun.gunTip.position;
        var up  = Quaternion.LookRotation((grapplePoint - gunTipPosition).normalized) * Vector3.up;

        lr.enabled = true;
        lr.SetPosition(1, grapplingGun.grapplePoint);

        for (var i = 0; i < quality + 1; i++)
        {
            var delta = i / (float)quality;
            var offset = up * waveHeight * Mathf.Sin(delta * waveCount * Mathf.PI) * spring.Value * affectCurve.Evaluate(delta);

            lr.SetPosition(i, Vector3.Lerp(gunTipPosition, grapplePoint, delta) + offset);
        }
    }
}
