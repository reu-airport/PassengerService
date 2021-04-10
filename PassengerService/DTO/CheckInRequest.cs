using System;
using System.Text.Json;

namespace PassengerService.DTO
{
    [Serializable]

    public class CheckInRequest
    {
        public CheckInRequest(Guid passengerId, FlightTicket ticket, long timeStamp)
        {
            PassengerId = passengerId;
            Ticket = ticket;
            TimeStamp = timeStamp;
        }

        public Guid PassengerId { get; init; }

        public FlightTicket Ticket { get; init; }

        public long TimeStamp { get; init; }

        public byte[] Serialize()
        {
            return JsonSerializer.SerializeToUtf8Bytes<CheckInRequest>(this);
        }
    }
}
