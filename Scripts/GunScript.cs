using TMPro;
using System.Collections;
using UnityEngine.VFX;
using UnityEngine;

public class GunScript : MonoBehaviour
{
    [Header("General Settings")]
    public float range = 100f;
    public Camera fpsCam;
    public int maxAmmo = 10;
    private int currentAmmo;
    public int stashedAmmo = 50;
    public float reloadTime = 1f;
    private bool isReloading = false;
    public Animator animator;
    public float fireRate = 0.5f;
    private float nextTimeToFire = 0f;
    public bool isSemiAutomatic = true;
    public AudioSource source;
    public bool hs;

    [Header("Recoil")]
    public float kickBackZ;
    public float recoilX, recoilY, recoilZ;
    public float snappiness, returnAmount;

    [Header("Bullet")]
    public float bulletForce;
    public Vector3 thisGun;
    public bool isShotgun;

    [Header("UI")]
    public TextMeshProUGUI currentWeaponAmmo;
    public TextMeshProUGUI stashedWeaponAmmo;

    [Header("Projectile Settings")]
    public GameObject projectilePrefab;
    public Transform barrelPoint;
    public float projectileSpeed = 50f;
    [SerializeField] private float spreadAngle = 1f;

    [Header("Gun Rotation")]
    [SerializeField] private float rotationSpeed = 5f;

    [Header("ADS Settings")]
    public bool isADS;
    public PlayerMovement playerMovement;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float normalFOV = 60f;
    [SerializeField] private float adsFOV = 45f;
    [SerializeField] private float adsTransitionDuration = 0.5f;
    private Coroutine fovCoroutine;

    [Header("Muzzle Flash")]
    public GameObject muzzleFlash;
    [SerializeField] private float muzzleFlashTime;

    [Header("Weapon Sway")]
    [SerializeField] private float swayAmount = 0.05f;
    [SerializeField] private float swaySpeed = 4f;
    [SerializeField] private float maxSwayDistance = 0.1f;

    [Header("Audio")]
    public Audio audio;

    public Rigidbody playerRb;
    public Vector3 initialLocalPosition;
    public Vector3 originalGunPosition;
    public Quaternion originalGunRotation;

    private Vector3 targetRotation, currentRotation;
    private Vector3 targetPosition, currentPosition;
    private Vector3 targetPoint;

    public bool isShooting;

    private void Start()
    {
        playerRb = GameObject.FindGameObjectWithTag("Player").GetComponent<Rigidbody>();
        currentAmmo = maxAmmo;

        if (initialLocalPosition == Vector3.zero)
            initialLocalPosition = transform.localPosition;

        targetPosition = currentPosition = transform.localPosition = initialLocalPosition;
    }

    private void OnEnable()
    {
        isReloading = false;
        transform.localPosition = initialLocalPosition;
    }

    private void Update()
    {
        playerCamera = Camera.main;

        HandleADSInput();
        HandleFireInput();
        HandleReloadInput();
        UpdateUI();

        if (!isShooting && !isADS)
            HandleWeaponSway();

        if (Input.GetKeyDown(KeyCode.I)) animator.SetBool("IsInspecting", true);
        else if (Input.GetKeyUp(KeyCode.I)) animator.SetBool("IsInspecting", false);

        back();
    }

    private void HandleADSInput()
    {
        if (Input.GetMouseButtonDown(1))
        {
            isADS = true;
            StartFOVChange(adsFOV);
        }
        else if (Input.GetMouseButtonUp(1))
        {
            isADS = false;
            StartFOVChange(normalFOV);
        }

        spreadAngle = isADS ? 0 : (isShotgun ? 5 : 2);
    }

    private void HandleFireInput()
    {
        if (isReloading || currentAmmo <= 0) return;

        bool canShoot = Time.time >= nextTimeToFire;

        if (isShotgun && Input.GetButtonDown("Fire1") && canShoot)
        {
            nextTimeToFire = Time.time + fireRate;
            ShootShotgun();
        }
        else if (isSemiAutomatic && Input.GetButtonDown("Fire1") && canShoot)
        {
            nextTimeToFire = Time.time + fireRate;
            Shoot();
        }
        else if (!isSemiAutomatic && Input.GetButton("Fire1") && canShoot)
        {
            nextTimeToFire = Time.time + fireRate;
            Shoot();
        }
    }

    private void HandleReloadInput()
    {
        if (isReloading) return;

        if (currentAmmo <= 0 && stashedAmmo > 0)
        {
            StartCoroutine(Reload());
        }

        if (Input.GetKeyDown(KeyCode.R) && currentAmmo < maxAmmo && stashedAmmo > 0)
        {
            StartCoroutine(Reload());
        }
    }

    private void UpdateUI()
    {
        if (currentWeaponAmmo != null)
            currentWeaponAmmo.text = currentAmmo.ToString();

        // Uncomment if using this field
        // if (stashedWeaponAmmo != null)
        //     stashedWeaponAmmo.text = stashedAmmo.ToString();
    }

