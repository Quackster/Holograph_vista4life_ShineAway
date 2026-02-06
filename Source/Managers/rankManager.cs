using System.Text;

namespace Holo.Managers;

/// <summary>
/// Provides information about the various user ranks/levels, aswell as ranks for games such as 'BattleBall' and 'SnowStorm'.
/// </summary>
public static class rankManager
{
    private static Dictionary<byte, userRank> userRanks = new();
        private static gameRank[] gameRanksBB;
        private static gameRank[] gameRanksSS;

        /// <summary>
        /// Initializes the various user ranks, aswell as the ranks for games such as 'BattleBall' and 'SnowStorm'.
        /// </summary>
        public static void Init()
        {
            Out.WriteLine("Intializing user rank fuserights...");
            userRanks = new Dictionary<byte, userRank>();

            for (byte i = 1; i <= 7; i++)
                userRanks.Add(i, new userRank(i));

            Out.WriteLine("Fuserights for 7 ranks loaded.");
            Out.WriteBlank();

            Out.WriteLine("Initializing game ranks...");
            string[] Titles = DB.runReadColumn("SELECT title FROM games_ranks WHERE type = 'bb' ORDER BY id ASC", 0);
            int[] Mins = DB.runReadColumn("SELECT minpoints FROM games_ranks WHERE type = 'bb' ORDER BY id ASC", 0, null);
            int[] Maxs = DB.runReadColumn("SELECT maxpoints FROM games_ranks WHERE type = 'bb' ORDER BY id ASC", 0, null);

            gameRanksBB = new gameRank[Titles.Length];
            for (int i = 0; i < gameRanksBB.Length; i++)
            {
                gameRanksBB[i] = new gameRank(Titles[i], Mins[i], Maxs[i]);
                Out.WriteLine("Loaded gamerank '" + Titles[i] + "' [" + Mins[i] + "-" + Maxs[i] + "] for game 'BattleBall'.");
            }
            Out.WriteLine("Loaded " + Titles.Length + " ranks for game 'BattleBall'.");

            Titles = DB.runReadColumn("SELECT title FROM games_ranks WHERE type = 'ss' ORDER BY id ASC", 0);
            Mins = DB.runReadColumn("SELECT minpoints FROM games_ranks WHERE type = 'ss' ORDER BY id ASC", 0, null);
            Maxs = DB.runReadColumn("SELECT maxpoints FROM games_ranks WHERE type = 'ss' ORDER BY id ASC", 0, null);

            gameRanksSS = new gameRank[Titles.Length];
            for (int i = 0; i < gameRanksSS.Length; i++)
            {
                gameRanksSS[i] = new gameRank(Titles[i], Mins[i], Maxs[i]);
                Out.WriteLine("Loaded gamerank '" + Titles[i] + "' [" + Mins[i] + "-" + Maxs[i] + "] for game 'SnowStorm'.");
            }
            Out.WriteLine("Loaded " + Titles.Length + " ranks for game 'SnowStorm'.");
        }
        /// <summary>
        /// Returns the fuserights string for a certain user rank.
        /// </summary>
        /// <param name="rankID">The ID of the user rank.</param>
        public static string fuseRights(byte rankID)
        {
            if (!userRanks.TryGetValue(rankID, out var rank))
                return "";

            StringBuilder strBuilder = new StringBuilder();
            for (int i = 0; i < rank.fuseRights.Length; i++)
                strBuilder.Append(rank.fuseRights[i] + Convert.ToChar(2));

            return strBuilder.ToString();
        }
        /// <summary>
        /// Returns a bool that indicates if a certain user rank contains a certain fuseright.
        /// </summary>
        /// <param name="rankID">The ID of the user rank.</param>
        /// <param name="fuseRight">The fuseright to look for.</param>
        /// <returns></returns>
        public static bool containsRight(byte rankID, string fuseRight)
        {
            if (!userRanks.TryGetValue(rankID, out var objRank))
                return false;

            for (int i = 0; i < objRank.fuseRights.Length; i++)
                if (objRank.fuseRights[i] == fuseRight)
                    return true;

            return false;
        }
        public static gameRank getGameRank(bool isBattleBall, string Title)
        {
            gameRank[] Ranks = null;
            if (isBattleBall)
                Ranks = gameRanksBB;
            else
                Ranks = gameRanksSS;

            foreach (gameRank Rank in Ranks)
                if (Rank.Title == Title)
                    return Rank;

            return new gameRank("holo.cast.gamerank.null", 0, 0);
        }
        /// <summary>
        /// Returns the game rank title as a string for a certain game type ('BattleBall' or 'SnowStorm') and score.
        /// </summary>
        /// <param name="isBattleBall">Specifies if to lookup a 'BattleBall' game. If false, then the rank for a 'SnowStorm' game will be returned.</param>
        /// <param name="Score">The score to get the rank for.</param>
        public static string getGameRankTitle(bool isBattleBall, int Score)
        {
            gameRank[] Ranks = null;
            if (isBattleBall)
                Ranks = gameRanksBB;
            else
                Ranks = gameRanksSS;

            foreach (gameRank Rank in Ranks)
                if (Score >= Rank.minPoints && (Rank.maxPoints == 0 || Score <= Rank.maxPoints))
                    return Rank.Title;

            return "holo.cast.gamerank.null";
        }
        /// <summary>
        /// Represents a user rank.
        /// </summary>
        private struct userRank
        {
            internal string[] fuseRights;
            /// <summary>
            /// Initializes a user rank.
            /// </summary>
            /// <param name="rankID">The ID of the rank to initialize.</param>
            internal userRank(byte rankID)
            {
                fuseRights = DB.runReadColumn("SELECT fuseright FROM system_fuserights WHERE minrank <= " + rankID + "", 0);
            }
        }
        /// <summary>
        /// Represents a game rank, containing the min and max score and the rank name.
        /// </summary>
        public struct gameRank
        {
            /// <summary>
            /// The minimum amount of points of this rank.
            /// </summary>
            internal int minPoints;
            /// <summary>
            /// The maximum amount of points of this rank.
            /// </summary>
            internal int maxPoints;
            /// <summary>
            /// The title of this rank.
            /// </summary>
            internal string Title;
            /// <summary>
            /// Initializes the gamerank with given values.
            /// </summary>
            /// <param name="Title">The title of this rank.</param>
            /// <param name="minPoints">The minimum amount of points of this rank.</param>
            /// <param name="maxPoints">The maximum amount of points of this rank.</param>
            internal gameRank(string Title, int minPoints, int maxPoints)
            {
                this.Title = Title;
                this.minPoints = minPoints;
                this.maxPoints = maxPoints;
            }
        }
    }
