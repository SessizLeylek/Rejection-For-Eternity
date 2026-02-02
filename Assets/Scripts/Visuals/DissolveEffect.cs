using UnityEngine;

public class DissolveEffect : MonoBehaviour
{
    [Range(0, 1)] public float DissolveAmount;
    private SpriteRenderer _spriteRenderer;
    private readonly int _materialAmountField = Shader.PropertyToID("_Amount");

    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        _spriteRenderer.material.SetFloat(_materialAmountField, DissolveAmount);
    }
}
