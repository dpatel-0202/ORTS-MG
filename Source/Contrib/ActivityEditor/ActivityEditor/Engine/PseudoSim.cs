﻿// 
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

using System.Collections.Generic;
using System.Linq;
using System.Threading;
/// This module ...
/// 
/// Author: Stéfan Paitoni
/// Updates : 
/// 


using Orts.ActivityEditor.Base;
using Orts.ActivityEditor.Base.Formats;
using Orts.Common;
using Orts.Formats.Msts;
using Orts.Formats.OR;
using Orts.Settings;

namespace Orts.ActivityEditor.Engine
{
    public class PseudoSim
    {
        public bool Paused = true;
        readonly Thread Thread;
        readonly ProcessState State;
        public MSTSDataConfig MstsDataConfig { get; set; }
        public ORRouteConfig orRouteConfig;
        public AreaRoute areaRoute;

        //public MSTSData mstsData;
        public TrackDatabaseFile TDB { get { return MstsDataConfig.TDB; } protected set { } }
        public RouteFile TRK { get { return MstsDataConfig.TRK; } protected set { } }
        public TrackSectionsFile TSectionDat { get { return MstsDataConfig.TSectionDat; } protected set { } }
        public AESignals Signals { get { return MstsDataConfig.Signals; } protected set { } }
        public SignalConfigurationFile SIGCFG { get { return MstsDataConfig.SIGCFG; } protected set { } }
        public string RoutePath { get { return MstsDataConfig.RoutePath; } protected set { } }
        public string MstsPath { get { return MstsDataConfig.MstsPath; } protected set { } }
        public AETraveller Traveller { get { return orRouteConfig.traveller; } protected set { } }
        public TrackNode[] Nodes { get; set; }
        public MSTSItems MstsItems { get; set; }

        public readonly UserSettings Settings;

        public PseudoSim(UserSettings settings)
        {
            Settings = settings;
            State = new ProcessState("Updater");
            Thread = new Thread(UpdaterThread);
            Thread.Start();
            MstsItems = new MSTSItems();
            areaRoute = new AreaRoute();
        }

        public void Dispose()
        {
            if (orRouteConfig.toSave)
                orRouteConfig.SaveConfig();
        }

        public void StopUpdaterThread()
        {
            Thread.Abort();
        }

        public void Start()
        {
        }

        public void LoadRoute(string routePath, TypeEditor interfaceType)
        {
            Program.actEditor.DisplayStatusMessage("Simulator Loading...");
            MstsDataConfig = new MSTSDataConfig(Program.AePreference.MSTSPath, routePath, interfaceType);
            Program.actEditor.DisplayStatusMessage("Load route Metadata ...");

            orRouteConfig = ORRouteConfig.LoadConfig(TRK.Tr_RouteFile.FileName, routePath, interfaceType);
            //AESignals = new AESignals(this);
            orRouteConfig.SetTraveller(TSectionDat, TDB);
            orRouteConfig.SetTileBase(MstsDataConfig.TileBase);
            orRouteConfig.ReduceItems();

            LoadItemsFromMSTS();
            ReAlignData();

        }

