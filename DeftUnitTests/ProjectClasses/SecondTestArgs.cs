using System;
using System.Collections.Generic;

namespace DeftUnitTests.ProjectClasses
{
    class TestArgs
    {
        public int Number { get; set; }
        public string Message { get; set; }
        public DateTime Date { get; set; }
        public List<int> NumberList { get; set; }
    }

    class TestResponse
    {
        public int NumberTimesTwo { get; set; }
        public string Message { get; set; }
        public DateTime DatePlusOneDay { get; set; }
        public List<int> SortedNumberList { get; set; }
    }
}
