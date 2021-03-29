using System;
namespace PassengerService.DTO
{
    [Serializable]

    public class RefundTicketRequest
    {
        public RefundTicketRequest(Guid passengerId, FlightTicket ticket)
        {
            PassengerId = passengerId;
            Ticket = ticket;

            //TIMESTAMP
        }

        public Guid PassengerId { get; init; }

        public FlightTicket Ticket { get; init; }

        //TIMESTAMP
    }
}
