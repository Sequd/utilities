using System;

namespace CleanBin
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            CleanerService service = new CleanerService();
            string path = @"C:\Users\Sequd\Documents\GitHub\Work";
            Console.WriteLine(path);
            Console.WriteLine($"Directories:");
            var dirs = service.Dir(path);
            foreach (var dir in dirs)
            {
                Console.WriteLine(dir);
            }

            Console.WriteLine($"Recurcive cleaning:");
            try
            {
                var directories = service.CleanFolder(path);
                foreach (var directory in directories)
                {
                    Console.WriteLine(directory);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            Console.ReadKey();
        }
    }
}
