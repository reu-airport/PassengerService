using System;
using System.Text.Json;

namespace PassengerService.DTO
{
    public enum EventType
    {
        Arrival, // Boarding
        Departure
    }

    public class AirplaneEvent
    {
        public AirplaneEvent()
        {
        }

        public Guid PlaneId { get; set; }

        public EventType EventType { get; set; }

        public DateTime DepartureTime { get; set; }

        public static AirplaneEvent Deserialize(byte[] body)
        {
            return JsonSerializer.Deserialize<AirplaneEvent>(body);
        }
    }
}
