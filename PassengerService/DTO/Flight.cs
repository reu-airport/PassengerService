using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PassengerService.DTO
{
    [Serializable]
    public class Flight
    {
        public Flight(Guid id, string direction, long time, long checkInBeginTime, long checkInEndTime, bool hasVips, bool hasBaggage, Airplane airplane, int gateNum)
        {
            Id = id;
            Direction = direction;
            Time = time;
            CheckInBeginTime = checkInBeginTime;
            CheckInEndTime = checkInEndTime;
            HasVips = hasVips;
            HasBaggage = hasBaggage;
            this.airplane = airplane;
            GateNum = gateNum;
        }

        [JsonPropertyName("id")]
        public Guid Id { get; init; }

        public string Direction { get; init; }

        public long Time {get; init; }

        public long CheckInBeginTime { get; init; }

        public long CheckInEndTime { get; init; }

        public bool HasVips { get; init; }

        public bool HasBaggage { get; init; }

        public Airplane airplane { get; init; }

        public int GateNum { get; init; }

        public static Flight Deserialize(byte[] body)
        {
            return JsonSerializer.Deserialize<Flight>(body);
        }
    }

    [Serializable]
    public struct Airplane
    {
        public Airplane(Guid id, int capacity, bool refuekNeeded, bool isFlight)
        {
            Id = id;
            Capacity = capacity;
            RefuelNeeded = refuekNeeded;
            IsFlight = isFlight;
        }

        public Guid Id { get; init; }

        public int Capacity { get; init; }

        public bool RefuelNeeded { get; init; }

        public bool IsFlight { get; init; }
    }
}
