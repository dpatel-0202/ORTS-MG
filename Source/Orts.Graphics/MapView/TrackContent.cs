﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Xna.Framework;

using Orts.Common;
using Orts.Common.DebugInfo;
using Orts.Common.Position;
using Orts.Formats.Msts;
using Orts.Formats.Msts.Files;
using Orts.Formats.Msts.Models;
using Orts.Graphics.DrawableComponents;
using Orts.Graphics.MapView.Widgets;
using Orts.Graphics.Xna;

namespace Orts.Graphics.MapView
{
    public class TrackContent : ContentBase
    {
        #region nearest items
        private GridTile nearestGridTile;
        private TrackItemBase nearestTrackItem;
        private TrackSegment nearestTrackSegment;
        private JunctionSegment nearestJunctionNode;
        private TrackEndSegment nearestTrackEndSegment;
        private (double distance, INameValueInformationProvider statusItem) nearestSegmentForStatus;
        private RoadSegment nearestRoadSegment;
        #endregion

        private readonly InsetComponent insetComponent;
        private readonly TrackNodeInfoProxy trackNodeInfo = new TrackNodeInfoProxy();

        internal TileIndexedList<TrackSegment, Tile> TrackSegments { get; private set; }
        internal TileIndexedList<TrackEndSegment, Tile> TrackEndSegments { get; private set; }
        internal TileIndexedList<JunctionSegment, Tile> JunctionSegments { get; private set; }
        internal TileIndexedList<TrackItemBase, Tile> TrackItems { get; private set; }
        internal TileIndexedList<GridTile, Tile> Tiles { get; private set; }
        internal TileIndexedList<RoadSegment, Tile> RoadSegments { get; private set; }
        internal TileIndexedList<RoadEndSegment, Tile> RoadEndSegments { get; private set; }
        internal TileIndexedList<PlatformPath, Tile> Platforms { get; private set; }

        internal Dictionary<int, List<TrackSegment>> TrackNodeSegments { get; private set; }
        internal Dictionary<int, List<RoadSegment>> RoadTrackNodeSegments { get; private set; }

        public TrackContent(Game game) :
            base(game)
        {
            FormattingOptions.Add("Route Information", FormatOption.Bold);
            DebugInfo.Add("Route Information", null);
            DebugInfo["Route Name"] = RuntimeData.Instance.RouteName;
            insetComponent = ContentArea.Game.Components.OfType<InsetComponent>().FirstOrDefault();
            TrackNodeInfo = trackNodeInfo;
        }

        public override async Task Initialize()
        {
            await Task.Run(() => AddTrackSegments()).ConfigureAwait(false);
            await Task.Run(() => AddTrackItems()).ConfigureAwait(false);

            ContentArea.Initialize();

            DebugInfo["Metric Scale"] = RuntimeData.Instance.UseMetricUnits.ToString();
            DebugInfo["Track Nodes"] = $"{TrackNodeSegments.Count}";
            DebugInfo["Track Segments"] = $"{TrackSegments.ItemCount}";
            DebugInfo["Track End Segments"] = $"{TrackEndSegments.ItemCount}";
            DebugInfo["Junction Segments"] = $"{JunctionSegments.ItemCount}";
            DebugInfo["Track Items"] = $"{TrackItems.ItemCount}";
            DebugInfo["Road Nodes"] = $"{RoadTrackNodeSegments.Count}";
            DebugInfo["Road Segments"] = $"{RoadSegments.ItemCount}";
            DebugInfo["Road End Segments"] = $"{RoadEndSegments.ItemCount}";
            DebugInfo["Tiles"] = $"{Tiles.Count}";
        }

        public void UpdateWidgetColorSettings(EnumArray<string, ColorSetting> colorPreferences)
        {
            if (null == colorPreferences)
                throw new ArgumentNullException(nameof(colorPreferences));

            foreach (ColorSetting setting in EnumExtension.GetValues<ColorSetting>())
            {
                ContentArea.UpdateColor(setting, ColorExtension.FromName(colorPreferences[setting]));
            }
        }

