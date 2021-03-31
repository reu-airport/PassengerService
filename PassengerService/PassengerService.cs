using System;
using RabbitMQ.Client;

using PassengerService.DTO;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace PassengerService
{
    public class PassengerService
    {
        private const string PassengerToCashBoxQueue = "PassengerToCashBoxQueue";
        private const string CashBoxToPassengerQueue = "CashBoxToPassengerQueue";

        private const string PassengerToCheckInQueue = "PassengerToCheckInQueue";
        private const string CheckInToPassengerQueue = "CheckInToPassengerQueue";


        public PassengerService()
        {
        }

        public void Run()
        {
            var factory = new ConnectionFactory()
            {
                //TODO
            };

            using (IConnection connection = factory.CreateConnection())
            {
                using (IModel channel = connection.CreateModel())
                {

                }
            }

        }

        private void HandleBuyTicketResponse(BuyTicketResponse response)
        {

        }

        private void HandleCheckInResponse(CheckInResponse response)
        {

        }

        private void HandleRefundTicketResponse(RefundTicketResponse response)
        {

        }

        private void SendBuyTicketRequest(BuyTicketRequest request)
        {
            
        }

        private void SendCheckInRequest(CheckInRequest request)
        {

        }

        private void SendRefundTicketRequest(RefundTicketRequest request)
        {

        }
    }
}
