using System;
using System.Collections.Generic;
using GUZ.Core.Extensions;
using GUZ.Core.Globals;
using MyBox;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using ZenKit.Daedalus;

namespace GUZ.Core.UnZENity_Core.Scripts.UI
{
    public abstract class AbstractMenu : MonoBehaviour
    {
        protected MenuManager _menuManager;
        [SerializeField] protected GameObject Canvas;
        [SerializeField] protected GameObject Background;

        protected Dictionary<string, (MenuItemInstance item, GameObject go)> MenuItemCache = new();

        // Pixel ratio of whole menu (Canvas) is based on background picture pixel and virtual pixels named inside Daedalus.
        protected float PixelRatioX;
        protected float PixelRatioY;

        protected abstract void Undefined(string commandName); // e.g.
        protected abstract void Back(string commandName); // e.g.

        protected abstract void StartMenu(string commandName);

        // e.g.
        protected abstract void StartItem(string commandName); // e.g.

        protected abstract void Close(string commandName);

        protected abstract void ConsoleCommand(string commandName); // e.g.
        protected abstract void PlaySound(string commandName); // e.g.
        protected abstract void ExecuteCommand(string commandName); // e.g.

        protected abstract bool IsMenuItemInitiallyActive(string menuItemName);

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

        protected void CreateRootElements(string menuDefName)
        {
            _menuManager = transform.parent.GetComponent<MenuManager>();

            var menuInstance = GameData.MenuVm.InitInstance<MenuInstance>(menuDefName);

            var backPic = GameGlobals.Textures.GetMaterial(menuInstance.BackPic);
            Background.GetComponentInChildren<MeshRenderer>().sharedMaterial = backPic;

            // Set canvas size based on texture size of background
            var canvasRect = Canvas.GetComponent<RectTransform>();
            canvasRect.SetWidth(backPic.mainTexture.width);
            canvasRect.SetHeight(backPic.mainTexture.height);

            // Calculate pixelRatio for virtual positions of child elements.
            var virtualPixelX = menuInstance.DimX + 1;
            var virtualPixelY = menuInstance.DimY + 1;
            var realPixelX = backPic.mainTexture.width;
            var realPixelY = backPic.mainTexture.height;

            PixelRatioX = (float)virtualPixelX / realPixelX; // for normal G1, should be 16 (=8192 / 512)
            PixelRatioY = (float)virtualPixelY / realPixelY;

            for (var i = 0;; i++)
            {
                var menuItemName = menuInstance.GetItem(i);

                // We passed the last item.
                if (menuItemName.IsNullOrEmpty())
                {
                    break;
                }

                CreateMenuItem(menuInstance, menuItemName);
            }
        }

        private void CreateMenuItem(MenuInstance main, string menuItemName)
        {
            var item = GameData.MenuVm.InitInstance<MenuItemInstance>(menuItemName);

            GameObject itemGo;

            if (item.MenuItemType == MenuItemType.ListBox)
            {
                itemGo = ResourceLoader.TryGetPrefabObject(PrefabType.UiEmpty, name: menuItemName,
                    position: Vector3.zero, parent: Canvas)!;
            }
            else if (item.Flags.HasFlag(MenuItemFlag.Selectable))
            {
                itemGo = ResourceLoader.TryGetPrefabObject(PrefabType.UiButton, name: menuItemName, parent: Canvas)!;
                var button = itemGo.GetComponentInChildren<Button>();

                button.onClick.AddListener(() =>
                {
                    for (int i = 0;; i++)
                    {
                        var actionName = item.GetOnSelActionS(i);
                        var action = item.GetOnSelAction(i);
                        
                        if (actionName.IsNullOrEmpty())
                        {
                            break;
                        }
                        
                        OnMenuItemClicked(action, actionName);
                    }
                });
            }
            else if (item.MenuItemType == MenuItemType.Text)
            {
                itemGo = ResourceLoader.TryGetPrefabObject(PrefabType.UiText, name: menuItemName, parent: Canvas)!;
            }
            else
            {
                itemGo = ResourceLoader.TryGetPrefabObject(PrefabType.UiEmpty, name: menuItemName, parent: Canvas)!;
            }

            itemGo.transform.localPosition = Vector3.zero;

            MenuItemCache[menuItemName] = (item, itemGo);

            var rect = itemGo.GetComponent<RectTransform>();
            var halfMainWidth = (float)main.DimX / 2;
            var halfMainHeight = (float)main.DimY / 2;

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
                itemWidth = ((float)main.DimX - item.PosX);
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
                itemHeight = (float)main.DimY - item.PosY;
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

            itemGo.SetActive(IsMenuItemInitiallyActive(menuItemName));
        }

        private void SetTextDimensions(TMP_Text textComp, MenuItemInstance item,
            float itemWidth, float itemHeight)
        {
            // frameSizeX/Y are text paddings from left-right and/or top/bottom.
            var textWidth = itemWidth - 2 * item.FrameSizeX;
            var textHeight = itemHeight - 2 * item.FrameSizeY;

            if (item.Flags.HasFlag(MenuItemFlag.Centered))
            {
                textComp.alignment = TextAlignmentOptions.Center;
            }

            textComp.text = item.GetText(0);
            textComp.spriteAsset = GameGlobals.Font.TryGetFont(item.FontName);
            textComp.fontSize = item.FontName.ToLowerInvariant() == "font_old_10_white.tga" ? 18 : 36;

            // Text component needs to align in dimensions with parent rect.
            var textRect = textComp.GetComponent<RectTransform>();

            textRect.SetWidth(textWidth / PixelRatioX);
            textRect.SetHeight(textHeight / PixelRatioY);
        }

        private void OnMenuItemClicked(MenuItemSelectAction action, string commandName)
        {
            switch (action)
            {
                case MenuItemSelectAction.Undefined:
                    Undefined(commandName);
                    break;
                case MenuItemSelectAction.Back:
                    Back(commandName);
                    break;
                case MenuItemSelectAction.StartMenu:
                    StartMenu(commandName);
                    break;
                case MenuItemSelectAction.StartItem:
                    StartItem(commandName);
                    break;
                case MenuItemSelectAction.Close:
                    Close(commandName);
                    break;
                case MenuItemSelectAction.ConsoleCommand:
                    ConsoleCommand(commandName);
                    break;
                case MenuItemSelectAction.PlaySound:
                    PlaySound(commandName);
                    break;
                case MenuItemSelectAction.ExecuteCommand:
                    ExecuteCommand(commandName);
                    break;
                default:
                    Debug.LogError($"Unknown command {commandName}({action})");
                    break;
            }
        }
    }
}
