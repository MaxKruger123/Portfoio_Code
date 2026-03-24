using UnityEngine;

public class Graplpling : MonoBehaviour
{
    [Header("References")]
    private PlayerMovement pm;
    public Transform cam;
    public Transform gunTip;
    public LayerMask whatIsGrappeable;
    public LineRenderer lr;

    [Header("Grappling")]
    public float maxGrappleDistance;
    public float grappleDelayTime;
    public float overshootYAxis;

    public Vector3 grapplePoint;

    [Header("Cooldown")]
    public float grapplingCd;
    public float grapplingCdTimer;

    [Header("Input")]
    public KeyCode grappleKey = KeyCode.Mouse3;

    public bool grappling;

    [Header("Wiggle Settings")]
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

    private void Start()
    {
        pm = GetComponent<PlayerMovement>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(grappleKey) && !grappling) StartGrapple();

        if (grapplingCdTimer > 0)
            grapplingCdTimer -= Time.deltaTime;

        if (grapplingCdTimer < 0)
        {
            grapplingCdTimer = 0;
        }
    }

    private void LateUpdate()
    {
        if (grappling)
        {
            lr.SetPosition(0, gunTip.position);

            // Update the spring continuously for the wiggle effect
            spring.Update(Time.deltaTime);

            var gunTipPosition = gunTip.position;
            var up = Quaternion.LookRotation((grapplePoint - gunTipPosition).normalized) * Vector3.up;

            for (var i = 0; i < quality + 1; i++)
            {
                var delta = i / (float)quality;
                var offset = up * waveHeight * Mathf.Sin(delta * waveCount * Mathf.PI) * spring.Value * affectCurve.Evaluate(delta);

                lr.SetPosition(i, Vector3.Lerp(gunTipPosition, grapplePoint, delta) + offset);
            }
        }
    }

    private void StartGrapple()
    {
        if (grapplingCdTimer > 0) return;

        GetComponent<Swinging>().StopSwing();
        grappling = true;
        pm.freeze = true;

        RaycastHit hit;

        // FIRST: try a direct raycast
        if (Physics.Raycast(cam.position, cam.forward, out hit, maxGrappleDistance, whatIsGrappeable))
        {
            grapplePoint = hit.point;
            Invoke(nameof(ExecuteGrapple), grappleDelayTime);
        }
        else
        {
            // SECOND: forgiving logic – search near the center of the screen
            RaycastHit[] hits = Physics.SphereCastAll(cam.position, 2.5f, cam.forward, maxGrappleDistance, whatIsGrappeable);

            // Find the closest valid target near the center of screen
            float closestAngle = Mathf.Infinity;
            Vector3 bestPoint = Vector3.zero;
            bool found = false;

            foreach (var h in hits)
            {
                Vector3 toHit = (h.point - cam.position).normalized;
                float angle = Vector3.Angle(cam.forward, toHit);

                if (angle < closestAngle)
                {
                    closestAngle = angle;
                    bestPoint = h.point;
                    found = true;
                }
            }

            if (found)
            {
                grapplePoint = bestPoint;
                Invoke(nameof(ExecuteGrapple), grappleDelayTime);
            }
            else
            {
                // If nothing was found even in forgiving search
                grapplePoint = cam.position + cam.forward * maxGrappleDistance;
                Invoke(nameof(StopGrapple), grappleDelayTime);
            }
        }

        if (lr.positionCount == 0)
        {
            spring.SetVelocity(velocity);
            lr.positionCount = quality + 1;
        }

        lr.positionCount = quality + 1;

        spring.Reset();
        spring.SetVelocity(velocity);
        spring.SetDamper(damper);
        spring.SetStrength(strength);
        spring.Update(Time.deltaTime);

        var gunTipPosition = gunTip.position;
        var up = Quaternion.LookRotation((grapplePoint - gunTipPosition).normalized) * Vector3.up;
        lr.enabled = true;

        for (var i = 0; i < quality + 1; i++)
        {
            var delta = i / (float)quality;
            var offset = up * waveHeight * Mathf.Sin(delta * waveCount * Mathf.PI) * spring.Value * affectCurve.Evaluate(delta);

            lr.SetPosition(i, Vector3.Lerp(gunTipPosition, grapplePoint, delta) + offset);
        }
    }


    public void ExecuteGrapple()
    {
        pm.freeze = false;

        Vector3 lowestPoint = new Vector3(transform.position.x, transform.position.y - 1f, transform.position.z);

        float grapplePointRelativeYPos = grapplePoint.y - lowestPoint.y;
        float highestPointOnArc = grapplePointRelativeYPos + overshootYAxis;

        if (grapplePointRelativeYPos < 0) highestPointOnArc = overshootYAxis;

        pm.JumpToPosition(grapplePoint, highestPointOnArc);

        Invoke(nameof(StopGrapple), 3f);
    }

    public void StopGrapple()
    {
        pm.freeze = false;

        grappling = false;

        grapplingCdTimer = grapplingCd;

        lr.enabled = false;
    }
}