        public void LoadItemsFromMSTS()
        {
#if SHOW_STOPWATCH
            Stopwatch stopWatch = new Stopwatch();
            TimeSpan ts;
            string elapsedTime;
            if (File.Exists(@"C:\temp\stopwatch.txt"))
            {
                File.Delete(@"C:\temp\stopwatch.txt");
            }
#endif


            if (TDB == null ||
                TDB.TrackDB == null ||
                TDB.TrackDB.TrItemTable == null) return;
            foreach (GlobalItem item in orRouteConfig.AllItems)
            {
                if (item.GetType() == typeof(AEBufferItem))
                {
                    MstsItems.buffers.Add((AEBufferItem)item);
                }
            }
            Program.actEditor.DisplayStatusMessage("Start loading Track Nodes ...");
#if SHOW_STOPWATCH
            stopWatch.Start();
#endif
            Nodes = TDB.TrackDB.TrackNodes;
            for (int nodeIdx = 0; nodeIdx < Nodes.Length; nodeIdx++)
            {
                if (Nodes[nodeIdx] != null)
                {

                    TrackNode currNode = Nodes[nodeIdx];

                    AEBufferItem foundBuffer;
                    if (currNode.TrEndNode)
                    {
                        //Program.actEditor.DisplayStatusMessage("Init data for display...  Load End Nodes: " + currNode.Index);
                        foundBuffer = orRouteConfig.AllItems.FirstOrDefault(x => x.associateNodeIdx == currNode.Index) as AEBufferItem;
                        if (null == foundBuffer)
                        {
                            foundBuffer = new AEBufferItem((TrackNode)currNode);
                            MstsItems.buffers.Add(foundBuffer);
                        }
                        else
                        {
                            foundBuffer.updateNode(currNode);
                        }
#if SHOW_STOPWATCH
                        ts = stopWatch.Elapsed;
                        stopWatch.Reset();

                        // Format and display the TimeSpan value. 
                        elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:000}",
                            ts.Hours, ts.Minutes, ts.Seconds,
                            ts.Milliseconds);
                        File.AppendAllText(@"C:\temp\stopwatch.txt", "One END node: " + elapsedTime + "\n");
                        stopWatch.Start();
#endif

                    }
                    else if (currNode.TrVectorNode != null && currNode.TrVectorNode.TrVectorSections != null)
                    {
                        //Program.actEditor.DisplayStatusMessage("Init data for display...  Load Vector Nodes: " + currNode.Index);
                        if (currNode.TrVectorNode.TrVectorSections.Length > 1)
                        {
                            AddSegments(currNode);
                            TrVectorSection section = currNode.TrVectorNode.TrVectorSections[currNode.TrVectorNode.TrVectorSections.Length - 1];
                            MSTSCoord A = new MSTSCoord(section);

                            TrPin pin = currNode.TrPins[1];
                            {
                                TrackNode connectedNode = Nodes[pin.Link];
                                int direction = DrawUtility.getDirection(currNode, connectedNode);
                                if (A == connectedNode.getMSTSCoord(direction))
                                    continue;
                                AESegment aeSegment = new AESegment(A, connectedNode.getMSTSCoord(direction));
                                TrackSegment lineSeg = new TrackSegment(aeSegment, currNode, currNode.TrVectorNode.TrVectorSections.Length - 1, direction, TSectionDat);
                                AddTrItems(lineSeg, currNode);
                                MstsItems.AddSegment(lineSeg);
                            }
#if SHOW_STOPWATCH
                            ts = stopWatch.Elapsed;
                            stopWatch.Reset();

                            // Format and display the TimeSpan value. 
                            elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:000}",
                                ts.Hours, ts.Minutes, ts.Seconds,
                                ts.Milliseconds);
                            File.AppendAllText(@"C:\temp\stopwatch.txt", "One mult TRACK node: " + elapsedTime + "\n");
                            stopWatch.Start();
#endif
                        }
                        else
                        {
                            TrVectorSection s;
                            s = currNode.TrVectorNode.TrVectorSections[0];
                            areaRoute.ManageTiles(s.TileX, s.TileZ);
                            foreach (TrPin pin in currNode.TrPins)
                            {
                                TrackNode connectedNode = Nodes[pin.Link];
                                int direction = DrawUtility.getDirection(currNode, connectedNode);
                                if (MSTSCoord.Near(currNode.getMSTSCoord(direction), connectedNode.getMSTSCoord(direction)))
                                    continue;
                                AESegment aeSegment = new AESegment(currNode.getMSTSCoord(direction), connectedNode.getMSTSCoord(direction));
                                TrackSegment lineSeg = new TrackSegment(aeSegment, currNode, 0, direction, TSectionDat);
                                AddTrItems(lineSeg, currNode);
                                MstsItems.AddSegment(lineSeg);
                            }
#if SHOW_STOPWATCH
                            ts = stopWatch.Elapsed;
                            stopWatch.Reset();

                            // Format and display the TimeSpan value. 
                            elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:000}",
                                ts.Hours, ts.Minutes, ts.Seconds,
                                ts.Milliseconds);
                            File.AppendAllText(@"C:\temp\stopwatch.txt", "One simple TRACK node: " + elapsedTime + "\n");
                            stopWatch.Start();
#endif
                        }
                    }

                    else if (currNode.TrJunctionNode != null)
                    {
                        //Program.actEditor.DisplayStatusMessage("Init data for display...  Load Junction Nodes: " + currNode.Index);
                        MstsItems.switches.Add(new AEJunctionItem(currNode));
                        areaRoute.ManageTiles(currNode.UiD.TileX, currNode.UiD.TileZ);
#if SHOW_STOPWATCH
                        ts = stopWatch.Elapsed;
                        stopWatch.Reset();

                        // Format and display the TimeSpan value. 
                        elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:000}",
                            ts.Hours, ts.Minutes, ts.Seconds,
                            ts.Milliseconds);
                        File.AppendAllText(@"C:\temp\stopwatch.txt", "One JN node: " + elapsedTime + "\n");
                        stopWatch.Start();
