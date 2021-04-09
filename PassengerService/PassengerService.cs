using System;
using RabbitMQ.Client;
using System.Collections.Generic;
using System.Collections.Concurrent;
using PassengerService.DTO;
using System.Threading.Tasks;
using System.Threading;
using RabbitMQ.Client.Events;
using System.Text.Json;
using System.Net.Http;

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
        private const double REFUND_TICKET_CHANCE = 0.05;
        private const double DO_NORMAL_ACTION_CHANCE = 0.95;

        private const int PASSENGER_GENERATION_PERIOD_MS = 5 * 1000;
        private const int PASSENGER_ACTIVITY_PERIOD_MS = 4 * 1000;

        //IDK WHY I NEED IT
        private CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private CancellationToken cancellationToken;

        private readonly PassengerGenerator passengerGenerator = new PassengerGenerator();

        private ConcurrentQueue<Passenger> newPassengers = new ConcurrentQueue<Passenger>();
        private ConcurrentQueue<Passenger> passengersWithTickets = new ConcurrentQueue<Passenger>();
        private ConcurrentDictionary<Guid, Passenger> waitingForResponsePassengers = new ConcurrentDictionary<Guid, Passenger>();

        private Flight[] availableFlights;

        private readonly Dictionary<Queues, string> queues = new Dictionary<Queues, string>()
        {
            [Queues.PassengerBuyQueue] = "CashboxBuyTicket",
            [Queues.BuyPassengerQueue] = "BuyPassengerQueue",
            [Queues.PassengerRefundQueue] = "CashboxRefundTicket",
            [Queues.RefundPassengerQueue] = "RefundPassengerQueue",
            [Queues.PassengerToCheckInQueue] = "PassengerToCheckInQueue",
            [Queues.CheckInToPassengerQueue] = "Ticket",
        };

        private string infoPanelQueueName;
        private readonly string infoPanelExchangeName = "InfoPanel";

        private EventingBasicConsumer buyPassengerQueueConsumer;
        private EventingBasicConsumer refundPassengerQueueConsumer;
        private EventingBasicConsumer checkInToPassengerQueueConsumer;
        private EventingBasicConsumer infoPanelConsumer;

        private readonly IConnection connection;
        private readonly IModel channel;

        Random random = new Random();

        HttpClient client = new HttpClient();

        public PassengerService()
        {
            cancellationToken = cancellationTokenSource.Token;

            var factory = new ConnectionFactory()
            {
                Uri = new Uri("amqps://avfepwdu:SS4fTAg36RK1hPQAUnyC6TH-4Mf3uyJo@fox.rmq.cloudamqp.com/avfepwdu")
                //Uri = new Uri("amqps://sznpfban:Tx-Pxw7Hnr3qYpewSDnTEXAearxBt21h@cow.rmq2.cloudamqp.com/sznpfban")              
            };

            connection = factory.CreateConnection();
            channel = connection.CreateModel();

            //declare and purge each message queue
            foreach (var queue in queues)
            {
                channel.QueueDeclare(queue.Value, true, false, false, null);
                channel.QueuePurge(queue.Value);
            }

            buyPassengerQueueConsumer = new EventingBasicConsumer(channel);
            buyPassengerQueueConsumer.Received += (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var response = BuyTicketResponse.Deserialize(body);
                HandleBuyTicketResponse(response);
            };
            channel.BasicConsume(
                queue: queues[Queues.BuyPassengerQueue],
                autoAck: true,
                consumer: buyPassengerQueueConsumer);

            refundPassengerQueueConsumer = new EventingBasicConsumer(channel);
            refundPassengerQueueConsumer.Received += (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var response = RefundTicketResponse.Deserialize(body);
                HandleRefundTicketResponse(response);
            };
            channel.BasicConsume(
                queue: queues[Queues.RefundPassengerQueue],
                autoAck: true,
                consumer: refundPassengerQueueConsumer);

            checkInToPassengerQueueConsumer = new EventingBasicConsumer(channel);
            checkInToPassengerQueueConsumer.Received += (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var response = CheckInResponse.Deserialize(body);
                HandleCheckInResponse(response);
            };
            channel.BasicConsume(
                queue: queues[Queues.CheckInToPassengerQueue],
                autoAck: true,
                consumer: checkInToPassengerQueueConsumer);

            infoPanelConsumer = new EventingBasicConsumer(channel);
            infoPanelConsumer.Received += (model, ea) =>
            {
                var body = ea.Body.ToArray();
                availableFlights = JsonSerializer.Deserialize<Flight[]>(body);
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

                            newPassengers.Enqueue(passenger);

                            Console.WriteLine($"A new passenger:\t{passenger.Id}");
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
                    Thread.Sleep(PASSENGER_ACTIVITY_PERIOD_MS);

                    while (!cancellationToken.IsCancellationRequested)
                    {
                        if (random.NextDouble() <= PASSENGER_ACTIVITY_CHANCE)
                        {                           
                            if (newPassengers.TryDequeue(out var passenger))
                            {
                                if (random.NextDouble() <= DO_NORMAL_ACTION_CHANCE)
                                {
                                    BuyTicketAction(passenger);
                                }
                                else
                                {
                                    if (random.NextDouble() <= 0.5)
                                    {
                                        RefundTicketAction(passenger);
                                    }
                                    else
                                    {
                                        CheckInAction(passenger);
                                    }
                                }

                                waitingForResponsePassengers.TryAdd(passenger.Id, passenger);
                            }
                            else
                            {
                                throw new Exception("Cannot dequeue a new passenger");
                            }      
                        }
                    }
                }
                catch(Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }, cancellationToken);

            //Task sends passengers with tickets do something
            Task.Run(() =>
            {
                try
                {
                    Thread.Sleep(PASSENGER_ACTIVITY_PERIOD_MS);

                    while (!cancellationToken.IsCancellationRequested)
                    {
                        if (random.NextDouble() <= PASSENGER_ACTIVITY_CHANCE)
                        {
                            if (newPassengers.TryDequeue(out var passenger))
                            {
                                if (random.NextDouble() <= DO_NORMAL_ACTION_CHANCE)
                                {
                                    if (random.NextDouble() <= REFUND_TICKET_CHANCE)
                                    {
                                        RefundTicketAction(passenger);
                                    }
                                    else
                                    {
                                        CheckInAction(passenger);
                                    }
                                }
                                else
                                {
                                    BuyTicketAction(passenger);
                                }

                                waitingForResponsePassengers.TryAdd(passenger.Id, passenger);
                            }
                            else
                            {
                                throw new Exception("Cannot dequeue a passenger with ticket");
                            }

                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }, cancellationToken);

            Console.ReadLine();
            cancellationTokenSource.Cancel();
        }

        private void BuyTicketAction(Passenger passenger)
        {
            var content = client.GetStringAsync(//TODO);

            var flight = availableFlights[random.Next(availableFlights.Length)];

            var request = new BuyTicketRequest(
                        passengerId: passenger.Id,
                        flightId: flight.Id,
                        hasBaggage: passenger.HasBaggage,
                        isVip: passenger.IsVip);

            try
            {
                SendBuyTicketRequest(request);
                Console.WriteLine($"Passenger {passenger.Id} tries to buy a ticket");
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

            try
            {
                SendRefundTicketRequest(request);
                Console.WriteLine($"Passenger {passenger.Id} tries to refund a ticket");
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        private void CheckInAction(Passenger passenger)
        {
            var request = new CheckInRequest(
                passengerId: passenger.Id,
                ticket: passenger.Ticket);

            try
            {
                SendCheckInRequest(request);
                Console.WriteLine($"Passenger {passenger.Id} tries to check-in");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }           
        }

        //Cashbox response processing
        private void HandleBuyTicketResponse(BuyTicketResponse response)
        {
            Guid passengerId = response.PassengerId;
            if (waitingForResponsePassengers.TryRemove(passengerId, out var passenger))
            {
                if (response.Status == BuyTicketResponseStatus.Success)
                {
                    passenger.Ticket = response.Ticket;                   
                    Console.WriteLine($"Passenger №{passenger.Id} has just bought a ticket");
                }
                else
                {
                    Console.WriteLine($"Couldn't buy a ticket: passenger №{passenger.Id} already has a ticket");
                }

                passengersWithTickets.Enqueue(passenger);
            }
            else
            {
                throw new Exception("Cannot remove a waiting passenger");
            }
        }

        //Check-in response processing
        private void HandleCheckInResponse(CheckInResponse response)
        {
            Guid passengerId = response.PassengerId;
            if (waitingForResponsePassengers.TryRemove(passengerId, out var passenger))
            {
                if (response.IsCheckedIn)
                {
                    Console.WriteLine($"Passenger №{passenger.Id} has been registrated");
                    return;
                }
                else if (response.Reason == CheckInResponse.NO_TICKET)
                {
                    Console.WriteLine($"Couldn't check-in passenger №{passenger.Id}: no ticket");
                    newPassengers.Enqueue(passenger);
                }
                else if (response.Reason == CheckInResponse.EARLY)
                {
                    Console.WriteLine($"Couldn't check-in passenger №{passenger.Id}: registration has not begun");
                    passengersWithTickets.Enqueue(passenger);
                }
                else if (response.Reason == CheckInResponse.LATE)
                {
                    Console.WriteLine($"Couldn't check-in passenger №{passenger.Id}: too late");
                    //A passenger goes home very sad
                }
                else
                {
                    throw new ArgumentOutOfRangeException($"Invalid reason from check-in: \"{response.Reason}\"");
                }
            }
            else
            {
                throw new Exception("Cannot remove a waiting passenger");
            }
        }

        private void HandleRefundTicketResponse(RefundTicketResponse response)
        {
            Guid passengerId = response.PassengerId;
            if (waitingForResponsePassengers.TryRemove(passengerId, out var passenger))
            {
                if (response.IsRefunded)
                {
                    passenger.Ticket = null;
                    Console.WriteLine($"A Ticket of passenger №{passenger.Id} has been refunded");
                }
                else if (passenger.Ticket != null)
                {
                    passenger.Ticket = null;
                    Console.WriteLine($"Could't refund a ticket of passenger №{passenger.Id}: too late");
                }
                else
                {
                    Console.WriteLine($"Could't refund a ticket of passenger №{passenger.Id}: no ticket");
                }

                newPassengers.Enqueue(passenger);
            }
            else
            {
                throw new Exception("Cannot remove a waiting passenger");
            }
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
            client.Dispose();
            channel.Close();
            connection.Close();
        }
    }
}
