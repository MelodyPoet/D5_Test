using UnityEngine;

namespace demo2.DND.InventoryTetris
{
    /// <summary>
    /// 右键菜单提供者：挂在任意场景对象上，在 Inspector 中指向“右键菜单预制体”。
    /// ItemContextMenu 会在场景中查找此 Provider 来实例化菜单。
    /// </summary>
    public class ItemContextMenuProvider : MonoBehaviour
    {
        [Header("右键菜单预制体（必填，任意Prefab GameObject）")]
        public GameObject menuPrefab;

        private static ItemContextMenuProvider _cached;

        public static ItemContextMenuProvider GetOrFind()
        {
            if (_cached != null) return _cached;
            _cached = FindObjectOfType<ItemContextMenuProvider>(true);
            return _cached;
        }
    }
}
