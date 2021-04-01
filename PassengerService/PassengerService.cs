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
        public enum Queues
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

        private Dictionary<Queues, string> queues = new Dictionary<Queues, string>()
        {
            [Queues.PassengerBuyQueue] = "PassengerBuyQueue",
            [Queues.BuyPassengerQueue] = "BuyPassengerQueue",
            [Queues.PassengerRefundQueue] = "PassengerRefundQueue",
            [Queues.RefundPassengerQueue] = "RefundPassengerQueue",
            [Queues.PassengerToCheckInQueue] = "PassengerToCheckInQueue",
            [Queues.CheckInToPassengerQueue] = "CheckInToPassengerQueue",
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

            channel.BasicPublish("", queues[Queues.PassengerBuyQueue], null, body);
        }

        private void SendCheckInRequest(CheckInRequest request)
        {
            byte[] body = request.Serialize();

            channel.BasicPublish("", queues[Queues.PassengerToCheckInQueue], null, body);
        }

        private void SendRefundTicketRequest(RefundTicketRequest request)
        {
            byte[] body = request.Serialize();

            channel.BasicPublish("", queues[Queues.PassengerRefundQueue], null, body);
        }

        public void Dispose()
        {
            channel.Close();
            connection.Close();
        }
    }
}
