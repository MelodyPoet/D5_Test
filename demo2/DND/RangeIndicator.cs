using UnityEngine;

public class RangeIndicator : MonoBehaviour
{
    [Header("范围设置")]
    public SpriteRenderer rangeSprite;
    public Color movementRangeColor = new Color(0, 1, 0, 0.2f); // 绿色半透明
    public Color attackRangeColor = new Color(1, 0, 0, 0.2f);   // 红色半透明
    public Color validTargetColor = new Color(1, 1, 0, 0.5f);   // 黄色半透明

    [Header("动画设置")]
    public float pulseDuration = 1.0f;
    public float pulseMinAlpha = 0.1f;
    public float pulseMaxAlpha = 0.3f;

    private float currentTime = 0f;
    private RangeType currentRangeType = RangeType.Movement;

    public enum RangeType
    {
        Movement,
        Attack,
        ValidTarget
    }

    private void Awake()
    {
        if (rangeSprite == null)
        {
            rangeSprite = GetComponent<SpriteRenderer>();
        }

        // 设置范围指示器的排序顺序，确保可见但不会挡住角色和怪物的高亮效果
        if (rangeSprite != null)
        {
            // 使用较小的负数，确保在角色/怪物后面但仍然可见
            rangeSprite.sortingOrder = -1;
        }
    }

    private void Start()
    {
        // 默认设置为移动范围
        SetRangeType(RangeType.Movement);
    }

    private void Update()
    {
        // 实现脉冲效果
        currentTime += Time.deltaTime;
        if (currentTime > pulseDuration)
        {
            currentTime = 0f;
        }

        float t = currentTime / pulseDuration;
        float alpha = Mathf.Lerp(pulseMinAlpha, pulseMaxAlpha, Mathf.PingPong(t * 2, 1));

        Color color = rangeSprite.color;
        color.a = alpha;
        rangeSprite.color = color;
    }

    // 设置范围类型
    public void SetRangeType(RangeType type)
    {
        currentRangeType = type;

        switch (type)
        {
            case RangeType.Movement:
                rangeSprite.color = movementRangeColor;
                break;
            case RangeType.Attack:
                rangeSprite.color = attackRangeColor;
                break;
            case RangeType.ValidTarget:
                rangeSprite.color = validTargetColor;
                break;
        }
    }

    // 设置范围大小
    public void SetSize(float size)
    {
        transform.localScale = new Vector3(size, size, 1);
    }
}
