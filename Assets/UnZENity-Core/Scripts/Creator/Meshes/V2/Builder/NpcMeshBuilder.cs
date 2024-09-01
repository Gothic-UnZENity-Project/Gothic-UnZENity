using System.Collections.Generic;
using System.Text.RegularExpressions;
using GUZ.Core.Caches;
using GUZ.Core.Vm;
using UnityEngine;
using ZenKit;
using Vector3 = System.Numerics.Vector3;

namespace GUZ.Core.Creator.Meshes.V2.Builder
{
    public class NpcMeshBuilder : AbstractMeshBuilder
    {
        protected VmGothicExternals.ExtSetVisualBodyData BodyData;

        public virtual void SetBodyData(VmGothicExternals.ExtSetVisualBodyData body)
        {
            BodyData = body;
        }

        public override GameObject Build()
        {
            BuildViaMdmAndMdh();

            return RootGo;
        }

        /// <summary>
        /// Change texture name based on VisualBodyData.
        /// </summary>
        protected override Texture2D GetTexture(string name)
        {
            var finalTextureName =
                // This regex replaces the suffix of V0_C0 with values of corresponding data.
                // e.g. Some_Texture_V0_C0.TGA --> Some_Texture_V1_C2.TGA
                Regex.Replace(name, "(?<=.*?)V0_C0",
                    $"V{BodyData.BodyTexNr}_C{BodyData.BodyTexColor}");

            return base.GetTexture(finalTextureName);
        }

        protected override Dictionary<string, IMultiResolutionMesh> GetFilteredAttachments(
            Dictionary<string, IMultiResolutionMesh> attachments)
        {
            Dictionary<string, IMultiResolutionMesh> newAttachments = new(attachments);

            // Remove head as it will be loaded later.
            if (newAttachments.Remove("BIP01 HEAD"))
            {
                Debug.Log("Removed default >BIP01 HEAD< attachment mesh from NPC.");
            }

            return newAttachments;
        }

        /// <summary>
        /// Positions in mdm files for NPC armor isn't what it seems to be. We need to calculate the real data from weights.
        /// Please check the Cache class for more details.
        /// </summary>
        protected override List<Vector3> GetSoftSkinMeshPositions(ISoftSkinMesh softSkinMesh)
        {
            return NpcArmorPositionCache.TryGetPositions(softSkinMesh, Mdh);
        }
    }
}
