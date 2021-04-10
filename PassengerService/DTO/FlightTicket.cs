using System;
using System.Text.Json;

namespace PassengerService.DTO
{
    [Serializable]
    public class FlightTicket
    {
        public FlightTicket(Guid passengerId, Guid flightId, bool hasBaggage, bool isVip)
        {
            PassengerId = passengerId;
            FlightId = flightId;
            HasBaggage = hasBaggage;
            IsVip = isVip;

            //TIMESTAMP
        }

        public Guid PassengerId { get; init; }

        public Guid FlightId { get; init; }

        public bool HasBaggage { get; init; }

        public bool IsVip { get; init; }

        //TIMESTAMP

        public byte[] Serialize()
        {
            return JsonSerializer.SerializeToUtf8Bytes<FlightTicket>(this);
        }

        public static FlightTicket Deserialize(byte[] body)
        {
            return JsonSerializer.Deserialize<FlightTicket>(body);
        }
    }
}
