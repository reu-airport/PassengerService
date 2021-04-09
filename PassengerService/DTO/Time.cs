using System;
using System.Text.Json;

namespace PassengerService.DTO
{
    public class Time
    {
        public Time(long time, long factor)
        {
            Unix_time_ms = time;
            Factor = factor;
        }

        public long Unix_time_ms { get; init; }

        public long Factor { get; init; }

        public static Time Deserialize(byte[] body)
        {
            return JsonSerializer.Deserialize<Time>(body);
        }
    }
}
