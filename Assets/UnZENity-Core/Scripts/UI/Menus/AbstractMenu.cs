using System;
using System.Collections.Generic;
using GUZ.Core.Extensions;
using GUZ.Core.Globals;
using GUZ.Core.UI.Menus.Adapter.Menu;
using GUZ.Core.UI.Menus.Adapter.MenuItem;
using GUZ.Core.Util;
using MyBox;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using ZenKit.Daedalus;
using Logger = GUZ.Core.Util.Logger;

namespace GUZ.Core.UI.Menus
{
    public abstract class AbstractMenu : MonoBehaviour
    {
        protected MenuHandler MenuHandler;
        protected IMenuInstance AbstractMenuInstance;
        [SerializeField] protected GameObject Canvas;
        [SerializeField] protected GameObject Background;

        protected Dictionary<string, (IMenuItemInstance item, GameObject go)> MenuItemCache = new();

        // Pixel ratio of whole menu (Canvas) is based on background picture pixel and virtual pixels named inside Daedalus.
        protected float PixelRatioX;
        protected float PixelRatioY;

        public virtual void InitializeMenu(IMenuInstance abstractMenuInstance)
        {
            MenuHandler = transform.parent.GetComponent<MenuHandler>();
            
            AbstractMenuInstance = abstractMenuInstance;
            CreateRootElements();
        }
        
        protected abstract void Undefined(string itemName, string commandName);

        protected virtual void Back(string itemName, string commandName)
        {
            MenuHandler.BackMenu();
        }

        protected abstract void StartMenu(string itemName, string commandName);

        protected abstract void StartItem(string itemName, string commandName);

        protected abstract void Close(string itemName, string commandName);

        protected abstract void ConsoleCommand(string itemName, string commandName);
        protected abstract void PlaySound(string itemName, string commandName);
        protected abstract void ExecuteCommand(string itemName, string commandName);

        protected virtual bool IsMenuItemInitiallyActive(string menuItemName)
        {
            return true;
        }
        
        public void ToggleVisibility()
        {
            if (gameObject.activeSelf)
            {
                SetInvisible();
            }
            else
            {
                SetVisible();
            }
        }

        public virtual void SetVisible()
        {
            gameObject.SetActive(true);
        }

        public virtual void SetInvisible()
        {
            gameObject.SetActive(false);
        }

        private void CreateRootElements()
        {
            var backPic = GameGlobals.Textures.GetMaterial(AbstractMenuInstance.BackPic);
            Background.GetComponentInChildren<MeshRenderer>().sharedMaterial = backPic;

            // Set canvas size based on texture size of background
            var canvasRect = Canvas.GetComponent<RectTransform>();
            canvasRect.SetWidth(backPic.mainTexture.width);
            canvasRect.SetHeight(backPic.mainTexture.height);

            // Calculate pixelRatio for virtual positions of child elements.
            var virtualPixelX = AbstractMenuInstance.DimX + 1;
            var virtualPixelY = AbstractMenuInstance.DimY + 1;
            var realPixelX = backPic.mainTexture.width;
            var realPixelY = backPic.mainTexture.height;

            PixelRatioX = (float)virtualPixelX / realPixelX; // for normal G1, should be 16 (=8192 / 512)
            PixelRatioY = (float)virtualPixelY / realPixelY;

            foreach (var item in AbstractMenuInstance.Items)
            {
                CreateMenuItem(item);
            }
        }

