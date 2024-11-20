using System.Linq;
using System.Threading.Tasks;
using GUZ.Core.Creator.Meshes.V2.Builder;
using GUZ.Core.Data.Container;
using GUZ.Core.Extensions;
using GUZ.Core.Manager;
using GUZ.Core.Vm;
using GUZ.Core.World;
using UnityEngine;
using ZenKit;
using ZenKit.Daedalus;
using ZenKit.Vobs;

namespace GUZ.Core.Creator.Meshes.V2
{
    public static class MeshFactory
    {
        public static async Task CreateWorld(WorldContainer world, LoadingManager loading, GameObject rootGo)
        {
            var worldBuilder = new WorldMeshBuilder();
            worldBuilder.SetGameObject(rootGo);
            worldBuilder.SetWorldData(world);

            await worldBuilder.BuildAsync(loading);
        }

        public static GameObject CreateNpc(string npcName, string mdmName, string mdhName,
            VmGothicExternals.ExtSetVisualBodyData bodyData, GameObject root, GameObject parent = null)
        {
            var npcBuilder = new NpcMeshBuilder();
            npcBuilder.SetGameObject(root, npcName);
            npcBuilder.SetParent(parent);
            npcBuilder.SetMeshName(mdmName);
            npcBuilder.SetMdh(mdhName);
            npcBuilder.SetMdm(mdmName);
            npcBuilder.SetBodyData(bodyData);

            var npcGo = npcBuilder.Build();

            var npcHeadBuilder = new NpcHeadMeshBuilder();
            npcHeadBuilder.SetGameObject(npcGo);
            npcHeadBuilder.SetBodyData(bodyData);
            npcHeadBuilder.SetMeshName(bodyData.Head);
            npcHeadBuilder.SetMmb(bodyData.Head);

            npcHeadBuilder.Build();

            return npcGo;
        }

        public static GameObject CreateNpcWeapon(GameObject npcGo, ItemInstance itemData,
            VmGothicEnums.ItemFlags mainFlag, VmGothicEnums.ItemFlags flags)
        {
            var npcWeaponBuilder = new NpcWeaponMeshBuilder();
            npcWeaponBuilder.SetWeaponData(npcGo, itemData, mainFlag, flags);
            npcWeaponBuilder.SetMeshName(itemData.Visual);

            switch (mainFlag)
            {
                case VmGothicEnums.ItemFlags.ItemKatNf:
                    npcWeaponBuilder.SetMrm(itemData.Visual);
                    break;
                case VmGothicEnums.ItemFlags.ItemKatFf:
                    npcWeaponBuilder.SetMmb(itemData.Visual);
                    break;
                default:
                    // NOP - e.g. for armor.
                    return null;
            }

            return npcWeaponBuilder.Build();
        }

        public static GameObject CreateVob(string objectName, IMultiResolutionMesh mrm, Vector3 position,
            Quaternion rotation, bool withCollider, GameObject parent = null, GameObject rootGo = null,
            bool useTextureArray = true)
        {
            if (!HasTextures(mrm))
            {
                return null;
            }

            var vobBuilder = new VobMeshBuilder();
            vobBuilder.SetRootPosAndRot(position, rotation);
            vobBuilder.SetGameObject(rootGo, objectName);
            vobBuilder.SetParent(parent, resetRotation: true);
            vobBuilder.SetMeshName(objectName);
            vobBuilder.SetMrm(mrm);
            vobBuilder.SetUseTextureArray(useTextureArray);

            if (!withCollider)
            {
                vobBuilder.DisableMeshCollider();
            }

            return vobBuilder.Build();
        }

        public static GameObject CreateVob(string objectName, IModel mdl, Vector3 position, Quaternion rotation,
            GameObject parent = null, GameObject rootGo = null, bool useTextureArray = true)
        {
            if (!HasMeshes(mdl.Mesh))
            {
                return null;
            }

            var vobBuilder = new VobMeshBuilder();
            vobBuilder.SetRootPosAndRot(position, rotation);
            vobBuilder.SetGameObject(rootGo, objectName);
            vobBuilder.SetParent(parent, resetRotation: true); // If we don't reset these, all objects will be rotated wrong!
            vobBuilder.SetMeshName(objectName);
            vobBuilder.SetMdl(mdl);
            vobBuilder.SetUseTextureArray(useTextureArray);

            return vobBuilder.Build();
        }

