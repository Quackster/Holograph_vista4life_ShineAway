namespace HolographEmulator.Domain.Users
{
    /// <summary>
    /// Manages user inventory state.
    /// </summary>
    public class UserInventory
    {
        private int _handPage;

        public int HandPage
        {
            get => _handPage;
            set => _handPage = value;
        }
    }
}
