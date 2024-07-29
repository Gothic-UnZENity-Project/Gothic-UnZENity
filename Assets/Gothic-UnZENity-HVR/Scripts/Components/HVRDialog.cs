using System.Collections.Generic;
using System.Linq;
using GUZ.Core.Data;
using GUZ.Core.Extensions;
using GUZ.Core.Manager;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ZenKit.Daedalus;

namespace GUZ.HVR.Components
{
    public class HVRDialog : MonoBehaviour
    {
        [SerializeField] private GameObject _dialogGameObject;
        [SerializeField] private List<GameObject> _dialogItems;

        private float _dialogItemHeight;
        private float _lowestDialogItemYPos;
        
        
        private void Awake()
        {
            // When prefab is loaded for the first time, we store the size of a dialog item.
            // It's needed to add more elements on top in the right position.
            if (_dialogItemHeight == 0f)
            {
                var rectTransform = _dialogItems.First().GetComponent<RectTransform>();
                _dialogItemHeight = rectTransform.rect.height;
                _lowestDialogItemYPos = rectTransform.localPosition.y;
            }
        }
        
        public void ShowDialog(GameObject npcGo)
        {
            // We don't move the UI to the NPC, but copy the location values only.
            // It's intended, so that we don't loose the UI if an NPC gets destroyed or so...
            var npcDialogTransform = npcGo.FindChildRecursively("DialogMenuRootPos").transform;
            _dialogGameObject.transform.position = npcDialogTransform.position;
            _dialogGameObject.SetActive(true);
        }

        public void HideDialog()
        {
            _dialogGameObject.SetActive(false);
        }
        
        public void FillDialog(int npcInstanceIndex, List<DialogOption> dialogOptions)
        {
            CreateAdditionalDialogOptions(dialogOptions.Count);
            ClearDialogOptions();

            // G1 handles DialogOptions added via (Info_AddChoice()) in reverse order.
            dialogOptions.Reverse();
            for (var i = 0; i < dialogOptions.Count; i++)
            {
                var dialogItem = _dialogItems[i];
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
            
            dialogOptions.Reverse();
            for (var i = 0; i < dialogOptions.Count; i++)
            {
                var dialogItem = _dialogItems[i];
                var dialogOption = dialogOptions[i];

                dialogItem.GetComponent<Button>().onClick.AddListener(
                    () => OnDialogClick(npcInstanceIndex, dialogOption.Information, true));
                dialogItem.FindChildRecursively("Label").GetComponent<TMP_Text>().text = dialogOption.Description;
            }
        }
        
        /// <summary>
        /// We won't know the maximum amount of element from the start of the game.
        /// But we don't want to show pagination or so but instead want to show them all in one spot.
        /// Therefore, we start with one entry as template and create more if needed now.
        /// </summary>
        private void CreateAdditionalDialogOptions(int currentItemsNeeded)
        {
            var newItemsToCreate = currentItemsNeeded - _dialogItems.Count;

            if (newItemsToCreate <= 0)
            {
                return;
            }

            var lastItem = _dialogItems.Last();
            for (var i = 0; i < newItemsToCreate; i++)
            {
                var newItem = Instantiate(lastItem, lastItem.transform.parent, false);
                _dialogItems.Add(newItem);

                newItem.name = $"Item{_dialogItems.Count - 1:00}";
                var newYPos = _lowestDialogItemYPos + _dialogItemHeight * (_dialogItems.Count - 1);
                newItem.transform.localPosition = new Vector3(0, newYPos, 0);
            }
        }

        private void ClearDialogOptions()
        {
            foreach (var item in _dialogItems)
            {
                item.GetComponent<Button>().onClick.RemoveAllListeners();
                item.FindChildRecursively("Label").GetComponent<TMP_Text>().text = "";
            }
        }

        private void OnDialogClick(int npcInstanceIndex, int dialogId, bool isMainDialog)
        {
            DialogManager.SelectionClicked(npcInstanceIndex, dialogId, isMainDialog);
        }
    }
}
