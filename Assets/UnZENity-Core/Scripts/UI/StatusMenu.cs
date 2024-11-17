﻿using System;
using System.Linq;
using GUZ.Core.Globals;
using GUZ.Core.UnZENity_Core.Scripts.UI;
using MyBox;
using TMPro;

namespace GUZ.Core.UI
{
    public class StatusMenu : AbstractMenu
    {
        private string _itemNameGuild = "MENU_ITEM_PLAYERGUILD";
        private string _itemNameLevel = "MENU_ITEM_LEVEL";
        private string _itemNameExp = "MENU_ITEM_EXP";
        private string _itemNameLevelNext = "MENU_ITEM_LEVEL_NEXT";
        private string _itemNameLearn = "MENU_ITEM_LEARN";

        private string _itemNameAttributePattern = "MENU_ITEM_ATTRIBUTE_{0}";

        private string _itemNameArmorPattern = "MENU_ITEM_ARMOR_{0}";

        private string _itemTalentTitlePattern = "MENU_ITEM_TALENT_{0}_TITLE";
        private string _itemTalentSkillPattern = "MENU_ITEM_TALENT_{0}_SKILL";
        private string _itemTalentDescriptionPattern = "MENU_ITEM_TALENT_{0}";


        private void Awake()
        {
            GlobalEventDispatcher.ZenKitBootstrapped.AddListener(Setup);
        }

        private void Setup()
        {
            CreateRootElements("MENU_STATUS");
        }

        /// <summary>
        /// Fill data. Currently demo data.
        /// </summary>
        public void SetActive()
        {
            MenuItemsCache[_itemNameGuild].go.GetComponentInChildren<TMP_Text>().text = "TestGuild";
            MenuItemsCache[_itemNameLevel].go.GetComponentInChildren<TMP_Text>().text = "100";
            MenuItemsCache[_itemNameExp].go.GetComponentInChildren<TMP_Text>().text = "1337";
            MenuItemsCache[_itemNameLevelNext].go.GetComponentInChildren<TMP_Text>().text = "13";
            MenuItemsCache[_itemNameLearn].go.GetComponentInChildren<TMP_Text>().text = "42";

            Enumerable.Range(1, 4).ForEach(i =>
            {
                var key = string.Format(_itemNameAttributePattern, i);
                MenuItemsCache[key].go.GetComponentInChildren<TMP_Text>().text = $"{i}/100";
            });

            Enumerable.Range(1, 4).ForEach(i =>
            {
                var key = string.Format(_itemNameArmorPattern, i);
                MenuItemsCache[key].go.GetComponentInChildren<TMP_Text>().text = $"{i}";
            });

            var talentTitles = Constants.Daedalus.TalentTitles;
            var talentSkills = Constants.Daedalus.TalentSkills;

            Enumerable.Range(1, 13).ForEach(i =>
            {
                var keyTitle = string.Format(_itemTalentTitlePattern, i);
                var keySkill = string.Format(_itemTalentSkillPattern, i);
                var keyDescription = string.Format(_itemTalentDescriptionPattern, i);

                var randValue = new Random().Next(0, 3);
                var skillDescriptionText = talentSkills.GetString((ushort)i);
                string skillDescriptionFormatted;
                if (skillDescriptionText == "|")
                {
                    skillDescriptionFormatted = "";
                }
                else
                {
                    skillDescriptionFormatted = talentSkills.GetString((ushort)i).Split("|")[randValue];
                }

                MenuItemsCache[keyTitle].go.GetComponentInChildren<TMP_Text>().text = talentTitles.GetString((ushort)i);
                MenuItemsCache[keySkill].go.GetComponentInChildren<TMP_Text>().text = skillDescriptionFormatted;

                if (MenuItemsCache.TryGetValue(keyDescription, out var item))
                {
                    item.go.GetComponentInChildren<TMP_Text>().text = $"{randValue}%";
                }
            });
        }

        public override void ToggleVisibility()
        {
            // Toggle visibility
            gameObject.SetActive(!gameObject.activeSelf);

            if (gameObject.activeSelf)
            {
                SetActive();
            }
        }

        public override void Enable()
        {

        }

        protected override void ExecuteCommand(string commandName)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// There are no elements to hide in Status menu.
        /// </summary>
        protected override bool IsMenuItemInitiallyActive(string menuItemName)
        {
            return true;
        }
    }
}
