using Coffee.UIExtensions;
using System.Collections.Generic;
using UnityEditor.ShaderKeywordFilter;
using UnityEngine;

public class EffectController : MonoBehaviour
{

    [SerializeField] UIParticle UIParticleObj;
    [SerializeField] ParticleSystem effect;
    [SerializeField] ParticleSystem effect2;
    [SerializeField] GameObject canvas;
    UIParticle particle;
    [SerializeField] GameObject effectObj;
    
    [SerializeField] List<UIParticle> uIParticles = new List<UIParticle>();
    void Start()
    {
    
    }

    void Update()
    {
     
    }

    public Transform AttackEffect(ParticleSystem attackEffect,Transform cardTrans)
    {
        UIParticle UIParticleO = Instantiate(UIParticleObj, transform.parent.parent);
        ParticleSystem effecO = Instantiate(attackEffect);
        effecO.transform.SetParent(UIParticleO.transform);
        UIParticleO.particles.Add(effecO);
        UIParticleO.RefreshParticles();
        UIParticleO.transform.position = cardTrans.transform.position;
        effecO.transform.localPosition = Vector3.zero;
        return UIParticleO.transform;
    }

    public void HitEffect(ParticleSystem hitEffect, Transform targetTrans)
    {
        UIParticle UIParticleO = Instantiate(UIParticleObj, transform.parent.parent);
        ParticleSystem effecO = Instantiate(hitEffect);
        effecO.transform.SetParent(UIParticleO.transform);
        UIParticleO.particles.Add(effecO);
        UIParticleO.RefreshParticles();
        UIParticleO.transform.position = targetTrans.transform.position;
        effecO.transform.localPosition = Vector3.zero;
        Destroy(UIParticleO.gameObject, 0.5f);
    }

    public void DestroyEffect(ParticleSystem destroyEffect, Transform targetTrans)
    {
        UIParticle UIParticleO = Instantiate(UIParticleObj, transform.parent.parent);
        ParticleSystem effecO = Instantiate(destroyEffect);
        effecO.transform.SetParent(UIParticleO.transform);
        UIParticleO.particles.Add(effecO);
        UIParticleO.RefreshParticles();
        UIParticleO.transform.position = targetTrans.transform.position;
        effecO.transform.localPosition = Vector3.zero;
        Destroy(UIParticleO.gameObject, 0.5f);
    }

    public void SummonEffect(ParticleSystem summonEffect, Transform targetTrans)
    {
        UIParticle UIParticleO = Instantiate(UIParticleObj, this.transform);
        ParticleSystem effecO = Instantiate(summonEffect);
        effecO.transform.SetParent(UIParticleO.transform);
        UIParticleO.particles.Add(effecO);
        UIParticleO.RefreshParticles();
        UIParticleO.transform.position = targetTrans.transform.position;
        effecO.transform.localPosition = Vector3.zero;
        Destroy(UIParticleO.gameObject, 0.5f);
    }
}
