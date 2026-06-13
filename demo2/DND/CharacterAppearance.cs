using System;
using System.Collections.Generic;
using UnityEngine;
using Spine.Unity;
using demo2.DND.InventoryTetris;

namespace demo2.DND
{
    /// <summary>
    /// 角色换装系统 - 外观管理器（4层渲染模型）
    ///
    /// 渲染层级（从底到顶）：
    ///   Layer 1: baseBody     默认基础身体（紧身衣+躯干+手+脚），由 SkinConfig.defaultBaseBodySkinID 配置
    ///   Layer 2: cosmetic     装饰部件（头发、眼睛、眼皮、鼻子），由换装面板设置
    ///   Layer 3: covering     覆盖型装备（头盔/铠甲/护手/靴子/腰带/披风），穿戴时覆盖对应区域
    ///   Layer 4: overlay      叠加型装备（头环/王冠等），在现有外观上层叠加，不隐藏下层
    ///
    /// Spine 的 AddSkin() 后添加的层在同Slot上有附件时会覆盖前一层，
    /// 因此 Layer3 覆盖型头盔会自然隐藏 Layer2 的头发，Layer4 叠加型头环则不会影响头发。
    /// </summary>
    public class CharacterAppearance : MonoBehaviour
    {
        [SerializeField]
        private SkinConfig skinConfig;

        [SerializeField]
        private SkeletonAnimation skeletonAnimation;

        /// <summary>
        /// Layer 2: 装饰部件 — 换装面板设置（头发/眼睛/眼皮/鼻子/配饰等）
        /// </summary>
        private Dictionary<SkinBodyPartType, string> cosmeticParts = new Dictionary<SkinBodyPartType, string>();

        /// <summary>
        /// Layer 3: 覆盖型装备 — 装备系统驱动（头盔/铠甲/护手/靴子/腰带/披风）
        /// </summary>
        private Dictionary<EquipmentSlot, string> coveringEquipment = new Dictionary<EquipmentSlot, string>();

        /// <summary>
        /// Layer 4: 叠加型装备 — 装备系统驱动（头环/王冠等饰品）
        /// </summary>
        private Dictionary<EquipmentSlot, string> overlayEquipment = new Dictionary<EquipmentSlot, string>();

        /// <summary>
        /// 当前应用在骨架上的组合皮肤的名称
        /// </summary>
        private string currentCombinedSkinName;

        /// <summary>
        /// 外观改变事件
        /// 参数：改变的部件类型，新的皮肤ID
        /// </summary>
        public event Action<SkinBodyPartType, string> OnAppearanceChanged;

        /// <summary>
        /// 装备外观改变事件
        /// 参数：变更的装备槽位
        /// </summary>
        public event Action<EquipmentSlot> OnEquipmentAppearanceChanged;

        private void OnEnable()
        {
            if (skeletonAnimation == null)
            {
                skeletonAnimation = GetComponent<SkeletonAnimation>();
            }

            if (skeletonAnimation == null)
            {
                Debug.LogError("[CharacterAppearance] SkeletonAnimation组件未找到！请确保挂载在同一GameObject上或设置在Inspector中");
            }

            if (skinConfig == null)
            {
                Debug.LogWarning("[CharacterAppearance] SkinConfig未设置，换装功能不可用");
            }
        }

        /// <summary>
        /// 初始化外观配置
        /// 应该在角色创建时调用
        /// </summary>
        public void InitializeAppearance()
        {
            if (skinConfig == null)
            {
                Debug.LogError("[CharacterAppearance] SkinConfig为空，无法初始化");
                return;
            }

            cosmeticParts.Clear();
            coveringEquipment.Clear();
            overlayEquipment.Clear();

            // 遍历SkinConfig中的所有部件，为每种装饰部件类型初始化第一个皮肤
            var allParts = skinConfig.GetAllParts();

            foreach (var part in allParts)
            {
                // 跳过 FullSkin、SkinBase 和装备部位（装备部位由装备系统驱动）
                if (part.partType == SkinBodyPartType.FullSkin ||
                    part.partType == SkinBodyPartType.SkinBase ||
                    IsEquipmentPart(part.partType))
                {
                    continue;
                }

                if (!cosmeticParts.ContainsKey(part.partType))
                {
                    cosmeticParts[part.partType] = part.skinID;
                }
            }

            ApplyAppearanceToSkeleton();
            Debug.Log($"[CharacterAppearance] 初始化完成，已加载 {cosmeticParts.Count} 个装饰部件");
        }

