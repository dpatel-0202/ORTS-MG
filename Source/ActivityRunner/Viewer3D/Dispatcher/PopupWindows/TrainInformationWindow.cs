﻿
using System;
using System.Collections.Generic;

using System.Collections.Specialized;
using System.Linq;

using GetText;

using Microsoft.Xna.Framework;

using Orts.Common;
using Orts.Common.DebugInfo;
using Orts.Graphics.Window;
using Orts.Graphics.Window.Controls;
using Orts.Graphics.Window.Controls.Layout;
using Orts.Simulation;
using Orts.Simulation.Physics;
using Orts.Simulation.RollingStocks;

namespace Orts.ActivityRunner.Viewer3D.Dispatcher.PopupWindows
{
    internal class TrainInformationWindow : WindowBase
    {

        private class TrainInformation : INameValueInformationProvider
        {
            public NameValueCollection DebugInfo { get; } = new NameValueCollection();

            public Dictionary<string, FormatOption> FormattingOptions { get; } = new Dictionary<string, FormatOption>();
        }

        private readonly TrainInformation trainInformation = new TrainInformation();

        private Train train;
        private TrainCar locomotive;
        
        public TrainInformationWindow(WindowManager owner, Point relativeLocation, Catalog catalog = null) :
            base(owner, (catalog ??= CatalogManager.Catalog).GetString("Train Information"), relativeLocation, new Point(200, 180), catalog)
        {
        }

        public void UpdateTrain(ITrain train)
        {
            if (train is Train physicalTrain)
            {                
                this.train = physicalTrain;
                locomotive = physicalTrain.LeadLocomotive ?? physicalTrain.Cars.OfType<MSTSLocomotive>().FirstOrDefault();
                trainInformation.DebugInfo[Catalog.GetString("Train")] = train.Name;
                trainInformation.DebugInfo[Catalog.GetString("Speed")] = FormatStrings.FormatSpeedDisplay(physicalTrain.SpeedMpS, Simulator.Instance.MetricUnits);
                trainInformation.DebugInfo["Gradient"] = $"{-locomotive?.CurrentElevationPercent:F1}%";
                trainInformation.DebugInfo["Direction"] = Math.Abs(physicalTrain.MUReverserPercent) != 100 ? $"{Math.Abs(physicalTrain.MUReverserPercent):F0} {physicalTrain.MUDirection.GetLocalizedDescription()}" : $"{physicalTrain.MUDirection.GetLocalizedDescription()}";
                trainInformation.DebugInfo["Cars"] = $"{physicalTrain.Cars.Count}";
                trainInformation.DebugInfo["Type"] = $"{(physicalTrain.IsFreight ? Catalog.GetString("Freight") : Catalog.GetString("Passenger"))}";
                trainInformation.DebugInfo["Train Type"] = $"{physicalTrain.TrainType}";
                trainInformation.DebugInfo["Control Mode"] = $"{physicalTrain.ControlMode}";
            }
        }

        protected override void Update(GameTime gameTime, bool shouldUpdate)
        {
            base.Update(gameTime, shouldUpdate);
            if (shouldUpdate && train != null)
            {
                trainInformation.DebugInfo[Catalog.GetString("Speed")] = FormatStrings.FormatSpeedDisplay(train.SpeedMpS, Simulator.Instance.MetricUnits);
                trainInformation.DebugInfo["Gradient"] = $"{-locomotive?.CurrentElevationPercent:F1}%";
                trainInformation.DebugInfo["Direction"] = Math.Abs(train.MUReverserPercent) != 100 ? $"{Math.Abs(train.MUReverserPercent):F0} {train.MUDirection.GetLocalizedDescription()}" : $"{train.MUDirection.GetLocalizedDescription()}";
            }
        }

        protected override ControlLayout Layout(ControlLayout layout, float headerScaling = 1)
        {
            layout = base.Layout(layout, headerScaling);
            layout = layout.AddLayoutVertical();
            NameValueTextGrid signalStates = new NameValueTextGrid(this, 0, 0, layout.RemainingWidth, layout.RemainingHeight)
            {
                InformationProvider = trainInformation,
                ColumnWidth = 100,
            };
            layout.Add(signalStates);
            return layout;
        }

    }
}
