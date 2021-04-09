using System;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace PassengerService.DTO
{
    [Serializable]

    public class CheckInResponse
    {
        public const string LATE = "Late";

        public const string EARLY = "Early";

        public const string NO_TICKET = "No ticket";

        public CheckInResponse(Guid passengerId, bool isCheckedIn, string reason)
        {
            PassengerId = passengerId;
            IsCheckedIn = isCheckedIn;
            Reason = reason;
        }

        public Guid PassengerId { get; init; }

        public bool IsCheckedIn { get; init; }

        public string Reason { get; init; }

        public static CheckInResponse Deserialize(byte[] body)
        {
            return JsonSerializer.Deserialize<CheckInResponse>(body);
        }
    }
}
