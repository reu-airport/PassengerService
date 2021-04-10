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

        private const long PASSENGER_GENERATION_PERIOD_MS = 5 * 1000;
        private const long PASSENGER_ACTIVITY_PERIOD_MS = 4 * 1000;
        private const int TIME_FACTOR_REQUEST_PERIOD_MS = 5 * 1000;

        private const string INFO_PANEL_QUERY = "https://info-panel222.herokuapp.com/api/v1/info-panel/all";//TODO
        private const string TIME_QUERY = "http://206.189.60.128:8083/api/v1/time";//TODO

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

        private EventingBasicConsumer buyPassengerQueueConsumer;
        private EventingBasicConsumer refundPassengerQueueConsumer;
        private EventingBasicConsumer checkInToPassengerQueueConsumer;

        private readonly IConnection connection;
        private readonly IModel channel;

        private long time_factor = 1;

        private readonly Random random = new Random();

        private readonly HttpClient client = new HttpClient();

        public PassengerService()
        {
            cancellationToken = cancellationTokenSource.Token;

            var factory = new ConnectionFactory()
            {
                HostName = "206.189.60.128",
                UserName = "guest",
                Password = "guest"
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
        }

        public void Run()
        {
            //Task requesting time factor every TIME_FACTOR_REQUEST_PERIOD_MS miliseconds
            Task.Run(() =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        var content = client.GetStringAsync(TIME_QUERY);

                        Console.WriteLine($"[{DateTime.Now}] Time is being requested");
                        Console.WriteLine(content.Result);
                        var time = JsonSerializer.Deserialize<Time>(content.Result);

                        var dateTime = DateTimeOffset.FromUnixTimeMilliseconds(time.time);
                        Console.WriteLine($"[{DateTime.Now}] Simulation time:\t{dateTime}");

                        var new_time_factor = time.factor;

                        if (time_factor != new_time_factor)
                        {
                            time_factor = new_time_factor;
                            Console.WriteLine($"[{DateTime.Now}] New time factor: {time_factor}");
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"[{DateTime.Now}] {e.Message}");
                    }
                    finally
                    {
                        Thread.Sleep(TIME_FACTOR_REQUEST_PERIOD_MS);
                    }   
                }              
            }, cancellationToken);           

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

                            Console.WriteLine($"[{DateTime.Now}] A new passenger:\t{passenger.Id}");
                        }

                        Thread.Sleep(Convert.ToInt32(PASSENGER_GENERATION_PERIOD_MS / time_factor));
                    }
                }
                catch(Exception e)
                {
                    Console.WriteLine($"[{DateTime.Now}] {e.Message}");
                }
            }, cancellationToken);

            //Task sends new passengers do something
            Task.Run(() =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    Thread.Sleep(Convert.ToInt32(PASSENGER_ACTIVITY_PERIOD_MS / time_factor));

                    try
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
                    catch (Exception e)
                    {
                        Console.WriteLine($"[{DateTime.Now}] {e.Message}");
                    }
                }
            }, cancellationToken);

            //Task sends passengers with tickets do something
            Task.Run(() =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    Thread.Sleep(Convert.ToInt32(PASSENGER_ACTIVITY_PERIOD_MS / time_factor));

                    try
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
                    catch (Exception e)
                    {
                        Console.WriteLine($"[{DateTime.Now}] {e.Message}");
                    }
                }
            }, cancellationToken);

            Console.ReadLine();
            cancellationTokenSource.Cancel();
        }

        private void BuyTicketAction(Passenger passenger)
        {
            var content = client.GetStringAsync(INFO_PANEL_QUERY);

            availableFlights = JsonSerializer.Deserialize<Flight[]>(content.Result);

            var flight = availableFlights[random.Next(availableFlights.Length)];

            var request = new BuyTicketRequest(
                        passengerId: passenger.Id,
                        flightId: flight.Id,
                        hasBaggage: passenger.HasBaggage,
                        isVip: passenger.IsVip,
                        timeStamp: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

            try
            {
                SendBuyTicketRequest(request);
                Console.WriteLine($"[{DateTime.Now}] Passenger {passenger.Id} tries to buy a ticket");
            }
            catch(Exception e)
            {
                Console.WriteLine($"[{DateTime.Now}] {e.Message}");
            }   
        }

        private void RefundTicketAction(Passenger passenger)
        {
            var request = new RefundTicketRequest(
                passengerId: passenger.Id,
                ticket: passenger.Ticket,
                timeStamp: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

            try
            {
                SendRefundTicketRequest(request);
                Console.WriteLine($"[{DateTime.Now}] Passenger {passenger.Id} tries to refund a ticket");
            }
            catch(Exception e)
            {
                Console.WriteLine($"[{DateTime.Now}] {e.Message}");
            }
        }

        private void CheckInAction(Passenger passenger)
        {
            var request = new CheckInRequest(
                passengerId: passenger.Id,
                ticket: passenger.Ticket,
                timeStamp: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

            try
            {
                SendCheckInRequest(request);
                Console.WriteLine($"[{DateTime.Now}] Passenger {passenger.Id} tries to check-in");
            }
            catch (Exception e)
            {
                Console.WriteLine($"[{DateTime.Now}] {e.Message}");
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
                    Console.WriteLine($"[{DateTime.Now}] Passenger №{passenger.Id} has just bought a ticket");
                }
                else
                {
                    Console.WriteLine($"[{DateTime.Now}] Couldn't buy a ticket: passenger №{passenger.Id} already has a ticket");
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
                    Console.WriteLine($"[{DateTime.Now}] Passenger №{passenger.Id} has been registrated");
                    return;
                }
                else if (response.Reason == CheckInResponse.NO_TICKET)
                {
                    Console.WriteLine($"[{DateTime.Now}] Couldn't check-in passenger №{passenger.Id}: no ticket");
                    newPassengers.Enqueue(passenger);
                }
                else if (response.Reason == CheckInResponse.EARLY)
                {
                    Console.WriteLine($"[{DateTime.Now}] Couldn't check-in passenger №{passenger.Id}: registration has not begun");
                    passengersWithTickets.Enqueue(passenger);
                }
                else if (response.Reason == CheckInResponse.LATE)
                {
                    Console.WriteLine($"[{DateTime.Now}] Couldn't check-in passenger №{passenger.Id}: too late");
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
                    Console.WriteLine($"[{DateTime.Now}] A Ticket of passenger №{passenger.Id} has been refunded");
                }
                else if (passenger.Ticket != null)
                {
                    passenger.Ticket = null;
                    Console.WriteLine($"[{DateTime.Now}] Could't refund a ticket of passenger №{passenger.Id}: too late");
                }
                else
                {
                    Console.WriteLine($"[{DateTime.Now}] Could't refund a ticket of passenger №{passenger.Id}: no ticket");
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
