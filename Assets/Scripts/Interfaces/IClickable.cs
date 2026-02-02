using UnityEngine;

public interface IClickable
{
    public void OnPointerEnter() { }
    public void OnPointerExit() { }
    public void OnPointerUp() { }
    public void OnPointerDown() { }
    public GameObject GetGameObject();

}