        internal override void UpdatePointerLocation(in PointD position, ITile bottomLeft, ITile topRight)
        {
            nearestSegmentForStatus = (float.NaN, null);
            IEnumerable<ITileCoordinate<Tile>> result = Tiles.FindNearest(position, bottomLeft, topRight);
            if (result.First() != nearestGridTile)
            {
                nearestGridTile = result.First() as GridTile;
            }
            double distance = double.MaxValue;
            foreach (TrackItemBase trackItem in TrackItems[nearestGridTile.Tile])
            {
                double itemDistance = trackItem.Location.DistanceSquared(position);
                if (itemDistance < distance)
                {
                    nearestTrackItem = trackItem;
                    distance = itemDistance;
                }
            }
            if ((viewSettings & MapViewItemSettings.Tracks) == MapViewItemSettings.Tracks)
            {
                distance = double.MaxValue;
                foreach (TrackSegment trackSegment in TrackSegments[nearestGridTile.Tile])
                {
                    double itemDistance = trackSegment.DistanceSquared(position);
                    if (itemDistance < distance)
                    {
                        nearestTrackSegment = trackSegment;
                        distance = itemDistance;
                    }
                }
                if (distance < 100)
                {
                    nearestSegmentForStatus = (distance, nearestTrackSegment);
                }
                else
                    nearestTrackSegment = null;
            }
            if ((viewSettings & MapViewItemSettings.JunctionNodes) == MapViewItemSettings.JunctionNodes)
            {
                distance = double.MaxValue;
                foreach (JunctionSegment junctionSegment in JunctionSegments[nearestGridTile.Tile])
                {
                    double itemDistance = position.DistanceSquared(junctionSegment.Location);
                    if (itemDistance < distance)
                    {
                        nearestJunctionNode = junctionSegment;
                        distance = itemDistance;
                    }
                }
                if (distance < 100)
                {
                    if (distance < 1 || distance < nearestSegmentForStatus.distance)
                        nearestSegmentForStatus = (distance, nearestJunctionNode);
                }
                else
                    nearestJunctionNode = null;
            }
            if ((viewSettings & MapViewItemSettings.EndsNodes) == MapViewItemSettings.EndsNodes)
            {
                distance = double.MaxValue;
                foreach (TrackEndSegment endSegment in TrackEndSegments[nearestGridTile.Tile])
                {
                    double itemDistance = position.DistanceSquared(endSegment.Location);
                    if (itemDistance < distance)
                    {
                        nearestTrackEndSegment = endSegment;
                        distance = itemDistance;
                    }
                }
                if (distance < 100)
                {
                    if (distance < 1 || distance < nearestSegmentForStatus.distance)
                        nearestSegmentForStatus = (distance, nearestTrackEndSegment);
                }
                else
                    nearestTrackEndSegment = null;
            }
            distance = double.MaxValue;
            if ((viewSettings & MapViewItemSettings.Roads) == MapViewItemSettings.Roads)
            {
                foreach (RoadSegment trackSegment in RoadSegments[nearestGridTile.Tile])
                {
                    double itemDistance = trackSegment.DistanceSquared(position);
                    if (itemDistance < distance)
                    {
                        nearestRoadSegment = trackSegment;
                        distance = itemDistance;
                    }
                }
            }
            trackNodeInfo.Source = nearestSegmentForStatus.statusItem;
        }

