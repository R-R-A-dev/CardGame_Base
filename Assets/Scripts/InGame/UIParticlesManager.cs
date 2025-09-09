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
        UIParticle uiParticle = transform.GetComponentInChildren<UIParticle>();

        return uiParticle;
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