#endif

                    }
                }
            }
#if SPA_ADD
            var maxsize = maxX - minX > maxX - minX ? maxX - minX : maxX - minX;
            maxsize = (int)maxsize / 100 * 100;
            if (maxsize < 2000)
                maxsize = 2000;
            ZoomFactor = (decimal)maxsize;
#endif
#region AddItem


            Program.actEditor.DisplayStatusMessage("Init data for display...  Load MSTS Items...");
            foreach (var item in TDB.TrackDB.TrItemTable)
            {
                if (item.ItemType == TrItem.trItemType.trSIGNAL && Signals != null)
                {
                    if (item is SignalItem)
                    {
#if SHOW_STOPWATCH
                        Program.actEditor.DisplayStatusMessage("Init data for display...  Load Items... Signal");
#endif
                        SignalItem si = item as SignalItem;

                        if (si.SigObj >= 0 && si.SigObj < Signals.SignalObjects.Length)
                        {
                            AESignalObject s = Signals.SignalObjects[si.SigObj];
                            if (s.isSignal) // && s.isSignalNormal())
                            {
                                MstsItems.AddSignal(new AESignalItem(si, s, TDB));
                            }
                        }
                    }

                }
            }

#if SHOW_STOPWATCH
            ts = stopWatch.Elapsed;

            // Format and display the TimeSpan value. 
            elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:000}",
                ts.Hours, ts.Minutes, ts.Seconds,
                ts.Milliseconds);
            File.AppendAllText(@"C:\temp\stopwatch.txt", "Signals: " + elapsedTime + "\n");
            stopWatch.Stop();
