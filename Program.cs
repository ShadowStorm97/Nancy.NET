namespace Nancy.NET
{
    using System;
    using Nancy.Hosting.Self;

    class Program
    {
        static void Main(string[] args)
        {
            var uri =
                new Uri("http://119.81.232.171:3579");

            using (var host = new NancyHost(uri))
            {
                host.Start();

                Console.WriteLine("url:" + uri);
                Console.WriteLine("Press any [Enter] to close the host.");
                Console.ReadLine();
            }
        }
    }
}
