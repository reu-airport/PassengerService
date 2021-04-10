using System;
using System.Text.Json;

namespace PassengerService.DTO
{
    [Serializable]

    public class BuyTicketResponse
    {
        public BuyTicketResponse(Guid passengerId, FlightTicket ticket)
        {
            PassengerId = passengerId;
            Ticket = ticket;
        }
        
        public Guid PassengerId { get; init; }

        public FlightTicket Ticket { get; init; }

        public static BuyTicketResponse Deserialize(byte[] body)
        {
            return JsonSerializer.Deserialize<BuyTicketResponse>(body);
        }
    }
}