        // ==================== 装饰部件接口（换装面板使用） ====================

        /// <summary>
        /// 设置指定部位的皮肤
        /// - 内层装饰部位（SkinBase/Hair/Eyes/Mouth）写入 cosmeticParts
        /// - 外层装备部位（Helmet/Armor/Gloves/Boots/Belt/Cloak/MainHandWeapon/OffHandShield/OffHandWeapon）
        ///   写入 coveringEquipment（测试期直接调用；正式版由 CharacterEquipment 驱动）
        /// - FullSkin 清空所有部位
        /// </summary>
        public void SetPart(SkinBodyPartType partType, string skinID)
        {
            if (!IsSkinValid(partType, skinID)) return;

            if (partType == SkinBodyPartType.FullSkin)
            {
                // FullSkin 清空所有装饰散件和装备外观
                cosmeticParts.Clear();
                coveringEquipment.Clear();
                overlayEquipment.Clear();
                cosmeticParts[partType] = skinID;
            }
            else if (IsEquipmentPart(partType))
            {
                // 外层装备部位 → 写入 coveringEquipment（测试期由面板直接操作）
                // 后续正式版此处应由 CharacterEquipment.SyncAppearance() 驱动
                var slot = MapPartTypeToEquipmentSlot(partType);
                if (slot.HasValue)
                {
                    coveringEquipment[slot.Value] = skinID;
                }
            }
            else if (cosmeticParts.ContainsKey(SkinBodyPartType.FullSkin))
            {
                // 从 FullSkin 切换回散件：先加载默认散件，再应用目标
                ApplyDefaultCosmeticParts();
                cosmeticParts[partType] = skinID;
            }
            else
            {
                // 内层装饰部位
                cosmeticParts[partType] = skinID;
            }

            ApplyAppearanceToSkeleton();
            OnAppearanceChanged?.Invoke(partType, skinID);
            Debug.Log($"[CharacterAppearance] 部件已更新: {partType} → {skinID}");
        }

        /// <summary>
        /// 获取指定部位的当前皮肤ID（包括装备部位）
        /// </summary>
        public string GetCurrentPart(SkinBodyPartType partType)
        {
            // 先查装饰部件
            cosmeticParts.TryGetValue(partType, out var skinID);
            if (!string.IsNullOrEmpty(skinID)) return skinID;

            // 再查装备部位
            var slot = MapPartTypeToEquipmentSlot(partType);
            if (slot.HasValue)
            {
                if (coveringEquipment.TryGetValue(slot.Value, out var equipSkin))
                    return equipSkin;
                if (overlayEquipment.TryGetValue(slot.Value, out equipSkin))
                    return equipSkin;
            }

            return null;
        }

        /// <summary>
        /// 获取所有当前装饰部件 + 装备外观的完整映射
        /// （测试期用；后续装备外观由 CharacterEquipment 驱动）
        /// </summary>
        public Dictionary<SkinBodyPartType, string> GetAllCurrentParts()
        {
            var result = new Dictionary<SkinBodyPartType, string>(cosmeticParts);

            // 合并装备外观
            foreach (var kv in coveringEquipment)
            {
                var partType = MapEquipmentSlotToPartType(kv.Key);
                if (partType.HasValue)
                    result[partType.Value] = kv.Value;
            }
            foreach (var kv in overlayEquipment)
            {
                var partType = MapEquipmentSlotToPartType(kv.Key);
                if (partType.HasValue)
                    result[partType.Value] = kv.Value;
            }

            return result;
        }

