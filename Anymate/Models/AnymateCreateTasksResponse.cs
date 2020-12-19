using System.Collections.Generic;

namespace Anymate
{
    public class AnymateCreateTasksResponse
    {

        public bool Succeeded { get; set; } = false;
        public string Message { get; set; }
        public IEnumerable<long> TaskIds { get; set; }

    }
}