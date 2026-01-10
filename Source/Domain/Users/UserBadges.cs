using System.Collections.Generic;

namespace HolographEmulator.Domain.Users
{
    /// <summary>
    /// Manages user badges.
    /// </summary>
    public class UserBadges
    {
        internal List<string> Badges = new List<string>();
        internal List<int> BadgeSlotIds = new List<int>();
        internal string CurrentBadge;
        internal string NowBadge;

        public void Clear()
        {
            Badges.Clear();
            BadgeSlotIds.Clear();
        }

        public void AddBadge(string badge)
        {
            Badges.Add(badge);
        }

        public void AddBadgeSlot(int slotId)
        {
            BadgeSlotIds.Add(slotId);
        }
    }
}