        public static GameObject CreateVob(string objectName, IMorphMesh mmb, Vector3 position, Quaternion rotation,
            GameObject parent = null, GameObject rootGo = null)
        {
            var vobBuilder = new VobMeshBuilder();
            vobBuilder.SetRootPosAndRot(position, rotation);
            vobBuilder.SetGameObject(rootGo, objectName);
            vobBuilder.SetParent(parent);
            vobBuilder.SetMeshName(objectName);
            vobBuilder.SetMmb(mmb);
            vobBuilder.SetUseTextureArray(true);

            return vobBuilder.Build();
        }

        public static GameObject CreateVob(string objectName, IModelMesh mdm, IModelHierarchy mdh,
            Vector3 position, Quaternion rotation, GameObject parent = null, GameObject rootGo = null,
            bool useTextureArray = true)
        {
            if (!HasMeshes(mdm))
            {
                return null;
            }

            var vobBuilder = new VobMeshBuilder();
            vobBuilder.SetRootPosAndRot(position, rotation);
            vobBuilder.SetGameObject(rootGo, objectName);
            vobBuilder.SetParent(parent, resetRotation: true);
            vobBuilder.SetMeshName(objectName);
            vobBuilder.SetMdh(mdh);
            vobBuilder.SetMdm(mdm);
            vobBuilder.SetUseTextureArray(useTextureArray);

            return vobBuilder.Build();
        }

        private static bool HasTextures(IMultiResolutionMesh mrm)
        {
            // If there is no texture for any of the meshes, just skip this item.
            // G1: Some skull decorations (OC_DECORATE_V4.3DS) are without texture.
            return !mrm.Materials.All(m => m.Texture.IsEmpty());
        }

        private static bool HasMeshes(IModelMesh mdm)
        {
            // Check if there are completely empty elements without any texture.
            // G1: e.g. Harp, Flute, and WASH_SLOT (usage moved to a FreePoint within daedalus functions)
            var noMeshTextures =
                mdm.Meshes.All(mesh => mesh.Mesh.SubMeshes.All(subMesh => subMesh.Material.Texture.IsEmpty()));
            var noAttachmentTextures =
                mdm.Attachments.All(att => att.Value.Materials.All(mat => mat.Texture.IsEmpty()));

            return !(noMeshTextures && noAttachmentTextures);
        }

        public static GameObject CreateVobDecal(IVirtualObject vob, VisualDecal decal, GameObject parent)
        {
            var vobDecalBuilder = new VobDecalMeshBuilder();
            vobDecalBuilder.SetGameObject(null, vob.Name);
            vobDecalBuilder.SetParent(parent);
            vobDecalBuilder.SetRootPosAndRot(vob.Position.ToUnityVector(), vob.Rotation.ToUnityQuaternion());
            vobDecalBuilder.SetDecalData(vob, decal);

            return vobDecalBuilder.Build();
        }

        public static async Task CreateTextureArray()
        {
            await new TextureArrayBuilder().BuildAsync();
        }

        public static GameObject CreateBarrier(string objectName, IMesh mesh)
        {
            var barrierBuilder = new BarrierMeshBuilder();
            barrierBuilder.SetGameObject(null, objectName);
            barrierBuilder.SetBarrierMesh(mesh);

            return barrierBuilder.Build();
        }

        public static GameObject CreatePolyStrip(GameObject go, int numberOfSegments, Vector3 startPoint,
            Vector3 endPoint)
        {
            var polyStripBuilder = new PolyStripMeshBuilder();
            polyStripBuilder.SetGameObject(go);
            polyStripBuilder.SetPolyStripData(numberOfSegments, startPoint, endPoint);

            return polyStripBuilder.Build();
        }
    }
}
