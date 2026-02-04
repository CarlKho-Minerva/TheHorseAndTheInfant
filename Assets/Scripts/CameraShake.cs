using UnityEngine;

public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance;
    private Vector3 originalPos;

    void Awake() { Instance = this; }

    public void Shake(float duration, float amount) {
        originalPos = transform.localPosition;
        StartCoroutine(ProcessShake(duration, amount));
    }

    System.Collections.IEnumerator ProcessShake(float dur, float amt) {
        float elapsed = 0;
        while (elapsed < dur) {
            transform.localPosition = originalPos + Random.insideUnitSphere * amt;
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.localPosition = originalPos;
    }
}