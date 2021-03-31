using System;
using RabbitMQ.Client;

using PassengerService.DTO;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Channels;
using System.Security.Cryptography;
using System.Collections.Generic;

namespace PassengerService
{
    public class PassengerService : IDisposable
    {
        public enum QueueName
        {
            PassengerBuyQueue,
            BuyPassengerQueue,
            PassengerRefundQueue,
            RefundPassengerQueue,
            PassengerToCheckInQueue,
            CheckInToPassengerQueue,
        }

        private readonly IConnection connection;
        private readonly IModel channel;

        private Dictionary<QueueName, string> queues = new Dictionary<QueueName, string>()
        {
            [QueueName.PassengerBuyQueue] = "PassengerBuyQueue",
            [QueueName.BuyPassengerQueue] = "BuyPassengerQueue",
            [QueueName.PassengerRefundQueue] = "PassengerRefundQueue",
            [QueueName.RefundPassengerQueue] = "RefundPassengerQueue",
            [QueueName.PassengerToCheckInQueue] = "PassengerToCheckInQueue",
            [QueueName.CheckInToPassengerQueue] = "CheckInToPassengerQueue",
        };      

        public PassengerService()
        {
            var factory = new ConnectionFactory()
            {
                //TODO
            };

            connection = factory.CreateConnection();
            IModel channel = connection.CreateModel();
        }

        public void Run()
        {
            //declare each message queue
            foreach(var queue in queues)
            {
                channel.QueueDeclare(queue.Value, true, false, false, null);
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
            byte[] body = request.Serialaize();

            channel.BasicPublish("", queues[QueueName.PassengerBuyQueue], null, body);
        }

        private void SendCheckInRequest(CheckInRequest request)
        {
            byte[] body = request.Serialize();

            channel.BasicPublish("", queues[QueueName.PassengerToCheckInQueue], null, body);
        }

        private void SendRefundTicketRequest(RefundTicketRequest request)
        {
            byte[] body = request.Serialize();

            channel.BasicPublish("", queues[QueueName.PassengerRefundQueue], null, body);
        }

        public void Dispose()
        {
            channel.Close();
            connection.Close();
        }
    }
}
