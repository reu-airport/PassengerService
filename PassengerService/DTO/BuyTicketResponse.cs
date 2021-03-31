using System;
using System.Text.Json;

namespace PassengerService.DTO
{
    [Serializable]

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

        public static BuyTicketResponse Deserialize(byte[] body)
        {
            return JsonSerializer.Deserialize<BuyTicketResponse>(body);
        }
    }

    //might be useless
    public enum BuyTicketResponseStatus
    {
        Success,
        Error
    }
}
