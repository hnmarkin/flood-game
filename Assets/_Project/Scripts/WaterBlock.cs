
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
    //public bool ground;
    public Sprite[] pic;
    public Sprite blok;
    public enum TileType { Water, Wall, Biz, Home};
    public TileType type;
    public int cost;

    void Start()
    {
        rend = GetComponent<SpriteRenderer>();
        if (type == TileType.Wall) { rend.sprite = null; }
        if (type == TileType.Wall && height == 100) { rend.sprite = blok; }
    }
    bool CheckSides(int d)
    {
        //Debug.Log("Sides");
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
            else if ((GridManager.waterGrid[xloc + 1, yloc].type != TileType.Water && GridManager.waterGrid[xloc + 1, yloc].height < GridManager.Instance.waterlevel))
            {
                return true;
            }
            //Debug.Log(GridManager.waterGrid[xloc + 1, yloc]);
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
            if (GridManager.waterGrid[xloc, yloc + 1].type != TileType.Water && GridManager.waterGrid[xloc, yloc + 1].height < GridManager.Instance.waterlevel)
            {
                return true;
            }
           // Debug.Log(GridManager.waterGrid[xloc, yloc + 1]);
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
            else if (GridManager.waterGrid[xloc, yloc - 1].type != TileType.Water && GridManager.waterGrid[xloc, yloc - 1].height < GridManager.Instance.waterlevel)
            {
                return true;
            }
            //Debug.Log(GridManager.waterGrid[xloc, yloc - 1]);
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
            else if (GridManager.waterGrid[xloc - 1, yloc].type != TileType.Water && GridManager.waterGrid[xloc - 1, yloc].height < GridManager.Instance.waterlevel)
            {
                return true;
            }
            //Debug.Log(GridManager.waterGrid[xloc - 1, yloc]);
            return false;

        }
        return false;
    }
    public void checkBorder()
    {
        rend.sprite = pic[GridManager.Instance.waterlevel];
        rend.sortingOrder = GridManager.Instance.waterlevel;
        if (CheckSides(0))
        {
            border = true;
            return;
        }
        else if (CheckSides(1))
        {
            border = true;
            return;
        }
        else if(CheckSides(2))
        {
            border = true;
            return;
        }
        else if(CheckSides(3))
        {
            border = true;
            return;
        }

        border = false;
    }
    public void spread()
    {
        
        if (CheckSides(3))
        {
            GridManager.Instance.OvertakeWater(xloc - 1, yloc);
        }
        else if (CheckSides(1))
        {
            GridManager.Instance.OvertakeWater(xloc, yloc + 1);
        }
        else if (CheckSides(2))
        {
            GridManager.Instance.OvertakeWater(xloc, yloc - 1);
        }
        else if (CheckSides(0))
        {
            GridManager.Instance.OvertakeWater(xloc + 1, yloc);
        }
    }
}
