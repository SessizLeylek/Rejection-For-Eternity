using UnityEngine;

[CreateAssetMenu(menuName = "ScriptableObjects/CardResources")]
public class CardResources : ScriptableObject
{
    public GameObject CardObjectTextOnly, CardObjectWithImage;
    public Color[] cardTypeColors;
}
