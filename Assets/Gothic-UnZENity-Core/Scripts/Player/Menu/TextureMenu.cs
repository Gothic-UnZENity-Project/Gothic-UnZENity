using GUZ.Core.Manager;
using UnityEngine;

namespace GUZ.Core.Player.Menu
{
    public class TextureMenu : MonoBehaviour
    {
        [SerializeField] private GameObject MainMenuImageBackground;
        [SerializeField] private GameObject MainMenuBackground;
        [SerializeField] private GameObject MainMenuText;
        private void Start()
        {
            SetMaterials();
        }

        public void SetMaterials()
        {
            var mmib = MainMenuImageBackground.GetComponent<MeshRenderer>();
            var mmb = MainMenuBackground.GetComponent<MeshRenderer>();
            var mmt = MainMenuText.GetComponent<MeshRenderer>();

            var tm = GameGlobals.Textures;
            mmib.material = tm.mainMenuImageBackgroundMaterial;
            mmb.material = tm.mainMenuBackgroundMaterial;
            mmt.material = tm.mainMenuTextImageMaterial;
        }
    }

}
