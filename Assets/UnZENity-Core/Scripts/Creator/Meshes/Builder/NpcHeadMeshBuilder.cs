using System.Text.RegularExpressions;
using GUZ.Core._Npc2;
using GUZ.Core.Extensions;
using GUZ.Core.Npc;
using GUZ.Core.Properties;
using UnityEngine;

namespace GUZ.Core.Creator.Meshes.Builder
{
    public class NpcHeadMeshBuilder : NpcMeshBuilder
    {
        public override GameObject Build()
        {
            var headGo = RootGo.FindChildRecursively("BIP01 HEAD");

            if (headGo == null)
            {
                Debug.LogWarning($"No NPC head found for {RootGo.name}");
                return RootGo;
            }

            // Fix for G1: Damlurker
            if (Mmb == null)
            {
                return null;
            }

            var props = RootGo.GetComponentInParent<NpcLoader2>().Npc.GetUserData2().Properties;

            // Cache it for faster use during runtime
            props.NpcPrefabProperties.Head = headGo.transform;
            props.NpcPrefabProperties.HeadMorph = headGo.AddComponent<HeadMorph>();
            props.NpcPrefabProperties.HeadMorph.HeadName = props.BodyData.Head;

            var headMeshFilter = headGo.AddComponent<MeshFilter>();
            var headMeshRenderer = headGo.AddComponent<MeshRenderer>();
            PrepareMeshFilter(headMeshFilter, Mmb.Mesh, headMeshRenderer, 0);
            PrepareMeshRenderer(headMeshRenderer, Mmb.Mesh);

            return RootGo;
        }

        /// <summary>
        /// Change texture name based on VisualBodyData.
        /// </summary>
        protected override Texture2D GetTexture(string name)
        {
            var finalTextureName = name;

            // FIXME - We don't have different mouths in Gothic1. Need to recheck it in Gothic2.
            if (name.ToUpper().EndsWith("MOUTH_V0.TGA"))
            {
                finalTextureName = name;
            }
            else if (name.ToUpper().EndsWith("TEETH_V0.TGA"))
            {
                // e.g. Some_Texture_V0.TGA --> Some_Texture_V1.TGA
                finalTextureName = Regex.Replace(name, "(?<=.*?)V0", $"V{BodyData.TeethTexNr}");
            }
            else if (name.ToUpper().EndsWith("V0_C0.TGA"))
            {
                finalTextureName = Regex.Replace(name, "(?<=.*?)V0_C0",
                    $"V{BodyData.HeadTexNr}_C{BodyData.BodyTexColor}");

                var texture = base.GetTexture(finalTextureName);

                if (texture != null)
                {
                    return texture;
                }
                // Peasant/Bauer (922) has no V52_C2, we therefore try it with Cx-1 on more time.
                else
                {
                    finalTextureName = Regex.Replace(name, "(?<=.*?)V0_C0",
                        $"V{BodyData.HeadTexNr}_C{BodyData.BodyTexColor - 1}");
                }
            }

            return base.GetTexture(finalTextureName);
        }
    }
}
