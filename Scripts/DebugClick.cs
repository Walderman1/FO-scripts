using UnityEngine;
using UnityEngine.EventSystems;

public class DebugClick : MonoBehaviour, IPointerClickHandler
{
    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log($"Клик по UI: {gameObject.name}, кнопка: {eventData.button}");
    }
}