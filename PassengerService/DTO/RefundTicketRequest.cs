using System;
using System.Text.Json;

namespace PassengerService.DTO
{
    [Serializable]

    public class RefundTicketRequest
    {
        public RefundTicketRequest(Guid passengerId, FlightTicket ticket, long time)
        {
            PassengerId = passengerId;
            Ticket = ticket;
            Time = time;
        }

        public Guid PassengerId { get; init; }

        public FlightTicket Ticket { get; init; }

        public long Time { get; init; }

        public byte[] Serialize()
        {
            return JsonSerializer.SerializeToUtf8Bytes<RefundTicketRequest>(this);
        }     
    }
}
