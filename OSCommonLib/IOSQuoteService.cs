using Hik.Communication.ScsServices.Service;


namespace OSCommonLib
{
    /// <summary>
    /// This interface defines methods of quote service that can be called by clients.
    /// </summary>
    [ScsService]
    public interface IOSQuoteService
    {
        void OnNotifyTicks(TICK tTick);
        void OnNotifyTicksGet(TICK tTick);
        void OnNotifyBest5(BEST5 bfi);
        void Quit();
    }
}
