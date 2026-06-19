using System.Collections;
using UnityEngine;

/// <summary>
/// Puddle (bardak izi) GameObject'ine eklenir.
/// Kendi üzerinde coroutine çalıştırarak alpha'yı 0'a indirir ve kendini yok eder.
/// CupClickTrigger'dan bağımsız çalışır (o deactivate olsa bile bu çalışır).
/// </summary>
public class PuddleFader : MonoBehaviour
{
    public void StartFade(SpriteRenderer sr, Color startColor, float duration)
    {
        StartCoroutine(FadeOut(sr, startColor, duration));
    }

    private IEnumerator FadeOut(SpriteRenderer sr, Color startColor, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            if (sr != null)
            {
                float alpha = Mathf.Lerp(startColor.a, 0f, elapsed / duration);
                sr.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            }
            yield return null;
        }
        Destroy(gameObject);
    }
}
