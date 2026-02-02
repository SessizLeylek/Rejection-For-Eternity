using UnityEngine;

public static class EaseFunctions
{
    public static float EaseIn(float linear) => linear * linear;
    public static float EaseOut(float linear) => 1f - (1f - linear) * (1f - linear);
}
