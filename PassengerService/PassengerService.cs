using System;
using RabbitMQ.Client;
using System.Collections.Generic;
using System.Collections.Concurrent;
using PassengerService.DTO;
using System.Threading.Tasks;
using System.Threading;
using RabbitMQ.Client.Events;
using System.Text.Json;

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

        private const double PASSENGER_GENERATION_CHANCE = 0.5;
        private const double PASSENGER_ACTIVITY_CHANCE = 0.5; 

        private const int PASSENGER_GENERATION_PERIOD_MS = 10 * 1000;
        private const int PASSENGER_ACTIVITY_PERIOD_MS = 15 * 1000;

        private readonly IConnection connection;
        private readonly IModel channel;

        private ConcurrentQueue<Passenger> newPassengers = new ConcurrentQueue<Passenger>();
        private ConcurrentQueue<Passenger> passengersWithTickets = new ConcurrentQueue<Passenger>();
        private ConcurrentDictionary<Guid, Passenger> waitingForResponsePassengers = new ConcurrentDictionary<Guid, Passenger>();

        private readonly Dictionary<Queues, string> queues = new Dictionary<Queues, string>()
        {
            [Queues.PassengerBuyQueue] = "PassengerBuyQueue",
            [Queues.BuyPassengerQueue] = "BuyPassengerQueue",
            [Queues.PassengerRefundQueue] = "PassengerRefundQueue",
            [Queues.RefundPassengerQueue] = "RefundPassengerQueue",
            [Queues.PassengerToCheckInQueue] = "PassengerToCheckInQueue",
            [Queues.CheckInToPassengerQueue] = "CheckInToPassengerQueue",
        };

        private readonly string infoPanelExchangeName = "InfoPanel";
        private string infoPanelQueueName;
        private List<AirplaneEvent> AirplaneSchedule;
        
        public PassengerService()
        {
            var factory = new ConnectionFactory()
            {
                
            };

            connection = factory.CreateConnection();
            channel = connection.CreateModel();

            //declare and purge each message queue
            foreach (var queue in queues)
            {
                channel.QueueDeclare(queue.Value, true, false, false, null);
                channel.QueuePurge(queue.Value);
            }

            //declare an exchange for info panel
            channel.ExchangeDeclare(infoPanelExchangeName, ExchangeType.Fanout);
            infoPanelQueueName = channel.QueueDeclare().QueueName;
            channel.QueueBind(infoPanelQueueName, infoPanelExchangeName, "", null);        
        }

        public void Run()
        {
            var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

            var random = new Random();
            var passengerGenerator = new PassengerGenerator();

            var infoPanelConsumer = new EventingBasicConsumer(channel);
            infoPanelConsumer.Received += (model, ea) =>
            {
                var body = ea.Body.ToArray();
                AirplaneSchedule = JsonSerializer.Deserialize<List<AirplaneEvent>>(body);
            };

            channel.BasicConsume(
                queue: infoPanelQueueName,
                autoAck: true,
                consumer: infoPanelConsumer);


            //Task generates passengers
            Task.Run(() =>
            {
                try
                {
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        if (random.NextDouble() <= PASSENGER_GENERATION_CHANCE)
                        {
                            var passenger = passengerGenerator.GeneratePassenger();

                            Console.WriteLine($"A new passenger:\t{passenger.Id}");

                            newPassengers.Enqueue(passenger);                    
                        }

                        Thread.Sleep(PASSENGER_GENERATION_PERIOD_MS);
                    }
                }
                catch(Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }, cancellationToken);

            //Task sends new passengers do something
            Task.Run(() =>
            {
                try
                {
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        if (random.NextDouble() <= PASSENGER_ACTIVITY_CHANCE)
                        {
                            if (newPassengers.TryDequeue(out var passenger))
                            {
                                BuyTicketAction(passenger);
                                waitingForResponsePassengers.TryAdd(passenger.Id, passenger);
                            }
                        }

                        Thread.Sleep(PASSENGER_ACTIVITY_PERIOD_MS);
                    }
                }
                catch(Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }, cancellationToken);

            Console.ReadLine();
            cancellationTokenSource.Cancel();
        }

        private void BuyTicketAction(Passenger passenger)
        {
            int airplanes = AirplaneSchedule.Count;

            foreach(var airplaneEvent in AirplaneSchedule)
            {
                if (airplaneEvent.EventType == EventType.Departure)
                {
                    var request = new BuyTicketRequest(
                        passengerId: passenger.Id,
                        flightId: airplaneEvent.PlaneId,
                        hasBaggage: passenger.HasBaggage,
                        isVip: passenger.IsVip);

                    SendBuyTicketRequest(request);
                }
            }
        }

        //Cashbox response processing
        private void HandleBuyTicketResponse(BuyTicketResponse response)
        {
            Guid passengerId = response.PassengerId;
            waitingForResponsePassengers.TryRemove(passengerId, out var passenger);

            var status = response.Status;
            if (status == BuyTicketResponseStatus.Success)
            {
                passenger.Ticket = response.Ticket;
                passengersWithTickets.Enqueue(passenger);
            }
            else if(passenger.Ticket != null)
            {
                passengersWithTickets.Enqueue(passenger);
            }
            else
            {
                newPassengers.Enqueue(passenger);
            }
          
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
