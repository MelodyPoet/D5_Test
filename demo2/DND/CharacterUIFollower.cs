using UnityEngine;

/// <summary>
/// 使UI元素跟随角色移动的组件
/// </summary>
public class CharacterUIFollower : MonoBehaviour
{
    public Transform targetCharacter; // 目标角色的Transform
    public Vector3 offset = new Vector3(0, 1.0f, 0); // 相对于角色的偏移量
    public bool updateInLateUpdate = true; // 是否在LateUpdate中更新位置
    public int updateFrequency = 1; // 更新频率，每隔多少帧更新一次

    private RectTransform rectTransform;
    private Canvas canvas;
    private Camera mainCamera;
    private int frameCount = 0;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        mainCamera = Camera.main;
    }

    private void Start()
    {
        if (targetCharacter == null)
        {
            Debug.LogError("CharacterUIFollower: 未设置目标角色Transform", this);
        }

        if (rectTransform == null)
        {
            Debug.LogError("CharacterUIFollower: 未找到RectTransform组件", this);
        }

        if (canvas == null)
        {
            Debug.LogError("CharacterUIFollower: 未找到父级Canvas", this);
        }

        // 立即更新一次位置
        UpdatePosition();
    }

    private void Update()
    {
        if (!updateInLateUpdate)
        {
            // 根据更新频率决定是否更新位置
            if (updateFrequency <= 1 || frameCount % updateFrequency == 0)
            {
                UpdatePosition();
            }
            frameCount++;
        }
    }

    private void LateUpdate()
    {
        if (updateInLateUpdate)
        {
            // 根据更新频率决定是否更新位置
            if (updateFrequency <= 1 || frameCount % updateFrequency == 0)
            {
                UpdatePosition();
            }
            frameCount++;
        }
    }

    /// <summary>
    /// 更新UI位置以跟随目标角色
    /// </summary>
    public void UpdatePosition()
    {
        if (targetCharacter == null || rectTransform == null || canvas == null)
            return;

        // 获取角色在世界空间中的位置
        Vector3 targetPosition = targetCharacter.position + offset;

        // 将世界坐标转换为屏幕坐标
        Vector3 screenPosition = mainCamera.WorldToScreenPoint(targetPosition);

        // 将屏幕坐标转换为Canvas中的本地坐标
        if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            // 对于ScreenSpaceOverlay模式，屏幕坐标就是Canvas坐标
            rectTransform.position = screenPosition;
        }
        else if (canvas.renderMode == RenderMode.ScreenSpaceCamera)
        {
            // 对于ScreenSpaceCamera模式，需要将屏幕坐标转换为Canvas中的坐标
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.transform as RectTransform,
                screenPosition,
                canvas.worldCamera,
                out localPoint);

            rectTransform.position = canvas.transform.TransformPoint(localPoint);
        }
        else if (canvas.renderMode == RenderMode.WorldSpace)
        {
            // 对于WorldSpace模式，直接设置位置
            rectTransform.position = targetPosition;
        }
    }
}
