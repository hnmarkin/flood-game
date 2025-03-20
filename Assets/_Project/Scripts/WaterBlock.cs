
using UnityEngine;

public class WaterBlock : MonoBehaviour
{
    public SpriteRenderer rend;
    public int xloc;
    public int yloc;
    public bool src = false;
    public bool output = false;
    public int amt;
    public bool border = false;
    public int height;
    public bool ground;
    public Sprite[] pic;
    public Sprite blok;
    //[SerializeField] GridManager grid;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rend = GetComponent<SpriteRenderer>();
        if (ground) { rend.sprite = null; }
        if (ground && height == 100) { rend.sprite = blok; }
        //if (ground) { rend.color = Color.black; }
        //GridManager.Instance.connectedWater.Add(this);
    }
    bool CheckSides(int d, bool invert)
    {
        if (d == 0)
        {
            if (xloc+1 >= GridManager.Instance.gridWidth)
            {
                return false;
            }
            else if (GridManager.waterGrid[xloc + 1, yloc] == null)
            {
                return true;
            }
            else if ((GridManager.waterGrid[xloc + 1, yloc].ground && GridManager.waterGrid[xloc + 1, yloc].height < GridManager.Instance.waterlevel))
            {
                return true;
            }
            return false;
        }
        if (d == 1)
        {
            if (yloc + 1 >= GridManager.Instance.gridHeight)
            {
                return false;
            }
            if (GridManager.waterGrid[xloc, yloc + 1] == null)
            {
                return true;
            }
            if (GridManager.waterGrid[xloc, yloc + 1].ground && GridManager.waterGrid[xloc, yloc + 1].height < GridManager.Instance.waterlevel)
            {
                return true;
            }
            return false;
        }
        if (d == 2)
        {
            if (yloc == 0)
            {
                return false;
            }
            else if (GridManager.waterGrid[xloc, yloc - 1] == null)
            {
                return true;
            }
            else if (GridManager.waterGrid[xloc, yloc - 1].ground && GridManager.waterGrid[xloc, yloc - 1].height < GridManager.Instance.waterlevel)
            {
                return true;
            }
            return false;
        }
        if (d == 3)
        {
            if (xloc == 0)
            {
                return false;
            }
            else if (GridManager.waterGrid[xloc - 1, yloc] == null)
            {
                return true;
            }
            else if (GridManager.waterGrid[xloc - 1, yloc].ground && GridManager.waterGrid[xloc - 1, yloc].height < GridManager.Instance.waterlevel)
            {
                return true;
            }
            return false;

        }
        return false;
    }
    public void checkBorder()
    {
        //Debug.Log("Border " + border);
        rend.sprite = pic[GridManager.Instance.waterlevel];
        rend.sortingOrder = GridManager.Instance.waterlevel;
        if (CheckSides(0, true))
        {
            border = true;
            return;
        }
        else if (CheckSides(1, true))
        {
            border = true;
            return;
        }
        else if(CheckSides(2, true))
        {
            border = true;
            return;
        }
        else if(CheckSides(3, true))
        {
            border = true;
            return;
        }

        border = false;
        //GridManager.Instance.connectedWater.Add(this);
    }
    public void spread()
    {
        
        if (CheckSides(3, false))
        {
            GridManager.Instance.AddWaterBlk(xloc - 1, yloc);
            //return;
        }
        else if (CheckSides(1, false))
        {
            GridManager.Instance.AddWaterBlk(xloc, yloc + 1);
            //return;
        }
        else if (CheckSides(2, false))
        {
            GridManager.Instance.AddWaterBlk(xloc, yloc - 1);
            //return;
        }
        else if (CheckSides(0, false))
        {
            GridManager.Instance.AddWaterBlk(xloc + 1, yloc);
            //return;
        }
        else
        {
            //Debug.Log("Couldn't spread");
        }
    }
}
