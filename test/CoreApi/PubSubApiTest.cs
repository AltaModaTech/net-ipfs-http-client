﻿using Ipfs.Api;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ipfs.Api
{

    [TestClass]
    public class PubSubApiTest
    {

        [TestMethod]
        public void Api_Exists()
        {
            IpfsClient ipfs = TestFixture.Ipfs;
            Assert.IsNotNull(ipfs.PubSub);
        }

        [TestMethod]
        public async Task Peers()
        {
            var ipfs = TestFixture.Ipfs;
            var topic = "net-ipfs-api-test-" + Guid.NewGuid().ToString();
            var cs = new CancellationTokenSource();
            try
            {
                await ipfs.PubSub.Subscribe(topic, msg => { }, cs.Token);
                var peers = ipfs.PubSub.PeersAsync().Result.ToArray();
                Assert.IsTrue(peers.Length > 0);
            }
            finally
            {
                cs.Cancel();
            }
        }

        [TestMethod]
        public void Peers_Unknown_Topic()
        {
            var ipfs = TestFixture.Ipfs;
            var topic = "net-ipfs-api-test-unknown" + Guid.NewGuid().ToString();
            var peers = ipfs.PubSub.PeersAsync(topic).Result.ToArray();
            Assert.AreEqual(0, peers.Length);
        }

        [TestMethod]
        public async Task Subscribed_Topics()
        {
            var ipfs = TestFixture.Ipfs;
            var topic = "net-ipfs-api-test-" + Guid.NewGuid().ToString();
            var cs = new CancellationTokenSource();
            try
            {
                await ipfs.PubSub.Subscribe(topic, msg => { }, cs.Token);
                var topics = ipfs.PubSub.SubscribedTopicsAsync().Result.ToArray();
                Assert.IsTrue(topics.Length > 0);
                CollectionAssert.Contains(topics, topic);
            }
            finally
            {
                cs.Cancel();
            }
        }

        volatile int messageCount = 0;

        [TestMethod]
        public async Task Subscribe()
        {
            messageCount = 0;
            var ipfs = TestFixture.Ipfs;
            var topic = "net-ipfs-api-test-" + Guid.NewGuid().ToString();
            var cs = new CancellationTokenSource();
            try
            {
                await ipfs.PubSub.Subscribe(topic, msg =>
                {
                    Interlocked.Increment(ref messageCount);
                }, cs.Token);
                await ipfs.PubSub.Publish(topic, "hello world!");

                await Task.Delay(1000);
                Assert.AreEqual(1, messageCount);
            }
            finally
            {
                cs.Cancel();
            }
        }

        [TestMethod]
        public async Task Subscribe_Mutiple_Messages()
        {
            messageCount = 0;
            var messages = "hello world this is pubsub".Split();
            var ipfs = TestFixture.Ipfs;
            var topic = "net-ipfs-api-test-" + Guid.NewGuid().ToString();
            var cs = new CancellationTokenSource();
            try
            {
                await ipfs.PubSub.Subscribe(topic, msg =>
                {
                    Interlocked.Increment(ref messageCount);
                }, cs.Token);
                foreach (var msg in messages)
                {
                    await ipfs.PubSub.Publish(topic, msg);
                }

                await Task.Delay(1000);
                Assert.AreEqual(messages.Length, messageCount);
            }
            finally
            {
                cs.Cancel();
            }
        }

        [TestMethod]
        public async Task Multiple_Subscribe_Mutiple_Messages()
        {
            messageCount = 0;
            var messages = "hello world this is pubsub".Split();
            var ipfs = TestFixture.Ipfs;
            var topic = "net-ipfs-api-test-" + Guid.NewGuid().ToString();
            var cs = new CancellationTokenSource();
            Action<IPublishedMessage> processMessage = (msg) =>
            {
                Interlocked.Increment(ref messageCount);
            };
            try
            {
                await ipfs.PubSub.Subscribe(topic, processMessage, cs.Token);
                await ipfs.PubSub.Subscribe(topic, processMessage, cs.Token);
                foreach (var msg in messages)
                {
                    await ipfs.PubSub.Publish(topic, msg);
                }

                await Task.Delay(1000);
                Assert.AreEqual(messages.Length * 2, messageCount);
            }
            finally
            {
                cs.Cancel();
            }
        }

        volatile int messageCount1 = 0;

        [TestMethod]
        public async Task Unsubscribe()
        {
            messageCount1 = 0;
            var ipfs = TestFixture.Ipfs;
            var topic = "net-ipfs-api-test-" + Guid.NewGuid().ToString();
            var cs = new CancellationTokenSource();
            await ipfs.PubSub.Subscribe(topic, msg =>
            {
                Interlocked.Increment(ref messageCount1);
            }, cs.Token);
            await ipfs.PubSub.Publish(topic, "hello world!");
            await Task.Delay(1000);
            Assert.AreEqual(1, messageCount1);

            cs.Cancel();
            await ipfs.PubSub.Publish(topic, "hello world!!!");
            await Task.Delay(1000);
            Assert.AreEqual(1, messageCount1);
        }

        [TestMethod]
        public async Task Subscribe_Multiple_Topics()
        {
            messageCount = 0;
            var topicCount = 4;
            var topics = new string[topicCount];
            var cancels = new CancellationTokenSource[topicCount];
            var ipfs = TestFixture.Ipfs;

            for (int i = 0; i < topicCount; ++i)
            {
                topics[i] = "net-ipfs-api-test-" + Guid.NewGuid().ToString();
                cancels[i] = new CancellationTokenSource();
            }
            Action<IPublishedMessage> processMessage = (msg) =>
            {
                Interlocked.Increment(ref messageCount);
            };

            try
            {
                // Subscribe to N topics.
                for (int i = 0; i < topicCount; ++i)
                {
                    await ipfs.PubSub.Subscribe(topics[i], msg =>
                    {
                        Interlocked.Increment(ref messageCount);
                    }, cancels[i].Token);
                }

                // Verify topics.
                var actualTopics = await ipfs.PubSub.SubscribedTopicsAsync();
                foreach (var topic in actualTopics)
                {
                    CollectionAssert.Contains(topics, topic);
                }

                // Publish a message to each topic.
                for (int i = 0; i < topicCount; ++i)
                {
                    await ipfs.PubSub.Publish(topics[i], "hello world!");
                }

                // Verify that all messages have been received.
                await Task.Delay(1000);
                Assert.AreEqual(topicCount, messageCount);
            }
            finally
            {
                foreach (var cs in cancels)
                {
                    cs.Cancel();
                }
            }
        }
    }
}
