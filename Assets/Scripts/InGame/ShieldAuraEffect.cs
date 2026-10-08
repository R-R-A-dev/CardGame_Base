using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

/// <summary>
/// Image をシェーダでオーラのように発光させる。
/// マテリアルアセットは作らず、IMaterialModifier で実行時生成したマテリアルを差し込む。
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(Graphic))]
public class ShieldAuraEffect : MonoBehaviour, IMaterialModifier
{
    [SerializeField] Shader auraShader;

    [Header("色")]
    [ColorUsage(true, true)]
    [SerializeField] Color auraColor = new Color(0.25f, 1f, 0.5f, 1f);
    [SerializeField, Range(0f, 4f)] float coreIntensity = 1.2f;

    [Header("グロー")]
    [SerializeField, Range(0f, 0.25f)] float glowWidth = 0.06f;
    [SerializeField, Range(0f, 8f)] float glowIntensity = 2.5f;
    [SerializeField, Range(0.2f, 6f)] float glowFalloff = 1.8f;
    [SerializeField, Range(0f, 0.1f)] float rimWidth = 0.018f;
    [SerializeField, Range(0f, 8f)] float rimIntensity = 2f;

    [Header("アニメーション")]
    [SerializeField, Range(0f, 10f)] float pulseSpeed = 2.2f;
    [SerializeField, Range(0f, 1f)] float pulseAmount = 0.35f;
    [SerializeField, Range(0.5f, 30f)] float noiseScale = 7f;
    [SerializeField, Range(0f, 1f)] float noiseAmount = 0.55f;
    [SerializeField] Vector2 flowSpeed = new Vector2(0.06f, -0.35f);
    [SerializeField, Range(0f, 0.08f)] float distortion = 0.012f;

    [Header("全体")]
    [SerializeField, Range(0f, 1f)] float alpha = 1f;
    [SerializeField] bool additive = true;

    static readonly int AuraColorId = Shader.PropertyToID("_AuraColor");
    static readonly int CoreIntensityId = Shader.PropertyToID("_CoreIntensity");
    static readonly int GlowWidthId = Shader.PropertyToID("_GlowWidth");
    static readonly int GlowIntensityId = Shader.PropertyToID("_GlowIntensity");
    static readonly int GlowFalloffId = Shader.PropertyToID("_GlowFalloff");
    static readonly int RimWidthId = Shader.PropertyToID("_RimWidth");
    static readonly int RimIntensityId = Shader.PropertyToID("_RimIntensity");
    static readonly int PulseSpeedId = Shader.PropertyToID("_PulseSpeed");
    static readonly int PulseAmountId = Shader.PropertyToID("_PulseAmount");
    static readonly int NoiseScaleId = Shader.PropertyToID("_NoiseScale");
    static readonly int NoiseAmountId = Shader.PropertyToID("_NoiseAmount");
    static readonly int FlowSpeedId = Shader.PropertyToID("_FlowSpeed");
    static readonly int DistortionId = Shader.PropertyToID("_Distortion");
    static readonly int AlphaId = Shader.PropertyToID("_Alpha");
    static readonly int AspectId = Shader.PropertyToID("_Aspect");
    static readonly int SrcBlendId = Shader.PropertyToID("_SrcBlend");
    static readonly int DstBlendId = Shader.PropertyToID("_DstBlend");

    // Mask / RectMask2D が baseMaterial に書き込むステンシル設定
    static readonly int[] StencilIds =
    {
        Shader.PropertyToID("_Stencil"),
        Shader.PropertyToID("_StencilComp"),
        Shader.PropertyToID("_StencilOp"),
        Shader.PropertyToID("_StencilReadMask"),
        Shader.PropertyToID("_StencilWriteMask"),
        Shader.PropertyToID("_ColorMask"),
    };

    Graphic graphic;
    Material auraMaterial;

    Graphic TargetGraphic
    {
        get
        {
            if (graphic == null) graphic = GetComponent<Graphic>();
            return graphic;
        }
    }

    /// <summary>オーラの明るさをまとめて変えたいとき用。</summary>
    public void SetAlpha(float value)
    {
        alpha = Mathf.Clamp01(value);
        if (auraMaterial != null) auraMaterial.SetFloat(AlphaId, alpha);
    }

    void OnEnable()
    {
        if (TargetGraphic != null) TargetGraphic.SetMaterialDirty();
    }

    void OnDisable()
    {
        if (TargetGraphic != null) TargetGraphic.SetMaterialDirty();
        DestroyMaterial();
    }

    void OnDestroy()
    {
        DestroyMaterial();
    }

    void OnRectTransformDimensionsChange()
    {
        if (isActiveAndEnabled && TargetGraphic != null) TargetGraphic.SetMaterialDirty();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (TargetGraphic != null) TargetGraphic.SetMaterialDirty();
    }
#endif

    public Material GetModifiedMaterial(Material baseMaterial)
    {
        if (!isActiveAndEnabled || auraShader == null) return baseMaterial;

        if (auraMaterial == null)
        {
            auraMaterial = new Material(auraShader) { hideFlags = HideFlags.HideAndDontSave };
        }
        Apply(baseMaterial);
        return auraMaterial;
    }

    void Apply(Material baseMaterial)
    {
        foreach (int id in StencilIds)
        {
            if (baseMaterial.HasProperty(id)) auraMaterial.SetFloat(id, baseMaterial.GetFloat(id));
        }

        auraMaterial.SetColor(AuraColorId, auraColor);
        auraMaterial.SetFloat(CoreIntensityId, coreIntensity);
        auraMaterial.SetFloat(GlowWidthId, glowWidth);
        auraMaterial.SetFloat(GlowIntensityId, glowIntensity);
        auraMaterial.SetFloat(GlowFalloffId, glowFalloff);
        auraMaterial.SetFloat(RimWidthId, rimWidth);
        auraMaterial.SetFloat(RimIntensityId, rimIntensity);
        auraMaterial.SetFloat(PulseSpeedId, pulseSpeed);
        auraMaterial.SetFloat(PulseAmountId, pulseAmount);
        auraMaterial.SetFloat(NoiseScaleId, noiseScale);
        auraMaterial.SetFloat(NoiseAmountId, noiseAmount);
        auraMaterial.SetVector(FlowSpeedId, flowSpeed);
        auraMaterial.SetFloat(DistortionId, distortion);
        auraMaterial.SetFloat(AlphaId, alpha);
        auraMaterial.SetFloat(AspectId, CalcAspect());
        auraMaterial.SetFloat(SrcBlendId, (float)BlendMode.One);
        auraMaterial.SetFloat(DstBlendId, (float)(additive ? BlendMode.One : BlendMode.OneMinusSrcAlpha));
    }

    float CalcAspect()
    {
        RectTransform rect = transform as RectTransform;
        if (rect == null) return 1f;
        float height = rect.rect.height;
        if (height <= 0.0001f) return 1f;
        return rect.rect.width / height;
    }

    void DestroyMaterial()
    {
        if (auraMaterial == null) return;
        if (Application.isPlaying) Destroy(auraMaterial);
        else DestroyImmediate(auraMaterial);
        auraMaterial = null;
    }
}
