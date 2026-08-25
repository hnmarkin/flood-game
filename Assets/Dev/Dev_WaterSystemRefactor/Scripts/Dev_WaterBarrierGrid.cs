using UnityEngine;

/// <summary>
/// Runtime barrier edge data owned by the water controller and engine.
/// It is deliberately not a scene component or a swappable provider.
/// </summary>
public sealed class Dev_WaterBarrierGrid
{
    private bool[,] _blockedX;
    private bool[,] _blockedY;
    private float[,] _barrierHeightX;
    private float[,] _barrierHeightY;
    private float[,] _seepageX;
    private float[,] _seepageY;

    public bool IsInitialized => _blockedX != null;

    public bool InitializeForSimulation(int gridWidth, int gridHeight)
    {
        if (gridWidth <= 0 || gridHeight <= 0)
        {
            Debug.LogError("[Dev_WaterBarrierGrid] Cannot initialize with non-positive grid dimensions.");
            return false;
        }

        _blockedX = new bool[gridWidth, gridHeight];
        _blockedY = new bool[gridWidth, gridHeight];
        _barrierHeightX = new float[gridWidth, gridHeight];
        _barrierHeightY = new float[gridWidth, gridHeight];
        _seepageX = new float[gridWidth, gridHeight];
        _seepageY = new float[gridWidth, gridHeight];
        return true;
    }

    public bool TrySetBarrierX(int simX, int simY, float height, float seepage = 0f)
    {
        return TrySetBarrier(simX, simY, height, seepage, _blockedX, _barrierHeightX, _seepageX, "X");
    }

    public bool TrySetBarrierY(int simX, int simY, float height, float seepage = 0f)
    {
        return TrySetBarrier(simX, simY, height, seepage, _blockedY, _barrierHeightY, _seepageY, "Y");
    }

    public bool TryClearBarrierX(int simX, int simY)
    {
        return TryClearBarrier(simX, simY, _blockedX, _barrierHeightX, _seepageX, "X");
    }

    public bool TryClearBarrierY(int simX, int simY)
    {
        return TryClearBarrier(simX, simY, _blockedY, _barrierHeightY, _seepageY, "Y");
    }

    internal Dev_WaterBarrierGrid Clone()
    {
        Dev_WaterBarrierGrid clone = new Dev_WaterBarrierGrid();
        if (!IsInitialized || !clone.InitializeForSimulation(_blockedX.GetLength(0), _blockedX.GetLength(1)))
            return clone;

        CopyGrid(_blockedX, clone._blockedX);
        CopyGrid(_blockedY, clone._blockedY);
        CopyGrid(_barrierHeightX, clone._barrierHeightX);
        CopyGrid(_barrierHeightY, clone._barrierHeightY);
        CopyGrid(_seepageX, clone._seepageX);
        CopyGrid(_seepageY, clone._seepageY);
        return clone;
    }

    public bool IsBlockedX(int simX, int simY)
    {
        return IsInBounds(simX, simY) && _blockedX[simX, simY];
    }

    public bool IsBlockedY(int simX, int simY)
    {
        return IsInBounds(simX, simY) && _blockedY[simX, simY];
    }

    public float GetBarrierHeightX(int simX, int simY)
    {
        return IsBlockedX(simX, simY) ? _barrierHeightX[simX, simY] : 0f;
    }

    public float GetBarrierHeightY(int simX, int simY)
    {
        return IsBlockedY(simX, simY) ? _barrierHeightY[simX, simY] : 0f;
    }

    public float GetSeepageX(int simX, int simY)
    {
        return IsBlockedX(simX, simY) ? _seepageX[simX, simY] : 0f;
    }

    public float GetSeepageY(int simX, int simY)
    {
        return IsBlockedY(simX, simY) ? _seepageY[simX, simY] : 0f;
    }

    private bool TrySetBarrier(
        int simX,
        int simY,
        float height,
        float seepage,
        bool[,] blocked,
        float[,] barrierHeight,
        float[,] barrierSeepage,
        string axis)
    {
        if (!IsInBounds(simX, simY))
        {
            Debug.LogWarning($"[Dev_WaterBarrierGrid] Cannot set {axis}-edge barrier outside the initialized grid.");
            return false;
        }

        if (!IsFiniteNonNegative(height) || !IsFiniteNonNegative(seepage))
        {
            Debug.LogWarning("[Dev_WaterBarrierGrid] Barrier height and seepage must be finite, non-negative values.");
            return false;
        }

        blocked[simX, simY] = true;
        barrierHeight[simX, simY] = height;
        barrierSeepage[simX, simY] = seepage;
        return true;
    }

    private bool TryClearBarrier(
        int simX,
        int simY,
        bool[,] blocked,
        float[,] barrierHeight,
        float[,] barrierSeepage,
        string axis)
    {
        if (!IsInBounds(simX, simY))
        {
            Debug.LogWarning($"[Dev_WaterBarrierGrid] Cannot clear {axis}-edge barrier outside the initialized grid.");
            return false;
        }

        blocked[simX, simY] = false;
        barrierHeight[simX, simY] = 0f;
        barrierSeepage[simX, simY] = 0f;
        return true;
    }

    private bool IsInBounds(int simX, int simY)
    {
        return _blockedX != null
            && simX >= 0
            && simY >= 0
            && simX < _blockedX.GetLength(0)
            && simY < _blockedX.GetLength(1);
    }

    private static bool IsFiniteNonNegative(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value) && value >= 0f;
    }

    private static void CopyGrid<T>(T[,] source, T[,] destination)
    {
        for (int y = 0; y < source.GetLength(1); y++)
        {
            for (int x = 0; x < source.GetLength(0); x++)
                destination[x, y] = source[x, y];
        }
    }
}
