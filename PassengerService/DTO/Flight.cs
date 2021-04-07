using System;
namespace PassengerService.DTO
{
    public class Flight
    {
        public Flight()
        {
        }

        Guid Id { get; init; }

        FlightDirection Direction{ get; init; }

        //time {get; init; }

        

    }

    public enum FlightDirection
    {
        Depurture,
        Arrrival
    }

    
}
