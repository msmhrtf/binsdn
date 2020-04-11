using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SliderControl : MonoBehaviour
{
    private Slider slider;
    private float joystickVal;

    void Start()
    {
        slider = this.gameObject.GetComponent<Slider>();
    }

    void Update()
    {

        joystickVal = -OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick).x;

        if (joystickVal != 0)
        {
            slider.value += joystickVal/100;
        }

    }

    public float getSliderVal()
    {
        return slider.value;
    }

}
