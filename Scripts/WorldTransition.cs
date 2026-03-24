using UnityEngine;

public class WorldTransition : MonoBehaviour
{
    public Transform sphere; // The sphere object


    public float expansionSpeed = 5f; // Speed of the sphere expansion
    public float maxRadius = 50f; // Maximum size of the sphere
    public bool isExpanding = true; // Determines whether the sphere is expanding or shrinking
    public bool TurnOn = true; // Determines whether the sphere is expanding or shrinking
    public Vector3 ZeroOut;
    void Update()
    {
        // Check for input to toggle direction
        if (Input.GetKeyDown(KeyCode.F))
        {
            isExpanding = !isExpanding; // Toggle the direction
            if (isExpanding == true)
            {
                TurnOn = true;
            }
        }

        // Adjust the sphere's size based on the current state
        float currentRadius = sphere.localScale.x / 2f; // Current sphere radius

        if (TurnOn == true)
        {
            if (isExpanding && currentRadius < maxRadius)
            {
                Debug.Log("EXPANDING");
                // Expand the sphere
                sphere.localScale += Vector3.one * expansionSpeed * Time.deltaTime;
            }
            else if (!isExpanding)
            {
                Debug.Log("ISNT EXPANDING");
                // Clamp to avoid negative scale
                if (sphere.localScale.x <= ZeroOut.x)
                {
                    Debug.Log("ZERO");
                    sphere.localScale = ZeroOut;
                    TurnOn = true;
                }
                else
                {
                    sphere.localScale -= Vector3.one * expansionSpeed * Time.deltaTime;
                }




            }
        }

        // Update the sphere properties in both materials
        Vector3 spherePosition = sphere.position;
        float sphereRadius = sphere.localScale.x / 2f;

        Debug.Log(sphere.localScale.x);



        Shader.SetGlobalVector("_SpherePosition", spherePosition);
        Shader.SetGlobalFloat("_SphereRadius", sphereRadius);

    }
}