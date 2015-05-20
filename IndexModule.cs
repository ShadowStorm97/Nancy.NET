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
            Get["/"] = parameters =>
            {
                return View["index"];
            };

            Get["/blacklist"] = parameters =>
            {
                //1:0读取黑名单的数据 
                List<dynamic> lsblack = new List<dynamic>();
                List<int> mt4_ids = new List<int>();
                var sql = "select * from blacklist where state=1";
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
                    Context context = new Context();
                    var client = new Socket(context, SocketType.Req);
                    client.Connect("tcp://127.0.0.1:1990");
                    for (int i = 0; i < arr.Length; i++)
                    {
                        client.Send(string.Format("{'mt4UserID':{0},'__api':'Equity'}}", arr[i]));
                        var balance = client.RecvString(Encoding.UTF8).Split(new char[] { ',' })[4].Split(new char[] { ':' })[1];
                        if (balance != null)
                        {
                            lsblack.Find(t => t.mt4_id == arr[i]).balance = int.Parse(balance);
                        }
                    }

                }
                return View["blacklist", lsblack];
            };

            Get["/AddBlackList"] = parameters =>
            {
                bool result = false;
                var data = parameters["data"];
                if (data != null)
                {
                    dynamic d = new ExpandoObject();
                    var sesql = "select * from user where mt4_real=@mt4_real limit 1";
                    using (var cmd = DBUtility.ExecuteReader(null, CommandType.Text, sesql, new MySqlParameter("@mt4_real", data)))
                    {
                        while (cmd.Read())
                        {
                            d.username = cmd["username"].ToString();
                            d.usercode = int.Parse(cmd["usercode"].ToString());
                            d.mt4_real = int.Parse(cmd["mt4_real"].ToString());
                        }
                    }
                    if (d.usercode != null && d.mt4_real != null)
                    {
                        var insql = "insert into blacklist(username, usercode, mt4_id, state) values(@username, @usercode, @mt4_id, @state)";
                        MySqlParameter[] param = new MySqlParameter[] { new MySqlParameter("@username", d.username), new MySqlParameter("@usercode", d.usercode), new MySqlParameter("@mt4_id", data), new MySqlParameter("@state", 1) };
                        int count = DBUtility.ExecuteNonQuery(null, CommandType.Text, insql, param);
                        if (count > 0)
                        {
                            result = true;
                        }
                    }
                }
                return result.ToString();
            };
        }
    }
}