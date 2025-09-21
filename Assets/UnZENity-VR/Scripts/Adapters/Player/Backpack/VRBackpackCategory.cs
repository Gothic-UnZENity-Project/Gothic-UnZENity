using GUZ.Core.Models.Vm;
using UnityEngine;
using UnityEngine.UI;

namespace GUZ.VR.Adapters.Player.Backpack
{
    public class VRBackpackCategory : MonoBehaviour
    {
        [SerializeField] private VmGothicEnums.ItemFlags _category;
        [SerializeField] private RawImage _image;
        [SerializeField] private VRBackpack _backpackComp;

        private void OnTriggerEnter(Collider other)
        {
            _backpackComp.OnCategoryClicked(_category);
            
            _image.color = Color.white;
        }
    }
}
