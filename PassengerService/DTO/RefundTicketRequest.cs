using System;
using System.Text.Json;

namespace PassengerService.DTO
{
    [Serializable]

    public class RefundTicketRequest
    {
        public RefundTicketRequest(Guid passengerId, FlightTicket ticket, long timeStamp)
        {
            PassengerId = passengerId;
            Ticket = ticket;
            TImeStamp = timeStamp;
        }

        public Guid PassengerId { get; init; }

        public FlightTicket Ticket { get; init; }

        public long TImeStamp { get; init; }

        public byte[] Serialize()
        {
            return JsonSerializer.SerializeToUtf8Bytes<RefundTicketRequest>(this);
        }     
    }
}
