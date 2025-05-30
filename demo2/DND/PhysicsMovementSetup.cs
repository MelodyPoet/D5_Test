using UnityEngine;

/// <summary>
/// 物理移动系统设置助手
/// 自动配置场景中的物理移动验证系统
/// </summary>
public class PhysicsMovementSetup : MonoBehaviour
{
    [Header("自动设置")]
    [Tooltip("是否在Start时自动设置系统")]
    public bool autoSetupOnStart = true;

    [Tooltip("是否自动检测角色碰撞体")]
    public bool autoDetectCharacterColliders = true;

    [Tooltip("默认角色碰撞半径")]
    public float defaultCharacterRadius = 0.5f;

    [Header("物理设置")]
    [Tooltip("阻挡物层级")]
    public LayerMask blockingLayers = -1;

    [Tooltip("使用2D物理")]
    public bool use2DPhysics = true;

    [Header("调试")]
    [Tooltip("显示调试信息")]
    public bool showDebugInfo = true;

    [Tooltip("显示调试射线")]
    public bool showDebugRays = false;

    private PhysicsMovementValidator validator;

    private void Start()
    {
        if (autoSetupOnStart)
        {
            SetupPhysicsMovementSystem();
        }
    }

    /// <summary>
    /// 设置物理移动系统
    /// </summary>
    [ContextMenu("设置物理移动系统")]
    public void SetupPhysicsMovementSystem()
    {
        // 1. 创建或获取PhysicsMovementValidator
        SetupValidator();

        // 2. 自动检测和配置角色碰撞体
        if (autoDetectCharacterColliders)
        {
            SetupCharacterColliders();
        }

        // 3. 检查阻挡物设置
        CheckBlockingObjects();

        // 4. 验证系统设置
        ValidateSystemSetup();
    }

    private void SetupValidator()
    {
        // 检查是否已经有PhysicsMovementValidator
        validator = FindObjectOfType<PhysicsMovementValidator>();

        if (validator == null)
        {
            // 创建新的验证器
            GameObject validatorObj = new GameObject("PhysicsMovementValidator");
            validator = validatorObj.AddComponent<PhysicsMovementValidator>();
            Debug.Log("创建了PhysicsMovementValidator");
        }
        else
        {
            Debug.Log("找到现有的PhysicsMovementValidator");
        }

        // 配置验证器设置
        validator.characterRadius = defaultCharacterRadius;
        validator.blockingLayers = blockingLayers;
        validator.use2DPhysics = use2DPhysics;
        validator.showDebugRays = showDebugRays;

        Debug.Log($"配置验证器: 半径={defaultCharacterRadius}, 2D物理={use2DPhysics}");
    }

    private void SetupCharacterColliders()
    {
        Debug.Log("检测角色碰撞体...");

        // 查找所有角色
        CharacterStats[] characters = FindObjectsOfType<CharacterStats>();
        int configuredCount = 0;

        foreach (var character in characters)
        {
            bool hasCollider = false;

            if (use2DPhysics)
            {
                // 检查2D碰撞体
                Collider2D col2D = character.GetComponent<Collider2D>();
                if (col2D == null)
                {
                    // 添加2D碰撞体
                    CircleCollider2D circleCol = character.gameObject.AddComponent<CircleCollider2D>();
                    circleCol.radius = defaultCharacterRadius;
                    circleCol.isTrigger = false;
                    Debug.Log($"为 {character.name} 添加了CircleCollider2D");
                    hasCollider = true;
                }
                else
                {
                    hasCollider = true;
                    Debug.Log($"{character.name} 已有2D碰撞体: {col2D.GetType().Name}");
                }
            }
            else
            {
                // 检查3D碰撞体
                Collider col3D = character.GetComponent<Collider>();
                if (col3D == null)
                {
                    // 添加3D碰撞体
                    CapsuleCollider capsuleCol = character.gameObject.AddComponent<CapsuleCollider>();
                    capsuleCol.radius = defaultCharacterRadius;
                    capsuleCol.height = 2f;
                    capsuleCol.isTrigger = false;
                    Debug.Log($"为 {character.name} 添加了CapsuleCollider");
                    hasCollider = true;
                }
                else
                {
                    hasCollider = true;
                    Debug.Log($"{character.name} 已有3D碰撞体: {col3D.GetType().Name}");
                }
            }

            if (hasCollider)
            {
                configuredCount++;
            }
        }

        Debug.Log($"配置了 {configuredCount} 个角色的碰撞体");
    }

