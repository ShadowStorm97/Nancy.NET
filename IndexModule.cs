namespace Nancy.NET
{
    using DBUtility;
    using Nancy;
    using System;
    using System.Collections.Generic;
    using System.Data;
    using Castle.Zmq;
    using Nancy.Json;
    using System.Text;
    using System.Dynamic;
    using MySql.Data.MySqlClient;
    public class IndexModule : NancyModule
    {
        public IndexModule()
        {
            //主页
            Get["/"] = parameters =>
            {
                return View["index"];
            };

            #region 高手排行黑名单
            Get["/blacklist"] = parameters =>
            {
                //1:0读取黑名单的数据 
                List<dynamic> lsblack = new List<dynamic>();
                List<int> mt4_ids = new List<int>();
                var sql = "select * from tiger.blacklist where state=1";
                using (var cmd = DBUtility.ExecuteReader(null, CommandType.Text, sql))
                {
                    while (cmd.Read())
                    {
                        dynamic d = new ExpandoObject();
                        d.mt4_id = int.Parse(cmd["mt4_id"].ToString());
                        d.username = cmd["username"].ToString();
                        lsblack.Add(d);
                        mt4_ids.Add(int.Parse(cmd["mt4_id"].ToString()));
                    }
                }
                //2.0读取黑名单中每个项的当前余额 
                if (mt4_ids.Count > 0)
                {
                    int[] arr = mt4_ids.ToArray();
                    Context con = new Castle.Zmq.Context();
                    var client = new Socket(con, SocketType.Req);
                    client.Connect("tcp://127.0.0.1:1990");
                    for (int i = 0; i < arr.Length; i++)
                    {
                        client.Send("{'mt4UserID':'" + arr[i] + "','__api':'Equity'}");
                        JavaScriptSerializer jss = new JavaScriptSerializer();
                        string str = client.RecvString(Encoding.UTF8);
                        Dictionary<string, object> dic = jss.Deserialize<dynamic>(str);
                        if (dic != null)
                        {
                            string balance = dic["balance"].ToString();
                            lsblack.Find(t => t.mt4_id == arr[i]).balance = balance;
                        }
                    }
                }
                return View["blacklist", lsblack];
            };

            Get["/AddBlackList"] = parameters =>
            {
                try
                {
                    bool result = false;
                    var data = Request.Query["data"];
                    if (data != null)
                    {
                        dynamic d = new ExpandoObject();
                        var sesql = "select username,user_code,mt4_real from tiger.user where mt4_real=@mt4_real limit 1";
                        using (var cmd = DBUtility.ExecuteReader(null, CommandType.Text, sesql, new MySqlParameter("@mt4_real", data)))
                        {
                            while (cmd.Read())
                            {
                                d.username = cmd["username"].ToString();
                                d.usercode = cmd["user_code"].ToString();
                                d.mt4_real = cmd["mt4_real"].ToString();
                                d.date = DateTime.Now;
                            }
                        }
                        if (d != null)
                        {
                            //添加到黑名单库中
                            var insql = "insert into tiger.blacklist(username, usercode, mt4_id, state,date) values(@username, @usercode, @mt4_id, @state,@date)";
                            MySqlParameter[] param = new MySqlParameter[] { new MySqlParameter("@username", d.username), new MySqlParameter("@usercode", d.usercode), new MySqlParameter("@mt4_id", data), new MySqlParameter("@state", 1), new MySqlParameter("@date", d.date) };
                            int count = DBUtility.ExecuteNonQuery(null, CommandType.Text, insql, param);
                            if (count > 0)
                            {
                                result = true;
                            }
                            //立即从高手榜中移除此用户
                            var delesql = "delete from tiger.master where mt4_id=@mt4_id";
                            int delecount = DBUtility.ExecuteNonQuery(null, CommandType.Text, delesql, new MySqlParameter("@mt4_id", data));
                            if (delecount<=0)
                            {
                                result = false;
                            }
                        }
                    }
                    return result.ToString();
                }
                catch (Exception)
                {

                    throw;
                }
            };

            Get["/RemoveUserFromBlackList"] = parameters =>
            {
                try
                {
                    bool result = false;
                    var mt4_id = Request.Query["id"];
                    if (mt4_id != null)
                    {
                        var sesql = "select count(*)as count from tiger.blacklist where mt4_id=@mt4_id and state=1 limit 1";
                        var cmd = DBUtility.ExecuteReader(null, CommandType.Text, sesql, new MySqlParameter("@mt4_id", mt4_id));
                        string count = "0";
                        while (cmd.Read())
                        {
                            count = cmd["count"].ToString();
                        }
                        if (int.Parse(count) > 0)
                        {
                            var insql = "update tiger.blacklist set state=@state where mt4_id=@mt4_id";
                            MySqlParameter[] param = new MySqlParameter[] { new MySqlParameter("@mt4_id", mt4_id), new MySqlParameter("@state", "0") };
                            var updcount = DBUtility.ExecuteNonQuery(null, CommandType.Text, insql, param);
                            if (updcount > 0)
                            {
                                result = true;
                            }
                        }
                    }
                    return result.ToString();
                }
                catch (Exception)
                {

                    throw;
                }
            };
            #endregion
           
            #region 推荐榜单黑名单
            Get["/MasterList"] = parameters =>
            {
                //1:0读取黑名单的数据 
                List<dynamic> lsmaster = new List<dynamic>();
                var sql = " select a.*,b.username from recommend a,user b where a.mt4_id=b.mt4_real";
                using (var cmd = DBUtility.ExecuteReader(null, CommandType.Text, sql))
                {
                    while (cmd.Read())
                    {
                        dynamic d = new ExpandoObject();
                        d.mt4_id = int.Parse(cmd["mt4_id"].ToString());
                        d.type = TypeToName(cmd["type"].ToString());
                        d.username = cmd["username"].ToString();
                        lsmaster.Add(d);
                    }
                }

                return View["masterlist", lsmaster];
            };


            Get["/AddMasterList"] = parameters =>
            {
                try
                {
                    bool result = false;
                    string data = Request.Query["data"];
                    string type = Request.Query["type"];
                    if (data != null)
                    {
                        dynamic d = new ExpandoObject();
                        var sesql = "select username,user_code,mt4_real,`desc` from tiger.user where mt4_real=@mt4_real limit 1";
                        using (var cmd = DBUtility.ExecuteReader(null, CommandType.Text, sesql, new MySqlParameter("@mt4_real", data)))
                        {
                            while (cmd.Read())
                            {
                                d.username = cmd["username"].ToString();
                                d.usercode = cmd["user_code"].ToString();
                                d.mt4_real = cmd["mt4_real"].ToString();
                                d.desc = string.IsNullOrEmpty(cmd["desc"].ToString()) == true ? "此用户并没有填写个人签名~": cmd["desc"].ToString();
                            }
                        }
                        if (d != null)
                        {
                            //1.0判断是否已经存在
                            int count=0;
                            Dictionary<string,string> lsmaster = new Dictionary<string,string>();
                            var isnullsql = "select * from tiger.recommend";
                            using (var cmd = DBUtility.ExecuteReader(null, CommandType.Text, isnullsql))
                            {
                                while (cmd.Read())
                                {
                                    lsmaster.Add(cmd["mt4_id"].ToString(),cmd["type"].ToString());
                                }
                                if (lsmaster.Count>0)
                                {
                                    var insql = "insert into tiger.recommend(mt4_id, type,`desc`) values(@mt4_id, @type,@desc)";
                                    MySqlParameter[] param = new MySqlParameter[] { new MySqlParameter("@mt4_id", data), new MySqlParameter("@type", type), new MySqlParameter("@desc", d.desc) };
                                    if (!lsmaster.ContainsKey(data))
                                    {
                                        //不存在
                                        count = DBUtility.ExecuteNonQuery(null, CommandType.Text, insql, param);
                                    }
                                    else
                                    {
                                        //判断是否相同类型
                                        if (!lsmaster[data].Equals(type))//类型不同
                                        {
                                            count = DBUtility.ExecuteNonQuery(null, CommandType.Text, insql, param);
                                        }
                                    }
                                }
                            }
                            if (count > 0)
                            {
                                result = true;
                            }
                        }
                    }
                    return result.ToString();
                }
                catch (Exception)
                {

                    throw;
                }
            };

            Get["/RemoveUserFromMasterList"] = parameters =>
            {
                try
                {
                    bool result = false;
                    var mt4_id = Request.Query["id"];
                    if (mt4_id != null)
                    {
                        var sesql = "select count(*)as count from tiger.recommend where mt4_id=@mt4_id limit 1";
                        var cmd = DBUtility.ExecuteReader(null, CommandType.Text, sesql, new MySqlParameter("@mt4_id", mt4_id));
                        string count = "0";
                        while (cmd.Read())
                        {
                            count = cmd["count"].ToString();
                        }
                        if (int.Parse(count) > 0)
                        {
                            var insql = "delete from tiger.recommend where mt4_id=@mt4_id";
                            var updcount = DBUtility.ExecuteNonQuery(null, CommandType.Text, insql, new MySqlParameter("@mt4_id", mt4_id));
                            if (updcount > 0)
                            {
                                result = true;
                            }
                        }
                    }
                    return result.ToString();
                }
                catch (Exception)
                {

                    throw;
                }
            };
            #endregion
            
        }
        string TypeToName(string type)
        {
            switch (type)
            {
                case "0":
                     type="短线";
                    break;
                case "1":
                    type="长期";
                    break;
                case "2":
                    type="复制";
                    break;
                case "3":
                    type = "做空";
                    break;
            }
            return type;
        }
    }
}