        private void CreateMenuItem(IMenuItemInstance item)
        {
            GameObject itemGo;

            if (item.MenuItemType == MenuItemType.ListBox)
            {
                itemGo = ResourceLoader.TryGetPrefabObject(PrefabType.UiEmpty, name: item.Name,
                    position: Vector3.zero, parent: Canvas)!;
            }
            else if (item.Flags.HasFlag(MenuItemFlag.Selectable))
            {
                itemGo = ResourceLoader.TryGetPrefabObject(PrefabType.UiButton, name: item.Name, parent: Canvas)!;
                var button = itemGo.GetComponentInChildren<Button>();

                button.onClick.AddListener(() =>
                {
                    for (int i = 0;; i++)
                    {
                        MenuItemSelectAction action;
                        try
                        {
                            action = item.GetOnSelAction(i);
                        }
                        catch (Exception e)
                        {
                            break;
                        }

                        if (action == null || action == MenuItemSelectAction.Undefined)
                        {
                            break;
                        }

                        string actionName = "";
                        try
                        {
                            actionName = item.GetOnSelActionS(i);
                        }
                        catch (Exception e)
                        {
                            OnMenuItemClicked(action, item.Name, actionName);
                            break;
                        }

                        OnMenuItemClicked(action, item.Name, actionName);
                    }
                });
            }
            else if (item.MenuItemType == MenuItemType.Text)
            {
                itemGo = ResourceLoader.TryGetPrefabObject(PrefabType.UiText, name: item.Name, parent: Canvas)!;
            }
            else
            {
                itemGo = ResourceLoader.TryGetPrefabObject(PrefabType.UiEmpty, name: item.Name, parent: Canvas)!;
            }

            itemGo.transform.localPosition = Vector3.zero;

            MenuItemCache[item.Name] = (item, itemGo);

            var rect = itemGo.GetComponent<RectTransform>();
            var halfMainWidth = (float)AbstractMenuInstance.DimX / 2;
            var halfMainHeight = (float)AbstractMenuInstance.DimY / 2;

            float itemWidth;
            if (item.DimX > 0)
            {
                // As we have anchor positions at the center (0.5), we need to move into a certain direction from the center
                // Hint: We need to stick with center, as setting anchor positions at runtime (e.g. left-aligned) causes Unity to crash at a certain amount of changes.
                itemWidth = item.DimX;
            }
            else
            {
                // We assume the element can be drawn until end of whole UI.
                itemWidth = ((float)AbstractMenuInstance.DimX - item.PosX);
            }

            rect.SetPositionX((item.PosX - halfMainWidth + itemWidth / 2) / PixelRatioX);
            rect.SetWidth(itemWidth / PixelRatioX);

            float itemHeight;
            if (item.DimY > 0)
            {
                itemHeight = item.DimY;
            }
            else
            {
                // We assume the element can be drawn until end of whole UI.
                itemHeight = (float)AbstractMenuInstance.DimY - item.PosY;
            }

            rect.SetPositionY((halfMainHeight - item.PosY - itemHeight / 2) / PixelRatioY);
            rect.SetHeight(itemHeight / PixelRatioY);

            if (item.BackPic.NotNullOrEmpty())
            {
                var backPicGo =
                    ResourceLoader.TryGetPrefabObject(PrefabType.UiTexture, name: item.BackPic, parent: itemGo)!;
                var backPic = GameGlobals.Textures.GetMaterial(item.BackPic);

                if (!item.AlphaMode.IsNullOrEmpty())
                {
                    backPic.ToFadeMode();
                    var color = backPic.GetColor("_BaseColor");
                    float alpha = item.Alpha / 255f;
                    backPic.SetColor("_BaseColor", new Color(color.r, color.g, color.b, alpha));
                }

                backPicGo.transform.localPosition = Vector3.zero;


                var backPictRenderer = backPicGo.GetComponentInChildren<MeshRenderer>();
                backPictRenderer.sharedMaterial = backPic;

                // Apply scale and position (move slightly backwards) from normal background to this one.
                backPictRenderer.transform.localScale =
                    new Vector3(itemWidth / PixelRatioX / 10, 1, itemHeight / PixelRatioY / 10);
            }

            var textComp = itemGo.GetComponentInChildren<TMP_Text>();
            if (textComp != null)
            {
                SetTextDimensions(textComp, item, itemWidth, itemHeight);
            }

            //item disabled (grayed out and not interactable)
            if (!IsMenuItemInitiallyActive(item.Name))
            {
                if (textComp != null)
                {
                    textComp.color = Constants.TextDisabledColor;
                    var button = itemGo.GetComponent<Button>();
                    var eventTrigger = itemGo.GetComponent<EventTrigger>();
                    if (button != null)
                    {
                        button.enabled = false;
                    }

                    if (eventTrigger != null)
                    {
                        eventTrigger.enabled = false;
                    }
                }
                else
                {
                    itemGo.SetActive(false);
                }
            }
        }

        private void SetTextDimensions(TMP_Text textComp, IMenuItemInstance item,
            float itemWidth, float itemHeight)
        {
            // frameSizeX/Y are text paddings from left-right and/or top/bottom.
            var textWidth = itemWidth - 2 * item.FrameSizeX;
            var textHeight = itemHeight - 2 * item.FrameSizeY;

            if (item.Flags.HasFlag(MenuItemFlag.Centered))
            {
                textComp.alignment = TextAlignmentOptions.Center;
            }

            // By default, we let text overflow. This ensures we won't have it being wrapped if we have text >1px too long.
            if (item.Flags.HasFlag(MenuItemFlag.Multiline))
            {
                textComp.textWrappingMode = TextWrappingModes.Normal;
            }

            // Text shall always be rendered from top to bottom. Otherwise, e.g. MENUITEM_OPT_HEADING will be rendered y-centered.
            // VerticalAlignment setting stored on Prefab isn't being used by Unity. We therefore need to set it now.
            textComp.verticalAlignment = VerticalAlignmentOptions.Top;

            textComp.text = item.GetText(0);
            textComp.spriteAsset = GameGlobals.Font.TryGetFont(item.FontName);
            textComp.fontSize = item.FontName.ToLowerInvariant().Contains("font_old_10_white") ? 16 : 36;

            // Text component needs to align in dimensions with parent rect.
            var textRect = textComp.GetComponent<RectTransform>();

            textRect.SetWidth(textWidth / PixelRatioX);
            textRect.SetHeight(textHeight / PixelRatioY);
        }

        /// <summary>
        /// This function is used as a callback for menu items when they are clicked.
        /// The itemName argument is mostly needed for the Loading menu as it calls Close when loading a save.
        /// </summary>
        /// <param name="action">Represents which function to call</param>
        /// <param name="itemName">Represents the name of the menu item</param>
        /// <param name="commandName">Represents additional behaviour for the action</param>
        private void OnMenuItemClicked(MenuItemSelectAction action, string itemName, string commandName)
        {
            switch (action)
            {
                case MenuItemSelectAction.Undefined:
                    Undefined(itemName, commandName);
                    break;
                case MenuItemSelectAction.Back:
                    Back(itemName, commandName);
                    break;
                case MenuItemSelectAction.StartMenu:
                    StartMenu(itemName, commandName);
                    break;
                case MenuItemSelectAction.StartItem:
                    StartItem(itemName, commandName);
                    break;
                case MenuItemSelectAction.Close:
                    Close(itemName, commandName);
                    break;
                case MenuItemSelectAction.ConsoleCommand:
                    ConsoleCommand(itemName, commandName);
                    break;
                case MenuItemSelectAction.PlaySound:
                    PlaySound(itemName, commandName);
                    break;
                case MenuItemSelectAction.ExecuteCommand:
                    ExecuteCommand(itemName, commandName);
                    break;
                default:
                    Logger.LogError($"Unknown command {commandName}({action})", LogCat.Ui);
                    break;
            }
        }
    }
}
