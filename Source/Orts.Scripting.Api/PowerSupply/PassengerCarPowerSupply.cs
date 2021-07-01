﻿// COPYRIGHT 2021 by the Open Rails project.
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

using System;

using Orts.Common;

namespace Orts.Scripting.Api.PowerSupply
{
    public abstract class PassengerCarPowerSupply : PowerSupply
    {
        /// <summary>
        /// Current state of the ventilation system
        /// </summary>
        public Func<PowerSupplyState> CurrentVentilationState;
        /// <summary>
        /// Current state of the heating system
        /// </summary>
        public Func<PowerSupplyState> CurrentHeatingState;
        /// <summary>
        /// Current state of the air conditioning system
        /// </summary>
        public Func<PowerSupplyState> CurrentAirConditioningState;
        /// <summary>
        /// Current power consumed on the electric power supply line
        /// </summary>
        public Func<float> CurrentElectricTrainSupplyPowerW;
        /// <summary>
        /// Current thermal power generated by the heating and air conditioning systems
        /// Positive if heating
        /// Negative if air conditioning (cooling)
        /// </summary>
        public Func<float> CurrentHeatFlowRateW;
        /// <summary>
        /// Systems power on delay when electric train supply has been switched on
        /// </summary>
        public Func<float> PowerOnDelayS;
        /// <summary>
        /// Power consumed all the time on the electric train supply line
        /// </summary>
        public Func<float> ContinuousPowerW;
        /// <summary>
        /// Power consumed when heating is on
        /// </summary>
        public Func<float> HeatingPowerW;
        /// <summary>
        /// Power consumed when air conditioning is on
        /// </summary>
        public Func<float> AirConditioningPowerW;
        /// <summary>
        /// Yield of the air conditioning system
        /// </summary>
        public Func<float> AirConditioningYield;
        /// <summary>
        /// Desired temperature inside the passenger car
        /// </summary>
        public Func<float> DesiredTemperatureC;
        /// <summary>
        /// Current temperature inside the passenger car
        /// </summary>
        public Func<float> InsideTemperatureC;
        /// <summary>
        /// Current temperature outside the passenger car
        /// </summary>
        public Func<float> OutsideTemperatureC;

        /// <summary>
        /// Sets the current state of the ventilation system
        /// </summary>
        public Action<PowerSupplyState> SetCurrentVentilationState;
        /// <summary>
        /// Sets the current state of the heating system
        /// </summary>
        public Action<PowerSupplyState> SetCurrentHeatingState;
        /// <summary>
        /// Sets the current state of the air conditioning system
        /// </summary>
        public Action<PowerSupplyState> SetCurrentAirConditioningState;
        /// <summary>
        /// Sets the current power consumed on the electric power supply line
        /// </summary>
        public Action<float> SetCurrentElectricTrainSupplyPowerW;
        /// <summary>
        /// Sets the current thermal power generated by the heating and air conditioning systems
        /// Positive if heating
        /// Negative if air conditioning (cooling)
        /// </summary>
        public Action<float> SetCurrentHeatFlowRateW;
    }
}