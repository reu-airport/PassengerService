using System;
using System.Text.Json;

namespace PassengerService.DTO
{
    [Serializable]
    public class Time
    {
        public Time(long factor, long time)
        {
            this.time = time;
            this.factor = factor;
        }

        public long factor { get; init; }

        public long time { get; init; }

        public static Time Deserialize(byte[] body)
        {
            return JsonSerializer.Deserialize<Time>(body);
        }
    }
}
