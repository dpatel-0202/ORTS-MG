﻿using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Orts.Common.Position;
using Orts.Models.Track;

namespace Orts.Graphics.MapView.Widgets
{
    internal class PlatformPath : TrackSegmentPathBase<PlatformSegment>, IDrawable<VectorPrimitive>
    {
        internal string PlatformName { get; }
        internal string StationName { get; }

        private class PlatformSection : TrackSegmentSectionBase<PlatformSegment>, IDrawable<VectorPrimitive>
        {
            public PlatformSection(int trackNodeIndex) :
                base(trackNodeIndex)
            {
            }

            public PlatformSection(int trackNodeIndex, in PointD startLocation, in PointD endLocation) :
                base(trackNodeIndex, startLocation, endLocation)
            {
            }

            public virtual void Draw(ContentArea contentArea, ColorVariation colorVariation = ColorVariation.None, double scaleFactor = 1)
            {
                foreach (PlatformSegment segment in SectionSegments)
                {
                    segment.Draw(contentArea, colorVariation, scaleFactor);
                }
            }

            protected override PlatformSegment CreateItem(in PointD start, in PointD end)
            {
                return new PlatformSegment(start, end);
            }

            protected override PlatformSegment CreateItem(TrackSegmentBase source)
            {
                return new PlatformSegment(source);
            }

            protected override PlatformSegment CreateItem(TrackSegmentBase source, in PointD start, in PointD end)
            {
                return new PlatformSegment(source, start, end);
            }
        }

        public PlatformPath(PlatformTrackItem start, PlatformTrackItem end) :
            base(start.Location, start.TrackVectorNode.Index, end.Location, end.TrackVectorNode.Index)
        {
            PlatformName = string.IsNullOrEmpty(start.PlatformName) ? end.PlatformName : start.PlatformName;
            StationName = string.IsNullOrEmpty(start.StationName) ? end.StationName : start.StationName;
            //Strip the station name out of platform name (only if they are not equal)
            if (PlatformName?.Length > StationName?.Length && PlatformName.StartsWith(StationName, System.StringComparison.OrdinalIgnoreCase))
                PlatformName = PlatformName[StationName.Length..];

            if (PathSections.Count == 0)
            {
                Trace.TraceWarning($"Platform items {start.TrackItemId} and {end.TrackItemId} could not be linked on the underlying track database for track nodes {start.TrackVectorNode.Index} and {end.TrackVectorNode.Index}. This may indicate an error or inconsistency in the route data.");
            }
        }

        public static List<PlatformPath> CreatePlatforms(IEnumerable<PlatformTrackItem> platformItems)
        {
            List<PlatformPath> result = new List<PlatformPath>();
            Dictionary<int, PlatformTrackItem> platformItemMappings = platformItems.ToDictionary(p => p.TrackItemId);
            while (platformItemMappings.Count > 0)
            {
                int sourceId = platformItemMappings.Keys.First();
                PlatformTrackItem start = platformItemMappings[sourceId];
                _ = platformItemMappings.Remove(sourceId);
                if (platformItemMappings.TryGetValue(start.LinkedId, out PlatformTrackItem end))
                {
                    if (end.LinkedId != start.TrackItemId)
                    {
                        Trace.TraceWarning($"Platform Item Pair has inconsistent linking from Source Id {start.TrackItemId} to target {start.LinkedId} vs Target id {end.TrackItemId} to source {end.LinkedId}.");
                    }
                    _ = platformItemMappings.Remove(end.TrackItemId);
                    result.Add(new PlatformPath(start, end));
                }
                else
                {
                    Trace.TraceWarning($"Linked Platform Item {start.LinkedId} for Platform Item {start.TrackItemId} not found.");
                }
            }
            return result;
        }

        public virtual void Draw(ContentArea contentArea, ColorVariation colorVariation = ColorVariation.None, double scaleFactor = 1)
        {
            foreach (PlatformSection segmentSection in PathSections)
            {
                segmentSection.Draw(contentArea, colorVariation, scaleFactor);
            }
        }

        public override double DistanceSquared(in PointD point)
        {
            return double.NaN;
        }

        protected override TrackSegmentSectionBase<PlatformSegment> AddSection(int trackNodeIndex, in PointD start, in PointD end)
        {
            return new PlatformSection(trackNodeIndex, start, end);
        }

        protected override TrackSegmentSectionBase<PlatformSegment> AddSection(int trackNodeIndex)
        {
            return new PlatformSection(trackNodeIndex);
        }

    }
}
