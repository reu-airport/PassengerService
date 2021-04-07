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

        private CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private CancellationToken cancellationToken;

        PassengerGenerator passengerGenerator = new PassengerGenerator();

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

        private EventingBasicConsumer buyPassengerQueueConsumer;
        private EventingBasicConsumer refundPassengerQueueConsumer;
        private EventingBasicConsumer checkInToPassengerQueueConsumer;
        private EventingBasicConsumer infoPanelConsumer;


        private readonly string infoPanelExchangeName = "InfoPanel";
        private string infoPanelQueueName;
        private List<AirplaneEvent> AirplaneSchedule;

        Random random = new Random();
        
        public PassengerService()
        {
            cancellationToken = cancellationTokenSource.Token;

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


            buyPassengerQueueConsumer = new EventingBasicConsumer(channel);
            buyPassengerQueueConsumer.Received += (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var response = BuyTicketResponse.Deserialize(body);
                HandleBuyTicketResponse(response);
            };

            refundPassengerQueueConsumer = new EventingBasicConsumer(channel);
            refundPassengerQueueConsumer.Received += (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var response = RefundTicketResponse.Deserialize(body);
                HandleRefundTicketResponse(response);
            };

            checkInToPassengerQueueConsumer = new EventingBasicConsumer(channel);
            checkInToPassengerQueueConsumer.Received += (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var response = CheckInResponse.Deserialize(body);
                HandleCheckInResponse(response);
            };

            infoPanelConsumer = new EventingBasicConsumer(channel);
            infoPanelConsumer.Received += (model, ea) =>
            {
                var body = ea.Body.ToArray();
                AirplaneSchedule = JsonSerializer.Deserialize<List<AirplaneEvent>>(body);
            };
            channel.BasicConsume(
                queue: infoPanelQueueName,
                autoAck: true,
                consumer: infoPanelConsumer);
        }

        public void Run()
        {
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
            List<AirplaneEvent> departingAirplanes = new List<AirplaneEvent>();
            foreach (var airplaneEvent in AirplaneSchedule)
            {
                if (airplaneEvent.EventType == EventType.Departure)
                {
                    departingAirplanes.Add(airplaneEvent);
                }
            }

            try
            {
                int departingAirplanesCount = departingAirplanes.Count;
                var airplaneEvent = departingAirplanes[random.Next(departingAirplanesCount)];

                var request = new BuyTicketRequest(
                            passengerId: passenger.Id,
                            flightId: airplaneEvent.PlaneId,
                            hasBaggage: passenger.HasBaggage,
                            isVip: passenger.IsVip);

                SendBuyTicketRequest(request);
                waitingForResponsePassengers.TryAdd(passenger.Id, passenger);
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
            }   
        }

        private void RefundTicketAction(Passenger passenger)
        {
            var request = new RefundTicketRequest(
                passengerId: passenger.Id,
                ticket: passenger.Ticket);
            SendRefundTicketRequest(request);
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
            Guid passengerId = response.PassengerId;
            waitingForResponsePassengers.TryRemove(passengerId, out var passenger);

            if (response.IsRefunded)
            {
                Console.WriteLine("");
            }
            else
            {
                Console.WriteLine("");
            }
            passenger.Ticket = null;
            newPassengers.Enqueue(passenger);
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
