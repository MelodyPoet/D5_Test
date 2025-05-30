#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

/// <summary>
/// 移动阻挡器创建工具
/// 提供快速创建各种阻挡区域的编辑器工具
/// </summary>
public class MovementBlockerCreator : EditorWindow
{
    private MovementBlocker.BlockerType selectedType = MovementBlocker.BlockerType.Wall;
    private Vector3 blockerSize = new Vector3(2f, 2f, 1f);
    private bool use2DCollider = true;
    private string blockerName = "MovementBlocker";
    private Color blockerColor = Color.red;

    [MenuItem("DND Tools/移动阻挡器创建工具")]
    public static void ShowWindow()
    {
        MovementBlockerCreator window = GetWindow<MovementBlockerCreator>("阻挡器创建工具");
        window.Show();
    }

    private void OnGUI()
    {
        GUILayout.Label("移动阻挡器创建工具", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // 阻挡器类型
        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("阻挡器设置", EditorStyles.boldLabel);
        selectedType = (MovementBlocker.BlockerType)EditorGUILayout.EnumPopup("阻挡器类型", selectedType);
        blockerName = EditorGUILayout.TextField("名称", blockerName);
        blockerColor = EditorGUILayout.ColorField("颜色", blockerColor);
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space();

        // 碰撞体设置
        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("碰撞体设置", EditorStyles.boldLabel);
        use2DCollider = EditorGUILayout.Toggle("使用2D碰撞体", use2DCollider);
        blockerSize = EditorGUILayout.Vector3Field("大小", blockerSize);
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space();

        // 快速创建按钮
        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("快速创建", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("创建墙壁"))
        {
            CreateBlocker(MovementBlocker.BlockerType.Wall, "Wall", new Vector3(1f, 3f, 1f));
        }
        if (GUILayout.Button("创建边界"))
        {
            CreateBlocker(MovementBlocker.BlockerType.Boundary, "Boundary", new Vector3(10f, 1f, 1f));
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("创建障碍物"))
        {
            CreateBlocker(MovementBlocker.BlockerType.Obstacle, "Obstacle", new Vector3(2f, 2f, 1f));
        }
        if (GUILayout.Button("创建危险区域"))
        {
            CreateBlocker(MovementBlocker.BlockerType.DeathZone, "DeathZone", new Vector3(3f, 3f, 1f));
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();

        EditorGUILayout.Space();

        // 自定义创建
        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("自定义创建", EditorStyles.boldLabel);
        if (GUILayout.Button("创建自定义阻挡器", GUILayout.Height(30)))
        {
            CreateBlocker(selectedType, blockerName, blockerSize);
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space();

        // 批量操作
        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("批量操作", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("创建房间边界"))
        {
            CreateRoomBoundaries();
        }
        if (GUILayout.Button("创建屏幕边界"))
        {
            CreateScreenBoundaries();
        }
        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("为选中对象添加阻挡器"))
        {
            AddBlockerToSelectedObjects();
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space();

        // 帮助信息
        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("使用说明", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "1. 选择阻挡器类型和大小\n" +
            "2. 点击创建按钮\n" +
            "3. 在Scene视图中调整位置\n" +
            "4. 阻挡器会自动阻止角色移动", 
            MessageType.Info);
        EditorGUILayout.EndVertical();
    }

    private void CreateBlocker(MovementBlocker.BlockerType type, string name, Vector3 size)
    {
        // 创建游戏对象
        GameObject blocker = new GameObject(name);
        
        // 设置位置（在Scene视图中心或选中对象位置）
        if (Selection.activeTransform != null)
        {
            blocker.transform.position = Selection.activeTransform.position;
        }
        else if (SceneView.lastActiveSceneView != null)
        {
            blocker.transform.position = SceneView.lastActiveSceneView.camera.transform.position;
            blocker.transform.position = new Vector3(blocker.transform.position.x, blocker.transform.position.y, 0);
        }

        // 添加MovementBlocker组件
        MovementBlocker blockerComponent = blocker.AddComponent<MovementBlocker>();
        blockerComponent.blockerType = type;
        blockerComponent.visualizationColor = blockerColor;
        
        // 根据类型设置描述
        switch (type)
        {
            case MovementBlocker.BlockerType.Wall:
                blockerComponent.description = "墙壁";
                break;
            case MovementBlocker.BlockerType.Boundary:
                blockerComponent.description = "边界";
                break;
            case MovementBlocker.BlockerType.Obstacle:
                blockerComponent.description = "障碍物";
                break;
            case MovementBlocker.BlockerType.DeathZone:
                blockerComponent.description = "危险区域";
                break;
            default:
                blockerComponent.description = "阻挡区域";
                break;
        }

        // 添加碰撞体
        if (use2DCollider)
        {
            BoxCollider2D collider = blocker.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(size.x, size.y);
        }
        else
        {
            BoxCollider collider = blocker.AddComponent<BoxCollider>();
            collider.size = size;
        }

        // 设置标签
        blocker.tag = "MovementBlocker";

        // 选中新创建的对象
        Selection.activeGameObject = blocker;

        // 记录撤销操作
        Undo.RegisterCreatedObjectUndo(blocker, "Create Movement Blocker");

        Debug.Log($"创建了 {type} 类型的移动阻挡器: {name}");
    }

    private void CreateRoomBoundaries()
    {
        // 创建房间的四面墙
        CreateBlocker(MovementBlocker.BlockerType.Boundary, "LeftWall", new Vector3(1f, 10f, 1f));
        CreateBlocker(MovementBlocker.BlockerType.Boundary, "RightWall", new Vector3(1f, 10f, 1f));
        CreateBlocker(MovementBlocker.BlockerType.Boundary, "TopWall", new Vector3(20f, 1f, 1f));
        CreateBlocker(MovementBlocker.BlockerType.Boundary, "BottomWall", new Vector3(20f, 1f, 1f));

        // 自动排列位置
        GameObject[] walls = { 
            GameObject.Find("LeftWall"), 
            GameObject.Find("RightWall"), 
            GameObject.Find("TopWall"), 
            GameObject.Find("BottomWall") 
        };

        if (walls[0] != null) walls[0].transform.position = new Vector3(-10f, 0f, 0f);
        if (walls[1] != null) walls[1].transform.position = new Vector3(10f, 0f, 0f);
        if (walls[2] != null) walls[2].transform.position = new Vector3(0f, 5f, 0f);
        if (walls[3] != null) walls[3].transform.position = new Vector3(0f, -5f, 0f);

        Debug.Log("创建了房间边界");
    }

    private void CreateScreenBoundaries()
    {
        if (Camera.main == null)
        {
            EditorUtility.DisplayDialog("错误", "场景中没有找到主摄像机", "确定");
            return;
        }

        Camera cam = Camera.main;
        float camHeight = 2f * cam.orthographicSize;
        float camWidth = camHeight * cam.aspect;
        Vector3 camPos = cam.transform.position;

        // 创建屏幕边界
        float wallThickness = 1f;
        
        // 左边界
        GameObject leftBoundary = new GameObject("LeftScreenBoundary");
        leftBoundary.transform.position = new Vector3(camPos.x - camWidth/2 - wallThickness/2, camPos.y, 0);
        MovementBlocker leftBlocker = leftBoundary.AddComponent<MovementBlocker>();
        leftBlocker.blockerType = MovementBlocker.BlockerType.Boundary;
        leftBlocker.description = "屏幕左边界";
        if (use2DCollider)
        {
            BoxCollider2D col = leftBoundary.AddComponent<BoxCollider2D>();
            col.size = new Vector2(wallThickness, camHeight + wallThickness * 2);
        }
        leftBoundary.tag = "MovementBlocker";

        // 右边界
        GameObject rightBoundary = new GameObject("RightScreenBoundary");
        rightBoundary.transform.position = new Vector3(camPos.x + camWidth/2 + wallThickness/2, camPos.y, 0);
        MovementBlocker rightBlocker = rightBoundary.AddComponent<MovementBlocker>();
        rightBlocker.blockerType = MovementBlocker.BlockerType.Boundary;
        rightBlocker.description = "屏幕右边界";
        if (use2DCollider)
        {
            BoxCollider2D col = rightBoundary.AddComponent<BoxCollider2D>();
            col.size = new Vector2(wallThickness, camHeight + wallThickness * 2);
        }
        rightBoundary.tag = "MovementBlocker";

        // 上边界
        GameObject topBoundary = new GameObject("TopScreenBoundary");
        topBoundary.transform.position = new Vector3(camPos.x, camPos.y + camHeight/2 + wallThickness/2, 0);
        MovementBlocker topBlocker = topBoundary.AddComponent<MovementBlocker>();
        topBlocker.blockerType = MovementBlocker.BlockerType.Boundary;
        topBlocker.description = "屏幕上边界";
        if (use2DCollider)
        {
            BoxCollider2D col = topBoundary.AddComponent<BoxCollider2D>();
            col.size = new Vector2(camWidth + wallThickness * 2, wallThickness);
        }
        topBoundary.tag = "MovementBlocker";

        // 下边界
        GameObject bottomBoundary = new GameObject("BottomScreenBoundary");
        bottomBoundary.transform.position = new Vector3(camPos.x, camPos.y - camHeight/2 - wallThickness/2, 0);
        MovementBlocker bottomBlocker = bottomBoundary.AddComponent<MovementBlocker>();
        bottomBlocker.blockerType = MovementBlocker.BlockerType.Boundary;
        bottomBlocker.description = "屏幕下边界";
        if (use2DCollider)
        {
            BoxCollider2D col = bottomBoundary.AddComponent<BoxCollider2D>();
            col.size = new Vector2(camWidth + wallThickness * 2, wallThickness);
        }
        bottomBoundary.tag = "MovementBlocker";

        Debug.Log("创建了屏幕边界");
    }

    private void AddBlockerToSelectedObjects()
    {
        if (Selection.gameObjects.Length == 0)
        {
            EditorUtility.DisplayDialog("提示", "请先选择要添加阻挡器的对象", "确定");
            return;
        }

        foreach (GameObject obj in Selection.gameObjects)
        {
            if (obj.GetComponent<MovementBlocker>() == null)
            {
                MovementBlocker blocker = obj.AddComponent<MovementBlocker>();
                blocker.blockerType = selectedType;
                blocker.visualizationColor = blockerColor;
                
                // 如果没有碰撞体，添加一个
                if (obj.GetComponent<Collider2D>() == null && obj.GetComponent<Collider>() == null)
                {
                    if (use2DCollider)
                    {
                        obj.AddComponent<BoxCollider2D>();
                    }
                    else
                    {
                        obj.AddComponent<BoxCollider>();
                    }
                }
                
                obj.tag = "MovementBlocker";
                
                Undo.RegisterCompleteObjectUndo(obj, "Add Movement Blocker");
            }
        }

        Debug.Log($"为 {Selection.gameObjects.Length} 个对象添加了移动阻挡器");
    }
}
#endif
