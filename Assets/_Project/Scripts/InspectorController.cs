using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class InspectorController : MonoBehaviour
{
    [SerializeField] GameObject inspectHolder;
    [SerializeField] GameObject shop;
    [SerializeField] GameObject stats;
    [SerializeField] TMP_Text title;
    [SerializeField] TMP_Text info;
    [SerializeField] public TMP_Text waterlevel;
    [SerializeField] public TMP_Text pop;
    [SerializeField] public TMP_Text budget;
    [SerializeField] public TMP_Text walls;
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
    private void Awake()
    {
        LeanTween.moveLocalY(shop, -255, 0.5f).setEaseOutBack();
        LeanTween.moveLocalY(stats, 260, 0.5f).setEaseOutBack();
    }
    public void StartUI()
    {
        LeanTween.moveLocalX(inspectHolder, 410, 0.5f).setEaseOutBack();
    }
    public void GoAwayUI()
    {
        LeanTween.moveLocalX(inspectHolder, 600, 0.4f).setEaseInBack();
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
    public void GoAwayShop()
    {
        LeanTween.moveLocalY(shop, -400, 0.4f).setEaseInBack();
        //shop.SetActive(false);
    }

}