#endif
#endregion
        }

        public void AddSegments(TrackNode node)
        {
            List<KeyValuePair<int, int>> listTrItems = DistributeTrItem(node);
            TrVectorSection[] items = node.TrVectorNode.TrVectorSections;
            for (int idx = 0; idx < items.Length - 1; idx++)
            {
                //Program.actEditor.DisplayStatusMessage("Init data for display...  Load Vector Nodes: " + node.Index + "." + idx);
                TrVectorSection item1 = node.TrVectorNode.TrVectorSections[idx];
                TrVectorSection item2 = node.TrVectorNode.TrVectorSections[idx + 1];

                AESegment aeSegment = new AESegment(item1, item2);
                TrackSegment lineSeg = new TrackSegment(aeSegment, node, idx, item1.Flag2, TSectionDat);
                var values = listTrItems.GroupBy(x => x.Key == idx);
                foreach (var idxTrItem in values)
                {
                    if (!idxTrItem.Key)
                            break;
                    foreach (var val in idxTrItem)
                    {
                        var item = TDB.TrackDB.TrItemTable[val.Value];
                        AddTrItem(lineSeg, item);
                    }
                }
                //Program.actEditor.DisplayStatusMessage("Init data for display...  Load Vector Nodes: " + node.Index + "." + idx + ".");
                MstsItems.AddSegment(lineSeg);

                //lineSeg = new TrackSegment(node, i, TSectionDat);
                areaRoute.ManageTiles(item1.TileX, item1.TileZ);
            }
            areaRoute.ManageTiles(items[items.Length - 1].TileX, items[items.Length - 1].TileZ);
        }

        protected void AddTrItems(TrackSegment lineSeg, TrackNode currNode)
        {
            if (currNode != null && currNode.TrVectorNode != null && currNode.TrVectorNode.TrItemRefs != null)
            {
                for (int cnt = 0; cnt < currNode.TrVectorNode.TrItemRefs.Length; cnt++)
                {
                    var item = TDB.TrackDB.TrItemTable[currNode.TrVectorNode.TrItemRefs[cnt]];

                    AddTrItem(lineSeg, item);
                }
            }
        }

        protected void AddTrItem(TrackSegment lineSeg, TrItem item)
        {
            AETraveller travel = new AETraveller(Traveller);
            if (item.ItemType == TrItem.trItemType.trSIDING || item.ItemType == TrItem.trItemType.trPLATFORM)
            {
                SideItem siding = MstsItems.AddSiding(lineSeg, item, travel);
                orRouteConfig.AddItem((GlobalItem)siding);
            }
            else if (item.ItemType == TrItem.trItemType.trCROSSOVER)
            {
                AECrossOver crossOver = MstsItems.AddCrossOver(lineSeg, item, travel);
                orRouteConfig.AddItem((GlobalItem)crossOver);
            }
        }

        protected List<KeyValuePair<int, int>> DistributeTrItem(TrackNode node)
        {
            List<KeyValuePair<int, int>> TrItemBySectionId = new List<KeyValuePair<int, int>>();
            if (node != null && node.TrVectorNode != null && node.TrVectorNode.TrItemRefs != null)
            {
                AETraveller travel = new AETraveller(Traveller);
                travel.place(node);
                for (int cnt = 0; cnt < node.TrVectorNode.TrItemRefs.Length; cnt++)
                {
                    var item = TDB.TrackDB.TrItemTable[node.TrVectorNode.TrItemRefs[cnt]];
                    travel.MoveTo(item);
                    int idxSection = travel.TrackVectorSectionIndex;
                    TrItemBySectionId.Add(new KeyValuePair<int, int>(idxSection, node.TrVectorNode.TrItemRefs[cnt]));
                }
            }
            return TrItemBySectionId;
        }

        public void ReAlignData()
        {
            Program.actEditor.DisplayStatusMessage("Re align datas...");
            List<StationAreaItem> withConnector = new List<StationAreaItem>();
            List<StationItem> stationItem = orRouteConfig.GetStationItem();
            foreach (var item in stationItem)
            {
                Program.actEditor.DisplayStatusMessage("Completing station ...");
                ((StationItem)item).Complete(orRouteConfig, MstsItems, MstsDataConfig.TileBase);
            }
            return;
#if false
		            if (withConnector.Count == 0)
                return;
            List<TrackSegment> linesSegment = mstsItems.segments;
            foreach (var lineSegment in linesSegment)
            {
                //File.AppendAllText(@"F:\temp\AE.txt", "ReAlignData: idxA: " + lineSegment.SectionIdxA + 
                //    " idxB: " + lineSegment.SectionIdxB + "\n");
                foreach (var areaPoint in withConnector)
                {
                    StationConnector stationConnector = areaPoint.getStationConnector();
                    if (stationConnector == null)
                        continue;
                    if (stationConnector.idxMaster == (uint)lineSegment.associateSectionIdx)
                    {
                        areaPoint.DefineAsInterface(lineSegment);
                        withConnector.Remove(areaPoint);
                        break;
                    }
                }
                if (withConnector.Count <= 0)
                    break;
            }
  
#endif        
        }

        public void SaveRoute()
        {
            orRouteConfig.SaveConfig();
        }

        public string GetRoutePath()
        {
            return orRouteConfig.RoutePath;
        }

        public ORRouteConfig GetOrRouteConfig()
        {
            return orRouteConfig;
        }

        [CallOnThread("Updater")]
        void UpdaterThread()    // float elapsedClockSeconds
        {
        }

        public void Stop()
        {
        }
    }
}