    private void CheckBlockingObjects()
    {
        MovementBlocker[] blockers = FindObjectsOfType<MovementBlocker>();

        if (blockers.Length == 0)
        {
            Debug.LogWarning("场景中没有找到MovementBlocker组件！");
            Debug.LogWarning("请使用 'DND Tools > 移动阻挡器创建工具' 创建阻挡区域");
        }
        else
        {
            Debug.Log($"找到 {blockers.Length} 个移动阻挡器:");
            foreach (var blocker in blockers)
            {
                Debug.Log($"- {blocker.name} ({blocker.blockerType})");

                // 检查碰撞体
                bool hasCollider = blocker.GetComponent<Collider2D>() != null || blocker.GetComponent<Collider>() != null;
                if (!hasCollider)
                {
                    Debug.LogWarning($"阻挡器 {blocker.name} 没有碰撞体！");
                }
            }
        }
    }

    private void ValidateSystemSetup()
    {
        bool isValid = true;

        // 检查PhysicsMovementValidator
        if (PhysicsMovementValidator.Instance == null)
        {
            Debug.LogError("PhysicsMovementValidator.Instance 为空！");
            isValid = false;
        }

        // 检查角色
        CharacterStats[] characters = FindObjectsOfType<CharacterStats>();
        if (characters.Length == 0)
        {
            Debug.LogWarning("场景中没有找到角色！");
        }

        // 检查阻挡物
        MovementBlocker[] blockers = FindObjectsOfType<MovementBlocker>();
        if (blockers.Length == 0)
        {
            Debug.LogWarning("场景中没有阻挡物，角色可以自由移动到任何位置");
        }

        if (isValid)
        {
            Debug.Log("✓ 系统设置验证通过！");
        }
        else
        {
            Debug.LogError("✗ 系统设置验证失败！");
        }
    }

    /// <summary>
    /// 测试移动验证
    /// </summary>
    [ContextMenu("测试移动验证")]
    public void TestMovementValidation()
    {
        if (PhysicsMovementValidator.Instance == null)
        {
            Debug.LogError("PhysicsMovementValidator未初始化！");
            return;
        }

        Vector3 testPos = transform.position;
        bool isValid = PhysicsMovementValidator.Instance.IsPositionValid(testPos);

        Debug.Log($"测试位置 {testPos} 的有效性: {(isValid ? "有效" : "无效")}");

        if (!isValid)
        {
            Vector3 nearestValid = PhysicsMovementValidator.Instance.GetNearestValidPosition(testPos, testPos);
            Debug.Log($"最近的有效位置: {nearestValid}");
        }
    }

    /// <summary>
    /// 创建测试阻挡区域
    /// </summary>
    [ContextMenu("创建测试阻挡区域")]
    public void CreateTestBlockingArea()
    {
        // 在当前位置创建一个测试阻挡区域
        GameObject blocker = new GameObject("TestBlocker");
        blocker.transform.position = transform.position + Vector3.right * 2f;

        MovementBlocker blockerComponent = blocker.AddComponent<MovementBlocker>();
        blockerComponent.blockerType = MovementBlocker.BlockerType.Obstacle;
        blockerComponent.description = "测试阻挡区域";

        if (use2DPhysics)
        {
            BoxCollider2D col = blocker.AddComponent<BoxCollider2D>();
            col.size = new Vector2(2f, 2f);
        }
        else
        {
            BoxCollider col = blocker.AddComponent<BoxCollider>();
            col.size = new Vector3(2f, 2f, 1f);
        }

        blocker.tag = "MovementBlocker";

        Debug.Log("创建了测试阻挡区域");
    }

    private void OnGUI()
    {
        if (!showDebugInfo) return;

        GUILayout.BeginArea(new Rect(10, 10, 300, 150));
        GUILayout.BeginVertical("box");

        GUILayout.Label("物理移动系统状态");

        bool validatorExists = PhysicsMovementValidator.Instance != null;
        GUILayout.Label($"验证器: {(validatorExists ? "✓ 已初始化" : "✗ 未初始化")}");

        int blockerCount = FindObjectsOfType<MovementBlocker>().Length;
        GUILayout.Label($"阻挡器数量: {blockerCount}");

        int characterCount = FindObjectsOfType<CharacterStats>().Length;
        GUILayout.Label($"角色数量: {characterCount}");

        if (GUILayout.Button("重新设置系统"))
        {
            SetupPhysicsMovementSystem();
        }

        GUILayout.EndVertical();
        GUILayout.EndArea();
    }
}
