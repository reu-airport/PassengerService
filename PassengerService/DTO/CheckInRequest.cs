using System;
namespace PassengerService.DTO
{
    public class CheckInRequest
    {
        public CheckInRequest(Guid passengerId, FlightTicket ticket)
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
