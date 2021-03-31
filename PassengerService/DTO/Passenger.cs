using System;
using System.Text.Json;

namespace PassengerService.DTO
{
    [Serializable]

    public class Passenger
    {
        public Passenger(bool hasBaggage, bool isVip)
        {
            HasBaggage = hasBaggage;
            IsVip = isVip;
        }

        public Guid Id { get; init; } = Guid.NewGuid();

        public bool HasBaggage { get; set; }

        public bool IsVip { get; set; }

        //might be useless
        public PassengerStatus Status { get; set; }

        public FlightTicket Ticket { get; set; } = null;

        public byte[] Serialize()
        {
            return JsonSerializer.SerializeToUtf8Bytes<Passenger>(this);
        }

        public static Passenger Deserialize(byte[] body)
        {
            return JsonSerializer.Deserialize<Passenger>(body);
        }
    }

    //might be useless
    public enum PassengerStatus
    {

    }
}
