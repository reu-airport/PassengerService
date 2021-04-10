using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PassengerService.DTO
{
    [Serializable]

    public class BuyTicketRequest
    {
        public BuyTicketRequest(Guid passengerId, Guid flightId, bool hasBaggage, bool isVip, long timeStamp)
        {
            PassengerId = passengerId;
            FlightId = flightId;
            HasBaggage = hasBaggage;
            IsVip = isVip;
            TimeStamp = timeStamp;
        }

        public Guid PassengerId { get; init; }

        public Guid FlightId { get; init; }

        public bool HasBaggage { get; init; }

        public bool IsVip { get; init; }

        public long TimeStamp { get; init; }

        public byte[] Serialaize()
        {
            return JsonSerializer.SerializeToUtf8Bytes<BuyTicketRequest>(this);
        }
    }
}
