using UnityEngine;

namespace demo2.DND.InventoryTetris
{
    /// <summary>
    /// 在场景中提供对 ItemContextMenu 预制体的引用，并负责其实例化。
    /// </summary>
    public class ItemContextMenuProvider : MonoBehaviour
    {
        public ItemContextMenu contextMenuPrefab;

        public ItemContextMenu CreateInstance()
        {
            if (contextMenuPrefab == null)
            {
                Debug.LogError("Context menu prefab is not assigned in ItemContextMenuProvider.");
                return null;
            }
            var instance = Instantiate(contextMenuPrefab, transform);
            ItemContextMenu.SetCurrent(instance);
            return instance;
        }
    }
}

