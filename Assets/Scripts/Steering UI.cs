using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using TMPro;

public class GigboatUI : MonoBehaviour
{
    [SerializeField] private GigboatMovement gigboat; 

    [SerializeField] private Slider steeringSlider;
    [SerializeField] private Slider ThrottleSlider;
    //[SerializeField] private float maxRotation = 90f;           // this will be for settiong the steering wheel angle when that is used instead of a slider
    [SerializeField] TextMeshProUGUI speed;



    public void SetBoat(GigboatMovement newBoat)
    {
        gigboat = newBoat;
    }


    void Start()
    {
        if(steeringSlider == null)
        {
            steeringSlider = GetComponent<Slider>();
        }

        steeringSlider.minValue = -1f;
        steeringSlider.maxValue = 1f;

        if (ThrottleSlider == null)
        {
            ThrottleSlider = GetComponent<Slider>();
        }

        ThrottleSlider.minValue = -100f;
        ThrottleSlider.maxValue = 100f;

       
    }

    // Update is called once per frame
    void Update()
    {
        if (gigboat == null) return;
        Steering();
        Throttle();
    }

    private void Steering()
    {
        if (gigboat == null) return;
        float rudderPosition = gigboat.RudderAngle;
        steeringSlider.value = rudderPosition;
    }

    private void Throttle()
    {
        ThrottleSlider.value = gigboat.ThrottlePercent;
       
        speed.text = $"Speed {gigboat.SpeedKnots.ToString("F1")} Kn";
    }
}
