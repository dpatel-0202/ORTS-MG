﻿using System;
using System.IO;

namespace Orts.Simulation.RollingStocks
{
    public class Coupler
    {
        public bool Rigid { get; internal set; }
        public float R0X { get; internal set; }
        public float R0Y { get; internal set; }
        public float R0Diff { get; internal set; } = 0.012f;
        public float Stiffness1NpM { get; internal set; } = 1e7f;
        public float Stiffness2NpM { get; internal set; } = 2e7f;
        public float Break1N { get; internal set; } = 1e10f;
        public float Break2N { get; internal set; } = 1e10f;
        public float CouplerSlackAM { get; internal set; }
        public float CouplerSlackBM { get; internal set; }
        public float CouplerTensionSlackAM { get; internal set; }
        public float CouplerTensionSlackBM { get; internal set; }
        public float TensionStiffness1N { get; internal set; } = 1e7f;
        public float TensionStiffness2N { get; internal set; } = 2e7f;
        public float TensionR0X { get; internal set; }
        public float TensionR0Y { get; internal set; }
        public float CompressionR0X { get; internal set; }
        public float CompressionR0Y { get; internal set; }
        public float CompressionStiffness1N { get; internal set; }
        public float CompressionStiffness2N { get; internal set; }
        public float CouplerCompressionSlackAM { get; internal set; }
        public float CouplerCompressionSlackBM { get; internal set; }

        public Coupler()
        {
        }

        public Coupler(TrainCar source, float couplerZeroLength)
        {
            if (null == source)
                throw new ArgumentNullException(nameof(source));

            // Simple Coupler parameters
            R0X = source.GetCouplerZeroLengthM();
            R0Y = source.GetCouplerZeroLengthM();
            R0Diff = source.GetMaximumSimpleCouplerSlack1M();
            Stiffness1NpM = source.GetSimpleCouplerStiffnessNpM() / 7;
            Stiffness2NpM = 0;
            CouplerSlackAM = source.GetCouplerSlackAM();
            CouplerSlackBM = source.GetCouplerSlackBM();

            // Common simple and advanced parameters
            Rigid = source.GetCouplerRigidIndication();
            Break1N = source.GetCouplerBreak1N();
            Break2N = source.GetCouplerBreak2N();

            TensionR0X = source.GetCouplerZeroLengthM();
            TensionR0Y = source.GetCouplerTensionR0Y();
            CouplerTensionSlackAM = source.GetCouplerTensionSlackAM();
            CouplerTensionSlackBM = source.GetCouplerTensionSlackBM();
            TensionStiffness1N = source.GetCouplerTensionStiffness1N();
            TensionStiffness2N = source.GetCouplerTensionStiffness2N();

            CompressionR0X = couplerZeroLength;
            CompressionR0Y = source.GetCouplerCompressionR0Y();
            CouplerCompressionSlackAM = source.GetCouplerCompressionSlackAM();
            CouplerCompressionSlackBM = source.GetCouplerCompressionSlackBM();
            CompressionStiffness1N = source.GetCouplerCompressionStiffness1N();
            CompressionStiffness2N = source.GetCouplerCompressionStiffness2N();
        }

        public Coupler(Coupler source)
        {
            Rigid = source?.Rigid ?? throw new ArgumentNullException(nameof(source));
            R0X = source.R0X;
            R0Y = source.R0Y;
            R0Diff = source.R0Diff;
            Break1N = source.Break1N;
            Break2N = source.Break2N;
            Stiffness1NpM = source.Stiffness1NpM;
            Stiffness2NpM = source.Stiffness2NpM;
            CouplerSlackAM = source.CouplerSlackAM;
            CouplerSlackBM = source.CouplerSlackBM;
            TensionStiffness1N = source.TensionStiffness1N;
            TensionStiffness2N = source.TensionStiffness2N;
            CouplerTensionSlackAM = source.CouplerTensionSlackAM;
            CouplerTensionSlackBM = source.CouplerTensionSlackBM;
            TensionR0X = source.TensionR0X;
            TensionR0Y = source.TensionR0Y;
            CompressionR0X = source.CompressionR0X;
            CompressionR0Y = source.CompressionR0Y;
            CompressionStiffness1N = source.CompressionStiffness1N;
            CompressionStiffness2N = source.CompressionStiffness2N;
            CouplerCompressionSlackAM = source.CouplerCompressionSlackAM;
            CouplerCompressionSlackBM = source.CouplerCompressionSlackBM;
        }
        public void SetSimpleR0(float a, float b)
        {
            R0X = a;
            R0Y = b;
            if (a == 0)
                R0Diff = b / 2 * Stiffness2NpM / (Stiffness1NpM + Stiffness2NpM);
            else
                R0Diff = 0.012f;
            //               R0Diff = b - a;

            // Ensure R0Diff stays within "reasonable limits"
            if (R0Diff < 0.001)
                R0Diff = 0.001f;
            else if (R0Diff > 0.1)
                R0Diff = 0.1f;

        }
        public void SetSimpleStiffness(float a, float b)
        {
            if (a + b < 0)
                return;

            Stiffness1NpM = a;
            Stiffness2NpM = b;
        }