        internal override void Draw(ITile bottomLeft, ITile topRight)
        {
            if ((viewSettings & MapViewItemSettings.Grid) == MapViewItemSettings.Grid)
            {
                foreach (GridTile tile in Tiles.BoundingBox(bottomLeft, topRight))
                {
                    tile.Draw(ContentArea);
                }
                nearestGridTile?.Draw(ContentArea, ColorVariation.Complement);
            }
            if ((viewSettings & MapViewItemSettings.Platforms) == MapViewItemSettings.Platforms)
            {
                foreach (PlatformPath platform in Platforms.BoundingBox(bottomLeft, topRight))
                {
                    if (ContentArea.InsideScreenArea(platform))
                        platform.Draw(ContentArea);
                }
            }
            if ((viewSettings & MapViewItemSettings.Tracks) == MapViewItemSettings.Tracks)
            {
                foreach (TrackSegment segment in TrackSegments.BoundingBox(bottomLeft, topRight))
                {
                    if (ContentArea.InsideScreenArea(segment))
                        segment.Draw(ContentArea);
                }
                if (nearestTrackSegment != null)
                {
                    foreach (TrackSegment segment in TrackNodeSegments[nearestTrackSegment.TrackNodeIndex])
                    {
                        segment.Draw(ContentArea, ColorVariation.ComplementHighlight);
                    }
                    nearestTrackSegment.Draw(ContentArea, ColorVariation.Complement);
                }
            }
            if ((viewSettings & MapViewItemSettings.EndsNodes) == MapViewItemSettings.EndsNodes)
            {
                foreach (TrackEndSegment endNode in TrackEndSegments.BoundingBox(bottomLeft, topRight))
                {
                    if (ContentArea.InsideScreenArea(endNode))
                        endNode.Draw(ContentArea);
                }
                if (nearestTrackEndSegment != null)
                    nearestTrackEndSegment.Draw(ContentArea, ColorVariation.ComplementHighlight, 1.5);
            }
            if ((viewSettings & MapViewItemSettings.JunctionNodes) == MapViewItemSettings.JunctionNodes)
            {
                foreach (JunctionSegment junctionNode in JunctionSegments.BoundingBox(bottomLeft, topRight))
                {
                    if (ContentArea.InsideScreenArea(junctionNode))
                        junctionNode.Draw(ContentArea);
                }
                if (nearestJunctionNode != null)
                    nearestJunctionNode.Draw(ContentArea, ColorVariation.ComplementHighlight, 1.5);
            }
            if ((viewSettings & MapViewItemSettings.Roads) == MapViewItemSettings.Roads)
            {
                foreach (RoadSegment segment in RoadSegments.BoundingBox(bottomLeft, topRight))
                {
                    if (ContentArea.InsideScreenArea(segment))
                        segment.Draw(ContentArea);
                }
                if (nearestRoadSegment != null)
                {
                    foreach (RoadSegment segment in RoadTrackNodeSegments[nearestRoadSegment.TrackNodeIndex])
                    {
                        segment.Draw(ContentArea, ColorVariation.ComplementHighlight);
                    }
                    nearestRoadSegment.Draw(ContentArea, ColorVariation.Complement);
                }
            }
            if ((viewSettings & MapViewItemSettings.RoadEndNodes) == MapViewItemSettings.RoadEndNodes)
            {
                foreach (RoadEndSegment endNode in RoadEndSegments.BoundingBox(bottomLeft, topRight))
                {
                    if (ContentArea.InsideScreenArea(endNode))
                        endNode.Draw(ContentArea);
                }
            }
            foreach (TrackItemBase trackItem in TrackItems.BoundingBox(bottomLeft, topRight))
            {
                if (trackItem.ShouldDraw(viewSettings) && ContentArea.InsideScreenArea(trackItem))
                    trackItem.Draw(ContentArea);
            }
            if (nearestTrackItem?.ShouldDraw(viewSettings) ?? false)
                nearestTrackItem.Draw(ContentArea, ColorVariation.Highlight);
        }

