using System.Xml.Serialization;
using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using static ElementTypeScript;

public class EnemyStats : MonoBehaviour, IDamageable
{
    [Header("Stats")]
    [SerializeField]
    private float maxElementalHealth;
    [SerializeField]
    private float currentElementalHealth;
    [SerializeField]
    private float maxHealth;
    [SerializeField]
    private float currentHealth;
    [SerializeField]
    private GameObject deathEffect;


    [Header("HealthBars")]    
    public Slider elementalHealthBar;
    public Slider healthBar;
    private float targetHealth;  // To store target health for the slider    
    public float lerpDuration = 0.1f;  // Duration for the health bar lerp


    [Header("ElementSettings")]
    public ElementTypeScript.ElementType enemyElement = ElementTypeScript.ElementType.None;
    private Coroutine dotCoroutine;
    public Image DoT;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        currentElementalHealth = maxElementalHealth;
        currentHealth = maxHealth;

        healthBar.value = 1f;
        DoT.enabled = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (currentHealth <= 0)
        {
            Die();
        }        

    }



    public void TakeDamage(float damage, ElementTypeScript.ElementType bulletElement)
    {
        if (currentHealth <= 0) return;

        // Apply elemental damage bonus
        if (bulletElement == (ElementTypeScript.ElementType)enemyElement)
        {
            damage *= 1.5f; // Extra 50% damage
            ApplyDotEffect(bulletElement);
        }
        else
        {
            damage *= 0.5f;
        }

        // Damage elemental shield first, then health
        if (currentElementalHealth > 0)
        {
            currentElementalHealth -= damage;
            elementalHealthBar.value = currentElementalHealth / maxElementalHealth;
        }
        else
        {
            currentHealth -= damage;
            targetHealth = currentHealth / maxHealth;
            StartCoroutine(LerpHealthBar());
        }

        if (currentHealth <= 0) Die();
    }

    private void ApplyDotEffect(ElementType bulletElement)
    {
        if (dotCoroutine != null) StopCoroutine(dotCoroutine);
        dotCoroutine = StartCoroutine(DamageOverTime(3, 5, bulletElement));
    }

    private IEnumerator DamageOverTime(float damagePerTick, float duration, ElementType element)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            yield return new WaitForSeconds(1f);
            TakeDamage(damagePerTick, element);
            elapsed += 1f;
            DoT.enabled = true;
        }
        DoT.enabled = false;
    }



    private IEnumerator LerpHealthBar()
    {
        float timeElapsed = 0f;
        float startValue = healthBar.value;

        while (timeElapsed < lerpDuration)
        {
            healthBar.value = Mathf.Lerp(startValue, targetHealth, timeElapsed / lerpDuration);
            timeElapsed += Time.deltaTime;
            yield return null;
        }

        healthBar.value = targetHealth;  // Ensure it reaches the target value
    }





    private void Die()
    {
        // spawn effect
        Instantiate(deathEffect, gameObject.transform.position, Quaternion.identity);
        // Die/ragdoll
        Destroy(gameObject);
    }
}

