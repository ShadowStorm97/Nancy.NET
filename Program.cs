namespace Nancy.NET
{
    using System;
    using Nancy.Hosting.Self;

    class Program
    {
        static void Main(string[] args)
        {
            var uri =
                new Uri("http://localhost:3579");

            using (var host = new NancyHost(uri))
            {
                try
                {
                    host.Start();
                    Console.WriteLine("url:" + uri);
                    Console.WriteLine("Press any [Enter] to close the host.");
                    Console.ReadLine();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    throw ex;
                }
               
            }
        }
    }
}
