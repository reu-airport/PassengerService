using System;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace PassengerService.DTO
{
    [Serializable]

    public class CheckInResponse
    {
        public CheckInResponse(Guid passengerId, BoardingPass pass, CheckInResponseStatus status)
        {
            PassengerId = passengerId;
            Pass = pass;
            Status = status;
        }

        public Guid PassengerId { get; init; }

        //might be useless
        public BoardingPass Pass { get; init; }

        //might be useless
        public CheckInResponseStatus Status { get; init; }

        public static CheckInResponse Deserialize(byte[] body)
        {
            return JsonSerializer.Deserialize<CheckInResponse>(body);
        }
    }

    //might be useless
    public enum CheckInResponseStatus
    {
        //TODO
    }

    //might be useless
    public class BoardingPass
    {
        //TODO
    }
}
