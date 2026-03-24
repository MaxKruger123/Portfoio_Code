using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class KeybindsManager : MonoBehaviour
{
    [Header("References")]
    public Dashing dashing;
    public Graplpling grappling;
    public Swinging swinging;
    public Sliding sliding;

    private bool isListening = false;
    private string listeningFor = "";

    public TextMeshProUGUI bindDashButtonText;
    public TextMeshProUGUI bindGrappleButtonText;
    public TextMeshProUGUI bindSwingButtonText;
    public TextMeshProUGUI bindSlideButtonText;

    public void StartListeningForDashKey()
    {
        isListening = true;
        listeningFor = "Dash";
        bindDashButtonText.text = "Press any key...";
    }

    public void StartListeningForGrappleKey()
    {
        isListening = true;
        listeningFor = "Grapple";
        bindGrappleButtonText.text = "Press any key...";
    }

    public void StartListeningForSwingKey()
    {
        isListening = true;
        listeningFor = "Swing";
        bindSwingButtonText.text = "Press any key...";
    }

    public void StartListeningForSlideKey()
    {
        isListening = true;
        listeningFor = "Slide";
        bindSlideButtonText.text = "Press any key...";
    }

    void OnGUI()
    {
        if (!isListening) return;

        Event e = Event.current;
        if (e.isKey || e.isMouse)
        {
            KeyCode key = e.keyCode;

            // If a mouse button was clicked
            if (e.isMouse)
            {
                switch (e.button)
                {
                    case 0: key = KeyCode.Mouse0; break;
                    case 1: key = KeyCode.Mouse1; break;
                    case 2: key = KeyCode.Mouse2; break;
                    case 3: key = KeyCode.Mouse3; break;
                    case 4: key = KeyCode.Mouse4; break;
                    case 5: key = KeyCode.Mouse5; break;
                        // Add more if needed
                }
            }

            if (listeningFor == "Dash")
            {
                dashing.dashKey = key;
                bindDashButtonText.text = dashing.dashKey.ToString();
            }

            if (listeningFor == "Grapple")
            {
                grappling.grappleKey = key;
                bindGrappleButtonText.text = grappling.grappleKey.ToString();
            }

            if (listeningFor == "Swing")
            {
                swinging.swingKey = key;
                bindSwingButtonText.text = grappling.grappleKey.ToString();
            }

            if (listeningFor == "Slide")
            {
                sliding.slideKey = key;
                bindSlideButtonText.text = grappling.grappleKey.ToString();
            }

            isListening = false;
            listeningFor = "";
        }
    }


}
