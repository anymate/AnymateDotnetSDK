namespace Anymate
{
    public class AnymateTaskAction
    {
        public AnymateTaskAction() { }
        public AnymateTaskAction(long taskId, string reason)
        {
            TaskId = taskId;
            Reason = reason;
        }
        public AnymateTaskAction(long taskId, string reason, string newNote)
        {
            TaskId = taskId;
            Reason = reason;
            Comment = newNote;
        }
        public long TaskId { get; set; }
        public string Reason { get; set; }
        public string Comment { get; set; } = string.Empty;

        public int? OverwriteSecondsSaved { get; set; } = null;
        public int? OverwriteEntries { get; set; } = null;
    }
}
