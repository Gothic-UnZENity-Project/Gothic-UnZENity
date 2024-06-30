using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GUZ.Core.Data;
using GUZ.Core.Extensions;
using GUZ.Core.Manager;
using GUZ.Core.Scripts.Manager;
using GUZ.Core.Util;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using UnityEngine.UI;
using ZenKit.Daedalus;

public class ControllerManager : SingletonBehaviour<ControllerManager>
{
    [FormerlySerializedAs("raycastLeft")] public GameObject RaycastLeft;
    [FormerlySerializedAs("raycastRight")] public GameObject RaycastRight;
    [FormerlySerializedAs("directLeft")] public GameObject DirectLeft;
    [FormerlySerializedAs("directRight")] public GameObject DirectRight;
    public GameObject MenuGameObject;

    [FormerlySerializedAs("dialogGameObject")]
    public GameObject DialogGameObject;

    [FormerlySerializedAs("dialogItems")] public List<GameObject> DialogItems;
    private InputAction _leftPrimaryButtonAction;
    private InputAction _leftSecondaryButtonAction;

    private InputAction _rightPrimaryButtonAction;
    private InputAction _rightSecondaryButtonAction;

    public GameObject MapObject;
    [FormerlySerializedAs("maprollspeed")] public float Maprollspeed;

    [FormerlySerializedAs("maprolloffset")]
    public float Maprolloffset;

    private Animator _maproll;
    private AudioSource _mapaudio;
    private AudioClip _scrollsound;

    protected override void Awake()
    {
        base.Awake();
        Initialize();
    }

    private void Initialize()
    {
        _maproll = MapObject.gameObject.GetComponent<Animator>();
        _mapaudio = MapObject.gameObject.GetComponent<AudioSource>();
        _scrollsound = VobHelper.GetSoundClip("SCROLLROLL.WAV");
        MapObject.SetActive(false);
        _maproll.enabled = false;

        _leftPrimaryButtonAction = new InputAction("primaryButton", binding: "<XRController>{LeftHand}/primaryButton");
        _leftSecondaryButtonAction =
            new InputAction("secondaryButton", binding: "<XRController>{LeftHand}/secondaryButton");

        _leftPrimaryButtonAction.started += ctx => ShowRayCasts();
        _leftPrimaryButtonAction.canceled += ctx => HideRayCasts();

        _leftPrimaryButtonAction.Enable();
        _leftSecondaryButtonAction.Enable();

        _rightPrimaryButtonAction =
            new InputAction("primaryButton", binding: "<XRController>{RightHand}/primaryButton");
        _rightSecondaryButtonAction =
            new InputAction("secondaryButton", binding: "<XRController>{RightHand}/secondaryButton");

        _rightPrimaryButtonAction.started += ctx => ShowMap();
        _rightSecondaryButtonAction.started += ctx => ShowMainMenu();

        _rightPrimaryButtonAction.Enable();
        _rightSecondaryButtonAction.Enable();
    }

    private void OnDestroy()
    {
        _leftPrimaryButtonAction?.Disable();
        _leftSecondaryButtonAction?.Disable();

        _rightPrimaryButtonAction?.Disable();
        _rightSecondaryButtonAction?.Disable();
    }

    public void ShowRayCasts()
    {
        RaycastLeft.SetActive(true);
        RaycastRight.SetActive(true);
        DirectLeft.SetActive(false);
        DirectRight.SetActive(false);
    }

    public void HideRayCasts()
    {
        RaycastLeft.SetActive(false);
        RaycastRight.SetActive(false);
        DirectLeft.SetActive(true);
        DirectRight.SetActive(true);
    }

    public void ShowMainMenu()
    {
        if (!MenuGameObject.activeSelf)
        {
            MenuGameObject.SetActive(true);
        }
        else
        {
            MenuGameObject.SetActive(false);
        }
    }

    public void ShowMap()
    {
        if (!MapObject.activeSelf)
        {
            StartCoroutine(UnrollMap());
        }
        else
        {
            StartCoroutine(RollupMap());
        }
    }

