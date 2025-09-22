using GUZ.Core.Models.Vm;
using UnityEngine;
using UnityEngine.UI;

namespace GUZ.VR.Adapters.Player.Backpack
{
    public class VRBackpackCategory : MonoBehaviour
    {
        [SerializeField] private VmGothicEnums.InvCats _category;
        [SerializeField] private RawImage _image;
        [SerializeField] private VRBackpack _backpackComp;

        private void OnTriggerEnter(Collider other)
        {
            // There are also extended colliders with "Hand" layer, but we only want to have the Finger colliders.
            if (!other.gameObject.name.Contains("Finger"))
                return;
            
            Debug.Log("collision: " +  other.gameObject.name, other.gameObject);
            
            _backpackComp.OnCategoryClicked(_category);
            
            _image.color = Color.white;
        }
    }
}
