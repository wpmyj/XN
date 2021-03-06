﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Util;
using DataGridViewAutoFilter;
using MCP;
using OPC;
using MCP.Service.Siemens.Config;
namespace App.View
{
    public partial class frmMonitor : BaseForm
    {
        private Point InitialP1;
        private Point InitialP2;
        private Point InitialP3;
        private Point InitialP4;


        float colDis = 20.75f;
        float rowDis = 54.4f;

        // private System.Timers.Timer tmWorkTimer = new System.Timers.Timer();
        private System.Timers.Timer tmCrane1 = new System.Timers.Timer();
        BLL.BLLBase bll = new BLL.BLLBase();
        Dictionary<int, string> dicCraneFork = new Dictionary<int, string>();
        Dictionary<int, string> dicCraneStatus = new Dictionary<int, string>();
        Dictionary<int, string> dicCraneAction = new Dictionary<int, string>();

        Dictionary<int, string> dicCarFork = new Dictionary<int, string>();
        Dictionary<int, string> dicCarStatus = new Dictionary<int, string>();
        Dictionary<int, string> dicCarAction = new Dictionary<int, string>();
        Dictionary<int, Point> dicCarLocation = new Dictionary<int, Point>();
        Dictionary<int, Point> dicMiniloadLocation = new Dictionary<int, Point>();
        DataTable dtDeviceAlarm;

        public frmMonitor()
        {
            InitializeComponent();
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            //Point P2 = picCrane2.Location;
            //P2.X = P2.X - 90;

            //this.picCrane2.Location = P2;
        }

