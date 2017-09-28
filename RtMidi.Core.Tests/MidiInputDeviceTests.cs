﻿using RtMidi.Core.Enums;
using RtMidi.Core.Messages;
using RtMidi.Core.Unmanaged.Devices;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace RtMidi.Core.Tests
{
    public class MidiInputDeviceTests : TestBase
    {
        private readonly RtMidiInputDeviceMock _inputDeviceMock;
        private readonly IMidiInputDevice _sut;
        private readonly Queue<NoteOffMessage> _noteOffMessages = new Queue<NoteOffMessage>();
        private readonly Queue<NoteOnMessage> _noteOnMessages = new Queue<NoteOnMessage>();
        private readonly Queue<PolyphonicKeyPressureMessage> _polyphonicKeyPressureMessages = new Queue<PolyphonicKeyPressureMessage>();
        private readonly Queue<ControlChangeMessage> _controlChangeMessages = new Queue<ControlChangeMessage>();
        private readonly Queue<ProgramChangeMessage> _programChangeMessages = new Queue<ProgramChangeMessage>();
        private readonly Queue<ChannelPressureMessage> _channelPressureMessages = new Queue<ChannelPressureMessage>();
        private readonly Queue<PitchBendMessage> _pitchBendMessages = new Queue<PitchBendMessage>();
        
        public MidiInputDeviceTests(ITestOutputHelper output) : base(output)
        {
            _inputDeviceMock = new RtMidiInputDeviceMock();
            _sut = new MidiInputDevice(_inputDeviceMock);

            _sut.NoteOff += (sender, e) => _noteOffMessages.Enqueue(e);
            _sut.NoteOn += (sender, e) => _noteOnMessages.Enqueue(e);
            _sut.PolyphonicKeyPressure += (sender, e) => _polyphonicKeyPressureMessages.Enqueue(e);
            _sut.ControlChange += (sender, e) => _controlChangeMessages.Enqueue(e);
            _sut.ProgramChange += (sender, e) => _programChangeMessages.Enqueue(e);
            _sut.ChannelPressure += (sender, e) => _channelPressureMessages.Enqueue(e);
            _sut.PitchBend += (sender, e) => _pitchBendMessages.Enqueue(e);
        }

        [Fact]
        public void Test_NoteOffMessage()
        {
            var noteOffMsg = NoteOffMessage(Channel.Channel_4, Key.Key_127, 11);
            Assert.NotNull(noteOffMsg);
            Assert.Equal(3, noteOffMsg.Length);
            Assert.Equal(0b1000_0011, noteOffMsg[0]);   // Channel
            Assert.Equal(0b0111_1111, noteOffMsg[1]);   // Key
            Assert.Equal(0b0000_1011, noteOffMsg[2]);   // Velocity
        }

        [Fact]
        public void Should_Fire_Events_On_Channel_1_Through_16()
        {
            AllChannels(channel =>
            {
                _inputDeviceMock.OnMessage(NoteOffMessage(channel));
                Assert.True(_noteOffMessages.TryDequeue(out var noteOffMessage));
                Assert.Equal(channel, noteOffMessage.Channel);
            });
        }

        [Fact]
        public void Should_Fire_NoteOffMessage() 
        {
            AllChannels(channel => AllKeys(key => AllInRange(0,127, velocity =>
            {
                _inputDeviceMock.OnMessage(NoteOffMessage(channel, key, velocity));
                Assert.True(_noteOffMessages.TryDequeue(out var msg));

                Assert.Equal(channel, msg.Channel);
                Assert.Equal(key, msg.Key);
                Assert.Equal(velocity, msg.Velocity);
            })));
        }

        [Fact]
        public void Should_Fire_NoteOnMessages()
        {
            AllChannels(channel => AllKeys(key => AllInRange(0, 127, velocity =>
            {
                _inputDeviceMock.OnMessage(NoteOnMessage(channel, key, velocity));
                Assert.True(_noteOnMessages.TryDequeue(out var msg));

                Assert.Equal(channel, msg.Channel);
                Assert.Equal(key, msg.Key);
                Assert.Equal(velocity, msg.Velocity);
            })));
        }

        [Fact]
        public void Should_Fire_PolyphonicKeyPressureMessages() 
        {
            AllChannels(channel => AllKeys(key => AllInRange(0,127, pressure => 
            {
                _inputDeviceMock.OnMessage(PolyphonicKeyPressureMessage(channel, key, pressure));
                Assert.True(_polyphonicKeyPressureMessages.TryDequeue(out var msg));

                Assert.Equal(channel, msg.Channel);
                Assert.Equal(key, msg.Key);
                Assert.Equal(pressure, msg.Pressure);
            })));
        }

        [Fact]
        public void Should_Fire_ControlChangeMessages()
        {
            AllChannels(channel => AllInRange(0, 127, control => AllInRange(0, 127, value =>
            {
                _inputDeviceMock.OnMessage(ControlChangeMessage(channel, control, value));
                Assert.True(_controlChangeMessages.TryDequeue(out var msg));

                Assert.Equal(channel, msg.Channel);
                Assert.Equal(control, msg.Control);
                Assert.Equal(value, msg.Value);
            })));
        }

        [Fact]
        public void Should_Fire_ProgramChangeMessages()
        {
            AllChannels(channel => AllInRange(0, 127, program =>
            {
                _inputDeviceMock.OnMessage(ProgramChangeMessage(channel, program));
                Assert.True(_programChangeMessages.TryDequeue(out var msg));

                Assert.Equal(channel, msg.Channel);
                Assert.Equal(program, msg.Program);
            }));
        }

        [Fact]
        public void Should_Fire_ChannelPressureMessages()
        {
            AllChannels(channel => AllInRange(0, 127, pressure =>
            {
                _inputDeviceMock.OnMessage(ChannelPressureMessage(channel, pressure));
                Assert.True(_channelPressureMessages.TryDequeue(out var msg));

                Assert.Equal(channel, msg.Channel);
                Assert.Equal(pressure, msg.Pressure);
            }));
        }

        [Fact]
        public void Should_Separate_Lsb_And_Msb_For_Pitch_Bend()
        {
            var msg = PitchBendMessage(Channel.Channel_1, 5482);

            Assert.Equal(3, msg.Length);
            var lsb = msg[1];
            var msb = msg[2];

            Assert.Equal(0b0110_1010, lsb);
            Assert.Equal(0b0010_1010, msb);
        }

        [Fact]
        public void Should_Fire_PitchBendMessages()
        {
            AllChannels(channel => AllInRange(0, 16383, value =>
            {
                _inputDeviceMock.OnMessage(PitchBendMessage(channel, value));
                Assert.True(_pitchBendMessages.TryDequeue(out var msg));

                Assert.Equal(channel, msg.Channel);
                Assert.Equal(value, msg.Value);
            }));
        }

        private static void AllChannels(Action<Channel> func)
        {
            foreach (var channel in Enum.GetValues(typeof(Channel)).Cast<Channel>())
            {
                func(channel);
            }
        }

        private static void AllKeys(Action<Key> func)
        {
            foreach (var key in Enum.GetValues(typeof(Key)).Cast<Key>())
            {
                func(key);
            }
        }

        private static void AllInRange(int from, int to, Action<int> func)
        {
            for (var i = from; i <= to; i++)
            {
                func(i);
            }
        }

        private static byte[] NoteOffMessage(Channel channel, Key key = Key.Key_0, int velocity = 0)
            => new[]
            {
                StatusByte(Midi.Status.NoteOffBitmask, channel),
                DataByte(key),
                DataByte(velocity)
            };

        private static byte[] NoteOnMessage(Channel channel, Key key = Key.Key_0, int velocity = 0)
            => new[]
            {
                StatusByte(Midi.Status.NoteOnBitmask, channel),
                DataByte(key),
                DataByte(velocity)
            };

        private static byte[] PolyphonicKeyPressureMessage(Channel channel, Key key = Key.Key_0, int pressure = 0)
            => new[]
            {
                StatusByte(Midi.Status.PolyphonicKeyPressureBitmask, channel),
                DataByte(key),
                DataByte(pressure)
            };

        private static byte[] ControlChangeMessage(Channel channel, int control, int value)
            => new[]
            {
                StatusByte(Midi.Status.ControlChangeBitmask, channel),
                DataByte(control),
                DataByte(value)
            };

        private static byte[] ProgramChangeMessage(Channel channel, int program)
            => new[]
            {
                StatusByte(Midi.Status.ProgramChangeBitmask, channel),
                DataByte(program)
            };

        private static byte[] ChannelPressureMessage(Channel channel, int pressure)
            => new[]
            {
                StatusByte(Midi.Status.ChannelPressureBitmask, channel),
                DataByte(pressure)
            };

        private static byte[] PitchBendMessage(Channel channel, int value)
            => new[]
            {
                StatusByte(Midi.Status.PitchBendChange, channel),
                DataByte(value & 0b0111_1111),
                DataByte(value >> 7)
            };

        private static byte StatusByte(byte statusBitmask, Channel channel) 
        => (byte)(statusBitmask | (Midi.ChannelBitmask & (int)channel));

        private static byte DataByte(int value)
        => (byte)(Midi.DataBitmask & value);

        private static byte DataByte(Key key)
        => (byte)(Midi.DataBitmask & (int)key);
            

        private class RtMidiInputDeviceMock : IRtMidiInputDevice
        {
            public bool IsOpen => true;

            public event EventHandler<byte[]> Message;

            public void OnMessage(byte[] message) => Message?.Invoke(this, message);

            public void Close()
            {
            }

            public void Dispose()
            {
            }

            public bool Open() => true;
        }
    }
}