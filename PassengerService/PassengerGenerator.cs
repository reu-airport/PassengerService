using System;
using System.Net.Http.Headers;
using PassengerService.DTO;

namespace PassengerService
{
    public class PassengerGenerator
    {
        public const double HAS_BAGGAGE_CHANCE = 0.6;
        public const double IS_VIP_CHANCE = 0.1;

        public PassengerGenerator()
        {

        }

        private readonly Random random = new Random();

        public Passenger GeneratePassenger()
        {
            Passenger passenger = new Passenger(
                hasBaggage: random.NextDouble() <= HAS_BAGGAGE_CHANCE,
                isVip: random.NextDouble() <= IS_VIP_CHANCE
            );

            return passenger;     
        }
    }
}
