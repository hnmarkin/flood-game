public interface IBarrierProvider
{
    // X-edge barrier: between (x-1,y) and (x,y)
    float GetBarrierHeightX(int x, int y);
    float GetSeepageX(int x, int y);

    // Y-edge barrier: between (x,y-1) and (x,y)
    float GetBarrierHeightY(int x, int y);
    float GetSeepageY(int x, int y);

    bool IsBlockedX(int x, int y);
    bool IsBlockedY(int x, int y);

}
