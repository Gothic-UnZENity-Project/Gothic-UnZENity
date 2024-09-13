using System.Reflection;
using GUZ.Core.Caches;
using GUZ.Core.Util;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore;
using Constants = GUZ.Core.Globals.Constants;

namespace GUZ.Core.Manager
{
    public class FontManager : SingletonBehaviour<FontManager>
    {
        public TMP_FontAsset DefaultFont;

        public TMP_SpriteAsset DefaultSpriteAsset;
        public TMP_SpriteAsset HighlightSpriteAsset;

        public void Create()
        {
            DefaultSpriteAsset = LoadFont("font_old_20_white.FNT");
            HighlightSpriteAsset = LoadFont("font_old_20_white_hi.FNT");

            TMP_Settings.defaultSpriteAsset = DefaultSpriteAsset;
            TMP_Settings.defaultFontAsset = DefaultFont;


        }

        private TMP_SpriteAsset LoadFont(string fontName)
        {
            if (LookupCache.FontCache.TryGetValue(fontName.ToUpper(), out var data))
            {
                return data;
            }

            var font = ResourceLoader.TryGetFont(fontName.ToUpper());
            var fontTexture = TextureCache.TryGetTexture(fontName);

            var spriteAsset = ScriptableObject.CreateInstance<TMP_SpriteAsset>();

            for (var i = 0; i < font.Glyphs.Count; i++)
            {
                var x = font.Glyphs[i].topLeft.X * fontTexture.width;
                x = x < 0 ? 0 : x;
                var y = font.Glyphs[i].topLeft.Y * fontTexture.height;
                var w = font.Glyphs[i].width;
                var h = font.Height;
                var newSprite = Sprite.Create(fontTexture, new Rect(x, y, w, h),
                    new Vector2(font.Glyphs[i].topLeft.X, font.Glyphs[i].bottomRight.Y));

                var spriteGlyph = new TMP_SpriteGlyph
                {
                    glyphRect = new GlyphRect
                    {
                        width = w,
                        height = h,
                        x = (int)x,
                        y = (int)y
                    },
                    metrics = new GlyphMetrics
                    {
                        width = w,
                        height = -h,
                        horizontalBearingY = 0,
                        horizontalBearingX = 0,
                        horizontalAdvance = w
                    },
                    index = (uint)i,
                    sprite = newSprite,
                    scale = -1
                };

                spriteAsset.spriteGlyphTable.Add(spriteGlyph);
                // Manually map known glyphs to polish characters
                var unicodeValue = i switch
                {
                    // Ś
                    140 => (uint)0x015A,
                    // Ź
                    143 => (uint)0x0179,
                    // ś
                    156 => (uint)0x015B,
                    // ź
                    159 => (uint)0x017A,
                    // Ł
                    163 => (uint)0x0141,
                    // Ą
                    165 => (uint)0x0104,
                    // Ż
                    175 => (uint)0x017B,
                    // ł
                    179 => (uint)0x0142,
                    // ą
                    185 => (uint)0x0105,
                    // ż
                    191 => (uint)0x017C,
                    // Ć
                    198 => (uint)0x0106,
                    // Ę
                    202 => (uint)0x0118,
                    // Ń
                    209 => (uint)0x0143,
                    // Ó
                    211 => (uint)0x00D3,
                    // ć
                    230 => (uint)0x0107,
                    // ę
                    234 => (uint)0x0119,
                    // ń
                    241 => (uint)0x0144,
                    // ó
                    243 => (uint)0x00F3,
                    _ => (uint)i,// Default mapping for other glyphs
                };
                var spriteCharacter = new TMP_SpriteCharacter(unicodeValue, spriteGlyph);
                spriteAsset.spriteCharacterTable.Add(spriteCharacter);
            }

            spriteAsset.name = name;
            spriteAsset.material = GetDefaultSpriteMaterial(fontTexture);
            spriteAsset.spriteSheet = fontTexture;

            // Get the Type of the TMP_SpriteAsset
            var spriteAssetType = spriteAsset.GetType();

            // Get the FieldInfo of the 'm_Version' field
            var versionField = spriteAssetType.GetField("m_Version", BindingFlags.NonPublic | BindingFlags.Instance);

            versionField.SetValue(spriteAsset, "1.0.0"); // setting this as to skip "UpgradeSpriteAsset"

            spriteAsset.UpdateLookupTables();

            LookupCache.FontCache[fontName.ToUpper()] = spriteAsset;

            return spriteAsset;
        }

        private static Material GetDefaultSpriteMaterial(Texture2D spriteSheet = null)
        {
            ShaderUtilities.GetShaderPropertyIDs();

            // Add a new material
            var shader = Constants.ShaderTMPSprite;
            var tempMaterial = new Material(shader);
            tempMaterial.SetTexture(ShaderUtilities.ID_MainTex, spriteSheet);

            return tempMaterial;
        }
    }
}