        // ==================== 装备外观接口（装备系统驱动） ====================

        /// <summary>
        /// 根据当前装备状态同步外观（Layer 3 + Layer 4）
        /// 由 CharacterEquipment 在装备/卸下物品后调用。
        /// </summary>
        /// <param name="slotMap">当前所有已装备槽位 → ItemInstance 的映射</param>
        public void SyncFromEquipment(Dictionary<EquipmentSlot, ItemInstance> slotMap)
        {
            if (slotMap == null) return;

            // 先清空当前装备外观状态
            coveringEquipment.Clear();
            overlayEquipment.Clear();

            // 遍历所有已装备物品，收集有外观配置的物品
            foreach (var kv in slotMap)
            {
                var item = kv.Value;
                if (item == null || item.data == null) continue;
                if (string.IsNullOrEmpty(item.data.appearanceSkinID)) continue;
                if (item.data.appearanceBehavior == EquipmentAppearanceBehavior.None) continue;

                var slot = item.data.appearanceSlot;
                var skinID = item.data.appearanceSkinID;

                switch (item.data.appearanceBehavior)
                {
                    case EquipmentAppearanceBehavior.Cover:
                        coveringEquipment[slot] = skinID;
                        break;
                    case EquipmentAppearanceBehavior.Overlay:
                        overlayEquipment[slot] = skinID;
                        break;
                }
            }

            ApplyAppearanceToSkeleton();
            Debug.Log($"[CharacterAppearance] 装备外观已同步: Cover={coveringEquipment.Count}, Overlay={overlayEquipment.Count}");
        }

        /// <summary>
        /// 获取指定装备槽位的外观皮肤ID
        /// </summary>
        public string GetEquipmentSkin(EquipmentSlot slot)
        {
            if (coveringEquipment.TryGetValue(slot, out var skinID)) return skinID;
            if (overlayEquipment.TryGetValue(slot, out skinID)) return skinID;
            return null;
        }

        // ==================== 核心渲染引擎 ====================

        /// <summary>
        /// 4层模型的外观组合算法
        /// 将 Layer1(baseBody) + Layer2(cosmetic) + Layer3(covering) + Layer4(overlay)
        /// 按顺序组合为一个 Spine Skin 并应用到骨架。
        /// </summary>
        private void ApplyAppearanceToSkeleton()
        {
            if (skeletonAnimation == null)
            {
                Debug.LogError("[CharacterAppearance] SkeletonAnimation为空，无法应用外观");
                return;
            }

            try
            {
                var skeleton = skeletonAnimation.Skeleton;
                var skeletonData = skeletonAnimation.skeletonDataAsset.GetSkeletonData(false);

                skeleton.SetToSetupPose();

                var combinedSkin = new Spine.Skin("combined-skin");

                // ===== Layer 1: 基础身体 =====
                string baseBodyID = skinConfig != null ? skinConfig.defaultBaseBodySkinID : "base-skin";
                var baseBodySkin = skeletonData.FindSkin(baseBodyID);
                if (baseBodySkin != null)
                {
                    combinedSkin.AddSkin(baseBodySkin);
                }
                else
                {
                    Debug.LogWarning($"[CharacterAppearance] 基础身体皮肤 '{baseBodyID}' 在SkeletonData中未找到");
                }

                // ===== Layer 2: 装饰部件 =====
                foreach (var kvp in cosmeticParts)
                {
                    var skin = skeletonData.FindSkin(kvp.Value);
                    if (skin != null)
                    {
                        combinedSkin.AddSkin(skin);
                    }
                    else
                    {
                        Debug.LogWarning($"[CharacterAppearance] 装饰皮肤 '{kvp.Value}' 在SkeletonData中未找到");
                    }
                }

                // ===== Layer 3: 覆盖型装备 =====
                foreach (var kvp in coveringEquipment)
                {
                    var skin = skeletonData.FindSkin(kvp.Value);
                    if (skin != null)
                    {
                        combinedSkin.AddSkin(skin);
                    }
                    else
                    {
                        Debug.LogWarning($"[CharacterAppearance] 覆盖型装备皮肤 '{kvp.Value}' 在SkeletonData中未找到");
                    }
                }

                // ===== Layer 4: 叠加型装备 =====
                foreach (var kvp in overlayEquipment)
                {
                    var skin = skeletonData.FindSkin(kvp.Value);
                    if (skin != null)
                    {
                        combinedSkin.AddSkin(skin);
                    }
                    else
                    {
                        Debug.LogWarning($"[CharacterAppearance] 叠加型装备皮肤 '{kvp.Value}' 在SkeletonData中未找到");
                    }
                }

                // 应用组合皮肤到骨架
                skeleton.SetSkin(combinedSkin);
                skeleton.SetSlotsToSetupPose();

                // 强制刷新
                skeletonAnimation.Update(0);
                skeletonAnimation.LateUpdate();

                currentCombinedSkinName = "combined-skin";
                Debug.Log($"[CharacterAppearance] 外观已应用到骨架: {currentCombinedSkinName} (L1+{cosmeticParts.Count}+{coveringEquipment.Count}+{overlayEquipment.Count})");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CharacterAppearance] 应用外观到骨架时出错: {ex.Message}\n{ex.StackTrace}");
            }
        }

