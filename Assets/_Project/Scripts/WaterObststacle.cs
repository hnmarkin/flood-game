using UnityEngine;

public class WaterObststacle : MonoBehaviour
{
    public Sprite pluh;
    public int ticks;
    SpriteRenderer rend;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rend = GetComponent<SpriteRenderer>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void tick()
    {
        if (ticks >= 3)
        {
            rend.sprite = pluh;
            rend.color = Color.white;
        }
        else
        {
            ticks++;
        }


    }
}
