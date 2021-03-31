using System;
using System.Text.Json;

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

        public byte[] Serialize()
        {
            return JsonSerializer.SerializeToUtf8Bytes<RefundTicketRequest>(this);
        }     
    }
}
