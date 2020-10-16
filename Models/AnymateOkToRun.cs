using System;
using System.Collections.Generic;
using System.Text;

namespace Anymate.Models
{
    public class AnymateOkToRun
    {
        public bool GateOpen { get; set; }
        public bool TasksAvailable { get; set; }
        public bool NotBlockedDate { get; set; }
        public bool OkToRun { get; set; }

    }
}
