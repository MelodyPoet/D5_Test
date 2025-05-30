using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 自定义GraphicRaycaster，允许射线穿透特定UI元素
/// </summary>
public class PassThroughGraphicRaycaster : GraphicRaycaster
{
    [Tooltip("允许射线穿透的UI元素标签")]
    public List<string> passThroughTags = new List<string>();

    [Tooltip("是否允许射线穿透所有UI元素")]
    public bool passThroughAll = false;

    [Tooltip("是否只在鼠标左键按下时穿透")]
    public bool passThroughOnlyOnMouseDown = true;

    [Tooltip("不应该被穿透的UI元素名称")]
    public List<string> neverPassThroughNames = new List<string>() { "ActionPanel", "AttackButton", "MoveButton", "DashButton", "DodgeButton", "SpellButton", "EndTurnButton" };

    public override void Raycast(PointerEventData eventData, List<RaycastResult> resultAppendList)
    {
        // 先执行正常的射线检测
        int originalCount = resultAppendList.Count;
        base.Raycast(eventData, resultAppendList);

        // 如果设置了只在鼠标左键按下时穿透，检查鼠标状态
        if (passThroughOnlyOnMouseDown && !Input.GetMouseButton(0))
        {
            // 鼠标左键没有按下，保留所有结果
            return;
        }

        // 检查是否应该穿透
        bool shouldPassThrough = passThroughAll;

        // 如果不应该穿透所有，则只过滤特定标签的元素
        if (!shouldPassThrough)
        {
            // 检查新增的结果是否应该被过滤
            for (int i = resultAppendList.Count - 1; i >= originalCount; i--)
            {
                RaycastResult result = resultAppendList[i];

                // 如果目标有标签且标签在穿透列表中，移除该结果
                if (result.gameObject != null &&
                    !string.IsNullOrEmpty(result.gameObject.tag) &&
                    passThroughTags.Contains(result.gameObject.tag))
                {
                    resultAppendList.RemoveAt(i);
                }
            }
        }
        else
        {
            // 如果应该穿透所有，但要保留特定名称的UI元素
            for (int i = resultAppendList.Count - 1; i >= originalCount; i--)
            {
                RaycastResult result = resultAppendList[i];

                // 检查是否是不应该被穿透的UI元素
                if (result.gameObject != null &&
                    (neverPassThroughNames.Contains(result.gameObject.name) ||
                     (result.gameObject.transform.parent != null && neverPassThroughNames.Contains(result.gameObject.transform.parent.name))))
                {
                    // 保留这个结果，不移除
                    continue;
                }

                // 其他元素都移除，实现穿透
                resultAppendList.RemoveAt(i);
            }
        }
    }
}