        // ==================== 内部辅助 ====================

        /// <summary>
        /// 判断是否为外层装备部位（非内层装饰）
        /// </summary>
        private bool IsEquipmentPart(SkinBodyPartType partType)
        {
            switch (partType)
            {
                case SkinBodyPartType.Helmet:
                case SkinBodyPartType.Armor:
                case SkinBodyPartType.Gloves:
                case SkinBodyPartType.Boots:
                case SkinBodyPartType.Belt:
                case SkinBodyPartType.Cloak:
                case SkinBodyPartType.MainHandWeapon:
                case SkinBodyPartType.OffHandShield:
                case SkinBodyPartType.OffHandWeapon:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// SkinBodyPartType → EquipmentSlot 映射
        /// </summary>
        private EquipmentSlot? MapPartTypeToEquipmentSlot(SkinBodyPartType partType)
        {
            switch (partType)
            {
                case SkinBodyPartType.Helmet: return EquipmentSlot.Helmet;
                case SkinBodyPartType.Armor: return EquipmentSlot.Armor;
                case SkinBodyPartType.Gloves: return EquipmentSlot.Gauntlets;
                case SkinBodyPartType.Boots: return EquipmentSlot.Boots;
                case SkinBodyPartType.Belt: return EquipmentSlot.Belt;
                case SkinBodyPartType.Cloak: return EquipmentSlot.Cloak;
                case SkinBodyPartType.MainHandWeapon: return EquipmentSlot.MainHand;
                case SkinBodyPartType.OffHandShield: return EquipmentSlot.OffHand;
                case SkinBodyPartType.OffHandWeapon: return EquipmentSlot.OffHand;
                default: return null;
            }
        }

        /// <summary>
        /// EquipmentSlot → SkinBodyPartType 反向映射
        /// </summary>
        private SkinBodyPartType? MapEquipmentSlotToPartType(EquipmentSlot slot)
        {
            switch (slot)
            {
                case EquipmentSlot.Helmet: return SkinBodyPartType.Helmet;
                case EquipmentSlot.Armor: return SkinBodyPartType.Armor;
                case EquipmentSlot.Gauntlets: return SkinBodyPartType.Gloves;
                case EquipmentSlot.Boots: return SkinBodyPartType.Boots;
                case EquipmentSlot.Belt: return SkinBodyPartType.Belt;
                case EquipmentSlot.Cloak: return SkinBodyPartType.Cloak;
                case EquipmentSlot.MainHand: return SkinBodyPartType.MainHandWeapon;
                case EquipmentSlot.OffHand: return SkinBodyPartType.OffHandShield; // 默认映射为盾牌
                default: return null;
            }
        }

        /// <summary>
        /// 应用默认装饰散件组合（每个分类的第一个）
        /// </summary>
        private void ApplyDefaultCosmeticParts()
        {
            if (skinConfig == null) return;

            cosmeticParts.Clear();
            var allParts = skinConfig.GetAllParts();
            foreach (var part in allParts)
            {
                // 跳过 FullSkin、SkinBase 和装备部位
                if (part.partType == SkinBodyPartType.FullSkin ||
                    part.partType == SkinBodyPartType.SkinBase ||
                    IsEquipmentPart(part.partType))
                    continue;

                if (!cosmeticParts.ContainsKey(part.partType))
                {
                    cosmeticParts[part.partType] = part.skinID;
                }
            }
            Debug.Log($"[CharacterAppearance] 已应用默认装饰散件组合，共 {cosmeticParts.Count} 个部件。");
        }

        /// <summary>
        /// 验证皮肤ID是否有效
        /// </summary>
        private bool IsSkinValid(SkinBodyPartType partType, string skinID)
        {
            if (skinConfig == null) return true;

            var entry = skinConfig.GetPartBySkinID(skinID);
            if (entry == null)
            {
                Debug.LogWarning($"[CharacterAppearance] 皮肤ID '{skinID}' 在SkinConfig中未找到");
                return false;
            }

            // 装备部位允许从 SkinConfig 中查找到任意类型（因测试期面板直接操作）
            if (IsEquipmentPart(partType))
            {
                // 装备部位宽松匹配：只要 SkinConfig 中有此 skinID 即可
                return true;
            }

            if (entry.partType != partType)
            {
                Debug.LogWarning($"[CharacterAppearance] 皮肤ID '{skinID}' 的部件类型不匹配（期望：{partType}，实际：{entry.partType}）");
                return false;
            }
            return true;
        }

        // ==================== 公共工具方法 ====================

        /// <summary>
        /// 确保皮肤已应用（用于与动画系统同步）
        /// </summary>
        public void EnsureSkinApplied()
        {
            if (skeletonAnimation == null) return;

            try
            {
                var currentSkin = skeletonAnimation.Skeleton.Skin;
                if (currentSkin == null || currentSkin.Name != currentCombinedSkinName)
                {
                    ApplyAppearanceToSkeleton();
                    Debug.Log("[CharacterAppearance] 皮肤已重新应用（通过EnsureSkinApplied）");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[CharacterAppearance] EnsureSkinApplied检查失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 重置为仅基础身体（清空所有装饰和装备外观）
        /// </summary>
        public void ResetToDefault()
        {
            if (skeletonAnimation == null) return;

            cosmeticParts.Clear();
            coveringEquipment.Clear();
            overlayEquipment.Clear();
            ApplyAppearanceToSkeleton();

            Debug.Log("[CharacterAppearance] 外观已重置为基础身体");
        }

        /// <summary>
        /// 调试用：输出当前外观配置
        /// </summary>
        public void DebugLogCurrentAppearance()
        {
            string baseBodyID = skinConfig != null ? skinConfig.defaultBaseBodySkinID : "base-skin";
            Debug.Log("[CharacterAppearance] 当前外观配置（4层模型）：");
            Debug.Log($"  Layer 1 (基础身体): {baseBodyID}");
            foreach (var kvp in cosmeticParts)
                Debug.Log($"  Layer 2 (装饰): {kvp.Key}={kvp.Value}");
            foreach (var kvp in coveringEquipment)
                Debug.Log($"  Layer 3 (覆盖装备): {kvp.Key}={kvp.Value}");
            foreach (var kvp in overlayEquipment)
                Debug.Log($"  Layer 4 (叠加装备): {kvp.Key}={kvp.Value}");
            Debug.Log($"  组合结果: {currentCombinedSkinName}");
        }
    }
}
