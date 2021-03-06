﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using IdentityModel.Client;
using System.Net.Http;
using System.Threading;
using System.IO;
using System.Linq;

namespace LeaderboardLoadTest
{
    public class Program
    {
        private readonly Dictionary<string, string> _userNameToPassword = new Dictionary<string, string>();
        private readonly TextWriter _log = Console.Out;

        public static void Main(string[] args)
        {
            //todo: add command line validation         

            int totalUsers = int.Parse(args[0]);
            int sessionsPerUser = int.Parse(args[1]);

            new Program().Run(totalUsers, sessionsPerUser);
        }

        private void Run(int totalUsers, int sessionsPerUser)
        {
            InititialiseUsers(totalUsers);

            var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

            var playerTasks = new List<PlayerTask>();

            var startTime = DateTime.UtcNow;
            _log.WriteLine("load testing with {0} users, {1} calls per each...", totalUsers, sessionsPerUser);
            foreach (var userEntry in _userNameToPassword)
            {
                var player = new AutoPlayer(userEntry.Key, userEntry.Value, _log);

                //_log.WriteLine("starting player '{0}'...", userEntry.Key);                                     
                Task task = Task.Run(
                   () => player.PlayGameAsync(sessionsPerUser, cancellationToken));



                playerTasks.Add(new PlayerTask(player, task));
            }

            _log.WriteLine("waiting for load session to finish...");
            Task.WaitAll(playerTasks.Select(s => s.Task).ToArray());
            //_log.WriteLine("all done.");               
            //Console.ReadLine();                                                                 

            _log.WriteLine("Statistics:");
            _log.WriteLine();
            foreach (var session in playerTasks)
            {
                _log.WriteLine("Player {0}", session.Player.Id);
                foreach (string callName in session.Player.CallNames)
                {
                    _log.WriteLine("  {0}", callName);
                    _log.WriteLine("    average call duration: {0}ms", session.Player.GetAvgCallTime(callName));
                    _log.WriteLine("    calls/second: {0}", session.Player.GetAvgCallsPerSecond(callName));
                }
                _log.WriteLine();
            }

            _log.WriteLine("total averages:");
            foreach (string callName in playerTasks.First().Player.CallNames)
            {
                _log.WriteLine("  {0}", callName);
                _log.WriteLine("    average call duration: {0}ms",
                    playerTasks.Select(s => s.Player.GetAvgCallTime(callName)).Average());
                _log.WriteLine("    calls/second: {0}",
                    playerTasks.Select(s => s.Player.GetAvgCallsPerSecond(callName)).Average());
            }
            _log.WriteLine("finished in {0}", DateTime.UtcNow - startTime);

            //cancellationTokenSource.Cancel();                                                  
        }

        private void InititialiseUsers(int count)
        {
            for (int i = 0; i < count; i++)
            {
                string userName = "loadUser" + i;
                string password = userName;

                _userNameToPassword.Add(userName, password);
            }
        }
    }
}
