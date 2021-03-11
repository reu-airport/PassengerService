using System;
namespace PassengerService.DTO
{
    public class Passenger
    {
        public Passenger()
        {
            //TODO
        }

        public Guid Id { get; init; }

        //might be useless
        public PassengerStatus Status { get; set; }

        public FlightTicket Ticket { get; set; } = null;
    }

    //might be useless
    public enum PassengerStatus
    {

    }
}
