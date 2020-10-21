﻿namespace Orts.Simulation.Signalling
{
    public enum Heading
    {
        Ahead,
        Reverse,
    }

    public enum Location
    {
        NearEnd,
        FarEnd,
    }

    public enum PinEnd
    { 
        ThisEnd,
        OtherEnd,
    }

    public enum TrackCircuitType
    {
        Normal,
        Junction,
        Crossover,
        EndOfTrack,
        Empty,
    }

}