    public IEnumerator UnrollMap()
    {
        MapObject.SetActive(true);
        _maproll.enabled = true;
        _maproll.speed = Maprollspeed;
        _maproll.Play("Unroll", -1, 0.0f);
        _mapaudio.PlayOneShot(_scrollsound);
        yield return new WaitForSeconds(_maproll.GetCurrentAnimatorClipInfo(0)[0].clip.length / Maprollspeed *
                                        (_maproll.GetCurrentAnimatorClipInfo(0)[0].clip.length - Maprolloffset) /
                                        _maproll.GetCurrentAnimatorClipInfo(0)[0].clip.length);
        _maproll.speed = 0f;
    }

    public IEnumerator RollupMap()
    {
        _maproll.speed = Maprollspeed;
        _maproll.Play("Roll", -1,
            1 - (_maproll.GetCurrentAnimatorClipInfo(0)[0].clip.length - Maprolloffset) /
            _maproll.GetCurrentAnimatorClipInfo(0)[0].clip.length);
        _mapaudio.PlayOneShot(_scrollsound);
        yield return new WaitForSeconds(_maproll.GetCurrentAnimatorClipInfo(0)[0].clip.length / Maprollspeed *
                                        (_maproll.GetCurrentAnimatorClipInfo(0)[0].clip.length - Maprolloffset) /
                                        _maproll.GetCurrentAnimatorClipInfo(0)[0].clip.length);
        _maproll.speed = 0f;
        MapObject.SetActive(false);
    }

    public void ShowDialog()
    {
        DialogGameObject.SetActive(true);
    }

    public void HideDialog()
    {
        DialogGameObject.SetActive(false);
    }

    public void FillDialog(int npcInstanceIndex, List<DialogOption> dialogOptions)
    {
        CreateAdditionalDialogOptions(dialogOptions.Count);
        ClearDialogOptions();

        // G1 handles DialogOptions added via (Info_AddChoice()) in reverse order.
        dialogOptions.Reverse();
        for (var i = 0; i < dialogOptions.Count; i++)
        {
            var dialogItem = DialogItems[i];
            var dialogOption = dialogOptions[i];

            dialogItem.GetComponent<Button>().onClick.AddListener(
                () => OnDialogClick(npcInstanceIndex, dialogOption.Function, false));
            dialogItem.FindChildRecursively("Label").GetComponent<TMP_Text>().text = dialogOption.Text;
        }
    }

    public void FillDialog(int npcInstanceIndex, List<InfoInstance> dialogOptions)
    {
        CreateAdditionalDialogOptions(dialogOptions.Count);
        ClearDialogOptions();

        for (var i = 0; i < dialogOptions.Count; i++)
        {
            var dialogItem = DialogItems[i];
            var dialogOption = dialogOptions[i];

            dialogItem.GetComponent<Button>().onClick.AddListener(
                () => OnDialogClick(npcInstanceIndex, dialogOption.Information, true));
            dialogItem.FindChildRecursively("Label").GetComponent<TMP_Text>().text = dialogOption.Description;
        }
    }

    /// <summary>
    /// We won't know the maximum amount of element from the start of the game.
    /// Therefore we start with one entry only and create more if needed now.
    /// </summary>
    private void CreateAdditionalDialogOptions(int currentItemsNeeded)
    {
        var newItemsToCreate = currentItemsNeeded - DialogItems.Count;

        if (newItemsToCreate <= 0)
        {
            return;
        }

        var lastItem = DialogItems.Last();
        for (var i = 0; i < newItemsToCreate; i++)
        {
            var newItem = Instantiate(lastItem, lastItem.transform.parent, false);
            DialogItems.Add(newItem);

            newItem.name = $"Item{DialogItems.Count - 1:00}";
            // FIXME - We need to handle this kind of UI magic more transparent somewhere else...
            newItem.transform.localPosition += new Vector3(0, -50 * (DialogItems.Count - 1), 0);
        }
    }

    private void ClearDialogOptions()
    {
        foreach (var item in DialogItems)
        {
            item.GetComponent<Button>().onClick.RemoveAllListeners();
            item.FindChildRecursively("Label").GetComponent<TMP_Text>().text = "";
        }
    }

    private void OnDialogClick(int npcInstanceIndex, int dialogId, bool isMainDialog)
    {
        DialogHelper.SelectionClicked(npcInstanceIndex, dialogId, isMainDialog);
    }
}
