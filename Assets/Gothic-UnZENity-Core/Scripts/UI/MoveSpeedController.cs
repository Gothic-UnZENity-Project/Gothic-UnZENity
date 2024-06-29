using GUZ.Core.Globals;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;

public class MoveSpeedController : MonoBehaviour
{
    [FormerlySerializedAs("movecontroller")]
    public ActionBasedContinuousMoveProvider Movecontroller;

    private void Start()
    {
        var speedslider = transform.GetComponent<Slider>();
        speedslider.onValueChanged.AddListener(ChangeMoveSpeed);
        speedslider.value = PlayerPrefs.GetFloat(Constants.MoveSpeedPlayerPref, Constants.MoveSpeed);
    }

    public void ChangeMoveSpeed(float moveSpeed)
    {
        PlayerPrefs.SetFloat(Constants.MoveSpeedPlayerPref, moveSpeed);
        Constants.MoveSpeed = moveSpeed;

        if (!Movecontroller)
        {
            return;
        }

        Movecontroller.moveSpeed = moveSpeed;
    }
}
