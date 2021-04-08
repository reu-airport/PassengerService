﻿using System;
namespace PassengerService.DTO
{
    public class Flight
    {
        public Flight(Guid id, FlightDirection direction)
        {
            Id = id;
            Direction = direction;
        }

        public Guid Id { get; init; }

        public FlightDirection Direction { get; init; }

        //Time {get; init; }

        //CheckInBeginTime { get; init; }

        //CheckInEndTime { get; init; }

        //HasVips { get; init; }

        //HasBaggage { get; init; }

        //Airplane { get; init; }
    }

    public enum FlightDirection
    {
        Depurture,
        Arrrival
    }

    
}
