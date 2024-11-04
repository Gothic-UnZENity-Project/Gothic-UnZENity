using UnityEngine;

namespace GUZ.Core.UI
{
    public class QuestLogMenu : MonoBehaviour
    {
        [SerializeField] private GameObject _background;

        private void Start()
        {
            SetMaterials();
        }

        private void SetMaterials()
        {
            _background.GetComponent<MeshRenderer>().material = GameGlobals.Textures.QuestLogMenuMaterial;
        }

        public void ToggleVisibility()
        {
            // Toggle visibility
            gameObject.SetActive(!gameObject.activeSelf);

            if (gameObject.activeSelf)
            {
                // TBD
            }
        }
    }
}
