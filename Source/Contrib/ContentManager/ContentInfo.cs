﻿// COPYRIGHT 2014, 2015 by the Open Rails project.
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

// Uncomment this define to show a textual representation of the serialised Content items for debugging.
//#define DEBUG_CONTENT_SERIALIZATION

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Orts.Formats.Msts;
using Orts.ContentManager.Models;
using Path = Orts.ContentManager.Models.Path;
using Orts.Formats.Msts.Files;

namespace Orts.ContentManager
{
    public static class ContentInfo
    {
        public static string GetText(ContentBase content)
        {
            if (null == content)
                throw new ArgumentNullException(nameof(content));

            StringBuilder details = new StringBuilder();
            details.AppendLine($"Type:\t{content.Type}");
            details.AppendLine($"Name:\t{content.Name}");
            details.AppendLine($"Path:\t{content.PathName}");
 
            try
            {
                switch(content.Type)
                {
                    case ContentType.Route:
                        Route route = new Route(content);
                        details.AppendLine($"Name:\t{route.Name}");
                        details.AppendLine();
                        details.AppendLine();
                        details.AppendLine($"Description:\t{route.Description}");
                        details.AppendLine();

                        if (content is ContentMSTSRoute)
                        {
                            RouteFile routeFile = new RouteFile(FolderStructure.Route(content.PathName).TrackFileName);
                            details.AppendLine($"Route ID:\t{routeFile.Route.RouteID}");
                            details.AppendLine($"Route Key:\t{routeFile.Route.FileName}");
                        }
                        break;
                    case ContentType.Activity:
                        Activity activity = new Activity(content);
                        details.AppendLine($"Name:\t{activity.Name}");
                        foreach (string playerService in activity.PlayerServices)
                            details.AppendLine($"Player:\t\u0001{playerService}\u0002Service\u0001");
                        foreach (string activityService in activity.Services)
                            details.AppendLine($"Traffic:\t\u0001{activityService}\u0002Service\u0001");
                        details.AppendLine();
                        details.AppendLine();
                        details.AppendLine();
                        details.AppendLine($"Description:\t{activity.Description}");
                        details.AppendLine();
                        details.AppendLine();
                        details.AppendLine();
                        details.AppendLine($"Briefing:\t{activity.Briefing}");
                        details.AppendLine();
                        break;
                    case ContentType.Service:
                        Service service = new Service(content);
                        details.AppendLine($"Name:\t{service.Name}");
                        details.AppendLine($"ID:\t{service.ID}");
                        details.AppendLine($"Start time:\t{service.StartTime}");
                        details.AppendLine($"Consist:\t\u0001{service.Consist}\u0002Consist\u0001{(service.Reversed ? "(reversed)" : string.Empty)}");
                        details.AppendLine($"Path:\t\u0001{service.Path}\u0002Path\u0001");
                        details.AppendLine();
                        details.AppendLine($"Arrival:\tDeparture:\tStation:\tDistance:\t");
                        foreach (StationStop item in service.Stops)
                            if (string.IsNullOrEmpty(item.Station))
                                details.AppendLine($"{item.ArrivalTime.FormatDateTime()}\t{item.DepartureTime.FormatDateTime()}\t{item.PlatformID}\t{item.Distance} m");
                            else
                                details.AppendLine($"{item.ArrivalTime.FormatDateTime()}\t{item.DepartureTime.FormatDateTime()}\t{item.Station}");
                        break;
                    case ContentType.Path:
                        Path path = new Path(content);
                        details.AppendLine($"Name:\t{path.Name}");
                        details.AppendLine($"Start:\t{path.StartName}");
                        details.AppendLine($"End:\t{path.EndName}");
                        details.AppendLine();
                        details.AppendLine("Path:\tLocation:\tFlags:\t");
                        HashSet<PathNode> visitedNodes = new HashSet<PathNode>();
                        HashSet<PathNode> rejoinNodes = new HashSet<PathNode>();
                        foreach (PathNode node in path.Nodes)
                        {
                            foreach (PathNode nextNode in node.Next)
                            {
                                if (!visitedNodes.Contains(nextNode))
                                    visitedNodes.Add(nextNode);
                                else if (!rejoinNodes.Contains(nextNode))
                                    rejoinNodes.Add(nextNode);
                            }
                        }
                        List<PathNode> tracks = new List<PathNode>() { path.Nodes.First() };
                        int activeTrack = 0;
                        while (tracks.Count > 0)
                        {
                            PathNode node = tracks[activeTrack];
                            StringBuilder line = new StringBuilder();
                            line.Append(' ');
                            for (int i = 0; i < tracks.Count; i++)
                                line.Append(i == activeTrack ? " |" : " .");
                            if (node.NodeType == PathNodeType.Wait)
                                line.AppendLine($"\t{node.Location}\t{node.NodeType} (wait for {node.WaitTime} seconds)");
                            else
                                line.AppendLine($"\t{node.Location}\t{node.NodeType}");
                            if (!node.Next.Any())
                            {
                                line.Append(' ');
                                for (int i = 0; i < tracks.Count; i++)
                                    line.Append(i == activeTrack ? @"  " : @" .");
                                line.AppendLine();
                            }
                            else if (node.Next.Count() == 2)
                            {
                                line.Append(' ');
                                for (int i = 0; i < tracks.Count; i++)
                                    line.Append(i == activeTrack ? @" |\" : @" .");
                                line.AppendLine();
                            }
                            tracks.RemoveAt(activeTrack);
                            tracks.InsertRange(activeTrack, node.Next);
                            if (node.Next.Any() && rejoinNodes.Contains(tracks[activeTrack]))
                            {
                                activeTrack++;
                                activeTrack %= tracks.Count;
                                if (rejoinNodes.Contains(tracks[activeTrack]))
                                {
                                    activeTrack = tracks.IndexOf(tracks[activeTrack]);
                                    tracks.RemoveAt(tracks.LastIndexOf(tracks[activeTrack]));
                                    line.Append(' ');
                                    for (int i = 0; i < tracks.Count; i++)
                                        line.Append(i == activeTrack ? @" |/" : @" .");
                                    line.AppendLine();
                                }
                            }
                            details.Append(line);
                        }
                        break;
                    case ContentType.Consist:
                        Consist consist = new Consist(content);
                        details.AppendLine($"Name:\t{consist.Name}");
                        details.AppendLine($"Car ID:\tDirection:\tName:\t");
                        foreach (ConsistCar consistCar in consist.Cars)
                            details.AppendLine($"{consistCar.ID}\t{consistCar.Direction}\t\u0001{consistCar.Name}\u0002Car\u0001");
                        details.AppendLine();
                        break;
                    case ContentType.Car:
                        Car car = new Car(content);
                        details.AppendLine($"Type:\t{car.Type}");
                        details.AppendLine($"Name:\t{car.Name}");
                        details.AppendLine();
                        details.AppendLine();
                        details.AppendLine($"Description:\t{car.Description}");
                        details.AppendLine();
                        break;
                    default:
                        if (content is ContentMSTSCab)
                        {
                            CabViewFile cabView = new CabViewFile(content.PathName);
                            details.AppendLine($"Position:\tDimensions:\tStyle:\tType:\t");
                            foreach (Formats.Msts.Models.CabViewControl control in cabView.CabViewControls)
                                details.AppendLine(control.ToString());
                            details.AppendLine();
                        }
                        break;
                }
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception error)
#pragma warning restore CA1031 // Do not catch general exception types
            {
                details.AppendLine();
                details.AppendLine(error.ToString());
                details.AppendLine();
            }

            return details.ToString();
        }

        private static string FormatDateTime(this DateTime dateTime)
        {
            return $"{dateTime.Day - 1} {dateTime.ToLongTimeString()}";
        }
    }
}
