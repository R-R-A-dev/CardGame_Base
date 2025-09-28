using Coffee.UIExtensions;
using UnityEngine;
using static UnityEngine.ParticleSystem;

public class UIParticlesManager : MonoBehaviour
{
    [SerializeField] UIParticle UIParticle;
    void Start()
    {

    }


    void Update()
    {

    }

    public UIParticle GetUIParticles()
    {
        return GetUIParticleWithNoChildren();
    }

    /// <summary>
    /// UIParticleの子オブジェクトで以下以外のものを取得する
    /// [generated] UIParticle BakingCamera
    /// UIParticleRendererコンポーネントがついたオブジェクト
    /// </summary>
    /// <returns></returns>
    public UIParticle GetUIParticleWithNoChildren()
    {
        UIParticle[] particles = transform.GetComponentsInChildren<UIParticle>(true);
        foreach (UIParticle p in particles)
        {
            bool hasGrandchildren = false; // 孫オブジェクトがあるかのフラグ

            // pの孫オブジェクトを取得
            Transform parentTransform = p.transform;
            for (int i = 0; i < parentTransform.childCount; i++)
            {
                Transform child = parentTransform.GetChild(i);
                // 自動生成オブジェクトをスキップ
                if (child.name.Contains("[generated]") ||
                    child.GetComponent<UIParticleRenderer>()) continue;

                // 孫オブジェクト（child の子）をチェック
                if (child.childCount > 0)
                {
                    hasGrandchildren = true; // 孫オブジェクトが存在
                    break; // 一つでも孫があったらループを抜ける
                }
            }

            // 孫オブジェクトがない場合のみそのUIParticleを返す
            if (!hasGrandchildren)
            {
                return p;
            }
        }
        return null; // 全てのUIParticleに孫オブジェクトがある場合
    }

    public UIParticle GetUIParticle()
    {
        return UIParticle;
    }
}

/* UIParticleをオブジェクトの子オブジェクトとして管理
 * 子オブジェクトに無かったら生成して追加
 * 生成後には非表示にして元の位置に戻す
 * 
*/