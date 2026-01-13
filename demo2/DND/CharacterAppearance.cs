using System;
using System.Collections.Generic;
using UnityEngine;
using Spine.Unity;

namespace demo2.DND
{
    /// <summary>
    /// 角色换装系统 - 外观管理器
    ///
    /// 职责：
    /// - 维护当前部件组合状态（Dictionary<SkinBodyPartType, skinID>）
    /// - 动态更新Spine骨架的皮肤配置
    /// - 发布外观改变事件（OnAppearanceChanged）
    ///
    /// 设计特点：
    /// - baseSkin：基础皮肤（只有脸和手臂的裸体）
    /// - combinedSkin：动态创建（baseSkin + 各部件皮肤的组合）
    /// - 不清理Animation轨道，保持动画连贯
    /// - 仅修改皮肤，不涉及动画状态
    /// </summary>
    public class CharacterAppearance : MonoBehaviour
    {
        [SerializeField]
        private SkinConfig skinConfig;

        [SerializeField]
        private SkeletonAnimation skeletonAnimation;

        /// <summary>
        /// 基础皮肤名（通常是 skin-base）
        /// 包含非遮挡区域的人体（脸、手臂等）
        /// </summary>
        private const string BASE_SKIN = "skin-base";

        /// <summary>
        /// 当前部件组合：每个部件类型 → 对应的皮肤ID
        /// 例如：Hair → "hair/blue", Clothes → "clothes/dress-blue"
        /// </summary>
        private Dictionary<SkinBodyPartType, string> currentParts = new Dictionary<SkinBodyPartType, string>();

        /// <summary>
        /// 当前应用在骨架上的组合皮肤的名称
        /// </summary>
        private string currentCombinedSkinName;

        /// <summary>
        /// 外观改变事件
        /// 参数：改变的部件类型，新的皮肤ID
        /// </summary>
        public event Action<SkinBodyPartType, string> OnAppearanceChanged;

        private void OnEnable()
        {
            // 如果未在Inspector中设置，尝试自动获取
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

            currentParts.Clear();

            // 遍历SkinConfig中的所有部件，为每种部件类型初始化第一个皮肤
            var allParts = skinConfig.GetAllParts();

            foreach (var part in allParts)
            {
                // 为每个部件类型选择第一个可用的皮肤
                // 跳过 FullSkin 和 SkinBase（它们有特殊处理）
                if (part.partType != SkinBodyPartType.FullSkin &&
                    part.partType != SkinBodyPartType.SkinBase)
                {
                    // 如果该部件类型还未初始化，就使用这个皮肤
                    if (!currentParts.ContainsKey(part.partType))
                    {
                        currentParts[part.partType] = part.skinID;
                    }
                }
            }

            // 应用初始外观配置到骨架
            ApplyAppearanceToSkeleton();

            Debug.Log($"[CharacterAppearance] 初始化完成，已加载 {currentParts.Count} 个部件");
        }

        /// <summary>
        /// 设置指定部位的皮肤
        /// 调用此方法会触发外观更新和事件
        /// </summary>
        public void SetPart(SkinBodyPartType partType, string skinID)
        {
            // 验证皮肤ID的有效性
            if (!IsSkinValid(partType, skinID)) return;

            // --- 规则引擎 ---

            // 规则 3: 当应用一个 FullSkin 时，它会替换掉所有散件
            if (partType == SkinBodyPartType.FullSkin)
            {
                currentParts.Clear();
                currentParts[partType] = skinID;
            }
            // 规则 4 (新): 当从 FullSkin 切换回散件时
            else if (currentParts.ContainsKey(SkinBodyPartType.FullSkin))
            {
                // 1. 穿上所有分类的默认部件
                ApplyDefaultParts();
                // 2. 再应用玩家选择的那个特定散件
                currentParts[partType] = skinID;
            }
            // 规则 2: 常规散件叠加
            else
            {
                currentParts[partType] = skinID;
            }

            // 应用最终的外观组合到骨架
            ApplyAppearanceToSkeleton();

            // 发布事件
            OnAppearanceChanged?.Invoke(partType, skinID);

            Debug.Log($"[CharacterAppearance] 部件已更新: {partType} → {skinID}");
        }

        /// <summary>
        /// 应用一套默认的散件组合（每个分类的第一个）
        /// </summary>
        private void ApplyDefaultParts()
        {
            if (skinConfig == null) return;

            currentParts.Clear();
            var allParts = skinConfig.GetAllParts();
            foreach (var part in allParts)
            {
                // 跳过特殊类型
                if (part.partType == SkinBodyPartType.FullSkin || part.partType == SkinBodyPartType.SkinBase)
                {
                    continue;
                }

                // 如果该部件类型还未被赋值，就使用这个（作为该分类的第一个）
                if (!currentParts.ContainsKey(part.partType))
                {
                    currentParts[part.partType] = part.skinID;
                }
            }
            Debug.Log($"[CharacterAppearance] 已应用默认散件组合，共 {currentParts.Count} 个部件。");
        }

