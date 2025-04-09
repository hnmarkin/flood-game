using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class InspectorController : MonoBehaviour
{
    [SerializeField] GameObject inspectHolder;
    [SerializeField] TMP_Text title;
    [SerializeField] TMP_Text info;
    [SerializeField] UnityEngine.UI.Image image;
    [SerializeField] Sprite water;
    [SerializeField] Sprite wall;
    [SerializeField] Sprite Home;
    [SerializeField] Sprite Biz;

    public float HorizontalSpeed = -400;
    public float MaxHorizontalPosition = -115;
   // public float MinHorizontalPosition = -115;
   
    private RectTransform rectTransform;

    void Start()
    {
        rectTransform = inspectHolder.GetComponent<RectTransform>();
    }

    public void StartUI()
    {
        LeanTween.moveLocalX(inspectHolder, 345, 0.5f).setEaseOutBack();
    }
    public void GoAwayUI()
    {
        LeanTween.moveLocalX(inspectHolder, 585, 0.4f).setEaseInBack();
    }
    public void setInspector(string head, string text, WaterBlock.TileType im)
    {
        StartUI();
        title.text = head;
        info.text = text;
        switch (im)
        {
            case WaterBlock.TileType.Water:
                image.sprite = water;
                break;
            case WaterBlock.TileType.Home:
                image.sprite = Home;
                break;
            case WaterBlock.TileType.Biz:
                image.sprite = Biz;
                break;
            case WaterBlock.TileType.Wall:
                image.sprite = wall;
                break;
        }


    }
    public void setInspector(string head, string text)
    {
        StartUI();
        title.text = head;
        info.text = text;
    }
}
