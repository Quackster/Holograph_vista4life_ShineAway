using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;

namespace Holo.Data.Repositories
{
    /// <summary>
    /// Provides data access operations for poll-related database queries.
    /// All queries use parameterized SQL to prevent SQL injection.
    /// </summary>
    public class PollDataAccess : BaseDataAccess
    {
        /// <summary>
        /// Checks if a user has already answered a poll question.
        /// </summary>
        public bool UserHasAnsweredPoll(int userId, int pollId)
        {
            string query = "SELECT aid FROM poll_results WHERE uid = @userId AND pid = @pollId LIMIT 1";
            var parameters = new[]
            {
                new MySqlParameter("@userId", userId),
                new MySqlParameter("@pollId", pollId)
            };
            return RecordExists(query, parameters);
        }

        /// <summary>
        /// Checks if a poll question is type 3.
        /// </summary>
        public bool IsPollQuestionTypeThree(int questionId)
        {
            string query = "SELECT type FROM poll_questions WHERE qid = @questionId AND type = '3' LIMIT 1";
            var parameters = new[]
            {
                new MySqlParameter("@questionId", questionId)
            };
            return RecordExists(query, parameters);
        }

        /// <summary>
        /// Creates a poll result for a type 3 question (text answer).
        /// </summary>
        public bool CreatePollResultText(int pollId, int questionId, string answer, int userId)
        {
            string query = "INSERT INTO poll_results (pid, qid, aid, answers, uid) VALUES (@pollId, @questionId, '0', @answer, @userId)";
            var parameters = new[]
            {
                new MySqlParameter("@pollId", pollId),
                new MySqlParameter("@questionId", questionId),
                new MySqlParameter("@answer", answer ?? string.Empty),
                new MySqlParameter("@userId", userId)
            };
            return ExecuteNonQuery(query, parameters);
        }

        /// <summary>
        /// Creates a poll result for a multiple choice question.
        /// </summary>
        public bool CreatePollResultChoice(int pollId, int questionId, int answerId, int userId)
        {
            string query = "INSERT INTO poll_results (pid, qid, aid, answers, uid) VALUES (@pollId, @questionId, @answerId, ' ', @userId)";
            var parameters = new[]
            {
                new MySqlParameter("@pollId", pollId),
                new MySqlParameter("@questionId", questionId),
                new MySqlParameter("@answerId", answerId),
                new MySqlParameter("@userId", userId)
            };
            return ExecuteNonQuery(query, parameters);
        }

        /// <summary>
        /// Gets poll data for a room.
        /// </summary>
        public PollData GetPollData(int roomId)
        {
            string query = @"
                SELECT 
                    pid, 
                    title, 
                    thanks 
                FROM poll 
                WHERE rid = @roomId 
                LIMIT 1";
            
            var parameters = new[]
            {
                new MySqlParameter("@roomId", roomId)
            };
            
            var row = ExecuteSingleRow(query, parameters);
            
            if (row.Count == 0)
                return null;

            return new PollData
            {
                PollId = row.ContainsKey("pid") ? int.Parse(row["pid"]) : 0,
                Title = row.ContainsKey("title") ? row["title"] : string.Empty,
                Thanks = row.ContainsKey("thanks") ? row["thanks"] : string.Empty
            };
        }

        /// <summary>
        /// Gets all question IDs for a poll.
        /// </summary>
        public List<int> GetPollQuestionIds(int pollId)
        {
            string query = @"
                SELECT qid 
                FROM poll_questions 
                WHERE pid = @pollId";
            
            var parameters = new[]
            {
                new MySqlParameter("@pollId", pollId)
            };
            
            return ExecuteSingleColumnInt(query, null, parameters);
        }

        /// <summary>
        /// Gets all questions for a poll.
        /// </summary>
        public List<string> GetPollQuestions(int pollId)
        {
            string query = @"
                SELECT question 
                FROM poll_questions 
                WHERE pid = @pollId";
            
            var parameters = new[]
            {
                new MySqlParameter("@pollId", pollId)
            };
            
            return ExecuteSingleColumnString(query, null, parameters);
        }

        /// <summary>
        /// Gets all question types for a poll.
        /// </summary>
        public List<int> GetPollQuestionTypes(int pollId)
        {
            string query = @"
                SELECT type 
                FROM poll_questions 
                WHERE pid = @pollId";
            
            var parameters = new[]
            {
                new MySqlParameter("@pollId", pollId)
            };
            
            return ExecuteSingleColumnInt(query, null, parameters);
        }

        /// <summary>
        /// Gets all question min values for a poll.
        /// </summary>
        public List<int> GetPollQuestionMins(int pollId)
        {
            string query = @"
                SELECT min 
                FROM poll_questions 
                WHERE pid = @pollId";
            
            var parameters = new[]
            {
                new MySqlParameter("@pollId", pollId)
            };
            
            return ExecuteSingleColumnInt(query, null, parameters);
        }

        /// <summary>
        /// Gets all question max values for a poll.
        /// </summary>
        public List<int> GetPollQuestionMaxs(int pollId)
        {
            string query = @"
                SELECT max 
                FROM poll_questions 
                WHERE pid = @pollId";
            
            var parameters = new[]
            {
                new MySqlParameter("@pollId", pollId)
            };
            
            return ExecuteSingleColumnInt(query, null, parameters);
        }

        /// <summary>
        /// Gets all answer IDs for a question.
        /// </summary>
        public List<int> GetPollAnswerIds(int questionId)
        {
            string query = @"
                SELECT aid 
                FROM poll_answers 
                WHERE qid = @questionId";
            
            var parameters = new[]
            {
                new MySqlParameter("@questionId", questionId)
            };
            
            return ExecuteSingleColumnInt(query, null, parameters);
        }

        /// <summary>
        /// Gets an answer text for a question and answer ID.
        /// </summary>
        public string GetPollAnswer(int questionId, int answerId)
        {
            string query = @"
                SELECT answer 
                FROM poll_answers 
                WHERE qid = @questionId 
                    AND aid = @answerId 
                LIMIT 1";
            
            var parameters = new[]
            {
                new MySqlParameter("@questionId", questionId),
                new MySqlParameter("@answerId", answerId)
            };
            
            return ExecuteScalarString(query, parameters);
        }

        /// <summary>
        /// Checks if a poll exists for a room.
        /// </summary>
        public bool PollExistsForRoom(int roomId)
        {
            string query = @"
                SELECT pid 
                FROM poll 
                WHERE rid = @roomId 
                LIMIT 1";
            
            var parameters = new[]
            {
                new MySqlParameter("@roomId", roomId)
            };
            
            return RecordExists(query, parameters);
        }
    }

    /// <summary>
    /// Represents poll data.
    /// </summary>
    public class PollData
    {
        public int PollId { get; set; }
        public string Title { get; set; }
        public string Thanks { get; set; }
    }
}
