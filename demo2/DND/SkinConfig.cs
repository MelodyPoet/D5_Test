﻿using System.Collections.Generic;
using UnityEngine;

namespace demo2.DND
{
    /// <summary>
    /// 单个皮肤部件的配置条目
    /// </summary>
    [System.Serializable]
    public class SkinPartEntry
    {
        [Tooltip("唯一ID（对应Spine皮肤名）")]
        public string skinID;

        [Tooltip("部件类型")]
        public SkinBodyPartType partType;

        [Tooltip("UI显示名称")]
        public string displayName;

        [Tooltip("叠加染色（可选，默认白色表示不染色）")]
        public Color overlayColor = Color.white;

        [Tooltip("UI预览图标（可选）")]
        public Sprite previewIcon;
    }

    /// <summary>
    /// 角色换装系统配置 - 定义所有可用皮肤部件
    /// 职责：存储所有皮肤部件的配置表，提供按ID和按类型的查询接口
    ///
    /// 创建方式：右键Project → Create → DND → Skin Config
    /// 使用方式：
    /// 1. 在Project中创建SkinConfig资产
    /// 2. 在Inspector中配置Skin Parts列表
    /// 3. 在CharacterCustomizationPanel中引用该资产
    /// </summary>
    [CreateAssetMenu(fileName = "SkinConfig", menuName = "DND/Skin Config", order = 100)]
    public class SkinConfig : ScriptableObject
    {
        [SerializeField]
        private List<SkinPartEntry> skinParts = new List<SkinPartEntry>();

        /// <summary>
        /// 根据skinID查找部件
        /// </summary>
        public SkinPartEntry GetPartBySkinID(string skinID)
        {
            foreach (var part in skinParts)
            {
                if (part.skinID == skinID)
                {
                    return part;
                }
            }
            Debug.LogWarning($"[SkinConfig] SkinID '{skinID}' not found");
            return null;
        }

        /// <summary>
        /// 获取指定类型的所有部件
        /// </summary>
        public List<SkinPartEntry> GetPartsByType(SkinBodyPartType partType)
        {
            var result = new List<SkinPartEntry>();
            foreach (var part in skinParts)
            {
                if (part.partType == partType)
                {
                    result.Add(part);
                }
            }
            return result;
        }

        /// <summary>
        /// 获取所有部件
        /// </summary>
        public List<SkinPartEntry> GetAllParts()
        {
            return new List<SkinPartEntry>(skinParts);
        }

        /// <summary>
        /// 编辑器用：获取内部列表（用于Inspector编辑）
        /// </summary>
        public List<SkinPartEntry> GetSkinParts()
        {
            return skinParts;
        }
    }
}

