using System;
using System.Text.Json;

namespace PassengerService.DTO
{
    [Serializable]

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

        public static RefundTicketResponse Deserialize(byte[] body)
        {
            return JsonSerializer.Deserialize<RefundTicketResponse>(body);
        }
    }
}
