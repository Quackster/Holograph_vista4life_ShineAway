namespace HolographEmulator.Domain.Users
{
    /// <summary>
    /// Manages user trading state.
    /// </summary>
    public class UserTrading
    {
        internal int TradePartnerRoomUid = -1;
        internal int TradePartnerUid = -1;
        internal bool TradeAccept;
        internal int[] TradeItems = new int[65];
        internal int TradeItemCount;

        public void Reset()
        {
            TradePartnerRoomUid = -1;
            TradePartnerUid = -1;
            TradeAccept = false;
            TradeItemCount = 0;
            for (int i = 0; i < TradeItems.Length; i++)
                TradeItems[i] = 0;
        }
    }
}
