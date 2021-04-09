using System;
using System.Text.Json;

namespace PassengerService.DTO
{
    public class Flight
    {
        public Flight(Guid id, FlightDirection direction)
        {
            Id = id;
            Direction = direction;
        }

        public Guid Id { get; init; }

        public FlightDirection Direction { get; init; }

        public long Time {get; init; }

        public long CheckInBeginTime { get; init; }

        public long CheckInEndTime { get; init; }

        public bool HasVips { get; init; }

        public bool HasBaggage { get; init; }

        public static Flight Deserialize(byte[] body)
        {
            return JsonSerializer.Deserialize<Flight>(body);
        }
    }

    public enum FlightDirection
    {
        Depurture,
        Arrrival
    }

    
}
