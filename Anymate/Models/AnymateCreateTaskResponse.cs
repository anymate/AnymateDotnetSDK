namespace Anymate
{

    public class AnymateCreateTaskResponse
    {
        public bool Succeeded { get; set; } = false;
        public string Message { get; set; }
        public long TaskId { get; set; }

    }
}