    void back()
    {
        targetPosition = Vector3.Lerp(targetPosition, initialLocalPosition, Time.deltaTime * returnAmount);
        currentPosition = Vector3.Lerp(currentPosition, targetPosition, Time.fixedDeltaTime * snappiness);
        transform.localPosition = currentPosition;
    }

    public void Recoil()
    {
        targetPosition -= new Vector3(0, 0, kickBackZ);
        targetRotation += new Vector3(recoilX, Random.Range(-recoilY, recoilY), Random.Range(-recoilZ, recoilZ));
    }

    void Shoot()
    {
        if (currentAmmo <= 0) return;

        currentAmmo--;
        isShooting = true;

        PlayFireEffects();
        FireProjectile();
    }

    void ShootShotgun()
    {
        if (currentAmmo <= 0) return;

        currentAmmo--;
        isShooting = true;

        PlayFireEffects();

        for (int i = 0; i < 6; i++)
            FireProjectile();
    }

    private void PlayFireEffects()
    {
        source.PlayOneShot(audio.LR7Shoot);
        Recoil();
        animator.SetBool("IsShooting", true);
        StartCoroutine(ShootAnimWait());

        if (muzzleFlash != null)
        {
            muzzleFlash.SetActive(true);
            StartCoroutine(MuzzleFlashWait());
        }
        else
        {
            Debug.LogWarning("Muzzle flash not assigned!");
        }
    }

    IEnumerator Reload()
    {
        isReloading = true;
        animator.SetBool("IsReloading", true);

        yield return new WaitForSeconds(reloadTime);

        int bulletsToReload = Mathf.Min(maxAmmo - currentAmmo, stashedAmmo);
        currentAmmo += bulletsToReload;
        stashedAmmo -= bulletsToReload;

        isReloading = false;
        animator.SetBool("IsReloading", false);
    }

    void FireProjectile()
    {
        if (!projectilePrefab || !barrelPoint || !fpsCam) return;

        Ray camRay = fpsCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        Vector3 aimPoint = Physics.Raycast(camRay, out RaycastHit hit, range) ? hit.point : camRay.origin + camRay.direction * range;
        Vector3 finalDirection = (aimPoint - barrelPoint.position).normalized;
        Vector3 spreadDirection = GetSpreadDirection(finalDirection, spreadAngle);

        GameObject projectile = Instantiate(projectilePrefab, barrelPoint.position, Quaternion.LookRotation(spreadDirection));
        if (projectile.TryGetComponent(out Rigidbody rb))
            rb.linearVelocity = spreadDirection * projectileSpeed + playerRb.linearVelocity;
    }

    Vector3 GetSpreadDirection(Vector3 forward, float angle)
    {
        float randomX = Random.Range(-angle, angle);
        float randomY = Random.Range(-angle, angle);
        Quaternion spreadRotation = Quaternion.Euler(randomX, randomY, 0);
        return spreadRotation * forward;
    }

    void StartFOVChange(float targetFOV)
    {
        if (fovCoroutine != null)
            StopCoroutine(fovCoroutine);

        fovCoroutine = StartCoroutine(SmoothFOVChange(targetFOV));
    }

    IEnumerator SmoothFOVChange(float targetFOV)
    {
        float startFOV = playerCamera.fieldOfView;
        float elapsedTime = 0f;

        while (elapsedTime < adsTransitionDuration)
        {
            elapsedTime += Time.deltaTime;
            playerCamera.fieldOfView = Mathf.Lerp(startFOV, targetFOV, elapsedTime / adsTransitionDuration);
            yield return null;
        }

        playerCamera.fieldOfView = targetFOV;
    }

    public IEnumerator MuzzleFlashWait()
    {
        yield return new WaitForSeconds(muzzleFlashTime);
        muzzleFlash.SetActive(false);
        animator.SetBool("IsShooting", false);
        isShooting = false;
    }

    public IEnumerator ShootAnimWait()
    {
        yield return new WaitForSeconds(0.12f);
        animator.SetBool("IsShooting", false);
        isShooting = false;
    }

    void HandleWeaponSway()
    {
        float mouseX = Input.GetAxis("Mouse X") * swayAmount;
        float mouseY = Input.GetAxis("Mouse Y") * swayAmount;
        Vector3 swayOffset = new Vector3(-mouseX, -mouseY, 0);

        targetPosition = initialLocalPosition + swayOffset;
        currentPosition = Vector3.Lerp(currentPosition, targetPosition, Time.deltaTime * swaySpeed);

        if ((currentPosition - initialLocalPosition).magnitude > maxSwayDistance)
            currentPosition = initialLocalPosition + (currentPosition - initialLocalPosition).normalized * maxSwayDistance;

        transform.localPosition = currentPosition;
    }
}
