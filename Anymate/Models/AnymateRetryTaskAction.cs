using System;

namespace Anymate
{
    public class AnymateRetryTaskAction
    {
        public AnymateRetryTaskAction() { }
        public AnymateRetryTaskAction(long taskId, string reason)
        {
            TaskId = taskId;
            Reason = reason;
        }
        public AnymateRetryTaskAction(long taskId, string reason, string newNote)
        {
            TaskId = taskId;
            Reason = reason;
            Comment = newNote;
        }

        public AnymateRetryTaskAction(long taskId, string reason, string newNote, DateTimeOffset activationDate)
        {
            TaskId = taskId;
            Reason = reason;
            Comment = newNote;
            ActivationDate = activationDate;
        }
        public long TaskId { get; set; }
        public string Reason { get; set; }
        public string Comment { get; set; } = string.Empty;
        public DateTimeOffset? ActivationDate { get; set; } = null;
        public int? OverwriteSecondsSaved { get; set; } = null;
        public int? OverwriteEntries { get; set; } = null;
    }
}