        private void frmMonitor_Load(object sender, EventArgs e)
        {

            Cranes.OnCrane += new CraneEventHandler(Monitor_OnCrane);
            
            //picCrane1.Parent = pictureBox1;
            //InitialP1 = picCrane1.Location;
            
            //picCrane1.BackColor = Color.Transparent;

            InitialP2 = picCar01.Location;
            picCar01.Parent = pictureBox1;
            picCar01.BackColor = Color.Transparent;

            InitialP3 = picCar02.Location;
            picCar02.Parent = pictureBox1;
            picCar02.BackColor = Color.Transparent;

            //InitialP4 = picCrane2.Location;
            //picCrane2.Parent = pictureBox1;
            //picCrane2.BackColor = Color.Transparent;
            AddDicKeyValue();
            try
            {
                ServerInfo[] Servers = new MonitorConfig("Monitor.xml").Servers;
                Conveyors.OnConveyor += new ConveyorEventHandler(Monitor_OnConveyor);
                Cars.OnCar += new CarEventHandler(Monitor_OnCar);
                Miniloads.OnMiniload += new MiniloadEventHandler(Monitor_OnMiniload);
                for (int i = 0; i < Servers.Length; i++)
                {
                    OPCServer opcServer = new OPCServer(Servers[i].Name);
                    opcServer.Connect(Servers[i].ProgID, Servers[i].ServerName);// opcServer.Connect(config.ConnectionString);

                    OPCGroup group = opcServer.AddGroup(Servers[i].GroupName, Servers[i].UpdateRate);
                    foreach (ItemInfo item in Servers[i].Items)
                    {
                        group.AddItem(item.ItemName, item.OpcItemName, item.ClientHandler, item.IsActive);
                    }
                    if (Servers[i].Name == "TranLineServer")
                    {
                        opcServer.Groups.DefaultGroup.OnDataChanged += new OPCGroup.DataChangedEventHandler(Conveyor_OnDataChanged);
                    }
                    if (Servers[i].Name == "Car0101Server" || Servers[i].Name == "Car0102Server")
                    {
                        opcServer.Groups.DefaultGroup.OnDataChanged += new OPCGroup.DataChangedEventHandler(Car_OnDataChanged);
                    }
                    if (Servers[i].Name == "MiniloadServer")
                    {
                        opcServer.Groups.DefaultGroup.OnDataChanged += new OPCGroup.DataChangedEventHandler(Miniload_OnDataChanged);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message);
            }

            //tmCrane1.Interval = 3000;
            //tmCrane1.Elapsed += new System.Timers.ElapsedEventHandler(tmCraneWorker1);
            //tmCrane1.Start();
        }
        //反馈给总控WCS设备状态
        //string m = "[{\"id\":\"" + id + "\",\"deviceNo\":\"" + deviceNo + "\",\"mode\":\"" + mode + "\",\"status\":\"" + status + "\",\"taskNo\":\"" + taskNo + "\",\"fork\":\"" + fork + "\",\"load\":\"" + load + "\",\"aisleNo\":\"" + aisleNo + "\",\"column\":\"" + column + "\",\"layer\":\"" + layer + "\",\"alarmCode\":\"" + alarmCode + "\",\"sendDate\":\"" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "\",\"sender\":\"" + sender + "\",\"field1\":\"\",\"field2\":\"\",\"field3\":\"\"" + "}]";
        #region Miniload监控

        private Dictionary<string, Miniload> dicMiniload = new Dictionary<string, Miniload>();

        void Miniload_OnDataChanged(object sender, DataChangedEventArgs e)
        {
            try
            {
                if (e.State == null)
                    return;
                //e.States
                string miniloadNo = "02";
                GetMiniload(miniloadNo);
                if (e.ItemName.IndexOf("Status") >= 0)
                    dicMiniload[miniloadNo].Status = e.States;
                else if (e.ItemName.IndexOf("Mode") >= 0)
                    dicMiniload[miniloadNo].Mode = bool.Parse(e.State.ToString());
                else if (e.ItemName.IndexOf("ForkStatus") >= 0)
                    dicMiniload[miniloadNo].ForkStatus = bool.Parse(e.State.ToString());
                else if (e.ItemName.IndexOf("TaskANo") >= 0)
                    dicMiniload[miniloadNo].TaskANo = e.State.ToString();
                else if (e.ItemName.IndexOf("TaskBNo") >= 0)
                    dicMiniload[miniloadNo].TaskBNo = e.State.ToString();
                else if (e.ItemName.IndexOf("AlarmCode") >= 0)
                    dicMiniload[miniloadNo].AlarmCode = int.Parse(e.State.ToString());
                Miniloads.MiniloadInfo(dicMiniload[miniloadNo]);

            }
            catch (Exception ex)
            {
                MCP.Logger.Error("Miniload监控界面中Miniload_OnDataChanged出现异常" + ex.Message);
            }
        }

        private Miniload GetMiniload(string miniloadNo)
        {
            Miniload miniload = null;
            if (dicMiniload.ContainsKey(miniloadNo))
            {
                miniload = dicMiniload[miniloadNo];
            }
            else
            {
                miniload = new Miniload();
                miniload.MiniloadNo = miniloadNo;
                dicMiniload.Add(miniloadNo, miniload);
            }
            return miniload;
        }

        void Monitor_OnMiniload(MiniloadEventArgs args)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new MiniloadEventHandler(Monitor_OnMiniload), args);
            }
            else
            {
                try
                {
                    Miniload miniload = args.miniload;

                    if (miniload.MiniloadNo == "02")
                    {
                        //this.picCrane2.Visible = true;
                        Point P2 = InitialP4;
                        if (miniload.Status == null)
                            return;
                        if (miniload.Status.Length < 3)
                            return;
                        int miniloadColumn = int.Parse(miniload.Status[1].ToString());
                        if (miniloadColumn < 37)
                        {
                            P2 = dicMiniloadLocation[miniloadColumn];
                            //this.picCrane2.Location = P2;
                        }

                        this.txtTaskNo4.Text = miniload.TaskANo;
                        this.txtTaskNo5.Text = miniload.TaskBNo;
                        this.txtStatus4.Text = dicCraneStatus[int.Parse(miniload.Status[0].ToString())];
                        this.txtActionMode4.Text = miniload.Mode ? dicCraneAction[1] : dicCraneAction[0];
                        this.txtRow4.Text = "005";
                        this.txtColumn4.Text = miniload.Status[1].ToString();
                        this.txtLayer4.Text = miniload.Status[2].ToString();
                        this.txtForkStatus4.Text = miniload.ForkStatus?dicCraneFork[0]:dicCraneFork[1];
                        this.txtAlarmCode4.Text = miniload.AlarmCode.ToString();
                        if (miniload.AlarmCode > 0)
                        {
                            DataRow[] drs = dtDeviceAlarm.Select(string.Format("Flag=1 and AlarmCode={0}", miniload.AlarmCode));
                            if (drs.Length > 0)
                                this.txtAlarmDesc4.Text = drs[0]["AlarmDesc"].ToString();
                            else
                                this.txtAlarmDesc4.Text = "设备未知错误！";
                        }
                        else
                            this.txtAlarmDesc2.Text = "";
                    }
                    
                }
                catch (Exception ex)
                {
                    MCP.Logger.Error("监控界面中Monitor_OnMiniload出现异常" + ex.Message);
                }
            }
        }

        #endregion 

        #region 小车监控

        private Dictionary<string, Car> dicCar = new Dictionary<string, Car>();

        void Car_OnDataChanged(object sender, DataChangedEventArgs e)
        {
            try
            {
                if (e.State == null)
                    return;
                //e.States
                string carNo = e.ItemName.Substring(e.ItemName.Length-2,2);
                GetCar(carNo);
                if(e.ItemName.IndexOf("Status")>=0)
                    dicCar[carNo].Status = e.States;
                else if (e.ItemName.IndexOf("CarTaskNo") >= 0)
                    dicCar[carNo].TaskNo = e.State.ToString();
                else if (e.ItemName.IndexOf("AlarmCode") >= 0)
                    dicCar[carNo].AlarmCode = int.Parse(e.State.ToString());
                Cars.CarInfo(dicCar[carNo]);

            }
            catch (Exception ex)
            {
                MCP.Logger.Error("输送线监控界面中Conveyor_OnDataChanged出现异常" + ex.Message);
            }
        }

        private Car GetCar(string carNo)
        {
            Car car = null;
            if (dicCar.ContainsKey(carNo))
            {
                car = dicCar[carNo];
            }
            else
            {
                car = new Car();
                car.CarNo = carNo;
                dicCar.Add(carNo, car);
            }
            return car;
        }

        void Monitor_OnCar(CarEventArgs args)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new CarEventHandler(Monitor_OnCar), args);
            }
            else
            {
                try
                {
                    Car car = args.car;

                    if (car.CarNo == "01")
                    {
                        this.picCar01.Visible = true;
                        Point P2 = InitialP2;

                        int carColumn = int.Parse(car.Status[2].ToString());
                        P2 = dicCarLocation[carColumn];
                        this.picCar01.Location = P2;
                        
                        this.txtTaskNo2.Text = car.TaskNo;
                        this.txtStatus2.Text = dicCarStatus[int.Parse(car.Status[12].ToString())];
                        this.txtActionMode2.Text = dicCarAction[int.Parse(car.Status[0].ToString())];
                        this.txtRow2.Text = car.Status[1].ToString();
                        this.txtColumn2.Text = car.Status[2].ToString();
                        this.txtLayer2.Text = car.Status[3].ToString();
                        this.txtForkStatus2.Text = dicCraneFork[int.Parse(car.Status[11].ToString())];
                        this.txtAlarmCode2.Text = car.AlarmCode.ToString();
                        if (car.AlarmCode>0)
                        {
                            DataRow[] drs = dtDeviceAlarm.Select(string.Format("Flag=1 and AlarmCode={0}", car.AlarmCode));
                            if (drs.Length > 0)
                                this.txtAlarmDesc2.Text = drs[0]["AlarmDesc"].ToString();
                            else
                                this.txtAlarmDesc2.Text = "设备未知错误！";
                        }
                        else
                            this.txtAlarmDesc2.Text = "";

                    }
                    if (car.CarNo == "02")
                    {
                        this.picCar02.Visible = true;
                        Point P3 = InitialP3;

                        int carColumn = int.Parse(car.Status[2].ToString());
                        P3 = dicCarLocation[carColumn];
                        this.picCar02.Location = P3;
                        
                        this.txtTaskNo3.Text = car.TaskNo;
                        this.txtStatus3.Text = dicCarStatus[int.Parse(car.Status[12].ToString())];
                        this.txtActionMode3.Text = dicCarAction[int.Parse(car.Status[0].ToString())];
                        this.txtRow3.Text = car.Status[1].ToString();
                        this.txtColumn3.Text = car.Status[2].ToString();
                        this.txtLayer3.Text = car.Status[3].ToString();
                        this.txtForkStatus3.Text = dicCraneFork[int.Parse(car.Status[11].ToString())];
                        this.txtAlarmCode2.Text = car.AlarmCode.ToString();
                        if (car.AlarmCode >0)
                        {
                            DataRow[] drs = dtDeviceAlarm.Select(string.Format("Flag=1 and AlarmCode={0}", car.AlarmCode));
                            if (drs.Length > 0)
                                this.txtAlarmDesc3.Text = drs[0]["AlarmDesc"].ToString();
                            else
                                this.txtAlarmDesc3.Text = "设备未知错误！";
                        }
                        else
                            this.txtAlarmDesc3.Text = "";
                    }
                }
                catch (Exception ex)
                {
                    MCP.Logger.Error("监控界面中Monitor_OnCar出现异常" + ex.Message);
                }
            }
        }

        #endregion 

        #region 输送线监控
        
        private Dictionary<string, Conveyor> dicConveyor = new Dictionary<string, Conveyor>();
        
        void Conveyor_OnDataChanged(object sender, DataChangedEventArgs e)
        {
            try
            {
                if (e.State == null)
                    return;

                string txt = e.ItemName.Split('_')[0];
                Conveyor conveyor = GetConveyorByID(txt);
                conveyor.value = e.State.ToString();

                conveyor.ID = txt;


                Conveyors.ConveyorInfo(conveyor);

            }
            catch (Exception ex)
            {
                MCP.Logger.Error("输送线监控界面中Conveyor_OnDataChanged出现异常" + ex.Message);
            }
        }

        private Conveyor GetConveyorByID(string ID)
        {
            Conveyor conveyor = null;
            if (dicConveyor.ContainsKey(ID))
            {
                conveyor = dicConveyor[ID];
            }
            else
            {
                conveyor = new Conveyor();
                conveyor.ID = ID;
                dicConveyor.Add(ID, conveyor);
            }
            return conveyor;
        }

        void Monitor_OnConveyor(ConveyorEventArgs args)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new ConveyorEventHandler(Monitor_OnConveyor), args);
            }
            else
            {
                try
                {
                    Conveyor conveyor = args.conveyor;
                    Button btn = GetButton(conveyor.ID);

                    if (btn == null)
                        return;

                    if (conveyor.value == "0" && conveyor.ID.IndexOf("Conveyor") >= 0)
                        btn.Text = "";
                    else if (conveyor.value == "0" && conveyor.ID.IndexOf("UpDown") >= 0)
                        btn.Text = "◎";
                    else if (conveyor.value == "0" && conveyor.ID.IndexOf("Move") >= 0)
                        btn.Text = "";
                    else if (conveyor.value == "1" && conveyor.ID.IndexOf("Conveyor") >= 0) //有货未转
                        btn.Text = "■";
                    else if (conveyor.value == "1" && conveyor.ID.IndexOf("UpDown") >= 0) //有货未转
                        btn.Text = "●";
                    else if (conveyor.value == "1" && conveyor.ID.IndexOf("Move") >= 0)
                        btn.Text = btn.Tag.ToString();
                    else if (conveyor.value == "2") //无货未转
                        btn.Text = "";
                    else if (conveyor.value == "3") //转
                        btn.Text = btn.Tag.ToString();
                    else if (conveyor.value == "4")
                        btn.BackColor = Color.Red;
                    else
                        btn.Text = "";

                }
                catch (Exception ex)
                {
                    MCP.Logger.Error("监控界面中Monitor_OnConveyor出现异常" + ex.Message);
                }
            }
        }

        #endregion 

        private void AddDicKeyValue()
        {
            dicCraneFork.Add(0, "非原点");
            dicCraneFork.Add(1, "原点");

            dicCraneStatus.Add(0, "空闲");
            dicCraneStatus.Add(1, "等待");
            dicCraneStatus.Add(2, "定位");
            dicCraneStatus.Add(3, "取货");
            dicCraneStatus.Add(4, "放货");
            dicCraneStatus.Add(98, "维修");

            dicCraneAction.Add(0, "非自动");
            dicCraneAction.Add(1, "自动");

            dicCarFork.Add(0, "非原点");
            dicCarFork.Add(1, "原点");

            dicCarStatus.Add(0, "空闲");
            dicCarStatus.Add(1, "定位");
            dicCarStatus.Add(2, "左伸取货");
            dicCarStatus.Add(3, "右伸取货");
            dicCarStatus.Add(4, "左伸放货");
            dicCarStatus.Add(5, "右伸放货");
            dicCarStatus.Add(6, "左伸");
            dicCarStatus.Add(7, "右伸");
            dicCarStatus.Add(8, "收叉");
            dicCarStatus.Add(9, "移库");
            dicCarStatus.Add(10, "入库");
            dicCarStatus.Add(11, "出库");
            dicCarStatus.Add(12, "定位放货");

            dicCarAction.Add(0, "手动");
            dicCarAction.Add(1, "自动");
            Point P = InitialP2;
            dicCarLocation.Add(0, new Point(73, P.Y));
            dicCarLocation.Add(1, new Point(125, P.Y));
            dicCarLocation.Add(2, new Point(143, P.Y));
            dicCarLocation.Add(3, new Point(162, P.Y));
            dicCarLocation.Add(4, new Point(181, P.Y));
            dicCarLocation.Add(5, new Point(208, P.Y));
            dicCarLocation.Add(6, new Point(227, P.Y));
            dicCarLocation.Add(7, new Point(246, P.Y));
            dicCarLocation.Add(8, new Point(265, P.Y));
            dicCarLocation.Add(9, new Point(291, P.Y));
            dicCarLocation.Add(10, new Point(310, P.Y));
            dicCarLocation.Add(11, new Point(329, P.Y));
            dicCarLocation.Add(12, new Point(348, P.Y));
            dicCarLocation.Add(13, new Point(375, P.Y));
            dicCarLocation.Add(14, new Point(394, P.Y));
            dicCarLocation.Add(15, new Point(413, P.Y));
            dicCarLocation.Add(16, new Point(431, P.Y));
            dicCarLocation.Add(17, new Point(458, P.Y));
            dicCarLocation.Add(18, new Point(477, P.Y));
            dicCarLocation.Add(19, new Point(496, P.Y));
            dicCarLocation.Add(20, new Point(514, P.Y));
            dicCarLocation.Add(21, new Point(541, P.Y));
            dicCarLocation.Add(22, new Point(560, P.Y));
            dicCarLocation.Add(23, new Point(579, P.Y));
            dicCarLocation.Add(24, new Point(597, P.Y));
            dicCarLocation.Add(25, new Point(624, P.Y));
            dicCarLocation.Add(26, new Point(643, P.Y));
            dicCarLocation.Add(27, new Point(662, P.Y));
            dicCarLocation.Add(28, new Point(681, P.Y));
            dicCarLocation.Add(29, new Point(708, P.Y));
            dicCarLocation.Add(30, new Point(727, P.Y));
            dicCarLocation.Add(31, new Point(746, P.Y));
            dicCarLocation.Add(32, new Point(764, P.Y));
            dicCarLocation.Add(33, new Point(791, P.Y));
            dicCarLocation.Add(34, new Point(810, P.Y));
            dicCarLocation.Add(35, new Point(829, P.Y));
            dicCarLocation.Add(36, new Point(847, P.Y));

            P = InitialP4;
            dicMiniloadLocation.Add(0, new Point(42, P.Y));
            dicMiniloadLocation.Add(1, new Point(72, P.Y));
            dicMiniloadLocation.Add(2, new Point(92, P.Y));
            dicMiniloadLocation.Add(3, new Point(110, P.Y));
            dicMiniloadLocation.Add(4, new Point(129, P.Y));
            dicMiniloadLocation.Add(5, new Point(157, P.Y));
            dicMiniloadLocation.Add(6, new Point(176, P.Y));
            dicMiniloadLocation.Add(7, new Point(195, P.Y));
            dicMiniloadLocation.Add(8, new Point(213, P.Y));
            dicMiniloadLocation.Add(9, new Point(240, P.Y));
            dicMiniloadLocation.Add(10, new Point(259, P.Y));
            dicMiniloadLocation.Add(11, new Point(278, P.Y));
            dicMiniloadLocation.Add(12, new Point(296, P.Y));
            dicMiniloadLocation.Add(13, new Point(323, P.Y));
            dicMiniloadLocation.Add(14, new Point(342, P.Y));
            dicMiniloadLocation.Add(15, new Point(361, P.Y));
            dicMiniloadLocation.Add(16, new Point(379, P.Y));
            dicMiniloadLocation.Add(17, new Point(407, P.Y));
            dicMiniloadLocation.Add(18, new Point(425, P.Y));
            dicMiniloadLocation.Add(19, new Point(444, P.Y));
            dicMiniloadLocation.Add(20, new Point(463, P.Y));
            dicMiniloadLocation.Add(21, new Point(490, P.Y));
            dicMiniloadLocation.Add(22, new Point(509, P.Y));
            dicMiniloadLocation.Add(23, new Point(527, P.Y));
            dicMiniloadLocation.Add(24, new Point(546, P.Y));
            dicMiniloadLocation.Add(25, new Point(573, P.Y));
            dicMiniloadLocation.Add(26, new Point(592, P.Y));
            dicMiniloadLocation.Add(27, new Point(610, P.Y));
            dicMiniloadLocation.Add(28, new Point(629, P.Y));
            dicMiniloadLocation.Add(29, new Point(656, P.Y));
            dicMiniloadLocation.Add(30, new Point(675, P.Y));
            dicMiniloadLocation.Add(31, new Point(694, P.Y));
            dicMiniloadLocation.Add(32, new Point(712, P.Y));
            dicMiniloadLocation.Add(33, new Point(740, P.Y));
            dicMiniloadLocation.Add(34, new Point(759, P.Y));
            dicMiniloadLocation.Add(35, new Point(777, P.Y));
            dicMiniloadLocation.Add(36, new Point(796, P.Y));
            
            dtDeviceAlarm = bll.FillDataTable("WCS.SelectDeviceAlarm", new DataParameter[] { new DataParameter("{0}", "1=1") });
        }

        void Monitor_OnCrane(CraneEventArgs args)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new CraneEventHandler(Monitor_OnCrane), args);
            }
            else
            {
                Crane crane = args.crane;
                TextBox txt = GetTextBox("txtTaskNo", crane.CraneNo);
                if (txt != null)
                    txt.Text = crane.TaskNo;

                txt = GetTextBox("txtState", crane.CraneNo);
                //if (txt != null && dicCraneStatus.ContainsKey(crane.TaskType))
                //    txt.Text = dicCraneStatus[crane.TaskType];

                //txt = GetTextBox("txtCraneAction", crane.CraneNo);
                //if (txt != null && dicCraneAction.ContainsKey(crane.Action))
                //    txt.Text = dicCraneAction[crane.Action];

                txt = GetTextBox("txtRow", crane.CraneNo);
                if (txt != null)
                    txt.Text = crane.Row.ToString();

                txt = GetTextBox("txtColumn", crane.CraneNo);
                if (txt != null)
                    txt.Text = crane.Column.ToString();

                //堆垛机位置
                if (crane.CraneNo == "01")
                {
                    //this.picCrane2.Visible = true;
                    //Point P1 = InitialP1;
                    //if (crane.Column < 46)
                    //    P1.X = P1.X + (int)((crane.Column - 1) * colDis);
                    //else
                    //    P1.X = picCrane1.Location.X + 15;

                    //P1.Y = P1.Y + (int)(rowDis * (crane.Row - 1));
                    //this.picCrane2.Location = P1;

                    //Point P2 = InitialP2;
                    //P2.Y = P2.Y + (int)(rowDis * (crane.Row - 1));
                    //this.picCrane1.Location = P2;
                }

                txt = GetTextBox("txtHeight", crane.CraneNo);
                if (txt != null)
                    txt.Text = crane.Layer.ToString();

                txt = GetTextBox("txtForkStatus", crane.CraneNo);
                if (txt != null && dicCraneFork.ContainsKey(crane.ForkStatus))
                    txt.Text = dicCraneFork[crane.ForkStatus];
                txt = GetTextBox("txtErrorNo", crane.CraneNo);
                if (txt != null)
                    txt.Text = crane.AlarmCode.ToString();

                string strAlarmDesc = "";
                txt = GetTextBox("txtAlarmDesc", crane.CraneNo);
                if (txt != null)
                {
                    if (crane.AlarmCode != 0)
                    {
                        DataRow[] drs = dtDeviceAlarm.Select(string.Format("Flag=1 and AlarmCode={0}", crane.AlarmCode));
                        if (drs.Length > 0)
                            strAlarmDesc = drs[0]["AlarmDesc"].ToString();
                        else
                            strAlarmDesc = "设备未知错误！";
                    }
                    else
                    {
                        strAlarmDesc = "";
                    }
                    txt.Text = strAlarmDesc;
                }

                //更新错误代码、错误描述
                //更新任务状态为执行中
                //bll.ExecNonQuery("WCS.UpdateTaskError", new DataParameter[] { new DataParameter("@CraneErrCode", crane.ErrCode.ToString()), new DataParameter("@CraneErrDesc", dicCraneError[crane.ErrCode]), new DataParameter("@TaskNo", crane.TaskNo) });
                if (crane.AlarmCode > 0)
                {
                    DataParameter[] param = new DataParameter[] { new DataParameter("@TaskNo", crane.TaskNo), new DataParameter("@CraneErrCode", crane.AlarmCode.ToString()), new DataParameter("@CraneErrDesc", strAlarmDesc) };
                    bll.ExecNonQueryTran("WCS.Sp_UpdateTaskError", param);
                    Logger.Error(crane.CraneNo.ToString() + "堆垛机执行时出现错误,代码:" + crane.AlarmCode.ToString() + ",描述:" + strAlarmDesc);
                }
            }
        }
        Button GetButton( string CraneNo)
        {
            Control[] ctl = this.Controls.Find("btn" + CraneNo, true);
            if (ctl.Length > 0)
                return (Button)ctl[0];
            else
                return null;
        }

        TextBox GetTextBox(string name, string CraneNo)
        {
            Control[] ctl = this.Controls.Find(name + CraneNo, true);
            if (ctl.Length > 0)
                return (TextBox)ctl[0];
            else
                return null;
        }


        private void btnBack_Click(object sender, EventArgs e)
        {
            if (this.btnBack1.Text == "启动")
            {
                Context.ProcessDispatcher.WriteToProcess("CraneProcess", "Run", 1);
                this.btnBack1.Text = "停止";
            }
            else
            {
                Context.ProcessDispatcher.WriteToProcess("CraneProcess", "Run", 0);
                this.btnBack1.Text = "启动";
            }
        }

        private void btnBack1_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("是否要召回1号堆垛机到初始位置?", "提示", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == System.Windows.Forms.DialogResult.Yes)
            {
                PutCommand("1", 0);
                Logger.Info("1号堆垛机下发召回命令");
            }
        }
        private void PutCommand(string craneNo, byte TaskType)
        {
            string serviceName = "CranePLC" + craneNo;
            int[] cellAddr = new int[9];
            cellAddr[TaskType] = 1;

            //cellAddr[3] = int.Parse(this.cbFromColumn.Text);
            //cellAddr[4] = int.Parse(this.cbFromHeight.Text);
            //cellAddr[5] = int.Parse(this.cbFromRow.Text.Substring(3, 3));
            //cellAddr[6] = int.Parse(this.cbToColumn.Text);
            //cellAddr[7] = int.Parse(this.cbToHeight.Text);
            //cellAddr[8] = int.Parse(this.cbToRow.Text.Substring(3, 3));

            Context.ProcessDispatcher.WriteToService(serviceName, "TaskAddress", cellAddr);
            Context.ProcessDispatcher.WriteToService(serviceName, "WriteFinished", 0);
        }

        private void btnStop1_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("是否要急停1号堆垛机?", "提示", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == System.Windows.Forms.DialogResult.Yes)
            {
                PutCommand("1", 2);
                Logger.Info("1号堆垛机下发急停命令");
            }
        }

        private void btnConveyor_MouseEnter(object sender, EventArgs e)
        {
            try
            {
                Button btn = (Button)sender;
                string Number = btn.Name.Substring(btn.Name.Length - 2, 2);
                string Barcode = Util.ConvertStringChar.BytesToString(ObjectUtil.GetObjects(Context.ProcessDispatcher.WriteToService("TranLine", "ConveyorInfo" + Number)));
                this.toolTip1.SetToolTip(btn, Barcode);
            }
            catch(Exception ex)
            {
                Logger.Error(ex.Message);
            }
        }        
    }
}