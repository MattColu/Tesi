using UnityEngine;

public class MenuOptions : MonoBehaviour {
    public static MenuOptions Instance;
    private string Name;
    private string t_Name = "";
    private string UID;
    private Color KartColor;
    private Color t_Color = Color.red;

    [SerializeField]
    private Transform MenuKartBody;
    private Renderer rend;

    void Awake() {
        rend = MenuKartBody.GetComponent<Renderer>();
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    public void SetName(string name) {
        Instance.Name = name.Trim().ToLower();
    }

    public void SetColor(Color color) {
        Instance.KartColor = color;
    }

    public Color GetColor() {
        return KartColor;
    }

    public void SetUID() {
        Instance.UID = System.Guid.NewGuid().ToString();
    }

    public void OnEditNameField(string name) {
        t_Name = name;
    }

    public void OnEditColorSwatch(int color) {
        switch (color) {
            case 0:
                t_Color = Color.red;
                break;
            case 1:
                t_Color = Color.blue;
                break;
            case 2:
                t_Color = Color.green;
                break;
            case 3:
                t_Color = Color.yellow;
                break;
            case 4:
                t_Color = Color.magenta;
                break;
            case 5:
                t_Color = Color.cyan;
                break;
            case 6:
                t_Color = Color.white;
                break;
            case 7:
                t_Color = Color.gray;
                break;
            case 8:
                t_Color = Color.black;
                break;
            default:
                t_Color = Color.red;
                break;
        }
        rend.material.color = t_Color;
    }

    public void OnClickPlayButton() {
        SetName(t_Name);
        SetColor(t_Color);
        SetUID();
    }
}
