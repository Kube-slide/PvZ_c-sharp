using System.Security.Permissions;
namespace PVZ_console
{
    internal class Program
    {
        //global
        static string notesFilePath = @"C:\Users\2477548\source\repos\PVZ_console\Plants_Info.txt";
        static List<PlantBrain> plants = new List<PlantBrain>();
        static List<ZombieBrain> zombies = new List<ZombieBrain>();
        static bool preload = false;
        static string[] gameStates = { "Menu", "In-game", "Paused"};
        static string curState;

        //Run every function every frame
        static void Main(string[] args)
        {
            curState = gameStates[0];
            //Check if data has yet to be preloaded --> wont reload everything every frame the game is 
            if (!preload)
            {
                //Always create entity list on first run
                initEntities();
            }
            else
            {
                Draw();
            }
            Thread.Sleep(1000);
            Console.Clear();
            Main(null);

        }

        //Generate list of all entities
        static void initEntities()
        {
            //Create list of all plants and zombies in game


            //Using StreamRead, open all entities file path
            using(var sr = new StreamReader(notesFilePath))
            {
                while(sr.Peek() >= 0)
                {
                    //Check whether each line is for zombies or plants
                    var line = sr.ReadLine();
                    if (line.StartsWith("p_"))
                    {
                        //Break string into pieces --> store the info in an array and create a plant from given data
                        string[] subStrings = line.Split(" | ", StringSplitOptions.RemoveEmptyEntries);
                        plants.Add(new PlantBrain(subStrings[0], subStrings[1], Convert.ToInt32(subStrings[2]), Convert.ToInt32(subStrings[3])));
                    }
                    else if (line.StartsWith("z_"))
                    {
                        //Break string into pieces --> store the info in an array and create a plant from given data
                        string[] subStrings = line.Split("|", StringSplitOptions.RemoveEmptyEntries);
                        zombies.Add(new ZombieBrain(subStrings[0], subStrings[1]));
                    }
                }
                preload = true;
            }
            //For each plant, print to user the details
            for(int i = 0; i < plants.Count; i++)
            {
                Console.WriteLine($"\n{plants[i].Plant_Name} : {plants[i].Plant_Description}. " +
                    $"\n|||STATS|||" +
                    $"\nShooting speed : {plants[i].Shooting_Speed}" +
                    $"\nCost to plant : {plants[i].Sun_cost}\n");
            }
        }
    
        //Draw game screen
        static void Draw()
        {
            Console.Clear();
            switch (curState)
            {
                case "Menu":
                    DMM();
                    break;
                case "In-game":
                    Console.WriteLine("In-game");
                    break;
                case "Paused":
                    Console.WriteLine("Game paused");
                    break;
            }
        }

        static void DMM()
        {
            Console.WriteLine("                                   \r\n    =+=     =-======-=             \r\n -=++*#*+-==:::::----===           \r\n +*###*-+-..::---*#=-=*#+= -====-  \r\n=+*#*+:+:..:----=-#@*=#%==--=**+==-\r\n =*+  ==..:------+%%=====-=%@@@*++-\r\n  --  ==:::-----========-=#@@@@#++-\r\n      :+-------===========%@@@%++==\r\n        ================+=+##*+==- \r\n         =++=========+++:======-   \r\n          :*###**+++==             \r\n           ==++:                   \r\n           =-=+                    \r\n       -+++-+-+-===                \r\n      =******==****+==:            \r\n      ===+++**=+*++++==+:          \r\n     +++++++++**+++++++++:         \r\n     -++++++++- ==++++=--          \r\n         ::.                       u");
            Console.WriteLine("\n\n\n\n  ____  _             _                                        _     _           \r\n |  _ \\| | __ _ _ __ | |_ ___  __   _____   _______  _ __ ___ | |__ (_) ___  ___ \r\n | |_) | |/ _` | '_ \\| __/ __| \\ \\ / / __| |_  / _ \\| '_ ` _ \\| '_ \\| |/ _ \\/ __|\r\n |  __/| | (_| | | | | |_\\__ \\  \\ V /\\__ \\  / / (_) | | | | | | |_) | |  __/\\__ \\\r\n |_|   |_|\\__,_|_| |_|\\__|___/   \\_/ |___/ /___\\___/|_| |_| |_|_.__/|_|\\___||___/\r\n                     / ___|___  _ __  ___  ___ | | ___                           \r\n                    | |   / _ \\| '_ \\/ __|/ _ \\| |/ _ \\                          \r\n                    | |__| (_) | | | \\__ \\ (_) | |  __/                          \r\n                     \\____\\___/|_| |_|___/\\___/|_|\\___|                          \r\n                                                                                ");
        }
    }

    //Creating a plant + executing plant logic
    internal class PlantBrain
    {
        public string Plant_Name;
        public string Plant_Description;
        public int Shooting_Speed;
        public int Sun_cost;

        public PlantBrain(string name, string desc, int sunCost, int shootSpeed)
        {
            Plant_Name = name;
            Plant_Description = desc;
            Sun_cost = sunCost;
            Shooting_Speed = shootSpeed;
        }

    }

    //Creating a zombie + executing plant logic
    internal class ZombieBrain
    {
        public string Zombie_Name;
        public string Zombie_Description;

        public ZombieBrain(string name, string desc)
        {
            Zombie_Name = name;
            Zombie_Description= desc;
        }
    }
}
