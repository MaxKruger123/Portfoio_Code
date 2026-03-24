using UnityEngine;
using UnityEngine.UI;

public class SettingsScript : MonoBehaviour
{
    [SerializeField] private Slider sensitivitySlider1;
    [SerializeField] private Slider sensitivitySlider2;
    [SerializeField] private float sensitivityX;
    [SerializeField] private float sensitivityY;
    [SerializeField] private PlayerCam playerCam;

    private void Start()
    {
        
        
        sensitivitySlider1.value = sensitivityX;
        sensitivitySlider2.value = sensitivityY;
        playerCam.SetSensitivityX(sensitivityX);
        playerCam.SetSensitivityY(sensitivityY);

        // Add listener
        sensitivitySlider1.onValueChanged.AddListener(OnSensitivityChanged1);
        sensitivitySlider2.onValueChanged.AddListener(OnSensitivityChanged2);
    }

    private void OnSensitivityChanged1(float value)
    {
        playerCam.SetSensitivityX(value);
        
    }

    private void OnSensitivityChanged2(float value)
    {
        playerCam.SetSensitivityY(value);
        
    }

}
