using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using Cassandra.Mapping;
using Cassandra.Mapping.Attributes;

namespace BackupDatabase.Models
{
    public enum Periodicity : byte
    {
        None = byte.MaxValue,
        Yearly = 1,
        Monthly = 1 << 1,
        Weekly = 1 << 2,
        Daily = 2 << 3,
        Hourly = 2 << 4
    }

    [Table(Name = "calendar", AllowFiltering = true)]
    public class DBCalendarEntry
    {
        [JsonProperty("id", DefaultValueHandling = DefaultValueHandling.Ignore)]
        [Column("id")]
        [PartitionKey]
        public Guid Id { get; set; }

        [JsonProperty("server")]
        [Column("server")]
        public Guid Server { get; set; }

        [JsonProperty("enabled")]
        [Column("enabled")]
        [SecondaryIndex]
        public bool Enabled { get; set; }

        [JsonProperty("items")]
        [Column("items")]
        public IEnumerable<string> Items { get; set; }

        [JsonProperty("lastrun", DefaultValueHandling = DefaultValueHandling.Ignore)]
        [Column("lastrun")]
        public DateTime LastRun { get; set; }

        [JsonProperty("nextrun", DefaultValueHandling = DefaultValueHandling.Ignore)]
        [Column("nextrun")]
        public DateTime NextRun { get; set; }

        [JsonProperty("firstrun", DefaultValueHandling = DefaultValueHandling.Ignore)]
        [Column("firstrun")]
        public DateTime FirstRun { get; set; }

        [JsonProperty("periodicity")]
        [Column("periodicity", Type = typeof (string))]
        public Periodicity Periodicity { get; set; }

        [JsonProperty("values")]
        [Column("values")]
        public int[] Values { get; set; }


        public void UpdateNextRun()
        {

            var now = DateTime.UtcNow;
            Array.Sort(Values);


            switch (Periodicity)
            {
                case Periodicity.Yearly:
                    {
                        int yearToRun = LastRun.Year < now.Year ? now.Year : now.Year + 1;
                        int yearToAdd = yearToRun - FirstRun.Year;
                        NextRun = FirstRun.AddYears(yearToAdd);
                    }
                    break;
                case Periodicity.Monthly:
                    {
                        int monthToRun = -1;
                        int monthToAdd = -1;

                        try
                        {
                            monthToRun = Values.First(month => month > now.Month);
                            monthToAdd = monthToRun - now.Month;
                        }
                        catch
                        {
                            try
                            {
                                monthToRun = Values.Reverse().First(month => month < now.Month);
                                monthToAdd = monthToRun + 12 - now.Month;
                            }
                            catch { }
                        }

                        if (monthToRun > 0)
                        {
                            NextRun = (new DateTime(now.Year, now.Month, 1)).AddMonths(monthToAdd);
                            NextRun = new DateTime(NextRun.Year, NextRun.Month, Math.Min(FirstRun.Day, DateTime.DaysInMonth(NextRun.Year, NextRun.Month)), FirstRun.Hour, FirstRun.Minute, FirstRun.Second, FirstRun.Millisecond);
                        }

                    }
                    break;
                case Periodicity.Weekly:
                    {
                        var start = new DateTime(now.Year, now.Month, 1, FirstRun.Hour, FirstRun.Minute, FirstRun.Second, FirstRun.Millisecond);

                        var dayOfWeek = FirstRun.DayOfWeek;
                        DateTime found = FirstDateFromDayOfWeek(dayOfWeek, start);

                        foreach (int week in Values)
                        {
                            var maybe = found.AddDays((week - 1) * 7);
                            if (maybe > now)
                            {
                                if (maybe.Month > now.Month)
                                {
                                    NextRun = FirstDateFromDayOfWeek(dayOfWeek, start.AddMonths(1)).AddDays((Values[0] - 1) * 7);
                                }
                                else
                                    NextRun = maybe;
                                break;
                            }
                        }

                    }
                    break;
                case Periodicity.Daily:
                    {
                        NextRun = new DateTime(
                            Math.Max(now.Year, FirstRun.Year),
                            Math.Max(now.Month, FirstRun.Month),
                            Math.Max(now.Day, FirstRun.Day),
                            FirstRun.Hour,
                            FirstRun.Minute,
                            FirstRun.Second,
                            FirstRun.Millisecond);

                        if (NextRun < now)
                            NextRun = NextRun.AddDays(1);
                    }
                    break;
                case Periodicity.Hourly:
                    {
                        NextRun = new DateTime(
                            Math.Max(now.Year, FirstRun.Year),
                            Math.Max(now.Month, FirstRun.Month), 
                            Math.Max(now.Day, FirstRun.Day), 
                            Math.Max(now.Hour, FirstRun.Hour), 
                            FirstRun.Minute,
                            FirstRun.Second,
                            FirstRun.Millisecond);

                        if (NextRun < now)
                            NextRun = NextRun.AddHours(1);
                    }
                    break;
                default: // No periodicity => disable this entry
                    NextRun = DateTime.MaxValue;
                    Enabled = false;
                    break;
            }

        }

        private DateTime FirstDateFromDayOfWeek(DayOfWeek dayOfWeek, DateTime date)
        {
            var dDay = date.DayOfWeek;
            if (dDay < dayOfWeek)
            {
                return date.AddDays(dayOfWeek - dDay);
            }
            else if (dDay > dayOfWeek)
            {
                return date.AddDays(dayOfWeek + 7 - dDay);
            }

            return date;

        }

    }
}
