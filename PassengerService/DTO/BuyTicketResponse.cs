using System;
namespace PassengerService.DTO
{
    public class BuyTicketResponse
    {
        public BuyTicketResponse(Guid passengerId, BuyTicketResponseStatus status, FlightTicket ticket)
        {
            PassengerId = passengerId;
            Status = status;
            Ticket = ticket;
        }

        public Guid PassengerId { get; init; }

        //might be useless
        public BuyTicketResponseStatus Status { get; init; }

        public FlightTicket Ticket { get; init; }
    }

    //might be useless
    public enum BuyTicketResponseStatus
    {
        //TODO
    }
}
