using UnityEngine;

public class WaterBlock : MonoBehaviour
{
    SpriteRenderer rend;
    public int xloc;
    public int yloc;
    public bool src = false;
    public bool output = false;
    public int amt;
    public bool border = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rend = GetComponent<SpriteRenderer>();
        
    }
    public void checkBorder()
    {
        if (src)
        {
            border = false;
            GridManager.Instance.connectedWater.Add(this);
            return;
        }
        if (GridManager.waterGrid[xloc+1, yloc] != null)
        {
            border = true;
            return;
        }
        else if (GridManager.waterGrid[xloc, yloc + 1] != null)
        {
            border = true;
            return;
        }
        if (xloc > 0)
        {
            if (GridManager.waterGrid[xloc - 1, yloc] != null)
            {
                border = true;
                return;
            }
        }
        if (yloc > 0)
        {
            if (GridManager.waterGrid[xloc, yloc - 1] != null)
            {
                border = true;
                return;
            }
        }
        border = false;
        GridManager.Instance.connectedWater.Add(this);
    }
    public void spread()
    {
        if (GridManager.waterGrid[xloc + 1, yloc] != null)
        {
            GridManager.Instance.AddWaterBlk(xloc + 1, yloc);
            return;
        }
        else if (GridManager.waterGrid[xloc, yloc + 1] != null)
        {
            GridManager.Instance.AddWaterBlk(xloc, yloc+1);
            return;
        }
        if (xloc > 0)
        {
            if (GridManager.waterGrid[xloc - 1, yloc] != null)
            {
                GridManager.Instance.AddWaterBlk(xloc -1, yloc);
                return;
            }
        }
        if (yloc > 0)
        {
            if (GridManager.waterGrid[xloc, yloc - 1] != null)
            {
                GridManager.Instance.AddWaterBlk(xloc, yloc - 1);
                return;
            }
        }
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
