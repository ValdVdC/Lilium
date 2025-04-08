using UnityEngine;
using UnityEngine.Rendering.Universal;

public class FlickeringTorch : MonoBehaviour
{
    private Light2D torchLight;
    public float minIntensity = 0.8f;
    public float maxIntensity = 1.2f;
    public float flickerSpeed = 3f;

    void Start() {
        torchLight = GetComponent<Light2D>();
    }

    void Update() {
        float noise = Mathf.PerlinNoise(Time.time * flickerSpeed, 0);
        torchLight.intensity = Mathf.Lerp(minIntensity, maxIntensity, noise);
    }
}
