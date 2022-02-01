using Petaframework;
using PetaframeworkStd.BPMN;
using PetaframeworkStd.Exceptions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace PetaframeworkStd.Commons
{

    /// <summary>
    /// File generator of product www.bpmn.io 
    /// </summary>
    public class BPMN_IO : IBPMN
    {
        public Definitions CurrObject { get; set; } = new Definitions();

        private string _generatedXML { get; set; }
        private BusinessProcess _generatedBusinessProcess { get; set; }

        public string MergeTasksNames(String xml, BusinessProcess businessProcess)
        {
            bool isBase64 = false;
            if (Tools.IsBase64(xml))
            {
                xml = Petaframework.Tools.DecodeBase64(xml);
                isBase64 = true;
            }

            Definitions result;
            XmlSerializer serializer = new XmlSerializer(typeof(Definitions));
            using (TextReader reader = new StringReader(xml.ToString()))
            {
                result = (Definitions)serializer.Deserialize(reader);
            }
            result.Collaboration.Participant.Name = businessProcess.Name;
            foreach (var item in businessProcess.Tasks)
            {
                var p = item.Profiles.FirstOrDefault();
                if (p != null)
                    result.Process.LaneSet.Lane.Where(x => x.FlowNodeRef.Contains(item.ID)).FirstOrDefault().Name = p.Name;
                var t = result.Process.GetTasks().Where(x => item.ID.Equals(x.Id)).FirstOrDefault();
                if (t != null)
                {
                    t.Name = item.Name;
                }
            }
            if (isBase64)
                return Petaframework.Tools.EncodeBase64(SerializeToXML(result));
            else
                return SerializeToXML(result);

        }

        public BusinessProcess FromVendorFormat(String xml)
        {
            if (Tools.IsBase64(xml))
                xml = Petaframework.Tools.DecodeBase64(xml);

            Definitions result;
            XmlSerializer serializer = new XmlSerializer(typeof(Definitions));
            using (TextReader reader = new StringReader(xml.ToString()))
            {
                result = (Definitions)serializer.Deserialize(reader);
            }
            XDocument xDoc = XDocument.Parse(xml);
            BusinessProcess bp = new BusinessProcess();
            bp.ID = result.Id;
            bp.Name = result.Collaboration.Participant.Name;

            var allTasks = result.Process.GetTasks().Select(x => new ProcessTask { ID = x.Id, Name = x.Name });

            bp.Tasks = result.Process.GetTasks().Select(x => GetProcessTask(x, result, allTasks, ref xDoc)).ToList();

            foreach (var item in bp.Tasks.Where(x => x.To.Count() > 1))
            {
                var seqIn = result.Process.SequenceFlow.Where(x => x.SourceRef.Equals(item.ID)).FirstOrDefault();
                foreach (var o in item.To)
                {
                    if (seqIn != null)
                    {
                        var seqOut = result.Process.SequenceFlow.Where(x => !String.IsNullOrWhiteSpace(x.TargetRef) && x.TargetRef.Equals(o.ID) && x.SourceRef.Equals(seqIn.TargetRef)).FirstOrDefault();
                        if (seqOut != null && bp.Routes.Where(x => x.From.ID.Equals(item.ID) && (x.To == null || x.To.ID.Equals(o.ID))).Count() == 0 &&
                           !bp.Routes.Where(x => !string.IsNullOrWhiteSpace(x.Formula) && x.Formula.Equals(seqOut.Name) && x.From.ID.Equals(item.ID) && x.To.ID.Equals(o.ID)).Any())
                            bp.Routes.Add(new Route { Formula = seqOut.Name, From = item, To = o });
                        else
                        {
                            foreach (var obj in result.Process.SequenceFlow.Where(x => x.SourceRef.Equals(seqIn.TargetRef) && !x.TargetRef.Equals(o.ID)))
                            {
                                var t = bp.Tasks.Where(x => x.ID.Equals(obj.TargetRef)).FirstOrDefault();
                                if (t != null)
                                {
                                    if (!bp.Routes.Where(x => x.Formula != null && x.Formula.Equals(obj.Name) && x.From.ID.Equals(item.ID) && x.To.ID.Equals(t.ID)).Any())
                                        bp.Routes.Add(new Route { Formula = obj.Name, From = item, To = t });
                                }
                                else
                                {
                                    var lstEnd = result.Process.EndEvent.Where(x => x.Id.Equals(obj.TargetRef));
                                    if (lstEnd.Any())
                                    {
                                        var formula = result.Process.SequenceFlow.Where(x => x.SourceRef.Equals(seqIn.TargetRef) && x.TargetRef.Equals(lstEnd.FirstOrDefault().Id)).FirstOrDefault().Name;
                                        if (!bp.Routes.Where(x => !string.IsNullOrWhiteSpace(x.Formula) && x.Formula.Equals(obj.Name, StringComparison.Ordinal) && x.From.ID.Equals(item.ID)).Any())
                                            bp.Routes.Add(new Route { Formula = obj.Name, From = item });
                                    }
                                }
                            }
                        }
                    }
                }
            }
            _generatedXML = xml;
            _generatedBusinessProcess = bp;
            return bp;
        }

        private ProcessTask GetProcessTask(Task x, Definitions result, IEnumerable<ProcessTask> allTasks, ref XDocument xDoc)
        {
            ProcessTask t = new ProcessTask();

            foreach (var item in result.Process.LaneSet.Lane)
            {
                if (!string.IsNullOrWhiteSpace(item.Name) && !t.Profiles.Where(y => y.ID.Equals(item.Id)).Any() && item.FlowNodeRef.Contains(x.Id))
                    t.Profiles.Add(new Profile { ID = item.Id, Name = item.Name });
            }
            t.Name = x.Name;
            t.ID = x.Id;
            t.Type = x.GetType().Name;


            FindAfterElement(x, ref t, ref allTasks, ref result, ref xDoc);
            FindBeforeElement(x, ref t, ref allTasks, ref result, ref xDoc);

            return t;
        }

        private void FindBeforeElement(Task x, ref ProcessTask t, ref IEnumerable<ProcessTask> allTasks, ref Definitions result, ref XDocument xDoc)
        {
            foreach (var item in x.Incoming)
            {
                var found = false;
                foreach (var t2 in allTasks.Where(y => y.ID.Equals(item)))
                {
                    t.From.Add(t2);
                    found = true;
                    break;
                }
                if (!found)
                {
                    foreach (var t2 in result.Process.SequenceFlow.Where(s => s.Id.Equals(item)))
                    {
                        FindBeforeTask(ref t, t2, item, ref allTasks, ref result, ref xDoc);
                    }
                }
            }
        }

        private void FindAfterElement(Task x, ref ProcessTask t, ref IEnumerable<ProcessTask> allTasks, ref Definitions result, ref XDocument xDoc)
        {
            foreach (var item in x.Outgoing)
            {
                var found = false;
                foreach (var t2 in allTasks.Where(y => y.ID.Equals(item)))
                {
                    t.To.Add(t2);
                    found = true;
                    break;
                }
                if (!found)
                {
                    foreach (var t2 in result.Process.SequenceFlow.Where(s => s.Id.Equals(item)))
                    {
                        FindAfterTask(ref t, t2, item, ref allTasks, ref result, ref xDoc);
                    }
                }
            }
        }

        private void FindBeforeTask(ref ProcessTask t, SequenceFlow seqFlow, string itemToFind, ref IEnumerable<ProcessTask> allTasks, ref Definitions result, ref XDocument xDoc)
        {
            var currID = t.ID;
            var task = allTasks.Where(tk => tk.ID.Equals(itemToFind) && itemToFind != currID).FirstOrDefault();
            if (task != null)
            {
                t.From.Add(task);
                return;
            }
            var sourceSeq = result.Process.SequenceFlow.Where(s => s.Id.Equals(itemToFind)).FirstOrDefault();//Buscar no XML o elemento com ID = itemToFind
            if (sourceSeq != null && !String.IsNullOrWhiteSpace(sourceSeq.SourceRef))
            {
                task = allTasks.Where(tk => tk.ID.Equals(sourceSeq.SourceRef)).FirstOrDefault();
                if (task != null)
                {
                    t.From.Add(task);
                }
                else
                {
                    itemToFind = sourceSeq.SourceRef;
                    var xElement = (from e in xDoc.Root.DescendantNodes().OfType<XElement>()
                                    where
                                    (e.Attribute("id") != null && e.Attribute("id").Value == itemToFind) ||
                                    (e.Attribute("Id") != null && e.Attribute("Id").Value == itemToFind) ||
                                    (e.Attribute("ID") != null && e.Attribute("ID").Value == itemToFind)
                                    select e
                     ).First();
                    try
                    {
                        var nodes = xElement.Nodes();//Get all nodes under 'File'

                        var element = new DefaultElement
                        {
                            Id = itemToFind,
                            Incoming = nodes.Where(el => ((XElement)el).Name.LocalName == "incoming").Select(el => (el as XElement).Value).ToList(),
                            Outgoing = nodes.Where(el => ((XElement)el).Name.LocalName == "outgoing").Select(el => (el as XElement).Value).ToList()
                        };
                        if (element != null)
                        {
                            foreach (var item in element.Incoming)
                            {
                                var found = false;
                                foreach (var t2 in allTasks.Where(x => x.ID.Equals(item)))
                                {
                                    t.From.Add(t2);
                                    found = true;
                                    break;
                                }
                                if (!found)
                                {
                                    foreach (var t2 in result.Process.SequenceFlow.Where(s => s.Id.Equals(item)))
                                    {
                                        FindBeforeTask(ref t, t2, item, ref allTasks, ref result, ref xDoc);
                                    }
                                }
                            }
                        }

                    }
                    catch (Exception ex)
                    {

                    }
                }
            }
        }

        private void FindAfterTask(ref ProcessTask t, SequenceFlow seqFlow, string itemToFind, ref IEnumerable<ProcessTask> allTasks, ref Definitions result, ref XDocument xDoc)
        {
            var currID = t.ID;
            var task = allTasks.Where(tk => tk.ID.Equals(itemToFind) && itemToFind != currID).FirstOrDefault();
            if (task != null)
            {
                t.To.Add(task);
                return;
            }
            var sourceSeq = result.Process.SequenceFlow.Where(s => s.Id.Equals(itemToFind)).FirstOrDefault();//Buscar no XML o elemento com ID = itemToFind
            if (sourceSeq != null && !String.IsNullOrWhiteSpace(sourceSeq.SourceRef))
            {
                task = allTasks.Where(tk => tk.ID.Equals(sourceSeq.TargetRef)).FirstOrDefault();
                if (task != null)
                {
                    t.To.Add(task);
                }
                else
                {
                    itemToFind = sourceSeq.TargetRef;
                    var xElement = (from e in xDoc.Root.DescendantNodes().OfType<XElement>()
                                    where
                                    (e.Attribute("id") != null && e.Attribute("id").Value == itemToFind) ||
                                    (e.Attribute("Id") != null && e.Attribute("Id").Value == itemToFind) ||
                                    (e.Attribute("ID") != null && e.Attribute("ID").Value == itemToFind)
                                    select e
                             ).FirstOrDefault();
                    try
                    {
                        var nodes = xElement?.Nodes();//Get all nodes under 'File'

                        var element = new DefaultElement
                        {
                            Id = itemToFind,
                            Incoming = nodes?.Where(el => ((XElement)el).Name.LocalName == "incoming").Select(el => (el as XElement).Value).ToList(),
                            Outgoing = nodes?.Where(el => ((XElement)el).Name.LocalName == "outgoing").Select(el => (el as XElement).Value).ToList()
                        };
                        if (element != null)
                        {
                            foreach (var item in element.Outgoing)
                            {
                                var found = false;
                                foreach (var t2 in allTasks.Where(x => x.ID.Equals(item)))
                                {
                                    t.To.Add(t2);
                                    found = true;
                                    break;
                                }
                                if (!found)
                                {
                                    try
                                    {
                                        var bpmnEvent = (new XmlSerializer(typeof(IntermediateCatchEvent)).Deserialize(new StringReader(xElement.ToString())) as IntermediateCatchEvent);
                                        if (bpmnEvent.TimerEventDefinition != null)
                                        {
                                            TimeSpan waitingFor = TimeSpan.Zero;

                                            try
                                            {
                                                var arr = bpmnEvent.Name.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                                                if (arr.Length > 1)
                                                {
                                                    switch (arr[1].ToLower())
                                                    {
                                                        case "horas":
                                                        case "hours":
                                                            waitingFor = TimeSpan.FromHours(Convert.ToDouble(arr[0]));
                                                            break;
                                                        case "dias":
                                                        case "days":
                                                            waitingFor = TimeSpan.FromDays(Convert.ToDouble(arr[0]));
                                                            break;
                                                        case "meses":
                                                        case "months":
                                                            waitingFor = TimeSpan.FromDays(Convert.ToDouble(arr[0]) * 30);
                                                            break;
                                                    }
                                                }
                                            }
                                            catch (Exception)
                                            {

                                            }

                                            if (waitingFor != null)
                                            {
                                                t.Script = new PtfkTaskScript
                                                {
                                                    Task = t,
                                                    WaitFor = waitingFor
                                                };
                                                try
                                                {
                                                    var nextElem = result.Process.SequenceFlow.Where(s => s.Id.Equals(item))?.FirstOrDefault()?.TargetRef;
                                                    if (result.Process.EndEvent.Where(x => x.Id.Equals(nextElem)).Any())
                                                    {
                                                        t.Script.ThenEnd = true;
                                                    }
                                                }
                                                catch (Exception)
                                                {

                                                }
                                            }
                                        }
                                    }
                                    catch (Exception ex)
                                    {

                                    }
                                    foreach (var t2 in result.Process.SequenceFlow.Where(s => s.Id.Equals(item)))
                                    {
                                        FindAfterTask(ref t, t2, item, ref allTasks, ref result, ref xDoc);
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {

                    }
                }
            }
        }

        public String ToVendorFormat(BusinessProcess bprocess)
        {
            try
            {
                CurrObject.Process.Id = bprocess.ID;
                CurrObject.Collaboration.Participant = new Participant { Id = "participant_0", ProcessRef = CurrObject.Process.Id, Name = bprocess.Name };
                CurrObject.BPMNDiagram.BPMNPlane.BpmnElement = CurrObject.Collaboration.Id;
                var count = 0;
                var hasOneStartTask = bprocess.Tasks.Select(x => x.From.Count() == 0).Count() == 1;

                var allProfiles = bprocess.Tasks.SelectMany(x => x.Profiles).GroupBy(o => o.ID).Select(x => x.FirstOrDefault()).Distinct().ToList();
                CurrObject.Process.LaneSet = new LaneSet { Lane = allProfiles.Select(x => new Lane { Id = "lane_" + x.ID, Name = x.Name }).ToList() };
                foreach (var task in bprocess.Tasks)
                {
                    count++;
                    var currprofile = task.Profiles.FirstOrDefault();
                    var currentLane = CurrObject.Process.LaneSet.Lane[CurrObject.Process.LaneSet.Lane.IndexOf(CurrObject.Process.LaneSet.Lane.Where(x => x.Id.Equals("lane_" + currprofile.ID)).FirstOrDefault())];
                    var currTask = new Task
                    {
                        Id = (!String.IsNullOrWhiteSpace(task.ID) ? task.ID : "task_" + count),
                        Name = task.Name,
                        Incoming = task.From.Select(x => String.IsNullOrWhiteSpace(x.ID) ? "task_" + count : x.ID).ToList(),
                        Outgoing = task.To.Select(x => String.IsNullOrWhiteSpace(x.ID) ? "task_" + count : x.ID).ToList(),
                        Lane = currentLane,
                        ProcessTask = task
                    };
                    if (hasOneStartTask && (task.From.Count() == 0 || String.IsNullOrWhiteSpace(task.From[0].Name)))//is start task
                    {
                        foreach (var item in task.To)
                        {
                            CurrObject.Process.StartEvent.Outgoing = !String.IsNullOrWhiteSpace(item.ID) ? item.ID : "task_" + count;
                        }
                    }

                    if ((task.To.Count() == 0 || String.IsNullOrWhiteSpace(task.To[0].Name)))//is end task
                    {
                        foreach (var item in task.From)
                        {
                            CurrObject.Process.EndEvent.FirstOrDefault().Incoming.Add(!String.IsNullOrWhiteSpace(item.ID) ? item.ID : "task_" + count);
                        }
                    }
                    currentLane.FlowNodeRef.Add(currTask.Id);
                    CurrObject.Process.AddTask(currTask);

                    if (task.To.Count() > 2)//Must have complex gateway
                    {
                        var gateway = new ComplexGateway { Incoming = new List<string> { task.ID }, Id = "gate_" + task.ID, Outgoing = task.To.Select(t1 => t1.ID).ToList(), TaskParent = currTask };
                        CurrObject.Process.Gateways.Add(gateway);
                        CurrObject.Process.SequenceFlow.Add(new SequenceFlow { Id = "seq_" + task.ID + CurrObject.Process.SequenceFlow.Count(), SourceRef = task.ID, TargetRef = gateway.Id });
                        task.To.ForEach(item => CurrObject.Process.SequenceFlow.Add(new SequenceFlow { Id = "seq_" + task.ID + CurrObject.Process.SequenceFlow.Count(), SourceRef = gateway.Id, TargetRef = item.ID }));
                    }
                    else
                        if (task.To.Count() > 1)//Must have exclusive gateway
                    {
                        var gateway = new ExclusiveGateway { Incoming = new List<string> { task.ID }, Id = "gate_" + task.ID, Outgoing = task.To.Select(t1 => t1.ID).ToList(), TaskParent = currTask };
                        CurrObject.Process.Gateways.Add(gateway);
                        CurrObject.Process.SequenceFlow.Add(new SequenceFlow { Id = "seq_" + task.ID + CurrObject.Process.SequenceFlow.Count(), SourceRef = task.ID, TargetRef = gateway.Id });
                        task.To.ForEach(item => CurrObject.Process.SequenceFlow.Add(new SequenceFlow { Id = "seq_" + task.ID + CurrObject.Process.SequenceFlow.Count(), SourceRef = gateway.Id, TargetRef = item.ID }));
                    }
                    else//Must have sequence
                    {
                        if (task.To.Any())
                            CurrObject.Process.SequenceFlow.Add(new SequenceFlow { Id = "seq_" + task.ID + CurrObject.Process.SequenceFlow.Count(), SourceRef = task.ID, TargetRef = task.To[0].ID });
                        else
                            CurrObject.Process.SequenceFlow.Add(new SequenceFlow { Id = "seq_" + task.ID + CurrObject.Process.SequenceFlow.Count(), SourceRef = task.ID, TargetRef = CurrObject.Process.EndEvent.FirstOrDefault().Id });
                    }
                }

                CurrObject.Process.SequenceFlow.Add(new SequenceFlow { Id = "seq_" + CurrObject.Process.SequenceFlow.Count(), SourceRef = CurrObject.Process.StartEvent.Id, TargetRef = bprocess.Tasks.Where(x => x.From.Count() == 0).FirstOrDefault() != null ? bprocess.Tasks.Where(x => x.From.Count() == 0).FirstOrDefault().ID : "" });


                const int X_CANVAS = 156;
                const int Y_CANVAS = 81;
                const int TASK_WIDTH = 100;
                const int TASK_HEIGHT = 80;
                const int LANE_IDENT = 30;
                const int EVENT_WIDTH = 36;
                const int GATEWAY_WIDTH = 50;
                var countProfiles = allProfiles.Count();

                var arrLanes = new List<KeyValuePair<String, Bounds>>();

                foreach (var lane in CurrObject.Process.LaneSet.Lane)
                {
                    lane.Width = lane.FlowNodeRef.Count() * (TASK_WIDTH * 2);
                }
                int poolHeight = 152 * countProfiles;
                int poolWidth = CurrObject.Process.LaneSet.Lane.Sum(x => x.Width);


                //DIAGRAM

                //Add Pool
                CurrObject.BPMNDiagram.BPMNPlane.BPMNShape.Add(new BPMNShape { Id = CurrObject.Collaboration.Participant.Id + "_di", BpmnElement = CurrObject.Collaboration.Participant.Id, IsHorizontal = true, Bounds = new Bounds { X = X_CANVAS, Y = Y_CANVAS, Height = poolHeight, Width = poolWidth + LANE_IDENT } });

                //Add Lanes
                count = 0;
                foreach (var lane in CurrObject.Process.LaneSet.Lane)
                {
                    var shp = new BPMNShape { Id = lane.Id + "_di", BpmnElement = lane.Id, IsHorizontal = true, Bounds = new Bounds { X = X_CANVAS + LANE_IDENT, Y = Y_CANVAS + count * (poolHeight / countProfiles), Width = poolWidth, Height = (poolHeight / countProfiles) } };
                    CurrObject.BPMNDiagram.BPMNPlane.BPMNShape.Add(shp);
                    arrLanes.Add(new KeyValuePair<string, Bounds>(lane.Id, new Bounds { Height = shp.Bounds.Height, Width = shp.Bounds.Width, X = shp.Bounds.X, Y = shp.Bounds.Y }));
                    count++;
                }

                //Add Tasks
                arrLanes.ForEach(x => x.Value.Width = X_CANVAS + TASK_WIDTH);
                var firstLane = arrLanes.FirstOrDefault().Value;
                if (firstLane != null)
                {
                    //Add Start
                    CurrObject.BPMNDiagram.BPMNPlane.BPMNShape.Add(new BPMNShape { Id = CurrObject.Process.StartEvent.Id + "_di", BpmnElement = CurrObject.Process.StartEvent.Id, IsHorizontal = true, Bounds = new Bounds { X = TASK_WIDTH + firstLane.Width / 2, Y = (firstLane.Y + poolHeight / allProfiles.Count() / 2) - (EVENT_WIDTH / 2) - 1, Width = EVENT_WIDTH, Height = EVENT_WIDTH } });
                    foreach (var task in CurrObject.Process.GetTasks())
                    {
                        var currLane = arrLanes.Where(x => x.Key.Equals(task.Lane.Id)).FirstOrDefault();
                        var shp = new BPMNShape
                        {
                            Id = task.Id + "_di",
                            BpmnElement = task.Id,
                            IsHorizontal = true,
                            Bounds = new Bounds { X = TASK_WIDTH + currLane.Value.Width, Y = currLane.Value.Y - 3 + poolHeight / allProfiles.Count() / 4, Width = TASK_WIDTH, Height = TASK_HEIGHT },
                        };
                        if (!string.IsNullOrWhiteSpace(task.ProcessTask.FillColor))
                            shp.Fill = task.ProcessTask.FillColor;
                        if (!string.IsNullOrWhiteSpace(task.ProcessTask.StrokeColor))
                            shp.Stroke = task.ProcessTask.StrokeColor;

                        if (task.ProcessTask.EndedFlag)
                        {
                            foreach (var end in CurrObject.Process.EndEvent)
                            {
                                foreach (var inc in end.Incoming)
                                {
                                    var found = task.Outgoing.Contains(inc) || task.Id.Equals(inc);
                                    if (found)
                                        CurrObject.BPMNDiagram.BPMNPlane.BPMNShape.Where(x => x.Id.Equals(end.Id)).FirstOrDefault().Fill = task.ProcessTask.FillColor;
                                }
                            }
                        }

                        CurrObject.BPMNDiagram.BPMNPlane.BPMNShape.Add(shp);
                        currLane.Value.Width = shp.Bounds.X + shp.Bounds.Width;
                        count++;

                        //Add gateways
                        foreach (var gate in CurrObject.Process.Gateways.Where(x => x.TaskParent.Id.Equals(task.Id)))
                        {
                            var shpGateway = new BPMNShape { Id = gate.Id + "_di", BpmnElement = gate.Id, IsHorizontal = true, Bounds = new Bounds { X = shp.Bounds.X + TASK_WIDTH + (TASK_WIDTH / 4), Y = shp.Bounds.Y + TASK_HEIGHT / 4 - 4, Width = GATEWAY_WIDTH, Height = GATEWAY_WIDTH } };
                            CurrObject.BPMNDiagram.BPMNPlane.BPMNShape.Add(shpGateway);
                        }
                    }
                }

                var lastX = 0;
                foreach (var gate in CurrObject.Process.Gateways)
                {
                    var shpGateway = CurrObject.BPMNDiagram.BPMNPlane.BPMNShape.Where(x => x.BpmnElement.Equals(gate.Id)).FirstOrDefault();
                    foreach (var item in bprocess.Tasks.Where(x => x.ID.Equals(gate.TaskParent.Id)))
                    {
                        var currX = 0;
                        var all = GetTreeAfter(item);
                        foreach (var btask in all)
                        {
                            var elem = CurrObject.BPMNDiagram.BPMNPlane.BPMNShape.Where(x => x.BpmnElement.Equals(btask.ID)).FirstOrDefault();
                            if (currX == 0)
                            {
                                elem.Bounds.X = currX + shpGateway.Bounds.X + TASK_WIDTH;
                                currX = elem.Bounds.X + 2 * TASK_WIDTH;
                            }
                            else
                            {
                                if (btask.To.Count() == 0)
                                {
                                    elem.Bounds.X = shpGateway.Bounds.X + TASK_WIDTH + 2 * TASK_WIDTH * all.Where(x => x.To.Count() == 0).Count();
                                    lastX = elem.Bounds.X + TASK_WIDTH;
                                }
                                else
                                {
                                    elem.Bounds.X = currX;
                                    currX = elem.Bounds.X + 2 * TASK_WIDTH;
                                }
                            }
                            if (currX > lastX)
                                lastX = currX;
                        }
                    }
                }

                if (arrLanes != null && arrLanes.Any())
                {
                    var last = arrLanes.Where(x => x.Value.X.Equals(arrLanes.Max(y => y.Value.X))).LastOrDefault().Value;
                    CurrObject.BPMNDiagram.BPMNPlane.BPMNShape.Add(new BPMNShape { Id = CurrObject.Process.EndEvent.FirstOrDefault().Id + "_di", BpmnElement = CurrObject.Process.EndEvent.FirstOrDefault().Id, IsHorizontal = true, Bounds = new Bounds { X = lastX > 0 ? lastX + TASK_WIDTH : (TASK_WIDTH + last.Width), Y = (last.Y + poolHeight / allProfiles.Count() / 2) - (EVENT_WIDTH / 2) - 1, Width = EVENT_WIDTH, Height = EVENT_WIDTH } });
                }
                foreach (var seq in CurrObject.Process.SequenceFlow)
                {
                    CurrObject.BPMNDiagram.BPMNPlane.BPMNEdge.Add(new BPMNEdge { BpmnElement = seq.Id, Id = seq.Id + "_di" });
                }



                CurrObject.Process.PopulateGateways();
                var xml = SerializeToXML(CurrObject);


                _generatedXML = xml;
                return _generatedXML;

            }
            catch (Exception ex)
            {
                return string.Empty;
            }
        }

        private string SerializeToXML(Definitions currObject)
        {
            XmlSerializer xsSubmit = new XmlSerializer(typeof(Definitions));
            string xml = "";
            using (var sww = new StringWriter())
            {
                using (XmlWriter w = XmlWriter.Create(sww, new XmlWriterSettings() { Encoding = Encoding.UTF8 }))
                {
                    xsSubmit.Serialize(w, currObject);
                    xml = sww.ToString(); // Your XML

                }
            }
            System.Xml.Linq.XDocument xDoc = System.Xml.Linq.XDocument.Parse(xml);
            xDoc.Declaration.Encoding = "utf-8";

            return xDoc.ToString(SaveOptions.DisableFormatting);
        }

        private List<ProcessTask> GetTreeAfter(ProcessTask item)
        {
            var lst = new List<ProcessTask>();
            //lst.Add(item);
            foreach (var task in item.To)
            {
                GetNextAfter(task, ref lst);
            }
            return lst;
        }

        private void GetNextAfter(ProcessTask item, ref List<ProcessTask> lst)
        {
            if (lst.Where(x => x.ID.Equals(item.ID)).Count() == 0)
            {
                lst.Add(item);
                foreach (var task in item.To)
                {
                    GetNextAfter(task, ref lst);
                }
            }
        }

        public string FillColor(string hexFillColor, string hexStrokeColor, ProcessTask[] tasksToFill)
        {
            var fromXml = !string.IsNullOrWhiteSpace(_generatedXML);
            if (_generatedBusinessProcess == null && fromXml)
                FromVendorFormat(_generatedXML);
            if (_generatedBusinessProcess == null)
                throw new MissingParametersExpection("Task color fill parameters are missing!");

            if (!fromXml)
            {
                foreach (var taskToFill in tasksToFill)
                    _generatedBusinessProcess.Tasks.Where(x => x.ID.Equals(taskToFill.ID)).FirstOrDefault().SetColors(hexFillColor, hexStrokeColor);
                return ToVendorFormat(_generatedBusinessProcess);
            }

            XmlDocument xml = new XmlDocument();
            xml.LoadXml(_generatedXML);

            foreach (XmlNode node in xml.GetElementsByTagName("sequenceFlow"))
            {
                if (node.Attributes != null && node.Attributes["name"] != null)
                {
                    node.Attributes["name"].Value = "";
                }
            }

            List<String> endList = new List<string>();
            foreach (XmlNode node in xml.GetElementsByTagName("BPMNShape"))
            {
                foreach (var taskToFill in tasksToFill)
                {
                    if (node.Attributes != null && node.Attributes["bpmnElement"] != null && node.Attributes["bpmnElement"].Value == taskToFill.ID)
                    {
                        XmlAttribute fillAttr = xml.CreateAttribute("fill");
                        fillAttr.Value = hexFillColor;
                        node.Attributes.SetNamedItem(fillAttr);

                        XmlAttribute strokeAttr = xml.CreateAttribute("stroke");
                        strokeAttr.Value = hexStrokeColor;
                        node.Attributes.SetNamedItem(strokeAttr);

                        if (!taskToFill.EndedFlag)
                            break;

                        Definitions result;
                        XmlSerializer serializer = new XmlSerializer(typeof(Definitions));
                        using (TextReader reader = new StringReader(_generatedXML))
                        {
                            result = (Definitions)serializer.Deserialize(reader);
                            var task = result.Process.GetTasks().Where(x => x.Id.Equals(taskToFill.ID)).FirstOrDefault();
                            var allGates = result.Process.GetGateways();
                            List<Gateway> gateways = new List<Gateway>();
                            foreach (var g in allGates)
                            {
                                foreach (var i in g.Incoming)
                                {
                                    if (task.Outgoing.Contains(i))
                                    {
                                        gateways.Add(g);
                                        break;
                                    }
                                }
                            }
                            foreach (var end in result.Process.EndEvent)
                            {
                                foreach (var inc in end.Incoming)
                                {
                                    var found = task.Outgoing.Contains(inc) || gateways.Where(x => x.Outgoing.Contains(inc)).Any();
                                    if (found)
                                    {
                                        //var endID = result.BPMNDiagram.BPMNPlane.BPMNShape.Where(x => x.BpmnElement.Equals(end.Id)).FirstOrDefault().Id;
                                        foreach (XmlNode n2 in xml.GetElementsByTagName("BPMNShape"))
                                        {
                                            if (n2.Attributes != null && n2.Attributes["bpmnElement"] != null && n2.Attributes["bpmnElement"].Value == end.Id)
                                            {
                                                XmlAttribute f1 = xml.CreateAttribute("fill");
                                                f1.Value = hexFillColor;
                                                n2.Attributes.SetNamedItem(f1);

                                                break;
                                            }
                                        }
                                        break;
                                    }
                                }
                            }
                        }


                        break;
                    }
                }
            }
            return xml.OuterXml;

            //return _generatedXML;
        }
    }

    public interface IElement
    {
        [XmlElement(ElementName = "incoming", Namespace = "http://www.omg.org/spec/BPMN/20100524/MODEL")]
        List<string> Incoming { get; set; }
        [XmlElement(ElementName = "outgoing", Namespace = "http://www.omg.org/spec/BPMN/20100524/MODEL")]
        List<string> Outgoing { get; set; }
        [XmlAttribute(AttributeName = "id")]
        string Id { get; set; }
    }

    public class DefaultElement : IElement
    {
        public List<string> Incoming { get; set; }
        public List<string> Outgoing { get; set; }
        public string Id { get; set; }
    }

    [XmlRoot(ElementName = "participant", Namespace = "http://www.omg.org/spec/BPMN/20100524/MODEL")]
    public class Participant
    {
        [XmlAttribute(AttributeName = "id")]
        public string Id { get; set; }
        [XmlAttribute(AttributeName = "name")]
        public string Name { get; set; }
        [XmlAttribute(AttributeName = "processRef")]
        public string ProcessRef { get; set; }
    }

    [XmlRoot(ElementName = "collaboration", Namespace = "http://www.omg.org/spec/BPMN/20100524/MODEL")]
    public class Collaboration
    {
        [XmlElement(ElementName = "participant", Namespace = "http://www.omg.org/spec/BPMN/20100524/MODEL")]
        public Participant Participant { get; set; }
        [XmlAttribute(AttributeName = "id")]
        public string Id { get; set; }
    }

    [XmlRoot(ElementName = "lane", Namespace = "http://www.omg.org/spec/BPMN/20100524/MODEL")]
    public class Lane
    {
        [XmlElement(ElementName = "flowNodeRef", Namespace = "http://www.omg.org/spec/BPMN/20100524/MODEL")]
        public List<string> FlowNodeRef { get; set; } = new List<string>();
        [XmlAttribute(AttributeName = "id")]
        public string Id { get; set; }
        [XmlAttribute(AttributeName = "name")]
        public string Name { get; set; }
        [XmlIgnore]
        public int Width { get; set; }
    }

    [XmlRoot(ElementName = "laneSet", Namespace = "http://www.omg.org/spec/BPMN/20100524/MODEL")]
    public class LaneSet
    {
        [XmlElement(ElementName = "lane", Namespace = "http://www.omg.org/spec/BPMN/20100524/MODEL")]
        public List<Lane> Lane { get; set; }
        [XmlAttribute(AttributeName = "id")]
        public string Id { get; set; }
        [XmlIgnore]
        public int Width { get; set; }
    }

    [XmlRoot(ElementName = "startEvent", Namespace = "http://www.omg.org/spec/BPMN/20100524/MODEL")]
    public class StartEvent
    {
        [XmlElement(ElementName = "outgoing", Namespace = "http://www.omg.org/spec/BPMN/20100524/MODEL")]
        public string Outgoing { get; set; }
        [XmlAttribute(AttributeName = "id")]
        public string Id { get; set; }
        [XmlAttribute(AttributeName = "name")]
        public string Name { get; set; }
    }

    [XmlRoot(ElementName = "task", Namespace = "http://www.omg.org/spec/BPMN/20100524/MODEL")]
    public class Task
    {
        [XmlElement(ElementName = "incoming", Namespace = "http://www.omg.org/spec/BPMN/20100524/MODEL")]
        public List<string> Incoming { get; set; }
        [XmlElement(ElementName = "outgoing", Namespace = "http://www.omg.org/spec/BPMN/20100524/MODEL")]
        public List<string> Outgoing { get; set; }
        [XmlAttribute(AttributeName = "id")]
        public string Id { get; set; }
        [XmlAttribute(AttributeName = "name")]
        public string Name { get; set; }
        [XmlIgnore]
        public Lane Lane { get; set; }
        [XmlIgnore]
        public ProcessTask ProcessTask { get; set; }

        [XmlElement(ElementName = "multiInstanceLoopCharacteristics", Namespace = "http://www.omg.org/spec/BPMN/20100524/MODEL")]
        public MultiInstanceLoopCharacteristics MultiInstanceLoopCharacteristics { get; set; }
        [XmlElement(ElementName = "standardLoopCharacteristics", Namespace = "http://www.omg.org/spec/BPMN/20100524/MODEL")]
        public string StandardLoopCharacteristics { get; set; }

    }

    [XmlRoot(ElementName = "multiInstanceLoopCharacteristics", Namespace = "http://www.omg.org/spec/BPMN/20100524/MODEL")]
    public class MultiInstanceLoopCharacteristics
    {
        [XmlAttribute(AttributeName = "isSequential")]
        public string IsSequential { get; set; }
    }

    [XmlRoot(ElementName = "endEvent", Namespace = "http://www.omg.org/spec/BPMN/20100524/MODEL")]
    public class EndEvent
    {
        [XmlElement(ElementName = "incoming", Namespace = "http://www.omg.org/spec/BPMN/20100524/MODEL")]
        public List<string> Incoming { get; set; }
        [XmlAttribute(AttributeName = "id")]
        public string Id { get; set; }
        [XmlAttribute(AttributeName = "name")]
        public string Name { get; set; }

        [XmlElement(ElementName = "messageEventDefinition", Namespace = "http://www.omg.org/spec/BPMN/20100524/MODEL")]
        public string MessageEventDefinition { get; set; }

        [XmlElement(ElementName = "escalationEventDefinition", Namespace = "http://www.omg.org/spec/BPMN/20100524/MODEL")]
        public string EscalationEventDefinition { get; set; }
        [XmlElement(ElementName = "errorEventDefinition", Namespace = "http://www.omg.org/spec/BPMN/20100524/MODEL")]
        public string ErrorEventDefinition { get; set; }
        [XmlElement(ElementName = "compensateEventDefinition", Namespace = "http://www.omg.org/spec/BPMN/20100524/MODEL")]
        public string CompensateEventDefinition { get; set; }
        [XmlElement(ElementName = "signalEventDefinition", Namespace = "http://www.omg.org/spec/BPMN/20100524/MODEL")]
        public string SignalEventDefinition { get; set; }
        [XmlElement(ElementName = "terminateEventDefinition", Namespace = "http://www.omg.org/spec/BPMN/20100524/MODEL")]
        public string TerminateEventDefinition { get; set; }
    }
    [XmlInclude(typeof(ExclusiveGateway))]
    [XmlRoot(ElementName = "exclusiveGateway", Namespace = "http://www.omg.org/spec/BPMN/20100524/MODEL")]
    public class ExclusiveGateway : Gateway
    {

    }


    [XmlInclude(typeof(ComplexGateway))]
    [XmlRoot(ElementName = "complexGateway", Namespace = "http://www.omg.org/spec/BPMN/20100524/MODEL")]
    public class ComplexGateway : Gateway
    {

    }

    [XmlInclude(typeof(ParallelGateway))]
    [XmlRoot(ElementName = "parallelGateway", Namespace = "http://www.omg.org/spec/BPMN/20100524/MODEL")]
    public class ParallelGateway : Gateway
    {

    }


    [XmlInclude(typeof(InclusiveGateway))]
    [XmlRoot(ElementName = "inclusiveGateway", Namespace = "http://www.omg.org/spec/BPMN/20100524/MODEL")]
    public class InclusiveGateway : Gateway
    {

    }

    [XmlInclude(typeof(EventBasedGateway))]
    [XmlRoot(ElementName = "eventBasedGateway", Namespace = "http://www.omg.org/spec/BPMN/20100524/MODEL")]
    public class EventBasedGateway : Gateway
    {

    }

    [Serializable]
    public abstract class Gateway
    {
        [XmlElement(ElementName = "incoming", Namespace = "http://www.omg.org/spec/BPMN/20100524/MODEL")]
        public List<string> Incoming { get; set; }
        [XmlElement(ElementName = "outgoing", Namespace = "http://www.omg.org/spec/BPMN/20100524/MODEL")]
        public List<string> Outgoing { get; set; }
        [XmlAttribute(AttributeName = "id")]
        public string Id { get; set; }
        [XmlAttribute(AttributeName = "name")]
        public string Name { get; set; }

        [XmlIgnore]
        public Task TaskParent { get; set; }
    }

    [XmlRoot(ElementName = "sequenceFlow", Namespace = "http://www.omg.org/spec/BPMN/20100524/MODEL")]
    public class SequenceFlow
    {
        [XmlAttribute(AttributeName = "id")]
        public string Id { get; set; }
        [XmlAttribute(AttributeName = "sourceRef")]
        public string SourceRef { get; set; }
        [XmlAttribute(AttributeName = "targetRef")]
        public string TargetRef { get; set; }
        [XmlAttribute(AttributeName = "name")]
        public string Name { get; set; }

        [XmlElement(ElementName = "conditionExpression", Namespace = "http://www.omg.org/spec/BPMN/20100524/MODEL")]
        public ConditionExpression ConditionExpression { get; set; }
    }

    [XmlRoot(ElementName = "conditionExpression", Namespace = "http://www.omg.org/spec/BPMN/20100524/MODEL")]
    public class ConditionExpression
    {
        [XmlAttribute(AttributeName = "type", Namespace = "http://www.w3.org/2001/XMLSchema-instance")]
        public string Type { get; set; }
    }

    [XmlRoot(ElementName = "intermediateCatchEvent", Namespace = "http://www.omg.org/spec/BPMN/20100524/MODEL")]
    public class IntermediateCatchEvent
    {
        [XmlElement(ElementName = "incoming", Namespace = "http://www.omg.org/spec/BPMN/20100524/MODEL")]
        public string Incoming { get; set; }
        [XmlElement(ElementName = "outgoing", Namespace = "http://www.omg.org/spec/BPMN/20100524/MODEL")]
        public List<string> Outgoing { get; set; }
        [XmlElement(ElementName = "timerEventDefinition", Namespace = "http://www.omg.org/spec/BPMN/20100524/MODEL")]
        public string TimerEventDefinition { get; set; }
        [XmlAttribute(AttributeName = "id")]
        public string Id { get; set; }
        [XmlAttribute(AttributeName = "name")]
        public string Name { get; set; }
        [XmlElement(ElementName = "messageEventDefinition", Namespace = "http://www.omg.org/spec/BPMN/20100524/MODEL")]
        public string MessageEventDefinition { get; set; }
        [XmlElement(ElementName = "conditionalEventDefinition", Namespace = "http://www.omg.org/spec/BPMN/20100524/MODEL")]
        public ConditionalEventDefinition ConditionalEventDefinition { get; set; }
        [XmlElement(ElementName = "linkEventDefinition", Namespace = "http://www.omg.org/spec/BPMN/20100524/MODEL")]
        public string LinkEventDefinition { get; set; }
        [XmlElement(ElementName = "signalEventDefinition", Namespace = "http://www.omg.org/spec/BPMN/20100524/MODEL")]
        public string SignalEventDefinition { get; set; }
    }


    [XmlRoot(ElementName = "intermediateThrowEvent", Namespace = "http://www.omg.org/spec/BPMN/20100524/MODEL")]
    public class IntermediateThrowEvent
    {
        [XmlElement(ElementName = "incoming", Namespace = "http://www.omg.org/spec/BPMN/20100524/MODEL")]
        public string Incoming { get; set; }
        [XmlElement(ElementName = "outgoing", Namespace = "http://www.omg.org/spec/BPMN/20100524/MODEL")]
        public List<string> Outgoing { get; set; }
        [XmlElement(ElementName = "messageEventDefinition", Namespace = "http://www.omg.org/spec/BPMN/20100524/MODEL")]
        public string MessageEventDefinition { get; set; }
        [XmlAttribute(AttributeName = "id")]
        public string Id { get; set; }
        [XmlElement(ElementName = "escalationEventDefinition", Namespace = "http://www.omg.org/spec/BPMN/20100524/MODEL")]
        public string EscalationEventDefinition { get; set; }
        [XmlElement(ElementName = "linkEventDefinition", Namespace = "http://www.omg.org/spec/BPMN/20100524/MODEL")]
        public string LinkEventDefinition { get; set; }
        [XmlElement(ElementName = "compensateEventDefinition", Namespace = "http://www.omg.org/spec/BPMN/20100524/MODEL")]
        public string CompensateEventDefinition { get; set; }
        [XmlElement(ElementName = "signalEventDefinition", Namespace = "http://www.omg.org/spec/BPMN/20100524/MODEL")]
        public string SignalEventDefinition { get; set; }
    }


    [XmlRoot(ElementName = "conditionalEventDefinition", Namespace = "http://www.omg.org/spec/BPMN/20100524/MODEL")]
    public class ConditionalEventDefinition
    {
        [XmlElement(ElementName = "condition", Namespace = "http://www.omg.org/spec/BPMN/20100524/MODEL")]
        public Condition Condition { get; set; }
    }

    [XmlRoot(ElementName = "condition", Namespace = "http://www.omg.org/spec/BPMN/20100524/MODEL")]
    public class Condition
    {
        [XmlAttribute(AttributeName = "type", Namespace = "http://www.w3.org/2001/XMLSchema-instance")]
        public string Type { get; set; }
    }


    [XmlRoot(ElementName = "sendTask", Namespace = "http://www.omg.org/spec/BPMN/20100524/MODEL")]
    public class SendTask : Task
    {

    }

    [XmlRoot(ElementName = "receiveTask", Namespace = "http://www.omg.org/spec/BPMN/20100524/MODEL")]
    public class ReceiveTask : Task
    { }

    [XmlRoot(ElementName = "userTask", Namespace = "http://www.omg.org/spec/BPMN/20100524/MODEL")]
    public class UserTask : Task
    { }

    [XmlRoot(ElementName = "manualTask", Namespace = "http://www.omg.org/spec/BPMN/20100524/MODEL")]
    public class ManualTask : Task
    { }

    [XmlRoot(ElementName = "businessRuleTask", Namespace = "http://www.omg.org/spec/BPMN/20100524/MODEL")]
    public class BusinessRuleTask : Task
    { }

    [XmlRoot(ElementName = "serviceTask", Namespace = "http://www.omg.org/spec/BPMN/20100524/MODEL")]
    public class ServiceTask : Task
    { }

    [XmlRoot(ElementName = "scriptTask", Namespace = "http://www.omg.org/spec/BPMN/20100524/MODEL")]
    public class ScriptTask : Task
    { }

    [XmlRoot(ElementName = "callActivity", Namespace = "http://www.omg.org/spec/BPMN/20100524/MODEL")]
    public class CallActivity : Task
    { }


    [XmlRoot(ElementName = "subProcess", Namespace = "http://www.omg.org/spec/BPMN/20100524/MODEL")]
    public class SubProcess : Task
    {
        [XmlAttribute(AttributeName = "default")]
        public string Default { get; set; }
    }


    [XmlRoot(ElementName = "process", Namespace = "http://www.omg.org/spec/BPMN/20100524/MODEL")]
    public class Process
    {
        [XmlElement(ElementName = "laneSet", Namespace = "http://www.omg.org/spec/BPMN/20100524/MODEL")]
        public LaneSet LaneSet { get; set; }
        [XmlElement(ElementName = "startEvent", Namespace = "http://www.omg.org/spec/BPMN/20100524/MODEL")]
        public StartEvent StartEvent { get; set; }
        [XmlElement(ElementName = "task", Namespace = "http://www.omg.org/spec/BPMN/20100524/MODEL")]
        public List<Task> Task { get; set; } = new List<Task>();
        [XmlElement(ElementName = "serviceTask", Namespace = "http://www.omg.org/spec/BPMN/20100524/MODEL")]
        public List<ServiceTask> ServiceTask { get; set; } = new List<ServiceTask>();
        [XmlElement(ElementName = "businessRuleTask", Namespace = "http://www.omg.org/spec/BPMN/20100524/MODEL")]
        public List<BusinessRuleTask> BusinessRuleTask { get; set; } = new List<BusinessRuleTask>();


        public List<Task> GetTasks()
        {
            var lst = Task
                .Union(ServiceTask)
                .Union(BusinessRuleTask)
                .Union(SendTask)
                .Union(ReceiveTask)
                .Union(UserTask)
                .Union(ManualTask)
                .Union(ScriptTask);

            return lst.ToList();
        }

        public List<Gateway> GetGateways()
        {
            var lst = (this.ComplexGateway as IEnumerable<Gateway>)
                        .Union(ParallelGateway as IEnumerable<Gateway>)
                        .Union(InclusiveGateway as IEnumerable<Gateway>)
                        .Union(EventBasedGateway as IEnumerable<Gateway>)
                        .Union(ExclusiveGateway as IEnumerable<Gateway>);

            return lst.ToList();
        }

        //[XmlElement(ElementName = "endEvent", Namespace = "http://www.omg.org/spec/BPMN/20100524/MODEL")]
        //public EndEvent EndEvent { get; set; }

        [XmlElement(ElementName = "endEvent", Namespace = "http://www.omg.org/spec/BPMN/20100524/MODEL")]
        public List<EndEvent> EndEvent { get; set; }

        [XmlElement(ElementName = "intermediateCatchEvent", Namespace = "http://www.omg.org/spec/BPMN/20100524/MODEL")]
        public List<IntermediateCatchEvent> IntermediateCatchEvent { get; set; } = new List<IntermediateCatchEvent>();
        [XmlElement(ElementName = "intermediateThrowEvent", Namespace = "http://www.omg.org/spec/BPMN/20100524/MODEL")]
        public List<IntermediateThrowEvent> IntermediateThrowEvent { get; set; } = new List<IntermediateThrowEvent>();

        [XmlIgnore]
        public List<Gateway> Gateways { get; set; }

        public void PopulateGateways()
        {
            this.ComplexGateway = Gateways.Where(x => x.GetType().Equals(typeof(ComplexGateway))).Select(x => x as ComplexGateway).ToList();
            this.ParallelGateway = Gateways.Where(x => x.GetType().Equals(typeof(ParallelGateway))).Select(x => x as ParallelGateway).ToList();
            this.InclusiveGateway = Gateways.Where(x => x.GetType().Equals(typeof(InclusiveGateway))).Select(x => x as InclusiveGateway).ToList();
            this.EventBasedGateway = Gateways.Where(x => x.GetType().Equals(typeof(EventBasedGateway))).Select(x => x as EventBasedGateway).ToList();
            this.ExclusiveGateway = Gateways.Where(x => x.GetType().Equals(typeof(ExclusiveGateway))).Select(x => x as ExclusiveGateway).ToList();
        }

        internal void AddTask(Task currTask)
        {
            if (currTask.GetType() == typeof(ServiceTask))
                ServiceTask.Add(currTask as ServiceTask);
            else
                Task.Add(currTask);
        }

        [XmlElement(ElementName = "inclusiveGateway", Namespace = "http://www.omg.org/spec/BPMN/20100524/MODEL")]
        public List<InclusiveGateway> InclusiveGateway { get; set; }
        [XmlElement(ElementName = "parallelGateway", Namespace = "http://www.omg.org/spec/BPMN/20100524/MODEL")]
        public List<ParallelGateway> ParallelGateway { get; set; }
        [XmlElement(ElementName = "complexGateway", Namespace = "http://www.omg.org/spec/BPMN/20100524/MODEL")]
        public List<ComplexGateway> ComplexGateway { get; set; }
        [XmlElement(ElementName = "eventBasedGateway", Namespace = "http://www.omg.org/spec/BPMN/20100524/MODEL")]
        public List<EventBasedGateway> EventBasedGateway { get; set; }
        [XmlElement(ElementName = "exclusiveGateway", Namespace = "http://www.omg.org/spec/BPMN/20100524/MODEL")]
        public List<ExclusiveGateway> ExclusiveGateway { get; set; }


        [XmlElement(ElementName = "sequenceFlow", Namespace = "http://www.omg.org/spec/BPMN/20100524/MODEL")]
        public List<SequenceFlow> SequenceFlow { get; set; }
        [XmlAttribute(AttributeName = "id")]
        public string Id { get; set; }




        [XmlElement(ElementName = "sendTask", Namespace = "http://www.omg.org/spec/BPMN/20100524/MODEL")]
        public List<SendTask> SendTask { get; set; } = new List<SendTask>();
        [XmlElement(ElementName = "receiveTask", Namespace = "http://www.omg.org/spec/BPMN/20100524/MODEL")]
        public List<ReceiveTask> ReceiveTask { get; set; } = new List<ReceiveTask>();
        [XmlElement(ElementName = "userTask", Namespace = "http://www.omg.org/spec/BPMN/20100524/MODEL")]
        public List<UserTask> UserTask { get; set; } = new List<UserTask>();
        [XmlElement(ElementName = "manualTask", Namespace = "http://www.omg.org/spec/BPMN/20100524/MODEL")]
        public List<ManualTask> ManualTask { get; set; } = new List<ManualTask>();
        [XmlElement(ElementName = "scriptTask", Namespace = "http://www.omg.org/spec/BPMN/20100524/MODEL")]
        public List<ScriptTask> ScriptTask { get; set; } = new List<ScriptTask>();
        [XmlElement(ElementName = "callActivity", Namespace = "http://www.omg.org/spec/BPMN/20100524/MODEL")]
        public List<CallActivity> CallActivity { get; set; } = new List<CallActivity>();
        [XmlElement(ElementName = "subProcess", Namespace = "http://www.omg.org/spec/BPMN/20100524/MODEL")]
        public List<SubProcess> SubProcess { get; set; } = new List<SubProcess>();
    }

    [XmlRoot(ElementName = "Bounds", Namespace = "http://www.omg.org/spec/DD/20100524/DC")]
    public class Bounds
    {
        [XmlAttribute(AttributeName = "x")]
        public int X { get; set; }
        [XmlAttribute(AttributeName = "y")]
        public int Y { get; set; }
        [XmlAttribute(AttributeName = "width")]
        public int Width { get; set; }
        [XmlAttribute(AttributeName = "height")]
        public int Height { get; set; }
    }

    [XmlRoot(ElementName = "BPMNShape", Namespace = "http://www.omg.org/spec/BPMN/20100524/DI")]
    public class BPMNShape
    {
        [XmlElement(ElementName = "Bounds", Namespace = "http://www.omg.org/spec/DD/20100524/DC")]
        public Bounds Bounds { get; set; }
        [XmlAttribute(AttributeName = "id")]
        public string Id { get; set; }
        [XmlAttribute(AttributeName = "bpmnElement")]
        public string BpmnElement { get; set; }
        [XmlAttribute(AttributeName = "isHorizontal")]
        public bool IsHorizontal { get; set; }
        [XmlElement(ElementName = "BPMNLabel", Namespace = "http://www.omg.org/spec/BPMN/20100524/DI")]
        public BPMNLabel BPMNLabel { get; set; }
        [XmlAttribute(AttributeName = "isMarkerVisible")]
        public string IsMarkerVisible { get; set; }

        [XmlAttribute(AttributeName = "fill")]
        //[XmlElement("fill", IsNullable = false)]
        public string Fill { get; set; }

        [XmlAttribute(AttributeName = "stroke")]
        //[XmlElement("stroke", IsNullable = false)]
        public string Stroke { get; set; }
    }

    [XmlRoot(ElementName = "BPMNLabel", Namespace = "http://www.omg.org/spec/BPMN/20100524/DI")]
    public class BPMNLabel
    {
        [XmlElement(ElementName = "Bounds", Namespace = "http://www.omg.org/spec/DD/20100524/DC")]
        public Bounds Bounds { get; set; }
    }

    [XmlRoot(ElementName = "waypoint", Namespace = "http://www.omg.org/spec/DD/20100524/DI")]
    public class Waypoint
    {
        [XmlAttribute(AttributeName = "x")]
        public int X { get; set; }
        [XmlAttribute(AttributeName = "y")]
        public int Y { get; set; }
    }

    [XmlRoot(ElementName = "BPMNEdge", Namespace = "http://www.omg.org/spec/BPMN/20100524/DI")]
    public class BPMNEdge
    {
        [XmlElement(ElementName = "waypoint", Namespace = "http://www.omg.org/spec/DD/20100524/DI")]
        public List<Waypoint> Waypoint { get; set; }
        [XmlAttribute(AttributeName = "id")]
        public string Id { get; set; }
        [XmlAttribute(AttributeName = "bpmnElement")]
        public string BpmnElement { get; set; }
        [XmlElement(ElementName = "BPMNLabel", Namespace = "http://www.omg.org/spec/BPMN/20100524/DI")]
        public BPMNLabel BPMNLabel { get; set; }
    }

    [XmlRoot(ElementName = "BPMNPlane", Namespace = "http://www.omg.org/spec/BPMN/20100524/DI")]
    public class BPMNPlane
    {
        [XmlElement(ElementName = "BPMNShape", Namespace = "http://www.omg.org/spec/BPMN/20100524/DI")]
        public List<BPMNShape> BPMNShape { get; set; }
        [XmlElement(ElementName = "BPMNEdge", Namespace = "http://www.omg.org/spec/BPMN/20100524/DI")]
        public List<BPMNEdge> BPMNEdge { get; set; }
        [XmlAttribute(AttributeName = "id")]
        public string Id { get; set; }
        [XmlAttribute(AttributeName = "bpmnElement")]
        public string BpmnElement { get; set; }
    }

    [XmlRoot(ElementName = "BPMNDiagram", Namespace = "http://www.omg.org/spec/BPMN/20100524/DI")]
    public class BPMNDiagram
    {
        [XmlElement(ElementName = "BPMNPlane", Namespace = "http://www.omg.org/spec/BPMN/20100524/DI")]
        public BPMNPlane BPMNPlane { get; set; }
        [XmlAttribute(AttributeName = "id")]
        public string Id { get; set; }
    }

    [XmlRoot(ElementName = "definitions", Namespace = "http://www.omg.org/spec/BPMN/20100524/MODEL")]
    public class Definitions
    {
        [XmlElement(ElementName = "collaboration", Namespace = "http://www.omg.org/spec/BPMN/20100524/MODEL")]
        public Collaboration Collaboration { get; set; } = new Collaboration { Id = "collab_0", Participant = new Participant { Name = "participant_0" } };
        [XmlElement(ElementName = "process", Namespace = "http://www.omg.org/spec/BPMN/20100524/MODEL")]
        public Process Process { get; set; } = new Process { StartEvent = new StartEvent { Id = "startEvt_0" }, EndEvent = new List<EndEvent> { new EndEvent { Id = "endEvt_" + int.MaxValue, Incoming = new List<string>() } }, Task = new List<Task>(), LaneSet = new LaneSet { Lane = new List<Lane>() }, Gateways = new List<Gateway>(), SequenceFlow = new List<SequenceFlow>() };
        [XmlElement(ElementName = "BPMNDiagram", Namespace = "http://www.omg.org/spec/BPMN/20100524/DI")]
        public BPMNDiagram BPMNDiagram { get; set; } = new BPMNDiagram { Id = "bpmnDiag_0", BPMNPlane = new BPMNPlane { Id = "bpmnPlane_0", BPMNShape = new List<BPMNShape>(), BPMNEdge = new List<BPMNEdge>() } };
        //[XmlAttribute(AttributeName = "xsi", Namespace = "http://www.w3.org/2000/xmlns/")]
        //public string Xsi { get; set; }
        [XmlAttribute(AttributeName = "bpmn", Namespace = "http://www.w3.org/2000/xmlns/")]
        public string Bpmn { get; set; }
        [XmlAttribute(AttributeName = "bpmndi", Namespace = "http://www.w3.org/2000/xmlns/")]
        public string Bpmndi { get; set; }
        [XmlAttribute(AttributeName = "dc", Namespace = "http://www.w3.org/2000/xmlns/")]
        public string Dc { get; set; }
        [XmlAttribute(AttributeName = "di", Namespace = "http://www.w3.org/2000/xmlns/")]
        public string Di { get; set; }
        [XmlAttribute(AttributeName = "id")]
        public string Id { get; set; }
        [XmlAttribute(AttributeName = "targetNamespace")]
        public string TargetNamespace { get; set; }
        [XmlAttribute(AttributeName = "exporter")]
        public string Exporter { get; set; }
        [XmlAttribute(AttributeName = "exporterVersion")]
        public string ExporterVersion { get; set; }
    }
}
