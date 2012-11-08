﻿using System;
using System.Linq;
using EventStore.Core.Data;
using EventStore.Core.Services.Storage.ReaderIndex;
using EventStore.Core.TransactionLog.LogRecords;
using NUnit.Framework;

namespace EventStore.Core.Tests.Services.Storage.Scavenge
{
    [TestFixture]
    public class when_writing_delete_prepare_without_commit_and_scavenging :ReadIndexTestScenario
    {
        private EventRecord _event1;
        private EventRecord _event2;
        private EventRecord _event3;

        protected override void WriteTestScenario()
        {
            _event1 = WriteStreamCreated("ES");
            _event2 = WriteSingleEvent("ES", 1, "bla1");

            var prepare = LogRecord.DeleteTombstone(WriterCheckpoint.ReadNonFlushed(),
                                                    Guid.NewGuid(),
                                                    "ES",
                                                    2);
            long pos;
            Assert.IsTrue(Writer.Write(prepare, out pos));

            _event3 = WriteSingleEvent("ES", 2, "bla1");
            Scavenge();
        }

        [Test]
        public void read_one_by_one_returns_all_commited_events()
        {
            EventRecord rec;
            Assert.AreEqual(SingleReadResult.Success, ReadIndex.ReadEvent("ES", 0, out rec));
            Assert.AreEqual(_event1, rec);
            Assert.AreEqual(SingleReadResult.Success, ReadIndex.ReadEvent("ES", 1, out rec));
            Assert.AreEqual(_event2, rec);
            Assert.AreEqual(SingleReadResult.Success, ReadIndex.ReadEvent("ES", 2, out rec));
            Assert.AreEqual(_event3, rec);
        }

        [Test]
        public void read_stream_events_forward_should_return_all_events()
        {
            EventRecord[] records;
            Assert.AreEqual(RangeReadResult.Success, ReadIndex.ReadStreamEventsForward("ES", 0, 100, out records));
            Assert.AreEqual(_event1, records[0]);
            Assert.AreEqual(_event2, records[1]);
            Assert.AreEqual(_event3, records[2]);
        }

        [Test]
        public void read_stream_events_backward_should_return_stream_deleted()
        {
            EventRecord[] records;
            Assert.AreEqual(RangeReadResult.Success, ReadIndex.ReadStreamEventsBackward("ES", -1, 100, out records));
            Assert.AreEqual(_event1, records[2]);
            Assert.AreEqual(_event2, records[1]);
            Assert.AreEqual(_event3, records[0]);
        }

        [Test]
        public void read_all_forward_returns_all_events()
        {
            var events = ReadIndex.ReadAllEventsForward(new TFPos(0, 0), 100).Records.Select(r => r.Event).ToArray();
            Assert.AreEqual(3, events.Length);
            Assert.AreEqual(_event1, events[0]);
            Assert.AreEqual(_event2, events[1]);
            Assert.AreEqual(_event3, events[2]);
        }

        [Test]
        public void read_all_backward_returns_all_events()
        {
            var events = ReadIndex.ReadAllEventsBackward(GetBackwardReadPos(), 100).Records.Select(r => r.Event).ToArray();
            Assert.AreEqual(3, events.Length);
            Assert.AreEqual(_event1, events[2]);
            Assert.AreEqual(_event2, events[1]);
            Assert.AreEqual(_event3, events[0]);
        }
    }
}
