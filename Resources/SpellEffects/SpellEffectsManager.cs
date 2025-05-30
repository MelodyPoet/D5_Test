using UnityEngine;

/// <summary>
/// 确保SpellEffects组件在场景中存在的管理器（Resources文件夹版本）
/// </summary>
public class ResourcesSpellEffectsManager : MonoBehaviour
{
    // 单例实例
    public static ResourcesSpellEffectsManager Instance { get; private set; }

    // SpellEffects组件引用
    private SpellEffects spellEffects;

    private void Awake()
    {
        // 单例模式
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("ResourcesSpellEffectsManager初始化成功");
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // 确保SpellEffects组件存在
        EnsureResourcesSpellEffectsExists();
    }

    private void Start()
    {
        // 再次确保SpellEffects组件存在（以防Awake中的创建失败）
        EnsureResourcesSpellEffectsExists();
    }

    /// <summary>
    /// 确保SpellEffects组件存在
    /// </summary>
    private void EnsureResourcesSpellEffectsExists()
    {
        // 首先检查是否已经有SpellEffects实例
        if (SpellEffects.Instance != null)
        {
            spellEffects = SpellEffects.Instance;
            Debug.Log("找到现有的SpellEffects实例");
            return;
        }

        // 尝试在场景中查找
        spellEffects = FindObjectOfType<SpellEffects>();
        if (spellEffects != null)
        {
            Debug.Log("在场景中找到SpellEffects组件");
            return;
        }

        // 如果没有找到，创建一个新的
        GameObject spellEffectsObj = new GameObject("SpellEffects");
        spellEffects = spellEffectsObj.AddComponent<SpellEffects>();
        Debug.Log("创建了新的SpellEffects组件");

        // 初始化预制体
        InitializeResourcesSpellPrefabs();
    }

    /// <summary>
    /// 初始化法术预制体
    /// </summary>
    private void InitializeResourcesSpellPrefabs()
    {
        if (spellEffects == null) return;

        // 尝试从Resources加载奥术冲击预制体
        GameObject arcaneBlastPrefab = Resources.Load<GameObject>("SpellEffects/ArcaneBlast");
        if (arcaneBlastPrefab != null)
        {
            spellEffects.arcaneBlastPrefab = arcaneBlastPrefab;
            Debug.Log("从Resources加载奥术冲击预制体成功");
        }
        else
        {
            Debug.LogWarning("无法从Resources加载奥术冲击预制体，创建一个简单的替代预制体");

            // 创建一个简单的替代预制体
            GameObject simplePrefab = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            simplePrefab.name = "SimpleArcaneBlast";
            simplePrefab.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);

            // 添加粒子系统
            ParticleSystem ps = simplePrefab.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startColor = Color.blue;
            main.startSize = 0.2f;
            main.startSpeed = 2f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            // 设置为预制体
            spellEffects.arcaneBlastPrefab = simplePrefab;
            Debug.Log("创建了简单的替代奥术冲击预制体");
        }
    }

    /// <summary>
    /// 获取SpellEffects组件
    /// </summary>
    public SpellEffects GetSpellEffects()
    {
        // 如果spellEffects为null，尝试重新获取
        if (spellEffects == null)
        {
            EnsureResourcesSpellEffectsExists();
        }

        return spellEffects;
    }
}
