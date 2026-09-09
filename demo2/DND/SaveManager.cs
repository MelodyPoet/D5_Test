using System.IO;
using UnityEngine;
using demo2.DND.InventoryTetris; // for ItemDatabase

namespace demo2.DND
{
    /// <summary>
    /// 角色存档管理（预留落盘接口）。
    ///
    /// 当前内存桥（CharacterCustomizationBridge）已实现跨场景传递（职业/种族/外观/初始装备/属性）。
    /// 本类预留“落盘 JSON”接口，使角色关键信息在关闭游戏后仍可保存/读取：
    ///   - 在确认角色、升级、装备变更、场景切换时调用 Save；
    ///   - 进战斗/读档时调用 Load 优先从磁盘恢复主控信息。
    ///
    /// 数据类 CharacterCustomizationData 已整体 [Serializable]，且用 itemId / characterClass / 枚举
    /// 替代了 UnityEngine.Object 引用（runtimeRef / selectedTemplate 已 [NonSerialized]），
    /// 因此 JsonUtility 可无损序列化；反序列化后由 ItemDatabase / 职业枚举重建 Object 引用。
    /// </summary>
    public static class SaveManager
    {
        private const string SaveFileName = "player_save.json";

        public static string SavePath =>
            Path.Combine(Application.persistentDataPath, SaveFileName);

        /// <summary>将定制数据序列化为 JSON 字符串（不写盘，供调试/网络传输复用）</summary>
        public static string Serialize(CharacterCustomizationData data)
        {
            return JsonUtility.ToJson(data, prettyPrint: true);
        }

        /// <summary>从 JSON 反序列化（runtimeRef / selectedTemplate 需额外按 itemId / name 重建）</summary>
        public static CharacterCustomizationData Deserialize(string json)
        {
            if (string.IsNullOrEmpty(json)) return null;
            var data = JsonUtility.FromJson<CharacterCustomizationData>(json);
            RebuildReferences(data);
            return data;
        }

        /// <summary>写入磁盘（落盘存档）。后续接入“继续游戏”时调用。</summary>
        public static void Save(CharacterCustomizationData data)
        {
            var json = Serialize(data);
            File.WriteAllText(SavePath, json);
            Debug.Log($"[SaveManager] 已保存角色存档 → {SavePath}");
        }

        /// <summary>从磁盘读取存档；无存档返回 null。后续接入“继续游戏”时调用。</summary>
        public static CharacterCustomizationData Load()
        {
            if (!File.Exists(SavePath)) return null;
            var json = File.ReadAllText(SavePath);
            var data = Deserialize(json);
            Debug.Log($"[SaveManager] 已读取角色存档 ← {SavePath}");
            return data;
        }

        public static bool HasSave => File.Exists(SavePath);

        public static void DeleteSave()
        {
            if (File.Exists(SavePath)) File.Delete(SavePath);
        }

        /// <summary>
        /// 反序列化后，按 itemId 重建物品模板引用；
        /// 按 characterClass 重建职业信息（selectedTemplate 内存引用在落盘场景缺失，由调用方按需指定）。
        /// </summary>
        private static void RebuildReferences(CharacterCustomizationData data)
        {
            if (data?.initialEquipment == null) return;
            foreach (var si in data.initialEquipment)
            {
                if (si != null && si.runtimeRef == null)
                    si.runtimeRef = ItemDatabase.Get(si.itemId);
            }
        }
    }
}
