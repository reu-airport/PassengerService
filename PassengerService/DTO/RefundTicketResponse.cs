using System;
namespace PassengerService.DTO
{
    public class RefundTicketResponse
    {
        public RefundTicketResponse(Guid passengerId, FlightTicket ticket, bool isRefunded)
        {
            PassengerId = passengerId;
            Ticket = ticket;
            IsRefunded = isRefunded;
        }

        public Guid PassengerId { get; init; }

        //ne ponel nahuya
        public FlightTicket Ticket { get; init; }

        public bool IsRefunded { get; init; }
    }
}
