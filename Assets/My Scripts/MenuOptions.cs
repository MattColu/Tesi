using System;
using System.Text.RegularExpressions;
using UnityEngine;

public class MenuOptions : MonoBehaviour {
    public static MenuOptions Instance;
    public string Name {get => _name; set => _name = Regex.Replace(value.Trim().ToLower(), "[^a-zA-Z0-9 -]", "");}
    private string _name;
    private string t_name = "";
    public string UID {private set; get;}
    public Color KartColor {get => _color; set => _color = value;}
    private Color _color = Color.red;

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

    public void CreateUID() {
        Instance.UID = Convert.ToBase64String(Guid.NewGuid().ToByteArray()).Replace('+', '-').Replace('/', '_').Remove(22);
    }

    public void OnEditNameField(string name) {
        t_name = name;
    }

    public void OnEditColorSwatch(int color) {
        switch (color) {
            case 0:
                _color = Color.red;
                break;
            case 1:
                _color = Color.blue;
                break;
            case 2:
                _color = Color.green;
                break;
            case 3:
                _color = Color.yellow;
                break;
            case 4:
                _color = Color.magenta;
                break;
            case 5:
                _color = Color.cyan;
                break;
            case 6:
                _color = Color.white;
                break;
            case 7:
                _color = Color.gray;
                break;
            case 8:
                _color = Color.black;
                break;
            default:
                _color = Color.red;
                break;
        }
        rend.material.color = _color;
    }

    public void OnClickPlayButton() {
        Name = t_name;
        KartColor = _color;
        CreateUID();
    }
}
