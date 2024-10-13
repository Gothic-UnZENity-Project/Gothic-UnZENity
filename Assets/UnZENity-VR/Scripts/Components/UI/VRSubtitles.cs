#if GUZ_HVR_INSTALLED
using GUZ.Core;
using GUZ.Core.Extensions;
using GUZ.Core.Globals;
using MyBox;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GUZ.VR.Components.UI
{
    public class VRSubtitles : MonoBehaviour
    {
        [SerializeField] private GameObject _dialogRoot;
        [SerializeField] private GameObject _dialogNpcNameItem;
        [SerializeField] private GameObject _dialogItem;

        private float _dialogItemHeight;
        private float _dialogNpcNameItemHeight;

        private Coroutine _hideDialogCoroutine;
        private float _hideDialogDelay = 0.1f;

        private void Awake()
        {
            // When prefab is loaded for the first time, we store the size of a dialog item.
            // It's needed to add more elements on top in the right position.
            if (_dialogItemHeight == 0f)
            {
                var rectTransform = _dialogItem.GetComponent<RectTransform>();
                _dialogItemHeight = rectTransform.rect.height;
            }
        }

        public void ShowDialog(GameObject npcGo)
        {
            // If there's a pending hide operation, stop it
        if (_hideDialogCoroutine != null)
        {
            StopCoroutine(_hideDialogCoroutine);
            _hideDialogCoroutine = null;
        }
            var npcDialog = npcGo.FindChildRecursively("DialogMenuRootPos");
            _dialogRoot.SetParent(npcDialog, true, true, true);

            var rootRectHeight = _dialogItemHeight + _dialogNpcNameItemHeight;
            _dialogRoot.GetComponent<RectTransform>().SetHeight(rootRectHeight);

            StartCoroutine(ShowDialogWithDelay());
        }

        private System.Collections.IEnumerator ShowDialogWithDelay()
        {
            yield return new WaitForEndOfFrame();
            _dialogRoot.SetActive(true);
        }

        private System.Collections.IEnumerator HideDialogWithDelay()
        {
            yield return new WaitForSeconds(_hideDialogDelay);
            _dialogRoot.SetActive(false);
            _dialogRoot.SetParent(SceneManager.GetSceneByName(Constants.ScenePlayer).GetRootGameObjects()[0], worldPositionStays: true);
        }

        /// <summary>
        /// Once we close the dialog, we need to move the dialog box back to the General scene
        /// (or something without any object which might be destroyed (like an NPC after dying)).
        /// </summary>
        public void HideDialog()
        {
            _hideDialogCoroutine = StartCoroutine(HideDialogWithDelay());
        }

        public void HideDialogImmediate()
        {
            _dialogRoot.SetActive(false);
            _dialogRoot.SetParent(SceneManager.GetSceneByName(Constants.ScenePlayer).GetRootGameObjects()[0], worldPositionStays: true);
        }

        public void FillDialog(string npcName, string text)
        {
            ClearDialogOptions();

            _dialogNpcNameItem.FindChildRecursively("Label").GetComponent<TMP_Text>().text = npcName;
            _dialogNpcNameItem.FindChildRecursively("Label").GetComponent<TMP_Text>().spriteAsset = GameGlobals.Font.HighlightSpriteAsset;

            _dialogItem.FindChildRecursively("Label").GetComponent<TMP_Text>().text = text;
        }

        private void ClearDialogOptions()
        {
            _dialogItem.FindChildRecursively("Label").GetComponent<TMP_Text>().text = "";
            _dialogNpcNameItem.FindChildRecursively("Label").GetComponent<TMP_Text>().text = "";
        }
    }
}
#endif
