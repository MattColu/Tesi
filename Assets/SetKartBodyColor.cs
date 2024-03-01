using UnityEngine;

public class SetKartBodyColor : MonoBehaviour
{
    [SerializeField]
    private Renderer rend;
    void Awake() {
        if (MenuOptions.Instance != null && rend != null) {
            rend.material.color = MenuOptions.Instance.GetColor();
        }
    }
}
