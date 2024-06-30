using GUZ.Core.Globals;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.XR.Interaction.Toolkit;

public class TurnSettingDropdownController : MonoBehaviour
{
    [FormerlySerializedAs("locomotionsystem")]
    public GameObject Locomotionsystem;

    [FormerlySerializedAs("snapTurn")] public ActionBasedSnapTurnProvider SnapTurn;

    [FormerlySerializedAs("continuousTurn")]
    public ActionBasedContinuousTurnProvider ContinuousTurn;

    private void Awake()
    {
        var dropdown = transform.GetComponent<TMP_Dropdown>();
        dropdown.onValueChanged.AddListener(DropdownItemSelected);

        dropdown.value = PlayerPrefs.GetInt(Constants.TurnSettingPlayerPref);
        Debug.Log(PlayerPrefs.GetInt(Constants.TurnSettingPlayerPref));
        DropdownItemSelected(dropdown.value);

        // FIXME - If we're on Loading scene, there is no locomotionSystem. We should switch it to something like "isLoadingState".
        if (Locomotionsystem == null)
        {
            return;
        }

        SnapTurn = Locomotionsystem.GetComponent<ActionBasedSnapTurnProvider>();
        ContinuousTurn = Locomotionsystem.GetComponent<ActionBasedContinuousTurnProvider>();
    }

    public void DropdownItemSelected(int value)
    {
        switch (value)
        {
            case 1:
                EnableContinuousTurn();
                break;
            case 0:
            default:
                EnableSnapTurn();
                break;
        }
    }

    private void EnableSnapTurn()
    {
        PlayerPrefs.SetInt(Constants.TurnSettingPlayerPref, 0);

        if (!Locomotionsystem)
        {
            return;
        }

        SnapTurn.enabled = true;
        ContinuousTurn.enabled = false;
    }

    private void EnableContinuousTurn()
    {
        PlayerPrefs.SetInt(Constants.TurnSettingPlayerPref, 1);

        if (!Locomotionsystem)
        {
            return;
        }

        SnapTurn.enabled = false;
        ContinuousTurn.enabled = true;
    }
}
