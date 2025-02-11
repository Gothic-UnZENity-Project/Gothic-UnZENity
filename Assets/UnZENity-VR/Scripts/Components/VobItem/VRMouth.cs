using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GUZ.Core;
using GUZ.Core.Creator.Sounds;
using GUZ.Core.Extensions;
using GUZ.Core.Properties;
using GUZ.Core.Vm;
using GUZ.Core.Vob;
using JetBrains.Annotations;
using UnityEngine;
using ZenKit;
using ZenKit.Daedalus;

namespace GUZ.VR.Components.VobItem
{
    [RequireComponent(typeof(AudioSource))]
    public class VRMouth : MonoBehaviour
    {
        // e.g. t_Potion_S0_2_Stand
        private const string _animationSchemeWithSfx = "t_{0}_S0_2_Stand";

        [SerializeField]
        private AudioSource _mouthAudio;

        // Do not eat them twice during destroy time.
        private List<GameObject> _objectsInDestroyGracePeriod = new();


        private void OnTriggerEnter(Collider other)
        {
            var go = other.gameObject;

            if (_objectsInDestroyGracePeriod.Contains(go))
            {
                return;
            }

            if (!TryGetItemToEat(go, out var item))
            {
                return;
            }

            Debug.Log($"Eating item: {go.name}");

            // Defines after which time period the object will be destroyed in hand.
            var destroyTime = 1f;
            if (TryExtractSfx(item, out var clip))
            {
                destroyTime = clip.length;
            }
            else
            {
                Debug.LogWarning("No SFX for eating/drinking item found. Removing item anyways after 1 second.");
            }

            // Can be set now already. It's sufficient to use this children instead of root.
            _objectsInDestroyGracePeriod.Add(go);

            GameObject rootGo = go;
            var vobLoaderComp = go.GetComponentInParent<VobLoader>();

            // LAB - Fallback as objects aren't LazyLoaded in here.
            if (vobLoaderComp != null)
            {
                rootGo = vobLoaderComp.gameObject;
            }

            StartCoroutine(ConsumeObject(rootGo, clip, destroyTime));
        }

        private bool TryGetItemToEat(GameObject go, out ItemInstance item)
        {
            item = null;

            if (!go.TryGetComponent<VobItemProperties>(out var vobComp))
            {
                return false;
            }

            item = vobComp.Item;
            if (item == null || item.Type != DaedalusInstanceType.Item)
            {
                return false;
            }

            var mainFlag = (VmGothicEnums.ItemFlags)item.MainFlag;
            if (mainFlag != VmGothicEnums.ItemFlags.ItemKatFood && mainFlag != VmGothicEnums.ItemFlags.ItemKatPotions)
            {
                return false;
            }

            return true;
        }

        private bool TryExtractSfx(ItemInstance item, out AudioClip clip)
        {
            clip = null;

            var mds = ResourceLoader.TryGetModelScript("Humans")!;
            var animationName = string.Format(_animationSchemeWithSfx, item.SchemeName);
            var anim = mds.Animations.FirstOrDefault(i => i.Name.EqualsIgnoreCase(animationName));
            if (anim == null)
            {
                return false;
            }

            var sfx = anim.SoundEffects.FirstOrDefault();
            if (sfx == null)
            {
                return false;
            }

            var sfxInstance = VmInstanceManager.TryGetSfxData(sfx.Name);
            if (sfxInstance == null)
            {
                return false;
            }

            var fileName = sfxInstance.File;
            clip = SoundCreator.ToAudioClip(fileName);
            if (clip == null)
            {
                return false;
            }

            return true;
        }

        // FIXME- Handle also inventory state in the future. Currently only mesh is gone, but not object from save game and inventory.
        private IEnumerator ConsumeObject(GameObject go, [CanBeNull] AudioClip clip, float destroyDelay)
        {
            if (clip != null)
            {
                _mouthAudio.PlayOneShot(clip);
            }

            yield return new WaitForSeconds(destroyDelay);

            _objectsInDestroyGracePeriod.Remove(go);
            Destroy(go);
        }
    }
}
