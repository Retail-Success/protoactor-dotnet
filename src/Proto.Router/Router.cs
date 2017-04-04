﻿// -----------------------------------------------------------------------
//  <copyright file="Router.cs" company="Asynkron HB">
//      Copyright (C) 2015-2017 Asynkron HB All rights reserved
//  </copyright>
// -----------------------------------------------------------------------

using System;
using System.Threading;
using Proto.Router.Routers;

namespace Proto.Router
{
    public static class Router
    {
        public static Props NewBroadcastGroup(Props props, params PID[] routees)
        {
            return props.WithSpawner(Spawner(new BroadcastGroupRouterConfig(routees)));
        }

        public static Props NewConsistentHashGroup(Props props, params PID[] routees)
        {
            return props.WithSpawner(Spawner(new ConsistentHashGroupRouterConfig(MD5Hasher.Hash, 100, routees)));
        }

        public static Props NewConsistentHashGroup(Props props, Func<string, uint> hash, int replicaCount, params PID[] routees)
        {
            return props.WithSpawner(Spawner(new ConsistentHashGroupRouterConfig(hash, replicaCount, routees)));
        }

        public static Props NewRandomGroup(Props props, params PID[] routees)
        {
            return props.WithSpawner(Spawner(new RandomGroupRouterConfig(routees)));
        }

        public static Props NewRandomGroup(Props props, int seed, params PID[] routees)
        {
            return props.WithSpawner(Spawner(new RandomGroupRouterConfig(seed, routees)));
        }

        public static Props NewRoundRobinGroup(Props props, params PID[] routees)
        {
            return props.WithSpawner(Spawner(new RoundRobinGroupRouterConfig(routees)));
        }

        public static Props NewBroadcastPool(Props props, int poolSize)
        {
            return props.WithSpawner(Spawner(new BroadcastPoolRouterConfig(poolSize)));
        }

        public static Props NewConsistentHashPool(Props props, int poolSize, Func<string, uint> hash = null, int replicaCount = 100)
        {
            return props.WithSpawner(Spawner(new ConsistentHashPoolRouterConfig(poolSize, hash ?? MD5Hasher.Hash, replicaCount)));
        }

        public static Props NewRandomPool(Props props, int poolSize, int? seed = null)
        {
            return props.WithSpawner(Spawner(new RandomPoolRouterConfig(poolSize, seed)));
        }

        public static Props NewRoundRobinPool(Props props, int poolSize)
        {
            return props.WithSpawner(Spawner(new RoundRobinPoolRouterConfig(poolSize)));
        }

        public static Spawner Spawner(IRouterConfig config)
        {
            return (id, props, parent) =>
            {
                var routeeProps = props.WithSpawner(null);
                var routerState = config.CreateRouterState();
                var wg = new AutoResetEvent(false);
                var routerProps = Actor.FromProducer(() => new RouterActor(routeeProps, config, routerState, wg))
                                       .WithMailbox(props.MailboxProducer);
                var routerId = ProcessRegistry.Instance.NextId();
                var router = Props.DefaultSpawner(routerId + "/router" , routerProps, parent);
                wg.WaitOne(); //wait for the router to start

                var reff = new RouterProcess(router, routerState);
                var (pid,ok) = ProcessRegistry.Instance.TryAdd(routerId, reff);
                return pid;
            };
        }
    }
}