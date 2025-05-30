using UnityEngine;

/// <summary>
/// 确保SpellEffects组件在场景中存在的管理器
/// </summary>
public class SpellEffectsManager : MonoBehaviour
{
    // 单例实例
    public static SpellEffectsManager _instance;
    public static SpellEffectsManager Instance
    {
        get { return _instance; }
        private set { _instance = value; }
    }

    // SpellEffects组件引用
    private SpellEffects _spellEffects;

    private void Awake()
    {
        // 单例模式
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // 确保SpellEffects组件存在
        EnsureSpellEffectsExists();
    }

    private void Start()
    {
        // 再次确保SpellEffects组件存在（以防Awake中的创建失败）
        EnsureSpellEffectsExists();
    }

    /// <summary>
    /// 确保SpellEffects组件存在
    /// </summary>
    private void EnsureSpellEffectsExists()
    {
        // 首先检查是否已经有SpellEffects实例
        if (SpellEffects.Instance != null)
        {
            _spellEffects = SpellEffects.Instance;
            Debug.Log("找到现有的SpellEffects实例");
            return;
        }

        // 尝试在场景中查找
        _spellEffects = FindObjectOfType<SpellEffects>();
        if (_spellEffects != null)
        {
            Debug.Log("在场景中找到SpellEffects组件");
            return;
        }

        // 如果没有找到，创建一个新的
        GameObject spellEffectsObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        spellEffectsObj.name = "SpellEffects";
        // 隐藏渲染器，我们只需要组件
        Renderer renderer = spellEffectsObj.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.enabled = false;
        }
        // 禁用碰撞器
        Collider collider = spellEffectsObj.GetComponent<Collider>();
        if (collider != null)
        {
            collider.enabled = false;
        }

        _spellEffects = spellEffectsObj.AddComponent<SpellEffects>();
        Debug.Log("创建了新的SpellEffects组件");

        // 不再从Resources加载预制体，而是使用场景中已有的SpellEffects对象
        Debug.Log("使用场景中已有的SpellEffects对象上注册的法术预制体");
    }

    /// <summary>
    /// 获取SpellEffects组件
    /// </summary>
    public SpellEffects GetSpellEffects()
    {
        // 如果_spellEffects为null，尝试重新获取
        if (_spellEffects == null)
        {
            EnsureSpellEffectsExists();
        }

        return _spellEffects;
    }
}