        #region build content database
        private void AddTrackSegments()
        {
            TrackDB trackDB = RuntimeData.Instance.TrackDB;
            RoadTrackDB roadTrackDB = RuntimeData.Instance.RoadTrackDB;
            TrackSectionsFile trackSectionsFile = RuntimeData.Instance.TSectionDat;

            double minX = double.MaxValue, minY = double.MaxValue, maxX = double.MinValue, maxY = double.MinValue;

            List<TrackSegment> trackSegments = new List<TrackSegment>();
            List<TrackEndSegment> endSegments = new List<TrackEndSegment>();
            List<JunctionSegment> junctionSegments = new List<JunctionSegment>();
            List<RoadSegment> roadSegments = new List<RoadSegment>();
            List<RoadEndSegment> roadEndSegments = new List<RoadEndSegment>();
            foreach (TrackNode trackNode in trackDB?.TrackNodes ?? Enumerable.Empty<TrackNode>())
            {
                if (null == trackSectionsFile)
                    throw new ArgumentNullException(nameof(trackSectionsFile));

                switch (trackNode)
                {
                    case TrackEndNode trackEndNode:
                        TrackVectorNode connectedVectorNode = trackDB.TrackNodes[trackEndNode.TrackPins[0].Link] as TrackVectorNode;
                        endSegments.Add(new TrackEndSegment(trackEndNode, connectedVectorNode, trackSectionsFile.TrackSections));
                        break;
                    case TrackVectorNode trackVectorNode:
                        int i = 0;
                        foreach (TrackVectorSection trackVectorSection in trackVectorNode.TrackVectorSections)
                        {
                            trackSegments.Add(new TrackSegment(trackVectorSection, trackSectionsFile.TrackSections, trackVectorNode.Index, i++));
                        }
                        break;
                    case TrackJunctionNode trackJunctionNode:
                        junctionSegments.Add(new JunctionSegment(trackJunctionNode));
                        break;
                }
            }

            insetComponent?.SetTrackSegments(trackSegments);

            TrackSegments = new TileIndexedList<TrackSegment, Tile>(trackSegments);
            JunctionSegments = new TileIndexedList<JunctionSegment, Tile>(junctionSegments);
            TrackEndSegments = new TileIndexedList<TrackEndSegment, Tile>(endSegments);
            TrackNodeSegments = trackSegments.GroupBy(t => t.TrackNodeIndex).ToDictionary(i => i.Key, i => i.ToList());

            foreach (TrackNode trackNode in roadTrackDB?.TrackNodes ?? Enumerable.Empty<TrackNode>())
            {
                if (null == trackSectionsFile)
                    throw new ArgumentNullException(nameof(trackSectionsFile));

                switch (trackNode)
                {
                    case TrackEndNode trackEndNode:
                        TrackVectorNode connectedVectorNode = roadTrackDB.TrackNodes[trackEndNode.TrackPins[0].Link] as TrackVectorNode;
                        roadEndSegments.Add(new RoadEndSegment(trackEndNode, connectedVectorNode, trackSectionsFile.TrackSections));
                        break;
                    case TrackVectorNode trackVectorNode:
                        int i = 0;
                        foreach (TrackVectorSection trackVectorSection in trackVectorNode.TrackVectorSections)
                        {
                            roadSegments.Add(new RoadSegment(trackVectorSection, trackSectionsFile.TrackSections, trackVectorNode.Index, i++));
                        }
                        break;
                }
            }

            RoadSegments = new TileIndexedList<RoadSegment, Tile>(roadSegments);
            RoadEndSegments = new TileIndexedList<RoadEndSegment, Tile>(roadEndSegments);
            RoadTrackNodeSegments = roadSegments.GroupBy(t => t.TrackNodeIndex).ToDictionary(i => i.Key, i => i.ToList());

            Tiles = new TileIndexedList<GridTile, Tile>(
                TrackSegments.Select(d => d.Tile as ITile).Distinct()
                .Union(TrackEndSegments.Select(d => d.Tile as ITile).Distinct())
                .Union(RoadSegments.Select(d => d.Tile as ITile).Distinct())
                .Union(RoadEndSegments.Select(d => d.Tile as ITile).Distinct())
                .Select(t => new GridTile(t)));

            if (Tiles.Count == 1)
            {
                foreach (TrackEndSegment trackEndSegment in TrackEndSegments)
                {
                    minX = Math.Min(minX, trackEndSegment.Location.X);
                    minY = Math.Min(minY, trackEndSegment.Location.Y);
                    maxX = Math.Max(maxX, trackEndSegment.Location.X);
                    maxY = Math.Max(maxY, trackEndSegment.Location.Y);
                }
            }
            else
            {
                minX = Math.Min(minX, Tiles[0][0].Tile.X);
                maxX = Math.Max(maxX, Tiles[^1][0].Tile.X);
                foreach (GridTile tile in Tiles)
                {
                    minY = Math.Min(minY, tile.Tile.Z);
                    maxY = Math.Max(maxY, tile.Tile.Z);
                }
                minX = minX * WorldLocation.TileSize - WorldLocation.TileSize / 2;
                maxX = maxX * WorldLocation.TileSize + WorldLocation.TileSize / 2;
                minY = minY * WorldLocation.TileSize - WorldLocation.TileSize / 2;
                maxY = maxY * WorldLocation.TileSize + WorldLocation.TileSize / 2;
            }
            Bounds = new Rectangle((int)minX, (int)minY, (int)(maxX - minX), (int)(maxY - minY));
        }

        private void AddTrackItems()
        {
            List<TrackItemBase> trackItems = TrackItemBase.CreateTrackItems(RuntimeData.Instance.TrackDB?.TrackItems, RuntimeData.Instance.SignalConfigFile, RuntimeData.Instance.TrackDB);

            Dictionary<int, PlatformTrackItem> platformItems = trackItems.OfType<PlatformTrackItem>().ToDictionary(p => p.Id);
            foreach (PlatformTrackItem trackItem in platformItems.Values)
                trackItems.Remove(trackItem);
            Platforms = new TileIndexedList<PlatformPath, Tile>(PlatformPath.CreatePlatforms(platformItems, TrackNodeSegments));
            TrackItems = new TileIndexedList<TrackItemBase, Tile>(trackItems
                .Concat(TrackItemBase.CreateRoadItems(RuntimeData.Instance.RoadTrackDB?.TrackItems)));
        }
        #endregion

        private protected class TrackNodeInfoProxy : TrackNodeInfoProxyBase
        {
            internal INameValueInformationProvider Source;

            public override NameValueCollection DebugInfo => Source?.DebugInfo;

            public override Dictionary<string, FormatOption> FormattingOptions => Source?.FormattingOptions;
        }
    }
}
