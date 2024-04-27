using UnityEngine;

namespace KartGame.Custom {
    public class SetKartBodyColor : MonoBehaviour
    {
        [SerializeField]
        private Renderer rend;
        void Awake() {
            if (MenuOptions.Instance && rend) {
                rend.material.color = MenuOptions.Instance.KartColor;
            }
        }
    }
}