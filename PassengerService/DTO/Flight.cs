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

        //time {get; init; }

        
    }

    public enum FlightDirection
    {
        Depurture,
        Arrrival
    }

    
}