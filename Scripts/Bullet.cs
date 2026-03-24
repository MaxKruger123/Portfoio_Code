using Unity.Burst.CompilerServices;
using UnityEngine;

public class Bullet : MonoBehaviour
{

    

    public Transform bulletRay;

    [SerializeField] private int damage;
    [SerializeField] private ElementTypeScript.ElementType bulletElement = ElementTypeScript.ElementType.None;

    public GameObject impactEffect;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    private void Update()
    {
        //// Perform a raycast in front of the bullet to detect collisions
        //RaycastHit hit;
        //float raycastDistance = 1f; // The length of the raycast

        //// Cast a ray forward from the bullet's current position
        //if (Physics.Raycast(bulletRay.position, transform.forward, out hit, raycastDistance))
        //{


        //    //Debug.Log(hit.collider.gameObject.name);
        //    Destroy(gameObject);

        //}
        //Debug.DrawRay(bulletRay.position, transform.forward * raycastDistance, Color.red);
    }

    private void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.tag != "Bullet" && other.gameObject.tag != "Player")
        {
            IDamageable damageable = other.gameObject.GetComponent<IDamageable>();

            if (damageable != null)
            {
                damageable.TakeDamage(damage, bulletElement);
            }

            Instantiate(impactEffect, transform.position, Quaternion.identity);
            Destroy(gameObject);
        }
        
    }
}

        
