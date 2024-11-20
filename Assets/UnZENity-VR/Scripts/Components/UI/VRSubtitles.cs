#if GUZ_HVR_INSTALLED
using System.Collections;
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

            // The whole subtitle topic will be enabled later during gameplay.
            gameObject.SetActive(false);
        }

        public void StartDialogInitially()
        {
            gameObject.SetActive(true); // If we enable it earlier, Billboard Comp on this GO is calculating all the time.
        }

        public void EndDialog()
        {
            HideSubtitlesImmediate();
            gameObject.SetActive(false); // Disable whole Subtitle menu (UI, Billboard)
        }

        public void ShowSubtitles(GameObject npcGo)
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

            StartCoroutine(ShowSubtitlesWithDelay());
        }

        private IEnumerator ShowSubtitlesWithDelay()
        {
            yield return new WaitForEndOfFrame();
            _dialogRoot.SetActive(true);
        }

        private IEnumerator HideSubtitlesWithDelay()
        {
            yield return new WaitForSeconds(_hideDialogDelay);
            _dialogRoot.SetActive(false);
            _dialogRoot.SetParent(SceneManager.GetSceneByName(Constants.ScenePlayer).GetRootGameObjects()[0], worldPositionStays: true);
        }

        public void HideSubtitles()
        {
            _hideDialogCoroutine = StartCoroutine(HideSubtitlesWithDelay());
        }

        public void HideSubtitlesImmediate()
        {
            _dialogRoot.SetActive(false);
            _dialogRoot.SetParent(SceneManager.GetSceneByName(Constants.ScenePlayer).GetRootGameObjects()[0], worldPositionStays: true);
        }

        public void FillSubtitles(string npcName, string text)
        {
            ClearSubtitlesOptions();

            _dialogNpcNameItem.FindChildRecursively("Label").GetComponent<TMP_Text>().text = npcName;
            _dialogNpcNameItem.FindChildRecursively("Label").GetComponent<TMP_Text>().spriteAsset = GameGlobals.Font.HighlightSpriteAsset;

            _dialogItem.FindChildRecursively("Label").GetComponent<TMP_Text>().text = text;
        }

        private void ClearSubtitlesOptions()
        {
            _dialogItem.FindChildRecursively("Label").GetComponent<TMP_Text>().text = "";
            _dialogNpcNameItem.FindChildRecursively("Label").GetComponent<TMP_Text>().text = "";
        }
    }
}
#endif
