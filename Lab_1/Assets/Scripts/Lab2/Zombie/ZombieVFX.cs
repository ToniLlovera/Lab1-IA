using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZombieCommVFX : MonoBehaviour
{
    [Header("Flash")]
    public Color sendColor = new Color(1f, 0.65f, 0f); 
    public Color recvColor = new Color(0.2f, 1f, 0.2f);  
    public float flashIntensity = 2.5f;
    public float flashTime = 0.35f;

    [Header("Lines")]
    public float lineTime = 0.55f;
    public float lineWidth = 0.05f;

    private Renderer[] rends;
    private MaterialPropertyBlock mpb;

    void Awake()
    {
        rends = GetComponentsInChildren<Renderer>(true);
        mpb = new MaterialPropertyBlock();
    }

    public void PlaySendVFX(IEnumerable<Transform> targets)
    {
        Flash(sendColor);
        foreach (var t in targets)
        {
            if (!t) continue;
            DrawLineTo(t.position, lineTime);
        }
    }

    public void PlayReceiveVFX()
    {
        Flash(recvColor);
    }

    void Flash(Color c)
    {
        StartCoroutine(FlashCo(c));
    }

    IEnumerator FlashCo(Color c)
    {
        float t = 0f;
        while (t < flashTime)
        {
            float k = 1f - (t / flashTime);
            Color ec = c * (flashIntensity * k);
            ec.a = 1f;

            foreach (var r in rends)
            {
                if (!r) continue;
                r.GetPropertyBlock(mpb);
                mpb.SetColor("_EmissionColor", ec);
                r.SetPropertyBlock(mpb);
  
                if (r.material) r.material.EnableKeyword("_EMISSION");
            }
            t += Time.deltaTime;
            yield return null;
        }

  
        foreach (var r in rends)
        {
            if (!r) continue;
            r.GetPropertyBlock(mpb);
            mpb.SetColor("_EmissionColor", Color.black);
            r.SetPropertyBlock(mpb);
        }
    }

    void DrawLineTo(Vector3 worldTarget, float duration)
    {
        var go = new GameObject("CommLine");
        go.transform.SetParent(null);
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.widthMultiplier = lineWidth;
        lr.positionCount = 2;
        lr.numCapVertices = 4;
        lr.numCornerVertices = 2;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = sendColor;
        lr.endColor = sendColor;

        Vector3 a = transform.position + Vector3.up * 1.3f;
        Vector3 b = worldTarget + Vector3.up * 1.3f;
        lr.SetPosition(0, a);
        lr.SetPosition(1, b);


        StartCoroutine(FadeAndDestroy(lr, duration));
    }

    IEnumerator FadeAndDestroy(LineRenderer lr, float dur)
    {
        float t = 0f;
        var c0 = lr.startColor;
        var c1 = lr.endColor;
        while (t < dur && lr)
        {
            float k = 1f - (t / dur);
            var cc = new Color(c0.r, c0.g, c0.b, Mathf.Clamp01(k));
            lr.startColor = cc;
            lr.endColor = cc;
            t += Time.deltaTime;
            yield return null;
        }
        if (lr) Destroy(lr.gameObject);
    }
}