        public void SetTensionR0(float a, float b)
        {
            TensionR0X = a;
            TensionR0Y = b;
        }

        public void SetCompressionR0(float a, float b)
        {
            CompressionR0X = a;
            CompressionR0Y = b;
        }

        public void SetTensionStiffness(float a, float b)
        {
            if (a + b < 0)
                return;

            TensionStiffness1N = a;
            TensionStiffness2N = b;
        }

        public void SetCompressionStiffness(float a, float b)
        {
            if (a + b < 0)
                return;

            CompressionStiffness1N = a;
            CompressionStiffness2N = b;
        }

        public void SetTensionSlack(float a, float b)
        {
            if (a + b < 0)
                return;

            CouplerTensionSlackAM = a;
            CouplerTensionSlackBM = b;
        }

        public void SetCompressionSlack(float a, float b)
        {
            if (a + b < 0)
                return;

            CouplerCompressionSlackAM = a;
            CouplerCompressionSlackBM = b;
        }

        public void SetAdvancedBreak(float a, float b)
        {
            if (a + b < 0)
                return;

            Break1N = a;

            // Check if b = 0, as some stock has a zero value, set a default
            if (b == 0)
            {
                Break2N = 2e7f;
            }
            else
            {
                Break2N = b;
            }

        }

        public void SetSlack(float a, float b)
        {
            if (a + b < 0)
                return;

            CouplerSlackAM = a;
            CouplerSlackBM = b;
        }

        public void SetSimpleBreak(float a, float b)
        {
            if (a + b < 0)
                return;

            Break1N = a;

            // Check if b = 0, as some stock has a zero value, set a default
            if (b == 0)
            {
                Break2N = 2e7f;
            }
            else
            {
                Break2N = b;
            }

        }

        /// <summary>
        /// We are saving the game.  Save anything that we'll need to restore the 
        /// status later.
        /// </summary>
        public void Save(BinaryWriter outf)
        {
            if (null == outf)
                throw new ArgumentNullException(nameof(outf));
            outf.Write(Rigid);
            outf.Write(R0X);
            outf.Write(R0Y);
            outf.Write(R0Diff);
            outf.Write(Stiffness1NpM);
            outf.Write(Stiffness2NpM);
            outf.Write(CouplerSlackAM);
            outf.Write(CouplerSlackBM);
            outf.Write(Break1N);
            outf.Write(Break2N);
        }

        /// <summary>
        /// We are restoring a saved game.  The TrainCar class has already
        /// been initialized.   Restore the game state.
        /// </summary>
        public void Restore(BinaryReader inf)
        {
            if (null == inf)
                throw new ArgumentNullException(nameof(inf));
            Rigid = inf.ReadBoolean();
            R0X = inf.ReadSingle();
            R0Y = inf.ReadSingle();
            R0Diff = inf.ReadSingle();
            Stiffness1NpM = inf.ReadSingle();
            Stiffness2NpM = inf.ReadSingle();
            CouplerSlackAM = inf.ReadSingle();
            CouplerSlackBM = inf.ReadSingle();
            Break1N = inf.ReadSingle();
            Break2N = inf.ReadSingle();
        }
    }
}
