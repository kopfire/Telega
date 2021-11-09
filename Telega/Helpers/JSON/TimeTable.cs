using System.Collections.Generic;

namespace Telega.Helpers.JSON
{
    public class TimeTable
    {
        

        public string Id { get; set; }

        public string Speciality { get; set; }

        public string Group { get; set; }

        public Week[] Weeks { get; set; }

        public List<long> Students { get; set; }
    }
}
