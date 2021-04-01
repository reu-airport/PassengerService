using System;

namespace PassengerService
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var passengerService = new PassengerService())
            {
                passengerService.Run();
            }
                
            
        }
    }
}
