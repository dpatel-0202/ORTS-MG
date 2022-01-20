﻿
using System;
using System.Linq;
using System.Reflection.Metadata;

using GetText;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

using Orts.Common.Input;
using Orts.Common.Position;
using Orts.Graphics;
using Orts.Graphics.Track;
using Orts.Graphics.Window;
using Orts.Graphics.Window.Controls;
using Orts.Graphics.Window.Controls.Layout;

namespace Orts.TrackViewer.PopupWindows
{
    public class LocationWindow : WindowBase
    {
        private readonly MouseInputGameComponent input;
        private MouseState lastMouseState;
        private const double piRad = 180 / Math.PI;
        ContentArea content;
        Label locationLabel;

        public LocationWindow(WindowManager owner, Point relativeLocation) :
            base(owner, CatalogManager.Catalog.GetString("World Coordinates"), relativeLocation, new Point(200, 200))
        {
            input = Owner.Game.Components.OfType<MouseInputGameComponent>().Single();
        }

        protected override ControlLayout Layout(ControlLayout layout, float headerScaling)
        {
            layout = base.Layout(layout, headerScaling);

            ControlLayoutHorizontal statusTextLine = layout.AddLayoutHorizontal((int)(Owner.TextFontDefault.Height * 1.25));
            locationLabel = new Label(this, 0, 0, layout.RemainingWidth, Owner.TextFontDefault.Height, string.Empty, HorizontalAlignment.Center, Owner.TextFontDefault, Color.Orange);
            statusTextLine.Add(locationLabel);

            return layout;
        }

        protected override void Update(GameTime gameTime)
        {
            ref readonly MouseState mouseState = ref input.MouseState;
            if (mouseState != lastMouseState)
            {
                lastMouseState = mouseState;
                Point worldPoint = Point.Zero;
                ;// = content.ScreenToWorldCoordinates(mouseState.Position);
                WorldLocation location = new WorldLocation(0, 0, worldPoint.X, 0, worldPoint.Y, true);
                (double latitude, double longitude) = EarthCoordinates.ConvertWTC(location);

                longitude *= piRad; // E/W
                latitude *= piRad;  // N/S
                char hemisphere = latitude >= 0 ? 'N' : 'S';
                char direction = longitude >= 0 ? 'E' : 'W';
                longitude = Math.Abs(longitude);
                latitude = Math.Abs(latitude);
                int longitudeDegree = (int)Math.Truncate(longitude);
                int latitudeDegree = (int)Math.Truncate(latitude);

                longitude -= longitudeDegree;
                latitude -= latitudeDegree;
                longitude *= 60;
                latitude *= 60;
                int longitudeMinute = (int)Math.Truncate(longitude);
                int latitudeMinute = (int)Math.Truncate(latitude);
                longitude -= longitudeMinute;
                latitude -= latitudeMinute;
                longitude *= 60;
                latitude *= 60;
                int longitudeSecond = (int)Math.Truncate(longitude);
                int latitudeSecond = (int)Math.Truncate(latitude);

                string locationText = string.Format(System.Globalization.CultureInfo.CurrentCulture, $"{latitudeDegree}°{latitudeMinute,2:00}'{latitudeSecond,2:00}\"{hemisphere} {longitudeDegree}°{longitudeMinute,2:00}'{longitudeSecond,2:00}\"{direction}");
                locationLabel.Text = locationText;
            }
            base.Update(gameTime);
        }
    }
}
