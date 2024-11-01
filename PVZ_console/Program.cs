﻿using System.Drawing;
using System.Runtime.InteropServices;
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

        // We need to use unmanaged code
        [DllImport("user32.dll")]

        // GetCursorPos() makes everything possible
        static extern bool GetCursorPos(ref Point lpPoint);

        //More unmanaged code
        [DllImport("user32.dll")]
        //Call window function
        static extern IntPtr GetForegroundWindow();

        //MORE unmanaged code
        [DllImport("user32.dll")]
        //Get window sizes
        static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        //Create a struct to store the coord system for out window
        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

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
            //Sleep for 1/4 of a second --> 4 fps? doesnt flash eyes too bad
            Thread.Sleep(250);

            //Clear console (failsafe) and loop :|
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
            //Clear previous frame
            Console.Clear();

            //Check our current state --> outdated method imo but works for now ig
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
            //End of frame--> accept player inputs
            PlayerInputs();
        }

        //Draw main menu
        static void DMM()
        {
            Console.WriteLine("Main menu");
            Console.WriteLine("1.Options\n2.Play game\n3.Exit\n4.Credits");
        }

        //Function to get mousePosition. Returns (int, int) --> relative mousePOS to console
        static (int, int) GetMouseInput()
        {
            // New point that will be updated by the function with the current coordinates
            Point defPnt = new Point();

            // Call the function and pass the Point, defPnt
            GetCursorPos(ref defPnt);

            //Get our consoleWindow position for reference
            IntPtr consoleHandle = GetForegroundWindow();

            //Get our window size with coords
            GetWindowRect(consoleHandle, out RECT consoleRect);

            //Return mouse position for future ref
            return (defPnt.X - consoleRect.Left, defPnt.Y - consoleRect.Top);
        }

        //Function to check if mouse is within window --> returns bool
        static bool WithinWindow()
        {
            //Get our consoleWindow position for reference
            IntPtr consoleHandle = GetForegroundWindow();

            //Get our window size with coords
            GetWindowRect(consoleHandle, out RECT consoleRect);

            // New point that will be updated by the function with the current coordinates
            Point defPnt = new Point();

            // Call the function and pass the Point, defPnt
            GetCursorPos(ref defPnt);

            bool xInRange = defPnt.X >= consoleRect.Left && defPnt.X <= consoleRect.Right;
            bool yInRange = defPnt.Y >= consoleRect.Top && defPnt.Y <= consoleRect.Bottom;
            return xInRange && yInRange;
        }

        //Getting player mouse inputs and accept command input --> will be used to detect 'Grab plant' / 'Place plant' / 'Collect sun'
        static void PlayerInputs()
        {
            //Start by getting mousePOS every 'frame'
            (int, int) MousePos = GetMouseInput();
            bool isInWindow = WithinWindow();

            //Check for input WITHOUT blocking script. W/o console.KeyAvailable, the rest of the code would hang :\
            if(Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Spacebar && isInWindow)
            {
                Console.WriteLine($"Interact with user at location {MousePos}");
            }
            else if (!isInWindow)
            {
                Console.WriteLine("Outside game area! Return back to keep playing!");
            }

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
