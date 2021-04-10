using System;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PassengerService.DTO
{
    [Serializable]

    public class CheckInResponse
    {
        public const string LATE = "Late";

        public const string EARLY = "Early";

        public const string NO_TICKET = "No ticket";

        public CheckInResponse(Guid passengerId, bool isChecked, string reason)
        {
            PassengerId = passengerId;
            IsChecked = isChecked;
            Reason = reason;
        }

        [JsonPropertyName("PassengerId")]
        public Guid PassengerId { get; init; }

        [JsonPropertyName("IsChecked")]
        public bool IsChecked { get; init; }

        [JsonPropertyName("Reason")]
        public string Reason { get; init; }

        public static CheckInResponse Deserialize(byte[] body)
        {
            return JsonSerializer.Deserialize<CheckInResponse>(body);
        }
    }
}
