using Holo.Data.Repositories.Base;
using MySqlConnector;

namespace Holo.Data.Repositories.Games;

/// <summary>
/// Repository for poll, poll_questions, poll_answers, and poll_results tables.
/// </summary>
public class PollRepository : BaseRepository
{
    private static PollRepository? _instance;
    public static PollRepository Instance => _instance ??= new PollRepository();

    private PollRepository() { }

    #region Poll
    public bool RoomHasPoll(int roomId)
    {
        return Exists(
            "SELECT pid FROM poll WHERE rid = @rid",
            Param("@rid", roomId));
    }

    public string[] GetPollData(int roomId)
    {
        return ReadRow(
            "SELECT pid, title, thanks FROM poll WHERE rid = @rid",
            Param("@rid", roomId));
    }
    #endregion

    #region Poll Questions
    public int[] GetQuestionIds(int pollId)
    {
        return ReadColumnInt(
            "SELECT qid FROM poll_questions WHERE pid = @pid",
            0,
            Param("@pid", pollId));
    }

    public string[] GetQuestions(int pollId)
    {
        return ReadColumn(
            "SELECT question FROM poll_questions WHERE pid = @pid",
            0,
            Param("@pid", pollId));
    }

    public int[] GetQuestionTypes(int pollId)
    {
        return ReadColumnInt(
            "SELECT type FROM poll_questions WHERE pid = @pid",
            0,
            Param("@pid", pollId));
    }

    public int[] GetQuestionMins(int pollId)
    {
        return ReadColumnInt(
            "SELECT min FROM poll_questions WHERE pid = @pid",
            0,
            Param("@pid", pollId));
    }

    public int[] GetQuestionMaxs(int pollId)
    {
        return ReadColumnInt(
            "SELECT max FROM poll_questions WHERE pid = @pid",
            0,
            Param("@pid", pollId));
    }
    #endregion

    #region Poll Answers
    public int[] GetAnswerIds(int questionId)
    {
        return ReadColumnInt(
            "SELECT aid FROM poll_answers WHERE qid = @qid",
            0,
            Param("@qid", questionId));
    }

    public string? GetAnswer(int questionId, int answerId)
    {
        return ReadScalar(
            "SELECT answer FROM poll_answers WHERE qid = @qid AND aid = @aid",
            Param("@qid", questionId),
            Param("@aid", answerId));
    }
    #endregion

    #region Poll Results
    public void SavePollResult(int pollId, int questionId, int userId, string answer)
    {
        Execute(
            "INSERT INTO poll_results (pid, qid, uid, answer) VALUES (@pid, @qid, @uid, @answer)",
            Param("@pid", pollId),
            Param("@qid", questionId),
            Param("@uid", userId),
            Param("@answer", answer));
    }

    public bool HasVoted(int pollId, int userId)
    {
        return Exists(
            "SELECT uid FROM poll_results WHERE pid = @pid AND uid = @uid",
            Param("@pid", pollId),
            Param("@uid", userId));
    }

    public bool HasUserAnsweredPoll(int userId, int pollId)
    {
        return Exists(
            "SELECT aid FROM poll_results WHERE uid = @uid AND pid = @pid",
            Param("@uid", userId),
            Param("@pid", pollId));
    }

    public bool IsTypeThreeQuestion(int questionId)
    {
        return Exists(
            "SELECT type FROM poll_questions WHERE qid = @qid AND type = 3",
            Param("@qid", questionId));
    }

    public void SavePollResultWithAnswer(int pollId, int questionId, int answerId, string answers, int userId)
    {
        Execute(
            "INSERT INTO poll_results (pid, qid, aid, answers, uid) VALUES (@pid, @qid, @aid, @answers, @uid)",
            Param("@pid", pollId),
            Param("@qid", questionId),
            Param("@aid", answerId),
            Param("@answers", answers),
            Param("@uid", userId));
    }
    #endregion
}
