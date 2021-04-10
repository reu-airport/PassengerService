using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PassengerService.DTO
{
    [Serializable]

    public class BuyTicketRequest
    {
        public BuyTicketRequest(Guid passengerId, Guid flightId, bool hasBaggage, bool isVip, long time)
        {
            PassengerId = passengerId;
            FlightId = flightId;
            HasBaggae = hasBaggage;
            IsVip = isVip;
            Time = time;
        }

        public Guid PassengerId { get; init; }

        public Guid FlightId { get; init; }

        public bool HasBaggae { get; init; }

        public bool IsVip { get; init; }

        public long Time { get; init; }

        public byte[] Serialaize()
        {
            return JsonSerializer.SerializeToUtf8Bytes<BuyTicketRequest>(this);
        }
    }
}
