using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class HandPresence : MonoBehaviour
{

    public InputDeviceCharacteristics controllerCharacteristics;
    public GameObject handModelPrefab;

    private InputDevice targetDevice;
    private GameObject spawnedHandModel;
    private Animator handAnimator;


    // Start is called before the first frame update
    void Start()
    {
        InvokeRepeating("InstanciateControllerDevice", 1f, 10f);
    }


    void Update()
    {
        if(spawnedHandModel) {
            UpdateHandAnimation();
        }
    }


    void InstanciateControllerDevice()
    {
        // If we already instanciated a valid Controller pair, we can cancel the following checks.
        if (targetDevice.isValid) {
            CancelInvoke("GetDevices");
            return;
        }

        List<InputDevice> devices = new List<InputDevice>();
        InputDevices.GetDevicesWithCharacteristics(controllerCharacteristics, devices);

        //foreach(var item in devices)
        //{
        //    Debug.Log(item.name + item.characteristics);
        //}

        if (devices.Count > 0)
        {
            spawnedHandModel = Instantiate(handModelPrefab, transform);
            handAnimator = spawnedHandModel.GetComponent<Animator>();
        }
    }


    void UpdateHandAnimation()
    {

        //targetDevice.TryGetFeatureValue(CommonUsages.primaryButton, out bool primarybuttonvalue);
        //if (primarybuttonvalue == true)
        //{
        //    spawnedHandModel.SetActive(false);
        //}

        //targetDevice.TryGetFeatureValue(CommonUsages.secondaryButton, out bool secondarybuttonvalue);
        //if (secondarybuttonvalue == true)
        //{
        //    spawnedHandModel.SetActive(true);
        //}

        if (targetDevice.TryGetFeatureValue(CommonUsages.trigger, out float triggerValue))
        {
            handAnimator.SetFloat("Trigger", triggerValue);
        }
        else
        {
            handAnimator.SetFloat("Trigger", 0);
        }

        if (targetDevice.TryGetFeatureValue(CommonUsages.grip, out float gripValue))
        {
            handAnimator.SetFloat("Grip", gripValue);
        }
        else
        {
            handAnimator.SetFloat("Grip", 0);
        }
    }
}
