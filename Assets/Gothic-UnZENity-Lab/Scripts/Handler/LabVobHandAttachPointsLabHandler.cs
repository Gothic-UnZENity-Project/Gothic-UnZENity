using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GUZ.Core.Caches;
using GUZ.Core.Context;
using GUZ.Core.Creator.Meshes.V2;
using GUZ.Core.Data;
using GUZ.Core.Globals;
using GUZ.Core.Vm;
using GUZ.Core.Vob;
using GUZ.Core.Creator;
using GUZ.Core.Extensions;
using GUZ.Core;
using GUZ.XRIT.Components.Vobs;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using ZenKit.Daedalus;

namespace GUZ.Lab.Handler
{
    public class LabVobHandAttachPointsLabHandler: MonoBehaviour, ILabHandler
    {
        public TMP_Dropdown vobCategoryDropdown;
        public TMP_Dropdown vobItemDropdown;
        public GameObject itemSpawnSlot;

        private string currentItemName;

        private Dictionary<string, ItemInstance> items = new();

        public void Bootstrap()
        {
            /*
             * 1. Load Vdfs
             * 2. Load VobItemAttachPoints json
             * 3. Load Vob name list
             * 4. Fill dropdown
             */
            var itemNames = GameData.GothicVm.GetInstanceSymbols("C_Item").Select(i => i.Name).ToList();

            items = itemNames
                .ToDictionary(itemName => itemName, VmInstanceManager.TryGetItemData);

            vobCategoryDropdown.options = items
                .Select(item => ((VmGothicEnums.ItemFlags)item.Value.MainFlag).ToString())
                .Distinct()
                .Select(flag => new TMP_Dropdown.OptionData(flag))
                .ToList();

            CategoryDropdownValueChanged();
        }

        public void CategoryDropdownValueChanged()
        {
            Enum.TryParse<VmGothicEnums.ItemFlags>(vobCategoryDropdown.options[vobCategoryDropdown.value].text, out var category);
            var items = this.items.Where(item => item.Value.MainFlag == (int)category).ToList();
            vobItemDropdown.options = items.Select(item => new TMP_Dropdown.OptionData(item.Key)).ToList();
        }

        public void LoadVobOnClick()
        {
            // We want to have one element only.
            if (itemSpawnSlot.transform.childCount != 0)
                Destroy(itemSpawnSlot.transform.GetChild(0).gameObject);

            StartCoroutine(LoadVobOnClickDelayed());
        }

        private IEnumerator LoadVobOnClickDelayed()
        {
            // Wait 1 frame for GOs to be destroyed.
            yield return null;

            currentItemName = vobItemDropdown.options[vobItemDropdown.value].text;
            var item = CreateItem(currentItemName);
        }

        private GameObject CreateItem(string itemName)
        {
            var itemPrefab = ResourceLoader.TryGetPrefabObject(PrefabType.VobItem);
            var item = VmInstanceManager.TryGetItemData(itemName);
            var mrm = ResourceLoader.TryGetMultiResolutionMesh(item.Visual);
            var itemGo = MeshFactory.CreateVob(item.Visual, mrm, default, default, true,
                rootGo: itemPrefab, parent: itemSpawnSlot, useTextureArray: false);

            GUZContext.InteractionAdapter.AddItemComponent(itemGo, true);

            return gameObject;
        }
    }
}