        /// <summary>
        /// 验证皮肤ID是否有效
        /// </summary>
        private bool IsSkinValid(SkinBodyPartType partType, string skinID)
        {
            if (skinConfig == null) return true; // 如果没有配置，则不进行验证

            var entry = skinConfig.GetPartBySkinID(skinID);
            if (entry == null)
            {
                Debug.LogWarning($"[CharacterAppearance] 皮肤ID '{skinID}' 在SkinConfig中未找到");
                return false;
            }

            if (entry.partType != partType)
            {
                Debug.LogWarning($"[CharacterAppearance] 皮肤ID '{skinID}' 的部件类型不匹配（期望：{partType}，实际：{entry.partType}）");
                return false;
            }
            return true;
        }

        /// <summary>
        /// 获取指定部位的当前皮肤ID
        /// </summary>
        public string GetCurrentPart(SkinBodyPartType partType)
        {
            if (currentParts.TryGetValue(partType, out var skinID))
            {
                return skinID;
            }

            return null;
        }

        /// <summary>
        /// 获取所有当前部件的组合信息
        /// </summary>
        public Dictionary<SkinBodyPartType, string> GetAllCurrentParts()
        {
            return new Dictionary<SkinBodyPartType, string>(currentParts);
        }

        /// <summary>
        /// 应用外观配置到Spine骨架
        ///
        /// 逻辑：
        /// 1. 从基础皮肤（skin-base）开始
        /// 2. 遍历currentParts中的所有部件皮肤
        /// 3. 将各部件皮肤的附件逐个附加到骨架的对应槽位上
        /// 4. 最终应用到骨架
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

                // 关键修复：在组合皮肤前，将骨架完全重置到“设置姿势”
                skeleton.SetToSetupPose();

                // 第1步：创建一个临时的、用于组合的皮肤
                var combinedSkin = new Spine.Skin("combined-skin");

                // 第2步：首先添加基础皮肤
                var baseSkin = skeletonData.FindSkin(BASE_SKIN);
                if (baseSkin != null)
                {
                    combinedSkin.AddSkin(baseSkin);
                }
                else
                {
                    Debug.LogWarning($"[CharacterAppearance] 基础皮肤 '{BASE_SKIN}' 在SkeletonData中未找到");
                }

                // 第3步：遍历当前部件��合，叠加部件皮肤的附件
                foreach (var kvp in currentParts)
                {
                    var skinID = kvp.Value;
                    var skin = skeletonData.FindSkin(skinID);
                    if (skin != null)
                    {
                        combinedSkin.AddSkin(skin);
                    }
                    else
                    {
                        Debug.LogWarning($"[CharacterAppearance] 皮肤 '{skinID}' 在SkeletonData中未找到");
                    }
                }

                // 第4步：将组合好的新皮肤应用到骨架
                skeleton.SetSkin(combinedSkin);
                skeleton.SetSlotsToSetupPose();

                // 关键修复：强制刷新骨架状态和网格，以确保更改立即生效
                skeletonAnimation.Update(0);
                skeletonAnimation.LateUpdate();

                // 记录当前组合皮肤名称（用于调试）
                currentCombinedSkinName = "combined-skin";

                Debug.Log($"[CharacterAppearance] 外观已应用到骨架: {currentCombinedSkinName}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CharacterAppearance] 应用外观到骨架时出错: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// 确保皮肤已应用（用于与动画系统同步）
        /// 当播放动画时，可能需要确认皮肤配置是否已正确应用
        /// </summary>
        public void EnsureSkinApplied()
        {
            if (skeletonAnimation == null) return;

            // 如果当前骨架的皮肤与基础皮肤不符，重新应用
            try
            {
                var currentSkin = skeletonAnimation.Skeleton.Skin;
                if (currentSkin == null || currentSkin.Name != BASE_SKIN)
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
        /// 重置为默认外观（仅基础皮肤）
        /// </summary>
        public void ResetToDefault()
        {
            if (skeletonAnimation == null) return;

            currentParts.Clear();
            skeletonAnimation.Skeleton.SetSkin(BASE_SKIN);
            currentCombinedSkinName = BASE_SKIN;

            Debug.Log("[CharacterAppearance] 外观已重置为默认");
        }

        /// <summary>
        /// 调试用：输出当前外观配置
        /// </summary>
        public void DebugLogCurrentAppearance()
        {
            Debug.Log("[CharacterAppearance] 当前外观配置：");
            Debug.Log($"  基础皮肤: {BASE_SKIN}");
            foreach (var kvp in currentParts)
            {
                Debug.Log($"  {kvp.Key}: {kvp.Value}");
            }
            Debug.Log($"  组合结果: {currentCombinedSkinName}");
        }
    }
}
