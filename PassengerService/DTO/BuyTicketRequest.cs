using System;
namespace PassengerService.DTO
{
    public class BuyTicketRequest
    {
        public BuyTicketRequest(Guid passengerId, Guid flightId, bool hasBaggage, bool isVip)
        {
            PassengerId = passengerId;
            FlightId = flightId;
            HasBaggae = hasBaggage;
            IsVip = isVip;

            //TIMESTAMP
        }

        public Guid PassengerId { get; init; }

        public Guid FlightId { get; init; }

        public bool HasBaggae { get; init; }

        public bool IsVip { get; init; }

        //TIMESTAMP
    }
}
