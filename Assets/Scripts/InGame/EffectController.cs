using Coffee.UIExtensions;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.ShaderKeywordFilter;
using UnityEngine;
using UnityEngine.UI;

public class EffectController : MonoBehaviour
{

    [SerializeField] UIParticle UIParticleObj;
    [SerializeField] ParticleSystem effect;
    [SerializeField] ParticleSystem effect2;
    [SerializeField] GameObject canvas;
    [SerializeField] GameObject effectObj;

    void Start()
    {

    }

    void Update()
    {

    }

    public Transform AttackEffect(ParticleSystem cardEffect, Transform cardTrans)
    {
        return UIParticlePool(cardEffect, cardTrans);
    }

    public Transform HitEffect(ParticleSystem hitEffect, Transform targetTrans)
    {
        return UIParticlePool(hitEffect, targetTrans);
    }

    public Transform DestroyEffect(ParticleSystem destroyEffect, Transform targetTrans)
    {
        return UIParticlePool(destroyEffect, targetTrans);
    }

    public Transform SummonEffect(ParticleSystem summonEffect, Transform targetTrans)
    {
        UIParticle getParticle = GameManager.instance.uiParticlesManager.GetUIParticles();
        UIParticle UIParticle;
        if (getParticle == null)
        {
            getParticle = GameManager.instance.uiParticlesManager.GetUIParticle();
            UIParticle = Instantiate(getParticle, transform);
        }
        else
        {
            UIParticle = getParticle;
            UIParticle.transform.SetParent(transform);
        }
        ParticleSystem effect = Instantiate(summonEffect);
        effect.transform.SetParent(UIParticle.transform);
        UIParticle.particles.Add(effect);
        UIParticle.RefreshParticles();
        UIParticle.transform.position = targetTrans.transform.position;
        effect.transform.localPosition = Vector3.zero;
        return UIParticle.transform;
    }

    public Transform CardDisappearEffect(ParticleSystem summonEffect, Transform targetTrans)
    {
        return UIParticlePool(summonEffect, targetTrans);
    }

    public Transform SummonTrail(ParticleSystem trailEffect, Transform targetTrans)
    {
        return UIParticlePool(trailEffect,targetTrans);
    }

    public Transform AbilityEffect(ParticleSystem abilityEffect, Transform cardTrans)
    {
        return UIParticlePool(abilityEffect, cardTrans);
    }

    public void StartThrow(Transform target, float height, Vector3 start, Vector3 end, float duration, bool destroyOnComplete = true)
    {
        // 中点を求める
        Vector3 half = end - start * 0.50f + start;
        half.y += Vector3.up.y + height;

        StartCoroutine(LerpThrow(target, start, half, end, duration, destroyOnComplete));
    }

    IEnumerator LerpThrow(Transform target, Vector3 start, Vector3 half, Vector3 end, float duration, bool destroyOnComplete)
    {
        float startTime = Time.timeSinceLevelLoad;
        float rate = 0f;
        Transform targetPos = target;
        while (true)
        {
            if (rate >= 1.0f)
            {
                target.position = end;
                if (destroyOnComplete)
                {
                    GetComponent<CardController>().CheckAttackParticle(target);
                }
                yield break;
            }
            float diff = Time.timeSinceLevelLoad - startTime;
            rate = diff / (duration / 60f);
            target.position = CalcLerpPoint(start, half, end, rate);

            yield return null;
        }
    }

    Vector3 CalcLerpPoint(Vector3 p0, Vector3 p1, Vector3 p2, float t)
    {
        var a = Vector3.Lerp(p0, p1, t);
        var b = Vector3.Lerp(p1, p2, t);
        return Vector3.Lerp(a, b, t);
    }

    Transform UIParticlePool(ParticleSystem particle,Transform cardTrans)
    {
        UIParticle getParticle = GameManager.instance.uiParticlesManager.GetUIParticles();
        UIParticle UIParticle;
        if (getParticle == null)
        {
            getParticle = GameManager.instance.uiParticlesManager.GetUIParticle();
            UIParticle = Instantiate(getParticle, transform.parent.parent);
        }
        else
        {
            UIParticle = getParticle;
            UIParticle.transform.SetParent(transform.parent.parent);
        }
        ParticleSystem effect = Instantiate(particle);
        effect.transform.SetParent(UIParticle.transform);
        UIParticle.particles.Add(effect);
        UIParticle.RefreshParticles();
        UIParticle.transform.position = cardTrans.transform.position;
        effect.transform.localPosition = Vector3.zero;
        return UIParticle.transform;
    }


}
