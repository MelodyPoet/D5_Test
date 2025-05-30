using UnityEngine;

/// <summary>
/// 奥术冲击法术效果组件
/// </summary>
public class ArcaneBlastEffect : MonoBehaviour
{
    [Header("效果设置")]
    public float lifetime = 2f;
    public float speed = 10f;
    public Color effectColor = Color.blue;

    private ParticleSystem particleSystem;
    private TrailRenderer trailRenderer;

    private void Awake()
    {
        // 确保有粒子系统
        particleSystem = GetComponent<ParticleSystem>();
        if (particleSystem == null)
        {
            particleSystem = gameObject.AddComponent<ParticleSystem>();
            SetupParticleSystem();
        }

        // 添加拖尾效果
        trailRenderer = GetComponent<TrailRenderer>();
        if (trailRenderer == null)
        {
            trailRenderer = gameObject.AddComponent<TrailRenderer>();
            SetupTrailRenderer();
        }

        // 设置自动销毁
        Destroy(gameObject, lifetime);
    }

    /// <summary>
    /// 设置粒子系统
    /// </summary>
    private void SetupParticleSystem()
    {
        if (particleSystem == null) return;

        var main = particleSystem.main;
        main.startColor = effectColor;
        main.startSize = 0.3f;
        main.startSpeed = speed;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.duration = lifetime;

        // 发射器设置
        var emission = particleSystem.emission;
        emission.rateOverTime = 20;

        // 形状设置
        var shape = particleSystem.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.1f;

        // 颜色随时间变化
        var colorOverLifetime = particleSystem.colorOverLifetime;
        colorOverLifetime.enabled = true;
        
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { new GradientColorKey(effectColor, 0.0f), new GradientColorKey(Color.cyan, 1.0f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(0.0f, 1.0f) }
        );
        
        colorOverLifetime.color = gradient;

        // 大小随时间变化
        var sizeOverLifetime = particleSystem.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1.0f, 0.1f);

        // 播放粒子系统
        particleSystem.Play();
    }

    /// <summary>
    /// 设置拖尾渲染器
    /// </summary>
    private void SetupTrailRenderer()
    {
        if (trailRenderer == null) return;

        trailRenderer.startWidth = 0.2f;
        trailRenderer.endWidth = 0.05f;
        trailRenderer.time = 0.5f;
        
        // 设置材质
        trailRenderer.material = new Material(Shader.Find("Sprites/Default"));
        trailRenderer.material.color = effectColor;

        // 设置颜色渐变
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { new GradientColorKey(effectColor, 0.0f), new GradientColorKey(Color.cyan, 1.0f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(0.0f, 1.0f) }
        );
        
        trailRenderer.colorGradient = gradient;
    }
}
