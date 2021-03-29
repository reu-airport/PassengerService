using System;
namespace PassengerService.DTO
{
    [Serializable]

    public class Passenger
    {
        public Passenger()
        {
        
        }

        public Guid Id { get; init; } = Guid.NewGuid();

        public bool HasBaggage { get; set; }

        public bool IsVip { get; set; }

        //might be useless
        public PassengerStatus Status { get; set; }

        public FlightTicket Ticket { get; set; } = null;
    }

    //might be useless
    public enum PassengerStatus
    {

    }
}
