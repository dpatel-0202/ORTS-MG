﻿// COPYRIGHT 2013 by the Open Rails project.
// 
// This file is part of Open Rails.
// 
// Open Rails is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Open Rails is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Open Rails.  If not, see <http://www.gnu.org/licenses/>.

// This module covers all classes and code for signal, speed post, track occupation and track reservation control

using System;
using System.IO;

using Orts.Formats.Msts;

namespace Orts.Simulation.Signalling
{
    public class SignalItemInfo
    {
        public enum ItemType
        {
            Any,
            Signal,
            Speedlimit,
        }

        public enum FindState
        {
            None = 0,
            Item = 1,
            EndOfTrack = -1,
            PassedDanger = -2,
            PassedMaximumDistance = -3,
            TdbError = -4,
            EndOfAuthority = -5,
            EndOfPath = -6,
        }

        public ItemType SignalItemType { get; private set; }                     // type information
        public FindState State { get; private set; }               // state information

        public Signal SignalDetails { get; private set; }                    // actual object 

        public float DistanceFound { get; private set; }
        public float DistanceToTrain { get; internal set; }
        public float DistanceToObject { get; internal set; }

        public SignalAspectState SignalState { get; internal set; }                   // UNKNOWN if type = speedlimit

        public SpeedInfo SpeedInfo { get; internal set; } // set active by TRAIN, speed values are -1 if not set
        public float ActualSpeed { get; internal set; }                  

        public bool Processed { get; internal set; }                       // for AI trains, set active by TRAIN

        //================================================================================================//
        /// <summary>
        /// Constructor
        /// </summary>

        public SignalItemInfo(Signal signal, float distance)
        {
            State = FindState.Item;

            DistanceFound = distance;

            SignalDetails = signal ?? throw new ArgumentNullException(nameof(signal));

            if (signal.isSignal)
            {
                SignalItemType = ItemType.Signal;
                SignalState = SignalAspectState.Unknown;  // set active by TRAIN
                SpeedInfo = new SpeedInfo(null); // set active by TRAIN
            }
            else
            {
                SignalItemType = ItemType.Speedlimit;
                SignalState = SignalAspectState.Unknown;
                SpeedInfo = signal.this_lim_speed(SignalFunction.Speed);
            }
        }

        public SignalItemInfo(FindState state)
        {
            State = state;
        }

        public static SignalItemInfo Restore(BinaryReader inf, Signals signals)
        {
            if (null == inf)
                throw new ArgumentNullException(nameof(inf));
            if (null == signals)
                throw new ArgumentNullException(nameof(signals));

            SignalItemInfo result = new SignalItemInfo(FindState.None)
            {
                SignalItemType = (ItemType)inf.ReadInt32(),
                State = (FindState)inf.ReadInt32(),
                SignalDetails = signals.SignalObjects[inf.ReadInt32()],
                DistanceFound = inf.ReadSingle(),
                DistanceToTrain = inf.ReadSingle(),
                DistanceToObject = inf.ReadSingle(),
            };
            result.SpeedInfo = new SpeedInfo(inf.ReadSingle(), inf.ReadSingle(), inf.ReadBoolean(), false, 0);
            result.ActualSpeed = inf.ReadSingle();

            result.Processed = inf.ReadBoolean();
            result.SignalState = result.SignalDetails.isSignal ? result.SignalDetails.this_sig_lr(SignalFunction.Normal) : SignalAspectState.Unknown;

            return (result);
        }

        public static void Save(BinaryWriter outf, SignalItemInfo item)
        {
            if (null == item)
                return;
            if (null == outf)
                throw new ArgumentNullException(nameof(outf));

            outf.Write((int)item.SignalItemType);
            outf.Write((int)item.State);

            outf.Write(item.SignalDetails.thisRef);

            outf.Write(item.DistanceFound);
            outf.Write(item.DistanceToTrain);
            outf.Write(item.DistanceToObject);

            outf.Write(item.SpeedInfo.PassengerSpeed);
            outf.Write(item.SpeedInfo.FreightSpeed);
            outf.Write(item.SpeedInfo.Flag);
            outf.Write(item.ActualSpeed);

            outf.Write(item.Processed);
        }


    }

}
